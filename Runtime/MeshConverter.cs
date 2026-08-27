using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanagumori.UnityPmx
{
    public sealed class MeshConverter
    {
        public Mesh Convert(PmxDocument document, PmxCoordinateConverter coordinates)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));

            ValidateSurfacePartition(document);

            int vertexCount = document.Vertices.Count;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                PmxVertex source = document.Vertices[i];
                vertices[i] = coordinates.ConvertPosition(source.Position);
                normals[i] = coordinates.ConvertNormal(source.Normal);
                uvs[i] = coordinates.ConvertUv(source.Uv);
            }

            var mesh = new Mesh
            {
                name = "PMX Mesh",
                indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = vertices,
                normals = normals,
                uv = uvs
            };

            mesh.subMeshCount = document.Materials.Count;
            int surfaceOffset = 0;
            for (int materialIndex = 0; materialIndex < document.Materials.Count; materialIndex++)
            {
                int count = document.Materials[materialIndex].SurfaceIndexCount;
                var triangles = new int[count];
                for (int sourceOffset = 0; sourceOffset < count; sourceOffset += 3)
                {
                    coordinates.ConvertTriangle(
                        document.SurfaceVertexIndices[surfaceOffset + sourceOffset],
                        document.SurfaceVertexIndices[surfaceOffset + sourceOffset + 1],
                        document.SurfaceVertexIndices[surfaceOffset + sourceOffset + 2],
                        triangles,
                        sourceOffset);
                }

                mesh.SetTriangles(triangles, materialIndex, false);
                surfaceOffset += count;
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ValidateSurfacePartition(PmxDocument document)
        {
            long total = 0;
            for (int i = 0; i < document.Materials.Count; i++)
            {
                int count = document.Materials[i].SurfaceIndexCount;
                if (count < 0 || count % 3 != 0)
                    throw new InvalidOperationException($"Material {i} surface count {count} is not a non-negative multiple of three.");
                total += count;
            }

            if (total != document.SurfaceVertexIndices.Count)
                throw new InvalidOperationException(
                    $"Material surface counts total {total}, but the document has {document.SurfaceVertexIndices.Count} surface indices.");
        }
    }
}
