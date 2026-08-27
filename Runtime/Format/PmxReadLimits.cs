using System;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxReadLimits
    {
        public static PmxReadLimits Default => new PmxReadLimits();

        public long MaxFileBytes { get; set; } = 2L * 1024 * 1024 * 1024;
        public int MaxStringBytes { get; set; } = 16 * 1024 * 1024;
        public int MaxVertices { get; set; } = 2_000_000;
        public int MaxSurfaceIndices { get; set; } = 12_000_000;
        public int MaxTextures { get; set; } = 100_000;
        public int MaxMaterials { get; set; } = 100_000;
        public int MaxBones { get; set; } = 250_000;
        public int MaxIkLinks { get; set; } = 1_000_000;
        public int MaxMorphs { get; set; } = 250_000;
        public int MaxMorphOffsets { get; set; } = 8_000_000;
        public int MaxDisplayFrames { get; set; } = 100_000;
        public int MaxDisplayFrameElements { get; set; } = 2_000_000;
        public int MaxRigidBodies { get; set; } = 500_000;
        public int MaxJoints { get; set; } = 500_000;
        public int MaxSoftBodies { get; set; } = 100_000;
        public int MaxSoftBodyAnchors { get; set; } = 2_000_000;
        public int MaxSoftBodyPins { get; set; } = 8_000_000;
        public int MaxTotalCollectionItems { get; set; } = 32_000_000;

        internal PmxReadLimits CloneValidated()
        {
            ValidatePositive(MaxFileBytes, nameof(MaxFileBytes));
            ValidatePositive(MaxStringBytes, nameof(MaxStringBytes));
            ValidatePositive(MaxVertices, nameof(MaxVertices));
            ValidatePositive(MaxSurfaceIndices, nameof(MaxSurfaceIndices));
            ValidatePositive(MaxTextures, nameof(MaxTextures));
            ValidatePositive(MaxMaterials, nameof(MaxMaterials));
            ValidatePositive(MaxBones, nameof(MaxBones));
            ValidatePositive(MaxIkLinks, nameof(MaxIkLinks));
            ValidatePositive(MaxMorphs, nameof(MaxMorphs));
            ValidatePositive(MaxMorphOffsets, nameof(MaxMorphOffsets));
            ValidatePositive(MaxDisplayFrames, nameof(MaxDisplayFrames));
            ValidatePositive(MaxDisplayFrameElements, nameof(MaxDisplayFrameElements));
            ValidatePositive(MaxRigidBodies, nameof(MaxRigidBodies));
            ValidatePositive(MaxJoints, nameof(MaxJoints));
            ValidatePositive(MaxSoftBodies, nameof(MaxSoftBodies));
            ValidatePositive(MaxSoftBodyAnchors, nameof(MaxSoftBodyAnchors));
            ValidatePositive(MaxSoftBodyPins, nameof(MaxSoftBodyPins));
            ValidatePositive(MaxTotalCollectionItems, nameof(MaxTotalCollectionItems));

            return (PmxReadLimits)MemberwiseClone();
        }

        private static void ValidatePositive(long value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name, value, "A PMX read limit must be positive.");
            }
        }
    }
}
