using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanagumori.UnityPmx
{
    internal static class PmxObjExporter
    {
        private const int MaxVertices = 10_000_000;
        private const long MaxIndices = 60_000_000;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static PmxModelExportResult Export(GameObject root, string objPath)
        {
            var sources = new List<MeshSource>();
            try
            {
                CollectSources(root, sources);
                ValidateSources(sources);
                string materialPath = Path.ChangeExtension(objPath, ".mtl");
                var materials = new MaterialRegistry(Path.GetDirectoryName(objPath));
                int triangleCount;
                int partCount;
                WriteObj(root, objPath, Path.GetFileName(materialPath), sources, materials,
                    out triangleCount, out partCount);
                WriteMtl(materialPath, materials.Entries);

                int vertexCount = 0;
                for (int i = 0; i < sources.Count; i++)
                    vertexCount = checked(vertexCount + sources[i].Mesh.vertexCount);
                return new PmxModelExportResult(objPath, materialPath,
                    vertexCount, triangleCount, partCount);
            }
            finally
            {
                for (int i = 0; i < sources.Count; i++) sources[i].Dispose();
            }
        }

        private static void CollectSources(GameObject root, List<MeshSource> destination)
        {
            Matrix4x4 worldToRoot = root.transform.worldToLocalMatrix;
            foreach (SkinnedMeshRenderer renderer in
                     root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null) continue;
                var baked = new Mesh { name = renderer.sharedMesh.name + " [OBJ Bake]" };
                renderer.BakeMesh(baked);
                destination.Add(new MeshSource(baked,
                    worldToRoot * renderer.transform.localToWorldMatrix,
                    renderer.sharedMaterials, renderer.name, true));
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null) continue;
                destination.Add(new MeshSource(filter.sharedMesh,
                    worldToRoot * filter.transform.localToWorldMatrix,
                    renderer.sharedMaterials, filter.name, false));
            }

            if (destination.Count == 0)
                throw new InvalidOperationException("The selected PMX model has no exportable meshes.");
        }

        private static void ValidateSources(List<MeshSource> sources)
        {
            long vertexTotal = 0;
            long indexTotal = 0;
            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                Mesh mesh = sources[sourceIndex].Mesh;
                if (!mesh.isReadable)
                    throw new InvalidOperationException(
                        $"Mesh '{mesh.name}' is not readable and cannot be exported to OBJ.");
                vertexTotal += mesh.vertexCount;
                if (vertexTotal > MaxVertices)
                    throw new InvalidOperationException(
                        $"OBJ export exceeds the {MaxVertices} vertex safety limit.");
                Vector2[] uvs = mesh.uv;
                Vector3[] normals = mesh.normals;
                if (uvs.Length != 0 && uvs.Length != mesh.vertexCount)
                    throw new InvalidOperationException($"Mesh '{mesh.name}' has an invalid UV count.");
                if (normals.Length != 0 && normals.Length != mesh.vertexCount)
                    throw new InvalidOperationException($"Mesh '{mesh.name}' has an invalid normal count.");

                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                        throw new InvalidOperationException(
                            $"Mesh '{mesh.name}' submesh {subMesh} is not triangle topology.");
                    long count = (long)mesh.GetIndexCount(subMesh);
                    if (count % 3 != 0)
                        throw new InvalidOperationException(
                            $"Mesh '{mesh.name}' submesh {subMesh} index count is not divisible by three.");
                    indexTotal += count;
                    if (indexTotal > MaxIndices)
                        throw new InvalidOperationException(
                            $"OBJ export exceeds the {MaxIndices} index safety limit.");
                }
            }
        }

        private static void WriteObj(GameObject root, string objPath, string materialFileName,
            List<MeshSource> sources, MaterialRegistry materials,
            out int triangleCount, out int partCount)
        {
            var utf8 = new UTF8Encoding(false);
            using (var writer = new StreamWriter(objPath, false, utf8, 64 * 1024))
            {
                writer.WriteLine("# UnityPMXEditor OBJ export");
                writer.WriteLine("# OBJ contains static geometry only; bones and skinning are not represented.");
                writer.Write("mtllib ");
                writer.WriteLine(materialFileName);
                writer.Write("o ");
                writer.WriteLine(SanitizeName(root.name, "PMX_Model"));

                int vertexOffset = 1;
                int uvOffset = 1;
                int normalOffset = 1;
                triangleCount = 0;
                partCount = 0;
                PmxModelAsset metadata = root.GetComponent<PmxRuntimeController>()?.ModelAsset;
                for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
                {
                    MeshSource source = sources[sourceIndex];
                    Mesh mesh = source.Mesh;
                    Vector3[] vertices = mesh.vertices;
                    Vector2[] uvs = mesh.uv;
                    Vector3[] normals = mesh.normals;
                    bool hasUvs = uvs.Length == vertices.Length;
                    bool hasNormals = normals.Length == vertices.Length;
                    Matrix4x4 normalMatrix = source.LocalToRoot.inverse.transpose;

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        Vector3 value = source.LocalToRoot.MultiplyPoint3x4(vertices[i]);
                        ValidateFinite(value, "vertex", sourceIndex, i);
                        writer.Write("v ");
                        WriteFloat(writer, value.x);
                        writer.Write(' ');
                        WriteFloat(writer, value.y);
                        writer.Write(' ');
                        WriteFloat(writer, -value.z);
                        writer.WriteLine();
                    }
                    if (hasUvs)
                    {
                        for (int i = 0; i < uvs.Length; i++)
                        {
                            ValidateFinite(uvs[i], "UV", sourceIndex, i);
                            writer.Write("vt ");
                            WriteFloat(writer, uvs[i].x);
                            writer.Write(' ');
                            WriteFloat(writer, uvs[i].y);
                            writer.WriteLine();
                        }
                    }
                    if (hasNormals)
                    {
                        for (int i = 0; i < normals.Length; i++)
                        {
                            Vector3 value = normalMatrix.MultiplyVector(normals[i]).normalized;
                            ValidateFinite(value, "normal", sourceIndex, i);
                            writer.Write("vn ");
                            WriteFloat(writer, value.x);
                            writer.Write(' ');
                            WriteFloat(writer, value.y);
                            writer.Write(' ');
                            WriteFloat(writer, -value.z);
                            writer.WriteLine();
                        }
                    }

                    for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    {
                        string sourceName = SourcePartName(metadata, sources.Count,
                            sourceIndex, subMesh, source.Name);
                        writer.Write("g part_");
                        writer.Write(partCount.ToString("D6", Invariant));
                        writer.Write('_');
                        writer.WriteLine(SanitizeName(sourceName, "unnamed"));
                        Material material = subMesh < source.Materials.Length
                            ? source.Materials[subMesh]
                            : null;
                        writer.Write("usemtl ");
                        writer.WriteLine(materials.Register(material, partCount));

                        int[] indices = mesh.GetIndices(subMesh);
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            ValidateIndex(indices[i], vertices.Length, sourceIndex, subMesh);
                            ValidateIndex(indices[i + 1], vertices.Length, sourceIndex, subMesh);
                            ValidateIndex(indices[i + 2], vertices.Length, sourceIndex, subMesh);
                            writer.Write("f ");
                            WriteFaceVertex(writer, indices[i], vertexOffset, uvOffset,
                                normalOffset, hasUvs, hasNormals);
                            writer.Write(' ');
                            WriteFaceVertex(writer, indices[i + 2], vertexOffset, uvOffset,
                                normalOffset, hasUvs, hasNormals);
                            writer.Write(' ');
                            WriteFaceVertex(writer, indices[i + 1], vertexOffset, uvOffset,
                                normalOffset, hasUvs, hasNormals);
                            writer.WriteLine();
                            triangleCount++;
                        }
                        partCount++;
                    }

                    vertexOffset = checked(vertexOffset + vertices.Length);
                    if (hasUvs) uvOffset = checked(uvOffset + uvs.Length);
                    if (hasNormals) normalOffset = checked(normalOffset + normals.Length);
                }
            }
        }

        private static void WriteMtl(string materialPath, IReadOnlyList<MaterialEntry> entries)
        {
            using (var writer = new StreamWriter(materialPath, false,
                       new UTF8Encoding(false), 16 * 1024))
            {
                writer.WriteLine("# UnityPMXEditor material approximation");
                for (int i = 0; i < entries.Count; i++)
                {
                    MaterialEntry entry = entries[i];
                    writer.Write("newmtl ");
                    writer.WriteLine(entry.Name);
                    Color diffuse = ReadColor(entry.Material, "_BaseColor", "_Color", Color.white);
                    Color specular = ReadColor(entry.Material, "_SpecColor", null, Color.black);
                    writer.Write("Kd "); WriteColor3(writer, diffuse); writer.WriteLine();
                    writer.Write("Ks "); WriteColor3(writer, specular); writer.WriteLine();
                    writer.Write("d "); WriteFloat(writer, diffuse.a); writer.WriteLine();
                    writer.WriteLine("illum 2");
                    string texturePath = entry.RelativeTexturePath;
                    if (!string.IsNullOrEmpty(texturePath))
                    {
                        writer.Write("map_Kd ");
                        writer.WriteLine(texturePath);
                    }
                    writer.WriteLine();
                }
            }
        }

        private static Color ReadColor(Material material, string primary, string fallback,
            Color defaultValue)
        {
            if (material == null) return defaultValue;
            if (!string.IsNullOrEmpty(primary) && material.HasProperty(primary))
                return material.GetColor(primary);
            if (!string.IsNullOrEmpty(fallback) && material.HasProperty(fallback))
                return material.GetColor(fallback);
            return defaultValue;
        }

        private static string SourcePartName(PmxModelAsset metadata, int sourceCount,
            int sourceIndex, int subMesh, string fallback)
        {
            if (metadata != null && sourceCount == 1 && sourceIndex == 0 &&
                subMesh < metadata.MaterialMetadata.Length)
            {
                PmxMaterialMetadata material = metadata.MaterialMetadata[subMesh];
                return string.IsNullOrWhiteSpace(material.EnglishName)
                    ? material.Name
                    : material.EnglishName;
            }
            return fallback + "_" + subMesh.ToString("D6", Invariant);
        }

        private static void WriteFaceVertex(TextWriter writer, int localIndex,
            int vertexOffset, int uvOffset, int normalOffset, bool hasUvs, bool hasNormals)
        {
            writer.Write((vertexOffset + localIndex).ToString(Invariant));
            if (!hasUvs && !hasNormals) return;
            writer.Write('/');
            if (hasUvs) writer.Write((uvOffset + localIndex).ToString(Invariant));
            if (!hasNormals) return;
            writer.Write('/');
            writer.Write((normalOffset + localIndex).ToString(Invariant));
        }

        private static void ValidateIndex(int index, int vertexCount, int source, int subMesh)
        {
            if (index < 0 || index >= vertexCount)
                throw new InvalidOperationException(
                    $"Mesh source {source} submesh {subMesh} index {index} is outside [0, {vertexCount}).");
        }

        private static void ValidateFinite(Vector3 value, string kind, int source, int index)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z))
                throw new InvalidOperationException(
                    $"Mesh source {source} {kind} {index} contains NaN or infinity.");
        }

        private static void ValidateFinite(Vector2 value, string kind, int source, int index)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y))
                throw new InvalidOperationException(
                    $"Mesh source {source} {kind} {index} contains NaN or infinity.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void WriteFloat(TextWriter writer, float value) =>
            writer.Write(value.ToString("R", Invariant));
        private static void WriteColor3(TextWriter writer, Color value)
        {
            WriteFloat(writer, value.r); writer.Write(' ');
            WriteFloat(writer, value.g); writer.Write(' ');
            WriteFloat(writer, value.b);
        }

        internal static string SanitizeName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-'
                    ? c
                    : '_');
            }
            return builder.Length == 0 ? fallback : builder.ToString();
        }

        private sealed class MeshSource : IDisposable
        {
            public MeshSource(Mesh mesh, Matrix4x4 localToRoot, Material[] materials,
                string name, bool ownsMesh)
            {
                Mesh = mesh;
                LocalToRoot = localToRoot;
                Materials = materials ?? Array.Empty<Material>();
                Name = name;
                OwnsMesh = ownsMesh;
            }

            public Mesh Mesh { get; }
            public Matrix4x4 LocalToRoot { get; }
            public Material[] Materials { get; }
            public string Name { get; }
            private bool OwnsMesh { get; }
            public void Dispose()
            {
                if (OwnsMesh && Mesh != null) UnityEngine.Object.DestroyImmediate(Mesh);
            }
        }

        private sealed class MaterialRegistry
        {
            private readonly string outputDirectory;
            private readonly Dictionary<Material, string> names = new Dictionary<Material, string>();
            private readonly List<MaterialEntry> entries = new List<MaterialEntry>();

            public MaterialRegistry(string outputDirectory)
            {
                this.outputDirectory = outputDirectory;
            }

            public IReadOnlyList<MaterialEntry> Entries => entries;

            public string Register(Material material, int stableIndex)
            {
                if (material != null && names.TryGetValue(material, out string existing))
                    return existing;
                string baseName = material != null ? material.name : "missing";
                string name = "material_" + stableIndex.ToString("D6", Invariant) + "_" +
                              SanitizeName(baseName, "unnamed");
                string texture = RelativeTexturePath(material, outputDirectory);
                entries.Add(new MaterialEntry(name, material, texture));
                if (material != null) names.Add(material, name);
                return name;
            }
        }

        private sealed class MaterialEntry
        {
            public MaterialEntry(string name, Material material, string relativeTexturePath)
            {
                Name = name;
                Material = material;
                RelativeTexturePath = relativeTexturePath;
            }

            public string Name { get; }
            public Material Material { get; }
            public string RelativeTexturePath { get; }
        }

        private static string RelativeTexturePath(Material material, string outputDirectory)
        {
            if (material == null) return null;
            Texture texture = material.mainTexture;
            if (texture == null) return null;
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath)) return null;
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullTexturePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            var baseUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(outputDirectory)));
            var textureUri = new Uri(fullTexturePath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(textureUri).ToString())
                .Replace('\\', '/');
        }

        private static string AppendDirectorySeparator(string value)
        {
            if (value.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                value.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return value;
            return value + Path.DirectorySeparatorChar;
        }
    }
}
