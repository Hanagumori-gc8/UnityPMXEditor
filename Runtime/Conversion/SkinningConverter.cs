using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public enum PmxAdvancedDeformMode
    {
        Strict = 0,
        Approximate = 1,
        PreserveOnly = 2
    }

    public sealed class SkinningConversionResult
    {
        internal SkinningConversionResult(BoneWeight[] boneWeights, int advancedDeformVertexCount,
            int fallbackVertexCount, bool usedApproximation, bool usesPreservationAnchor,
            int preservationAnchorBoneIndex, string warning)
        {
            BoneWeights = boneWeights;
            AdvancedDeformVertexCount = advancedDeformVertexCount;
            FallbackVertexCount = fallbackVertexCount;
            UsedApproximation = usedApproximation;
            UsesPreservationAnchor = usesPreservationAnchor;
            PreservationAnchorBoneIndex = preservationAnchorBoneIndex;
            Warning = warning;
        }

        public BoneWeight[] BoneWeights { get; }
        public int AdvancedDeformVertexCount { get; }
        public int FallbackVertexCount { get; }
        public bool UsedApproximation { get; }
        public bool UsesPreservationAnchor { get; }
        public int PreservationAnchorBoneIndex { get; }
        public string Warning { get; }
    }

    public sealed class SkinningConverter
    {
        public SkinningConversionResult Convert(PmxDocument document, PmxAdvancedDeformMode mode)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!Enum.IsDefined(typeof(PmxAdvancedDeformMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (document.Vertices.Count > 0 && document.Bones.Count == 0)
                throw new InvalidOperationException("A PMX with vertices cannot be skinned without bones.");

            var weights = new BoneWeight[document.Vertices.Count];
            int advancedCount = 0;
            int fallbackCount = 0;
            for (int i = 0; i < document.Vertices.Count; i++)
            {
                PmxVertexDeform deform = document.Vertices[i].Deform;
                bool advanced = deform.Type == PmxVertexWeightType.Sdef ||
                                deform.Type == PmxVertexWeightType.Qdef;
                if (advanced)
                {
                    advancedCount++;
                    if (mode == PmxAdvancedDeformMode.Strict)
                        throw new InvalidOperationException(
                            $"Vertex {i} uses {deform.Type}, which is not exactly supported in Stage 3 Strict mode.");
                    if (mode == PmxAdvancedDeformMode.PreserveOnly)
                    {
                        weights[i] = new BoneWeight
                        {
                            boneIndex0 = document.Bones.Count,
                            weight0 = 1f
                        };
                        continue;
                    }
                }

                bool usedFallback;
                weights[i] = ConvertLinearWeights(deform, document.Bones.Count, i, out usedFallback);
                if (usedFallback) fallbackCount++;
            }

            bool approximated = advancedCount > 0 && mode == PmxAdvancedDeformMode.Approximate;
            string warning = approximated
                ? $"Approximated {advancedCount} SDEF/QDEF vertices as linear BDEF weights. " +
                  "This is not exact SDEF or QDEF support."
                : null;
            bool preserved = advancedCount > 0 && mode == PmxAdvancedDeformMode.PreserveOnly;
            return new SkinningConversionResult(weights, advancedCount, fallbackCount,
                approximated, preserved, preserved ? document.Bones.Count : -1, warning);
        }

        public void ApplyToMesh(Mesh mesh, SkinningConversionResult skinning,
            SkeletonConversionResult skeleton)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (skinning == null) throw new ArgumentNullException(nameof(skinning));
            if (skeleton == null) throw new ArgumentNullException(nameof(skeleton));
            if (skinning.BoneWeights.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    $"Bone weight count {skinning.BoneWeights.Length} does not match mesh vertex count {mesh.vertexCount}.");
            if (skeleton.Bindposes.Length != skeleton.Bones.Length)
                throw new InvalidOperationException("Skeleton bindpose and bone counts do not match.");

            if (skinning.UsesPreservationAnchor)
            {
                if (skinning.PreservationAnchorBoneIndex != skeleton.Bones.Length)
                    throw new InvalidOperationException(
                        $"PMX preservation anchor index {skinning.PreservationAnchorBoneIndex} " +
                        $"does not match skeleton bone count {skeleton.Bones.Length}.");
                int anchorIndex = skeleton.EnsurePreservedDeformAnchor(skeleton.SkeletonRoot.parent);
                if (anchorIndex != skinning.PreservationAnchorBoneIndex)
                    throw new InvalidOperationException("Unexpected PMX preserved deform anchor index.");
            }

            mesh.bindposes = skeleton.RendererBindposes;
            mesh.boneWeights = skinning.BoneWeights;
        }

        private static BoneWeight ConvertLinearWeights(PmxVertexDeform deform, int boneCount,
            int vertexIndex, out bool usedFallback)
        {
            var indices = new List<int>(4);
            var values = new List<float>(4);
            int firstValidBone = -1;
            for (int i = 0; i < deform.BoneIndices.Count; i++)
            {
                int boneIndex = deform.BoneIndices[i];
                float weight = i < deform.Weights.Count ? deform.Weights[i] : 0f;
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
                    throw new InvalidOperationException(
                        $"Vertex {vertexIndex} has invalid bone weight {weight} at influence {i}.");
                if (boneIndex < -1 || boneIndex >= boneCount)
                    throw new InvalidOperationException(
                        $"Vertex {vertexIndex} bone index {boneIndex} is outside [-1, {boneCount}).");
                if (boneIndex < 0) continue;
                if (firstValidBone < 0) firstValidBone = boneIndex;
                if (weight == 0f) continue;

                int existing = indices.IndexOf(boneIndex);
                if (existing >= 0) values[existing] += weight;
                else
                {
                    indices.Add(boneIndex);
                    values.Add(weight);
                }
            }

            usedFallback = false;
            float total = 0f;
            for (int i = 0; i < values.Count; i++) total += values[i];
            if (indices.Count == 0 || total <= 0f)
            {
                usedFallback = true;
                indices.Clear();
                values.Clear();
                indices.Add(firstValidBone >= 0 ? firstValidBone : 0);
                values.Add(1f);
                total = 1f;
            }

            for (int i = 0; i < values.Count; i++) values[i] /= total;
            var result = new BoneWeight();
            SetInfluence(ref result, 0, indices, values);
            SetInfluence(ref result, 1, indices, values);
            SetInfluence(ref result, 2, indices, values);
            SetInfluence(ref result, 3, indices, values);
            return result;
        }

        private static void SetInfluence(ref BoneWeight target, int slot,
            List<int> indices, List<float> weights)
        {
            int boneIndex = slot < indices.Count ? indices[slot] : 0;
            float weight = slot < weights.Count ? weights[slot] : 0f;
            switch (slot)
            {
                case 0: target.boneIndex0 = boneIndex; target.weight0 = weight; break;
                case 1: target.boneIndex1 = boneIndex; target.weight1 = weight; break;
                case 2: target.boneIndex2 = boneIndex; target.weight2 = weight; break;
                case 3: target.boneIndex3 = boneIndex; target.weight3 = weight; break;
            }
        }
    }
}
