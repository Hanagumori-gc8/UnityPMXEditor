using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public interface IMaterialConverter
    {
        Material Convert(PmxMaterial material, PmxMaterialConversionContext context);
    }

    public sealed class PmxMaterialConversionContext
    {
        public PmxMaterialConversionContext(int materialIndex, Texture2D mainTexture,
            Texture2D environmentTexture, Texture2D toonTexture)
        {
            if (materialIndex < 0) throw new ArgumentOutOfRangeException(nameof(materialIndex));
            MaterialIndex = materialIndex;
            MainTexture = mainTexture;
            EnvironmentTexture = environmentTexture;
            ToonTexture = toonTexture;
        }

        public int MaterialIndex { get; }
        public Texture2D MainTexture { get; }
        public Texture2D EnvironmentTexture { get; }
        public Texture2D ToonTexture { get; }
    }
}
