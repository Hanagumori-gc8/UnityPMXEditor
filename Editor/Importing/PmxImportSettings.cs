using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [Serializable]
    public sealed class PmxImportSettings
    {
        [SerializeField, Min(0.000001f)] private float scale = 0.1f;
        [SerializeField] private PmxAdvancedDeformMode advancedDeformMode =
            PmxAdvancedDeformMode.PreserveOnly;
        [SerializeField] private PmxRuntimeCapabilityPath runtimeCapability =
            PmxRuntimeCapabilityPath.StandardApproximate;
        [SerializeField] private PmxMmdCompatibilityFallback mmdCompatibilityFallback =
            PmxMmdCompatibilityFallback.Reject;
        [SerializeField] private PmxPhysicsImportMode physicsMode = PmxPhysicsImportMode.None;
        [SerializeField] private PmxPhysicsSettings physicsSettings = new PmxPhysicsSettings();

        public float Scale
        {
            get => scale;
            set => scale = value;
        }

        public PmxAdvancedDeformMode AdvancedDeformMode
        {
            get => advancedDeformMode;
            set => advancedDeformMode = value;
        }

        public PmxRuntimeCapabilityPath RuntimeCapability
        {
            get => runtimeCapability;
            set => runtimeCapability = value;
        }

        public PmxMmdCompatibilityFallback MmdCompatibilityFallback
        {
            get => mmdCompatibilityFallback;
            set => mmdCompatibilityFallback = value;
        }

        public PmxPhysicsImportMode PhysicsMode
        {
            get => physicsMode;
            set => physicsMode = value;
        }

        public PmxPhysicsSettings PhysicsSettings => physicsSettings;

        internal void Validate()
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                throw new PmxImportValidationException(
                    $"Import scale must be finite and greater than zero, but was {scale}.");
            if (!Enum.IsDefined(typeof(PmxAdvancedDeformMode), advancedDeformMode))
                throw new PmxImportValidationException(
                    $"Unknown advanced deform mode value {(int)advancedDeformMode}.");
            if (!Enum.IsDefined(typeof(PmxRuntimeCapabilityPath), runtimeCapability))
                throw new PmxImportValidationException(
                    $"Unknown runtime capability value {(int)runtimeCapability}.");
            if (!Enum.IsDefined(typeof(PmxMmdCompatibilityFallback), mmdCompatibilityFallback))
                throw new PmxImportValidationException(
                    $"Unknown MMD compatibility fallback value {(int)mmdCompatibilityFallback}.");
            if (!Enum.IsDefined(typeof(PmxPhysicsImportMode), physicsMode))
                throw new PmxImportValidationException(
                    $"Unknown physics import mode value {(int)physicsMode}.");
            if (physicsSettings == null)
                throw new PmxImportValidationException("Physics settings are missing.");
            try
            {
                physicsSettings.Validate();
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new PmxImportValidationException(
                    $"Invalid physics setting '{exception.ParamName}'.");
            }
        }
    }
}
