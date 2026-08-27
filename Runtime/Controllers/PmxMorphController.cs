using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [DisallowMultipleComponent]
    public sealed class PmxMorphController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SpecColorId = Shader.PropertyToID("_SpecColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        [SerializeField] private PmxModelAsset modelAsset;
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        [SerializeField] private float importScale = 0.1f;

        [NonSerialized] private bool initialized;
        [NonSerialized] private float[] directWeights;
        [NonSerialized] private float[] effectiveWeights;
        [NonSerialized] private int[] dependencyOrder;
        [NonSerialized] private Vector3[] boneTranslations;
        [NonSerialized] private Quaternion[] boneRotations;
        [NonSerialized] private Mesh runtimeMesh;
        [NonSerialized] private Vector2[] baselineUv;
        [NonSerialized] private List<Vector2> workingUv;
        [NonSerialized] private Color[] workingDiffuse;
        [NonSerialized] private Color[] workingSpecular;
        [NonSerialized] private float[] workingSmoothness;
        [NonSerialized] private MaterialPropertyBlock[] materialBlocks;
        [NonSerialized] private PmxCoordinateConverter coordinates;

        public PmxModelAsset ModelAsset => modelAsset;
        public int MorphCount => directWeights?.Length ?? modelAsset?.MorphMetadata.Length ?? 0;

        internal void Configure(PmxModelAsset asset, SkinnedMeshRenderer renderer, float scale)
        {
            modelAsset = asset;
            targetRenderer = renderer;
            importScale = scale;
            initialized = false;
        }

        public void SetMorphWeight(int morphIndex, float weight)
        {
            EnsureInitialized();
            if (morphIndex < 0 || morphIndex >= directWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(morphIndex));
            if (float.IsNaN(weight) || float.IsInfinity(weight))
                throw new ArgumentOutOfRangeException(nameof(weight));
            directWeights[morphIndex] = weight;
        }

        public float GetMorphWeight(int morphIndex)
        {
            EnsureInitialized();
            if (morphIndex < 0 || morphIndex >= directWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(morphIndex));
            return directWeights[morphIndex];
        }

        public float GetEffectiveMorphWeight(int morphIndex)
        {
            EnsureInitialized();
            if (morphIndex < 0 || morphIndex >= effectiveWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(morphIndex));
            return effectiveWeights[morphIndex];
        }

        public void ResetAllMorphWeights()
        {
            EnsureInitialized();
            Array.Clear(directWeights, 0, directWeights.Length);
        }

        internal Vector3 GetBoneTranslation(int boneIndex) => boneTranslations[boneIndex];
        internal Quaternion GetBoneRotation(int boneIndex) => boneRotations[boneIndex];

        internal void EvaluateMorphFrame()
        {
            EnsureInitialized();
            for (int i = 0; i < effectiveWeights.Length; i++)
                effectiveWeights[i] = directWeights[i];

            for (int orderIndex = 0; orderIndex < dependencyOrder.Length; orderIndex++)
            {
                int sourceIndex = dependencyOrder[orderIndex];
                float sourceWeight = effectiveWeights[sourceIndex];
                if (sourceWeight == 0f) continue;
                PmxMorphMetadata source = modelAsset.MorphMetadata[sourceIndex];
                if (source.RawType != (byte)PmxMorphType.Group &&
                    source.RawType != (byte)PmxMorphType.Flip) continue;
                for (int offsetIndex = 0; offsetIndex < source.Offsets.Length; offsetIndex++)
                {
                    PmxMorphOffsetMetadata offset = source.Offsets[offsetIndex];
                    int target = offset.MorphIndex;
                    if (target < 0 || target >= effectiveWeights.Length)
                        throw new InvalidOperationException(
                            $"Morph {sourceIndex} dependency target {target} is out of range.");
                    effectiveWeights[target] += sourceWeight * offset.Weight;
                }
            }

            ResetFrameBuffers();
            for (int morphIndex = 0; morphIndex < modelAsset.MorphMetadata.Length; morphIndex++)
            {
                float weight = effectiveWeights[morphIndex];
                PmxMorphMetadata morph = modelAsset.MorphMetadata[morphIndex];
                if (morph.RawType == (byte)PmxMorphType.Vertex)
                {
                    if (morph.BlendShapeIndex >= 0)
                        targetRenderer.SetBlendShapeWeight(morph.BlendShapeIndex, weight * 100f);
                }
                else if (weight != 0f && morph.RawType == (byte)PmxMorphType.Bone)
                    ApplyBoneMorph(morph, weight);
                else if (weight != 0f && morph.RawType == (byte)PmxMorphType.Uv)
                    ApplyUvMorph(morph, weight);
                else if (weight != 0f && morph.RawType == (byte)PmxMorphType.Material)
                    ApplyMaterialMorph(morph, weight);
            }

            if (workingUv != null) runtimeMesh.SetUVs(0, workingUv);
            ApplyMaterialBlocks();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            if (modelAsset == null) throw new InvalidOperationException("PmxMorphController has no PmxModelAsset.");
            if (targetRenderer == null) targetRenderer = GetComponent<SkinnedMeshRenderer>();
            if (targetRenderer == null) throw new InvalidOperationException("PmxMorphController requires SkinnedMeshRenderer.");

            coordinates = new PmxCoordinateConverter(importScale);
            int morphCount = modelAsset.MorphMetadata.Length;
            directWeights = new float[morphCount];
            effectiveWeights = new float[morphCount];
            dependencyOrder = BuildDependencyOrder(modelAsset.MorphMetadata);
            boneTranslations = new Vector3[modelAsset.BoneMetadata.Length];
            boneRotations = new Quaternion[modelAsset.BoneMetadata.Length];

            bool hasUvMorph = false;
            for (int i = 0; i < modelAsset.MorphMetadata.Length; i++)
                if (modelAsset.MorphMetadata[i].RawType == (byte)PmxMorphType.Uv) { hasUvMorph = true; break; }
            if (hasUvMorph)
            {
                runtimeMesh = Instantiate(targetRenderer.sharedMesh);
                runtimeMesh.name = targetRenderer.sharedMesh.name + " Runtime UV";
                targetRenderer.sharedMesh = runtimeMesh;
                baselineUv = runtimeMesh.uv;
                workingUv = new List<Vector2>(baselineUv.Length);
                for (int i = 0; i < baselineUv.Length; i++) workingUv.Add(baselineUv[i]);
            }
            else runtimeMesh = targetRenderer.sharedMesh;

            int materialCount = modelAsset.MaterialMetadata.Length;
            workingDiffuse = new Color[materialCount];
            workingSpecular = new Color[materialCount];
            workingSmoothness = new float[materialCount];
            materialBlocks = new MaterialPropertyBlock[materialCount];
            for (int i = 0; i < materialCount; i++) materialBlocks[i] = new MaterialPropertyBlock();
            initialized = true;
        }

        private void ResetFrameBuffers()
        {
            for (int i = 0; i < boneTranslations.Length; i++)
            {
                boneTranslations[i] = Vector3.zero;
                boneRotations[i] = Quaternion.identity;
            }
            if (workingUv != null)
                for (int i = 0; i < baselineUv.Length; i++) workingUv[i] = baselineUv[i];
            for (int i = 0; i < workingDiffuse.Length; i++)
            {
                PmxMaterialMetadata material = modelAsset.MaterialMetadata[i];
                workingDiffuse[i] = new Color(material.Diffuse.x, material.Diffuse.y,
                    material.Diffuse.z, material.Diffuse.w);
                workingSpecular[i] = new Color(material.Specular.x, material.Specular.y,
                    material.Specular.z, 1f);
                workingSmoothness[i] = Mathf.Clamp01(material.SpecularStrength / 100f);
            }
        }

        private void ApplyBoneMorph(PmxMorphMetadata morph, float weight)
        {
            for (int i = 0; i < morph.Offsets.Length; i++)
            {
                PmxMorphOffsetMetadata offset = morph.Offsets[i];
                int boneIndex = offset.BoneIndex;
                if (boneIndex < 0 || boneIndex >= boneTranslations.Length)
                    throw new InvalidOperationException($"Bone morph target {boneIndex} is out of range.");
                boneTranslations[boneIndex] += coordinates.ConvertPositionDelta(
                    new PmxVector3(offset.Translation.x, offset.Translation.y, offset.Translation.z)) * weight;
                Quaternion delta = coordinates.ConvertRotation(offset.Rotation);
                boneRotations[boneIndex] = boneRotations[boneIndex] *
                                           Quaternion.SlerpUnclamped(Quaternion.identity, delta, weight);
            }
        }

        private void ApplyUvMorph(PmxMorphMetadata morph, float weight)
        {
            if (workingUv == null) return;
            for (int i = 0; i < morph.Offsets.Length; i++)
            {
                PmxMorphOffsetMetadata offset = morph.Offsets[i];
                int vertexIndex = offset.VertexIndex;
                if (vertexIndex < 0 || vertexIndex >= workingUv.Count)
                    throw new InvalidOperationException($"UV morph target {vertexIndex} is out of range.");
                workingUv[vertexIndex] = workingUv[vertexIndex] +
                                         coordinates.ConvertUvDelta(offset.UvDelta) * weight;
            }
        }

        private void ApplyMaterialMorph(PmxMorphMetadata morph, float weight)
        {
            for (int i = 0; i < morph.Offsets.Length; i++)
            {
                PmxMorphOffsetMetadata offset = morph.Offsets[i];
                if (offset.MaterialIndex == -1)
                {
                    for (int materialIndex = 0; materialIndex < workingDiffuse.Length; materialIndex++)
                        ApplyMaterialOffset(materialIndex, offset, weight);
                }
                else
                {
                    if (offset.MaterialIndex < 0 || offset.MaterialIndex >= workingDiffuse.Length)
                        throw new InvalidOperationException($"Material morph target {offset.MaterialIndex} is out of range.");
                    ApplyMaterialOffset(offset.MaterialIndex, offset, weight);
                }
            }
        }

        private void ApplyMaterialOffset(int index, PmxMorphOffsetMetadata offset, float weight)
        {
            Color diffuse = new Color(offset.Diffuse.x, offset.Diffuse.y, offset.Diffuse.z, offset.Diffuse.w);
            Color specular = new Color(offset.Specular.x, offset.Specular.y, offset.Specular.z, 1f);
            float smoothness = Mathf.Clamp01(offset.SpecularStrength / 100f);
            if (offset.RawOperation == 0)
            {
                workingDiffuse[index] *= Color.LerpUnclamped(Color.white, diffuse, weight);
                workingSpecular[index] *= Color.LerpUnclamped(Color.white, specular, weight);
                workingSmoothness[index] *= Mathf.LerpUnclamped(1f, smoothness, weight);
            }
            else
            {
                workingDiffuse[index] += diffuse * weight;
                workingSpecular[index] += specular * weight;
                workingSmoothness[index] += smoothness * weight;
            }
        }

        private void ApplyMaterialBlocks()
        {
            for (int i = 0; i < materialBlocks.Length; i++)
            {
                MaterialPropertyBlock block = materialBlocks[i];
                block.Clear();
                block.SetColor(BaseColorId, workingDiffuse[i]);
                block.SetColor(ColorId, workingDiffuse[i]);
                block.SetColor(SpecColorId, workingSpecular[i]);
                block.SetFloat(SmoothnessId, Mathf.Clamp01(workingSmoothness[i]));
                targetRenderer.SetPropertyBlock(block, i);
            }
        }

        private static int[] BuildDependencyOrder(PmxMorphMetadata[] morphs)
        {
            int count = morphs.Length;
            var indegree = new int[count];
            var outgoing = new List<int>[count];
            for (int i = 0; i < count; i++) outgoing[i] = new List<int>();
            for (int source = 0; source < count; source++)
            {
                byte type = morphs[source].RawType;
                if (type != (byte)PmxMorphType.Group && type != (byte)PmxMorphType.Flip) continue;
                for (int offsetIndex = 0; offsetIndex < morphs[source].Offsets.Length; offsetIndex++)
                {
                    int target = morphs[source].Offsets[offsetIndex].MorphIndex;
                    if (target < 0 || target >= count)
                        throw new InvalidOperationException($"Morph {source} dependency target {target} is out of range.");
                    outgoing[source].Add(target);
                    indegree[target]++;
                }
            }

            var ready = new SortedSet<int>();
            for (int i = 0; i < count; i++) if (indegree[i] == 0) ready.Add(i);
            var order = new int[count];
            int written = 0;
            while (ready.Count > 0)
            {
                int source = ready.Min;
                ready.Remove(source);
                order[written++] = source;
                for (int i = 0; i < outgoing[source].Count; i++)
                {
                    int target = outgoing[source][i];
                    if (--indegree[target] == 0) ready.Add(target);
                }
            }
            if (written != count)
                throw new InvalidOperationException("Group/Flip morph dependencies contain a cycle.");
            return order;
        }
    }
}
