using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public enum PmxPhysicsImportMode
    {
        None = 0,
        Experimental = 1
    }

    [Serializable]
    public sealed class PmxPhysicsSettings
    {
        [SerializeField, Min(0f)] private float jointDamper = 1f;
        [SerializeField, Min(0.01f)] private float minimumMass = 0.001f;
        [SerializeField, Min(0f)] private float maxDepenetrationVelocity = 10f;
        [SerializeField] private bool enableOnInstantiate = true;

        public float JointDamper { get => jointDamper; set => jointDamper = value; }
        public float MinimumMass { get => minimumMass; set => minimumMass = value; }
        public float MaxDepenetrationVelocity
        { get => maxDepenetrationVelocity; set => maxDepenetrationVelocity = value; }
        public bool EnableOnInstantiate
        { get => enableOnInstantiate; set => enableOnInstantiate = value; }

        internal void Validate()
        {
            ValidateFiniteNonNegative(jointDamper, nameof(JointDamper));
            if (!IsFinite(minimumMass) || minimumMass <= 0f)
                throw new ArgumentOutOfRangeException(nameof(MinimumMass));
            ValidateFiniteNonNegative(maxDepenetrationVelocity, nameof(MaxDepenetrationVelocity));
        }

        private static void ValidateFiniteNonNegative(float value, string name)
        {
            if (!IsFinite(value) || value < 0f) throw new ArgumentOutOfRangeException(name);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
