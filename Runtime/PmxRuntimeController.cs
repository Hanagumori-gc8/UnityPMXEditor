using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class PmxRuntimeController : MonoBehaviour
    {
        private static readonly PmxFrameUpdateStage[] UpdateStages =
        {
            PmxFrameUpdateStage.MorphDependencies,
            PmxFrameUpdateStage.VertexUvMaterialMorphs,
            PmxFrameUpdateStage.BoneMorphs,
            PmxFrameUpdateStage.BoneGrant,
            PmxFrameUpdateStage.InverseKinematics
        };
        private static readonly ProfilerMarker EvaluateFrameMarker =
            new ProfilerMarker("UnityPMXEditor.PmxRuntimeController.EvaluateFrame");

        [SerializeField] private PmxModelAsset modelAsset;
        [SerializeField] private PmxMorphController morphController;
        [SerializeField] private PmxBoneController boneController;
        [SerializeField] private PmxRuntimeCapabilityPath requestedCapability =
            PmxRuntimeCapabilityPath.StandardApproximate;
        [SerializeField] private PmxMmdCompatibilityFallback compatibilityFallback =
            PmxMmdCompatibilityFallback.Reject;
        [SerializeField] private PmxCompatibilityReport compatibilityReport;

        [NonSerialized] private bool initialized;
        [NonSerialized] private int evaluatedFrameCount;

        public static IReadOnlyList<PmxFrameUpdateStage> DeterministicUpdateOrder => UpdateStages;
        public PmxRuntimeCapabilityPath RequestedCapability => requestedCapability;
        public PmxRuntimeCapabilityPath ActiveCapability => compatibilityReport?.ActivePath ?? requestedCapability;
        public PmxCompatibilityReport CompatibilityReport => compatibilityReport;
        public int EvaluatedFrameCount => evaluatedFrameCount;
        public PmxMorphController MorphController => morphController;
        public PmxBoneController BoneController => boneController;

        public PmxCompatibilityReport SetCapability(PmxRuntimeCapabilityPath capability,
            PmxMmdCompatibilityFallback fallback)
        {
            if (!Enum.IsDefined(typeof(PmxRuntimeCapabilityPath), capability))
                throw new ArgumentOutOfRangeException(nameof(capability));
            if (!Enum.IsDefined(typeof(PmxMmdCompatibilityFallback), fallback))
                throw new ArgumentOutOfRangeException(nameof(fallback));
            requestedCapability = capability;
            compatibilityFallback = fallback;
            initialized = false;
            EnsureInitialized();
            return compatibilityReport;
        }

        internal void Configure(PmxModelAsset asset, PmxMorphController morph,
            PmxBoneController bone, PmxRuntimeCapabilityPath capability,
            PmxMmdCompatibilityFallback fallback)
        {
            modelAsset = asset;
            morphController = morph;
            boneController = bone;
            requestedCapability = capability;
            compatibilityFallback = fallback;
            initialized = false;
            EnsureInitialized();
        }

        public void EvaluateFrame()
        {
            using (EvaluateFrameMarker.Auto())
            {
                EnsureInitialized();
                morphController.EvaluateMorphFrame();
                boneController.ApplyBoneFrame(morphController, evaluatedFrameCount);
                evaluatedFrameCount++;
            }
        }

        private void LateUpdate() => EvaluateFrame();

        private void EnsureInitialized()
        {
            if (initialized) return;
            if (modelAsset == null) throw new InvalidOperationException("PmxRuntimeController has no PmxModelAsset.");
            if (morphController == null) morphController = GetComponent<PmxMorphController>();
            if (boneController == null) boneController = GetComponent<PmxBoneController>();
            if (morphController == null || boneController == null)
                throw new InvalidOperationException(
                    "PmxRuntimeController requires PmxMorphController and PmxBoneController.");

            if (requestedCapability == PmxRuntimeCapabilityPath.MmdCompatible &&
                modelAsset.AdvancedDeformVertexCount > 0)
            {
                const string reason =
                    "MmdCompatible requires a dedicated SDEF/QDEF backend, which is not implemented.";
                if (compatibilityFallback == PmxMmdCompatibilityFallback.Reject)
                {
                    compatibilityReport = new PmxCompatibilityReport
                    {
                        RequestedPath = requestedCapability,
                        ActivePath = requestedCapability,
                        Status = PmxFeatureSupportStatus.Rejected,
                        Message = reason
                    };
                    throw new InvalidOperationException(reason);
                }

                compatibilityReport = new PmxCompatibilityReport
                {
                    RequestedPath = requestedCapability,
                    ActivePath = PmxRuntimeCapabilityPath.StandardApproximate,
                    Status = PmxFeatureSupportStatus.Approximated,
                    Message = reason + " Runtime execution was downgraded to StandardApproximate."
                };
            }
            else
            {
                compatibilityReport = new PmxCompatibilityReport
                {
                    RequestedPath = requestedCapability,
                    ActivePath = requestedCapability,
                    Status = PmxFeatureSupportStatus.Approximated,
                    Message = requestedCapability == PmxRuntimeCapabilityPath.MmdCompatible
                        ? "MmdCompatible morph/grant/IK ordering is active, but documented semantic differences remain."
                        : "StandardApproximate execution is active; documented PMX/MMD differences apply."
                };
            }
            initialized = true;
        }
    }
}
