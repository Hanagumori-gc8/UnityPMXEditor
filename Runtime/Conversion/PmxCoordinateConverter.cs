using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxCoordinateConverter
    {
        public PmxCoordinateConverter(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale,
                    "PMX import scale must be finite and greater than zero.");
            }

            Scale = scale;
        }

        public float Scale { get; }

        public Vector3 ConvertPosition(PmxVector3 value)
            => new Vector3(value.X * Scale, value.Y * Scale, -value.Z * Scale);

        public Vector3 ConvertPositionDelta(PmxVector3 value)
            => ConvertPosition(value);

        public Vector3 ConvertNormal(PmxVector3 value)
        {
            var normal = new Vector3(value.X, value.Y, -value.Z);
            return normal.sqrMagnitude > 0f ? normal.normalized : Vector3.zero;
        }

        public Quaternion ConvertRotation(Vector4 value)
        {
            var rotation = new Quaternion(-value.x, -value.y, value.z, value.w);
            float magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y +
                                         rotation.z * rotation.z + rotation.w * rotation.w);
            if (magnitude <= 0f) return Quaternion.identity;
            float inverse = 1f / magnitude;
            return new Quaternion(rotation.x * inverse, rotation.y * inverse,
                rotation.z * inverse, rotation.w * inverse);
        }

        public Vector2 ConvertUv(PmxVector2 value) => new Vector2(value.X, 1f - value.Y);

        public Vector2 ConvertUvDelta(Vector4 value) => new Vector2(value.x, -value.y);

        public float ConvertScale(float value) => value * Scale;

        public void ConvertTriangle(int vertexA, int vertexB, int vertexC, int[] destination, int offset)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (offset < 0 || offset > destination.Length - 3)
                throw new ArgumentOutOfRangeException(nameof(offset));

            destination[offset] = vertexA;
            destination[offset + 1] = vertexC;
            destination[offset + 2] = vertexB;
        }
    }
}
