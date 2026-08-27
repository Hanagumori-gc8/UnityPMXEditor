using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [DisallowMultipleComponent]
    public sealed class PmxBoneController : MonoBehaviour
    {
        [SerializeField] private PmxModelAsset modelAsset;
        [SerializeField] private Transform[] bones = Array.Empty<Transform>();
        [SerializeField, Min(1)] private int maxIkIterations = 256;

        [NonSerialized] private bool initialized;
        [NonSerialized] private Vector3[] baselinePositions;
        [NonSerialized] private Quaternion[] baselineRotations;
        [NonSerialized] private int[] deformationOrder;

        public IReadOnlyList<Transform> Bones => bones;
        public int LastAppliedFrame { get; private set; } = -1;

        internal void Configure(PmxModelAsset asset, Transform[] importedBones)
        {
            modelAsset = asset;
            bones = importedBones ?? Array.Empty<Transform>();
            initialized = false;
        }

        internal void ApplyBoneFrame(PmxMorphController morphController, int frameNumber)
        {
            EnsureInitialized();
            if (morphController == null) throw new ArgumentNullException(nameof(morphController));

            ResetBaseline();
            ApplyBoneMorphs(morphController);
            ApplyGrants();
            ApplyInverseKinematics();
            LastAppliedFrame = frameNumber;
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            if (modelAsset == null) throw new InvalidOperationException("PmxBoneController has no PmxModelAsset.");
            if (bones == null || bones.Length != modelAsset.BoneMetadata.Length)
                throw new InvalidOperationException(
                    $"Bone Transform count {bones?.Length ?? 0} does not match metadata count {modelAsset.BoneMetadata.Length}.");

            baselinePositions = new Vector3[bones.Length];
            baselineRotations = new Quaternion[bones.Length];
            deformationOrder = new int[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) throw new InvalidOperationException($"Bone Transform {i} is missing.");
                baselinePositions[i] = bones[i].localPosition;
                baselineRotations[i] = bones[i].localRotation;
                deformationOrder[i] = i;
            }
            Array.Sort(deformationOrder, CompareBoneOrder);
            initialized = true;
        }

        private int CompareBoneOrder(int left, int right)
        {
            int layerComparison = modelAsset.BoneMetadata[left].Layer.CompareTo(
                modelAsset.BoneMetadata[right].Layer);
            return layerComparison != 0 ? layerComparison : left.CompareTo(right);
        }

        private void ResetBaseline()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = baselinePositions[i];
                bones[i].localRotation = baselineRotations[i];
            }
        }

        private void ApplyBoneMorphs(PmxMorphController morphController)
        {
            for (int orderIndex = 0; orderIndex < deformationOrder.Length; orderIndex++)
            {
                int boneIndex = deformationOrder[orderIndex];
                bones[boneIndex].localPosition = baselinePositions[boneIndex] +
                                                 morphController.GetBoneTranslation(boneIndex);
                bones[boneIndex].localRotation = baselineRotations[boneIndex] *
                                                 morphController.GetBoneRotation(boneIndex);
            }
        }

        private void ApplyGrants()
        {
            for (int orderIndex = 0; orderIndex < deformationOrder.Length; orderIndex++)
            {
                int boneIndex = deformationOrder[orderIndex];
                PmxBoneMetadata metadata = modelAsset.BoneMetadata[boneIndex];
                if (!metadata.HasInheritParent) continue;
                int parentIndex = metadata.InheritParentBoneIndex;
                if (parentIndex < 0 || parentIndex >= bones.Length) continue;
                float influence = metadata.InheritWeight;
                if ((metadata.RawFlags & (ushort)PmxBoneFlags.InheritTranslation) != 0)
                {
                    Vector3 parentDelta = bones[parentIndex].localPosition - baselinePositions[parentIndex];
                    bones[boneIndex].localPosition += parentDelta * influence;
                }
                if ((metadata.RawFlags & (ushort)PmxBoneFlags.InheritRotation) != 0)
                {
                    Quaternion parentDelta = Quaternion.Inverse(baselineRotations[parentIndex]) *
                                             bones[parentIndex].localRotation;
                    bones[boneIndex].localRotation = bones[boneIndex].localRotation *
                                                     Quaternion.SlerpUnclamped(
                                                         Quaternion.identity, parentDelta, influence);
                }
            }
        }

        private void ApplyInverseKinematics()
        {
            for (int orderIndex = 0; orderIndex < deformationOrder.Length; orderIndex++)
            {
                int ikBoneIndex = deformationOrder[orderIndex];
                PmxIkMetadata ik = modelAsset.BoneMetadata[ikBoneIndex].InverseKinematics;
                if (ik == null || ik.TargetBoneIndex < 0 || ik.TargetBoneIndex >= bones.Length) continue;
                Transform effector = bones[ik.TargetBoneIndex];
                Vector3 targetPosition = bones[ikBoneIndex].position;
                int iterations = Mathf.Min(Mathf.Max(ik.LoopCount, 0), maxIkIterations);
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    if ((effector.position - targetPosition).sqrMagnitude < 0.00000001f) break;
                    for (int linkIndex = 0; linkIndex < ik.Links.Length; linkIndex++)
                    {
                        PmxIkLinkMetadata linkMetadata = ik.Links[linkIndex];
                        int boneIndex = linkMetadata.BoneIndex;
                        if (boneIndex < 0 || boneIndex >= bones.Length) continue;
                        Transform link = bones[boneIndex];
                        Vector3 toEffector = effector.position - link.position;
                        Vector3 toTarget = targetPosition - link.position;
                        if (toEffector.sqrMagnitude <= 0.00000001f ||
                            toTarget.sqrMagnitude <= 0.00000001f) continue;

                        Quaternion delta = Quaternion.FromToRotation(toEffector, toTarget);
                        delta.ToAngleAxis(out float angle, out Vector3 axis);
                        if (angle > 180f) angle -= 360f;
                        float maxAngle = Mathf.Abs(ik.AngleLimit) * Mathf.Rad2Deg;
                        if (maxAngle > 0f) angle = Mathf.Clamp(angle, -maxAngle, maxAngle);
                        link.rotation = Quaternion.AngleAxis(angle, axis) * link.rotation;
                        if (linkMetadata.HasLimits) ApplyLinkLimits(link, linkMetadata);
                    }
                }
            }
        }

        private static void ApplyLinkLimits(Transform link, PmxIkLinkMetadata metadata)
        {
            Vector3 radians = NormalizeEuler(link.localEulerAngles * Mathf.Deg2Rad);
            radians.x = Mathf.Clamp(radians.x, metadata.MinimumAngle.x, metadata.MaximumAngle.x);
            radians.y = Mathf.Clamp(radians.y, metadata.MinimumAngle.y, metadata.MaximumAngle.y);
            radians.z = Mathf.Clamp(radians.z, metadata.MinimumAngle.z, metadata.MaximumAngle.z);
            link.localRotation = Quaternion.Euler(radians * Mathf.Rad2Deg);
        }

        private static Vector3 NormalizeEuler(Vector3 radians)
        {
            radians.x = NormalizeAngle(radians.x);
            radians.y = NormalizeAngle(radians.y);
            radians.z = NormalizeAngle(radians.z);
            return radians;
        }

        private static float NormalizeAngle(float value)
        {
            while (value > Mathf.PI) value -= Mathf.PI * 2f;
            while (value < -Mathf.PI) value += Mathf.PI * 2f;
            return value;
        }
    }
}
