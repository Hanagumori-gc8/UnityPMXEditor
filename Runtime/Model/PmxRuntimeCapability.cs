using System;

namespace Hanagumori.UnityPmx
{
    public enum PmxRuntimeCapabilityPath
    {
        StandardApproximate = 0,
        MmdCompatible = 1
    }

    public enum PmxMmdCompatibilityFallback
    {
        Reject = 0,
        DowngradeToStandardApproximate = 1
    }

    [Serializable]
    public sealed class PmxCompatibilityReport
    {
        public PmxRuntimeCapabilityPath RequestedPath;
        public PmxRuntimeCapabilityPath ActivePath;
        public PmxFeatureSupportStatus Status;
        public string Message;
    }

    public enum PmxFrameUpdateStage
    {
        MorphDependencies = 0,
        VertexUvMaterialMorphs = 1,
        BoneMorphs = 2,
        BoneGrant = 3,
        InverseKinematics = 4
    }
}
