using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanagumori.UnityPmx
{
    public sealed class DefaultMaterialConverter : IMaterialConverter
    {
        public Material Convert(PmxMaterial material, PmxMaterialConversionContext context)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (context == null) throw new ArgumentNullException(nameof(context));

            Shader shader = FindDefaultShader();
            var result = new Material(shader)
            {
                name = $"PMX Material {context.MaterialIndex:D6} [Approximation]"
            };

            var diffuse = new Color(material.Diffuse.X, material.Diffuse.Y,
                material.Diffuse.Z, material.Diffuse.W);
            SetColorIfPresent(result, "_BaseColor", diffuse);
            SetColorIfPresent(result, "_Color", diffuse);
            SetTextureIfPresent(result, "_BaseMap", context.MainTexture);
            SetTextureIfPresent(result, "_MainTex", context.MainTexture);

            var specular = new Color(material.Specular.X, material.Specular.Y,
                material.Specular.Z, 1f);
            SetColorIfPresent(result, "_SpecColor", specular);
            if (result.HasProperty("_Smoothness"))
                result.SetFloat("_Smoothness", Mathf.Clamp01(material.SpecularStrength / 100f));

            return result;
        }

        private static Shader FindDefaultShader()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultMaterial != null && pipeline.defaultMaterial.shader != null)
                return pipeline.defaultMaterial.shader;

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
            if (shader == null)
                throw new InvalidOperationException("No render-pipeline default or built-in fallback shader is available.");
            return shader;
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property)) material.SetColor(property, value);
        }

        private static void SetTextureIfPresent(Material material, string property, Texture texture)
        {
            if (texture != null && material.HasProperty(property)) material.SetTexture(property, texture);
        }
    }
}
