using System;
using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public enum PmxModelExportFormat
    {
        Fbx,
        Obj
    }

    public sealed class PmxModelExportResult
    {
        internal PmxModelExportResult(string modelPath, string materialPath,
            int vertexCount, int triangleCount, int partCount)
        {
            ModelPath = modelPath;
            MaterialPath = materialPath;
            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            PartCount = partCount;
        }

        public string ModelPath { get; }
        public string MaterialPath { get; }
        public int VertexCount { get; }
        public int TriangleCount { get; }
        public int PartCount { get; }
    }

    public static class PmxModelExporter
    {
        public static PmxModelExportResult Export(GameObject source, string outputPath,
            PmxModelExportFormat format)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Enum.IsDefined(typeof(PmxModelExportFormat), format))
                throw new ArgumentOutOfRangeException(nameof(format));

            string extension = format == PmxModelExportFormat.Fbx ? ".fbx" : ".obj";
            string fullPath = NormalizeOutputPath(outputPath, extension);
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Export path has no parent directory.", nameof(outputPath));
            Directory.CreateDirectory(directory);

            PmxModelExportResult result = format == PmxModelExportFormat.Fbx
                ? ExportFbx(source, fullPath)
                : PmxObjExporter.Export(source, fullPath);
            ImportProjectAssetIfNeeded(result.ModelPath);
            if (!string.IsNullOrEmpty(result.MaterialPath))
                ImportProjectAssetIfNeeded(result.MaterialPath);
            return result;
        }

        private static PmxModelExportResult ExportFbx(GameObject source, string fullPath)
        {
            CountGeometry(source, out int vertexCount, out int triangleCount, out int partCount);
            if (partCount == 0)
                throw new InvalidOperationException("The selected PMX model has no exportable mesh parts.");

            GameObject instance = UnityEngine.Object.Instantiate(source);
            instance.name = source.name;
            instance.hideFlags = HideFlags.DontSave;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);
            try
            {
                string exportedPath = ModelExporter.ExportObject(fullPath, instance);
                if (string.IsNullOrEmpty(exportedPath) || !File.Exists(exportedPath))
                    throw new InvalidOperationException("Unity FBX Exporter did not produce an FBX file.");
                return new PmxModelExportResult(Path.GetFullPath(exportedPath), null,
                    vertexCount, triangleCount, partCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        internal static string NormalizeOutputPath(string outputPath, string requiredExtension)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Export path must not be empty.", nameof(outputPath));
            if (string.IsNullOrEmpty(requiredExtension) || requiredExtension[0] != '.')
                throw new ArgumentException("Required extension is invalid.", nameof(requiredExtension));

            string fullPath = Path.GetFullPath(outputPath);
            if (!string.Equals(Path.GetExtension(fullPath), requiredExtension,
                    StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Export path must use the '{requiredExtension}' extension.", nameof(outputPath));
            return fullPath;
        }

        private static void CountGeometry(GameObject source, out int vertices,
            out int triangles, out int parts)
        {
            long vertexTotal = 0;
            long triangleTotal = 0;
            long partTotal = 0;
            foreach (SkinnedMeshRenderer renderer in
                     source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) continue;
                vertexTotal += mesh.vertexCount;
                partTotal += mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    triangleTotal += mesh.GetIndexCount(i) / 3;
            }

            foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.GetComponent<SkinnedMeshRenderer>() != null) continue;
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                vertexTotal += mesh.vertexCount;
                partTotal += mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    triangleTotal += mesh.GetIndexCount(i) / 3;
            }

            if (vertexTotal > int.MaxValue || triangleTotal > int.MaxValue || partTotal > int.MaxValue)
                throw new InvalidOperationException("Exported geometry exceeds supported item counts.");
            vertices = (int)vertexTotal;
            triangles = (int)triangleTotal;
            parts = (int)partTotal;
        }

        private static void ImportProjectAssetIfNeeded(string fullPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string normalized = Path.GetFullPath(fullPath);
            string prefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar,
                                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
            string assetPath = normalized.Substring(prefix.Length).Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
