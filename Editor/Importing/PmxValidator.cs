using System;
using System.IO;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxImportValidationException : IOException
    {
        public PmxImportValidationException(string message) : base(message) { }
    }

    public sealed class PmxValidator
    {
        public void ValidateForImport(PmxDocument document, PmxImportSettings settings)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();

            long surfaceTotal = 0;
            for (int i = 0; i < document.Materials.Count; i++)
            {
                int count = document.Materials[i].SurfaceIndexCount;
                if (count < 0 || count % 3 != 0)
                    throw new PmxImportValidationException(
                        $"Material {i} surface count {count} must be a non-negative multiple of three.");
                surfaceTotal += count;
            }

            if (surfaceTotal != document.SurfaceVertexIndices.Count)
                throw new PmxImportValidationException(
                    $"Material surface counts total {surfaceTotal}, but the PMX contains " +
                    $"{document.SurfaceVertexIndices.Count} surface indices.");

            if (document.SurfaceVertexIndices.Count > 0 && document.Materials.Count == 0)
                throw new PmxImportValidationException("A PMX with surfaces must declare at least one material.");

            for (int i = 0; i < document.Vertices.Count; i++)
            {
                PmxVertex vertex = document.Vertices[i];
                ValidateFinite(vertex.Position.X, $"vertex {i} position X");
                ValidateFinite(vertex.Position.Y, $"vertex {i} position Y");
                ValidateFinite(vertex.Position.Z, $"vertex {i} position Z");
                ValidateFinite(vertex.Normal.X, $"vertex {i} normal X");
                ValidateFinite(vertex.Normal.Y, $"vertex {i} normal Y");
                ValidateFinite(vertex.Normal.Z, $"vertex {i} normal Z");
                ValidateFinite(vertex.Uv.X, $"vertex {i} UV X");
                ValidateFinite(vertex.Uv.Y, $"vertex {i} UV Y");
            }
        }

        public string NormalizeTextureAssetPath(string pmxAssetPath, string texturePath)
        {
            if (string.IsNullOrWhiteSpace(pmxAssetPath))
                throw new ArgumentException("A PMX asset path is required.", nameof(pmxAssetPath));
            string sourcePath = pmxAssetPath.Replace('\\', '/');
            bool isAssetsPath = sourcePath.StartsWith("Assets/", StringComparison.Ordinal);
            bool isPackagesPath = sourcePath.StartsWith("Packages/", StringComparison.Ordinal);
            if ((!isAssetsPath && !isPackagesPath) ||
                !sourcePath.EndsWith(".pmx", StringComparison.OrdinalIgnoreCase))
                throw new PmxImportValidationException(
                    $"PMX source path '{pmxAssetPath}' must be a project-relative Assets/*.pmx " +
                    "or Packages/*.pmx path.");

            if (string.IsNullOrWhiteSpace(texturePath))
                throw new PmxImportValidationException("PMX texture paths cannot be empty.");
            if (texturePath.IndexOf('\0') >= 0)
                throw new PmxImportValidationException("PMX texture paths cannot contain a NUL character.");

            string relative = texturePath.Replace('\\', '/');
            if (relative.StartsWith("/", StringComparison.Ordinal) ||
                relative.StartsWith("//", StringComparison.Ordinal) ||
                relative.IndexOf("://", StringComparison.Ordinal) >= 0 ||
                (relative.Length >= 2 && char.IsLetter(relative[0]) && relative[1] == ':'))
                throw new PmxImportValidationException(
                    $"Texture path '{texturePath}' is absolute or URI-like and is not portable.");

            string[] segments = relative.Split('/');
            var normalizedSegments = new System.Collections.Generic.List<string>(segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (segment.Length == 0 || segment == ".") continue;
                if (segment == "..")
                    throw new PmxImportValidationException(
                        $"Texture path '{texturePath}' contains a directory traversal segment.");
                if (segment.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                    throw new PmxImportValidationException(
                        $"Texture path '{texturePath}' contains characters that are not portable Unity asset names.");
                normalizedSegments.Add(segment);
            }

            if (normalizedSegments.Count == 0)
                throw new PmxImportValidationException($"Texture path '{texturePath}' has no file component.");

            int slash = sourcePath.LastIndexOf('/');
            string sourceDirectory = slash >= 0 ? sourcePath.Substring(0, slash) : "Assets";
            string normalized = sourceDirectory + "/" + string.Join("/", normalizedSegments);
            string requiredRoot = isAssetsPath ? "Assets/" : "Packages/";
            if (!normalized.StartsWith(requiredRoot, StringComparison.Ordinal))
                throw new PmxImportValidationException(
                    $"Texture path '{texturePath}' does not resolve inside the source Unity asset root.");
            return normalized;
        }

        private static void ValidateFinite(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new PmxImportValidationException($"PMX {field} must be finite, but was {value}.");
        }
    }
}
