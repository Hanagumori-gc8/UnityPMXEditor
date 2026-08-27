using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public sealed class BlendShapeConversionResult
    {
        internal BlendShapeConversionResult(int[] morphToBlendShapeIndex, int blendShapeCount)
        {
            MorphToBlendShapeIndex = morphToBlendShapeIndex;
            BlendShapeCount = blendShapeCount;
        }

        public int[] MorphToBlendShapeIndex { get; }
        public int BlendShapeCount { get; }
    }

    public sealed class BlendShapeConverter
    {
        public BlendShapeConversionResult Convert(PmxDocument document, Mesh mesh,
            PmxCoordinateConverter coordinates)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
            if (mesh.vertexCount != document.Vertices.Count)
                throw new InvalidOperationException(
                    $"Mesh vertex count {mesh.vertexCount} does not match PMX vertex count {document.Vertices.Count}.");

            var mapping = new int[document.Morphs.Count];
            for (int i = 0; i < mapping.Length; i++) mapping[i] = -1;
            int blendShapeIndex = 0;
            int vertexCount = document.Vertices.Count;
            for (int morphIndex = 0; morphIndex < document.Morphs.Count; morphIndex++)
            {
                PmxMorph morph = document.Morphs[morphIndex];
                if (morph.Type != PmxMorphType.Vertex) continue;

                var deltaVertices = new Vector3[vertexCount];
                var deltaNormals = new Vector3[vertexCount];
                var deltaTangents = new Vector3[vertexCount];
                for (int offsetIndex = 0; offsetIndex < morph.Offsets.Count; offsetIndex++)
                {
                    if (!(morph.Offsets[offsetIndex] is PmxVertexMorphOffset offset))
                        throw new InvalidOperationException(
                            $"Vertex morph {morphIndex} contains a non-vertex offset at {offsetIndex}.");
                    if (offset.VertexIndex < 0 || offset.VertexIndex >= vertexCount)
                        throw new InvalidOperationException(
                            $"Vertex morph {morphIndex} offset {offsetIndex} references vertex " +
                            $"{offset.VertexIndex}, outside [0, {vertexCount}).");

                    deltaVertices[offset.VertexIndex] +=
                        coordinates.ConvertPositionDelta(offset.Translation);
                }

                string stableName = $"PMX Vertex Morph {morphIndex:D6}";
                mesh.AddBlendShapeFrame(stableName, 100f,
                    deltaVertices, deltaNormals, deltaTangents);
                mapping[morphIndex] = blendShapeIndex++;
            }

            return new BlendShapeConversionResult(mapping, blendShapeIndex);
        }
    }
}
