using System.Collections.Generic;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxDocument
    {
        internal PmxDocument(
            PmxHeader header,
            string name,
            string englishName,
            string comment,
            string englishComment,
            List<PmxVertex> vertices,
            List<int> surfaceVertexIndices,
            List<long> surfaceIndexOffsets,
            List<PmxTexture> textures,
            List<PmxMaterial> materials,
            List<PmxBone> bones,
            List<PmxMorph> morphs,
            List<PmxDisplayFrame> displayFrames,
            List<PmxRigidBody> rigidBodies,
            List<PmxJoint> joints,
            List<PmxSoftBody> softBodies)
        {
            Header = header;
            Name = name;
            EnglishName = englishName;
            Comment = comment;
            EnglishComment = englishComment;
            Vertices = vertices;
            SurfaceVertexIndices = surfaceVertexIndices;
            SurfaceIndexOffsets = surfaceIndexOffsets;
            var surfaces = new List<PmxSurface>(surfaceVertexIndices.Count / 3);
            for (int i = 0; i < surfaceVertexIndices.Count; i += 3)
            {
                surfaces.Add(new PmxSurface(surfaceIndexOffsets[i], surfaceVertexIndices[i],
                    surfaceVertexIndices[i + 1], surfaceVertexIndices[i + 2]));
            }
            Surfaces = surfaces;
            Textures = textures;
            Materials = materials;
            Bones = bones;
            Morphs = morphs;
            DisplayFrames = displayFrames;
            RigidBodies = rigidBodies;
            Joints = joints;
            SoftBodies = softBodies;
        }

        public PmxHeader Header { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public string Comment { get; }
        public string EnglishComment { get; }
        public IReadOnlyList<PmxVertex> Vertices { get; }
        public IReadOnlyList<int> SurfaceVertexIndices { get; }
        public IReadOnlyList<PmxSurface> Surfaces { get; }
        public IReadOnlyList<PmxTexture> Textures { get; }
        public IReadOnlyList<PmxMaterial> Materials { get; }
        public IReadOnlyList<PmxBone> Bones { get; }
        public IReadOnlyList<PmxMorph> Morphs { get; }
        public IReadOnlyList<PmxDisplayFrame> DisplayFrames { get; }
        public IReadOnlyList<PmxRigidBody> RigidBodies { get; }
        public IReadOnlyList<PmxJoint> Joints { get; }
        public IReadOnlyList<PmxSoftBody> SoftBodies { get; }

        internal IReadOnlyList<long> SurfaceIndexOffsets { get; }
    }
}
