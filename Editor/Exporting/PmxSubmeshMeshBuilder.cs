using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanagumori.UnityPmx
{
    internal static class PmxSubmeshMeshBuilder
    {
        public static Mesh BuildFullVertexPart(Mesh source, int subMeshIndex)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (subMeshIndex < 0 || subMeshIndex >= source.subMeshCount)
                throw new ArgumentOutOfRangeException(nameof(subMeshIndex));
            var result = UnityEngine.Object.Instantiate(source);
            result.name = source.name + $" Part {subMeshIndex:D6}";
            result.subMeshCount = 1;
            result.SetIndices(source.GetIndices(subMeshIndex), MeshTopology.Triangles, 0, false);
            result.RecalculateBounds();
            return result;
        }

        public static Mesh Build(Mesh source, int subMeshIndex)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (subMeshIndex < 0 || subMeshIndex >= source.subMeshCount)
                throw new ArgumentOutOfRangeException(nameof(subMeshIndex));
            if (!source.isReadable)
                throw new InvalidOperationException(
                    $"Mesh '{source.name}' must be readable for per-part FBX export.");
            if (source.GetTopology(subMeshIndex) != MeshTopology.Triangles)
                throw new InvalidOperationException(
                    $"Mesh '{source.name}' submesh {subMeshIndex} is not triangle topology.");

            int[] sourceIndices = source.GetIndices(subMeshIndex);
            var remap = new Dictionary<int, int>(sourceIndices.Length);
            var sourceVertexIndices = new List<int>();
            var indices = new int[sourceIndices.Length];
            for (int i = 0; i < sourceIndices.Length; i++)
            {
                int sourceIndex = sourceIndices[i];
                if (sourceIndex < 0 || sourceIndex >= source.vertexCount)
                    throw new InvalidOperationException(
                        $"Mesh '{source.name}' submesh {subMeshIndex} index {sourceIndex} " +
                        $"is outside [0, {source.vertexCount}).");
                if (!remap.TryGetValue(sourceIndex, out int destinationIndex))
                {
                    destinationIndex = sourceVertexIndices.Count;
                    remap.Add(sourceIndex, destinationIndex);
                    sourceVertexIndices.Add(sourceIndex);
                }
                indices[i] = destinationIndex;
            }

            var result = new Mesh
            {
                name = source.name + $" Part {subMeshIndex:D6}",
                indexFormat = sourceVertexIndices.Count > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            CopyVertexChannels(source, result, sourceVertexIndices);
            result.SetIndices(indices, MeshTopology.Triangles, 0, false);
            result.bindposes = source.bindposes;
            CopyBlendShapes(source, result, sourceVertexIndices);
            result.RecalculateBounds();
            return result;
        }

        private static void CopyVertexChannels(Mesh source, Mesh destination,
            IReadOnlyList<int> sourceVertexIndices)
        {
            destination.vertices = Select(source.vertices, sourceVertexIndices);
            CopyIfComplete(source.normals, source.vertexCount, sourceVertexIndices,
                values => destination.normals = values);
            CopyIfComplete(source.tangents, source.vertexCount, sourceVertexIndices,
                values => destination.tangents = values);
            CopyIfComplete(source.colors, source.vertexCount, sourceVertexIndices,
                values => destination.colors = values);
            CopyIfComplete(source.boneWeights, source.vertexCount, sourceVertexIndices,
                values => destination.boneWeights = values);

            for (int channel = 0; channel < 8; channel++)
            {
                var sourceUv = new List<Vector4>();
                source.GetUVs(channel, sourceUv);
                if (sourceUv.Count == 0) continue;
                if (sourceUv.Count != source.vertexCount)
                    throw new InvalidOperationException(
                        $"Mesh '{source.name}' UV channel {channel} has {sourceUv.Count} values " +
                        $"for {source.vertexCount} vertices.");
                var destinationUv = new List<Vector4>(sourceVertexIndices.Count);
                for (int i = 0; i < sourceVertexIndices.Count; i++)
                    destinationUv.Add(sourceUv[sourceVertexIndices[i]]);
                destination.SetUVs(channel, destinationUv);
            }
        }

        private static void CopyBlendShapes(Mesh source, Mesh destination,
            IReadOnlyList<int> sourceVertexIndices)
        {
            if (source.blendShapeCount == 0) return;
            var deltaVertices = new Vector3[source.vertexCount];
            var deltaNormals = new Vector3[source.vertexCount];
            var deltaTangents = new Vector3[source.vertexCount];
            for (int shape = 0; shape < source.blendShapeCount; shape++)
            {
                int frameCount = source.GetBlendShapeFrameCount(shape);
                string shapeName = source.GetBlendShapeName(shape);
                for (int frame = 0; frame < frameCount; frame++)
                {
                    Array.Clear(deltaVertices, 0, deltaVertices.Length);
                    Array.Clear(deltaNormals, 0, deltaNormals.Length);
                    Array.Clear(deltaTangents, 0, deltaTangents.Length);
                    source.GetBlendShapeFrameVertices(shape, frame,
                        deltaVertices, deltaNormals, deltaTangents);
                    destination.AddBlendShapeFrame(shapeName,
                        source.GetBlendShapeFrameWeight(shape, frame),
                        Select(deltaVertices, sourceVertexIndices),
                        Select(deltaNormals, sourceVertexIndices),
                        Select(deltaTangents, sourceVertexIndices));
                }
            }
        }

        private static T[] Select<T>(T[] source, IReadOnlyList<int> indices)
        {
            var result = new T[indices.Count];
            for (int i = 0; i < indices.Count; i++) result[i] = source[indices[i]];
            return result;
        }

        private static void CopyIfComplete<T>(T[] source, int sourceVertexCount,
            IReadOnlyList<int> sourceVertexIndices, Action<T[]> assign)
        {
            if (source.Length == 0) return;
            if (source.Length != sourceVertexCount)
                throw new InvalidOperationException(
                    $"Mesh vertex channel has {source.Length} values for " +
                    $"{sourceVertexCount} vertices.");
            assign(Select(source, sourceVertexIndices));
        }
    }
}
