namespace Hanagumori.UnityPmx
{
    public sealed class PmxHeader
    {
        internal PmxHeader(
            float version,
            PmxTextEncoding textEncoding,
            byte additionalUvCount,
            byte vertexIndexSize,
            byte textureIndexSize,
            byte materialIndexSize,
            byte boneIndexSize,
            byte morphIndexSize,
            byte rigidBodyIndexSize)
        {
            Version = version;
            TextEncoding = textEncoding;
            AdditionalUvCount = additionalUvCount;
            VertexIndexSize = vertexIndexSize;
            TextureIndexSize = textureIndexSize;
            MaterialIndexSize = materialIndexSize;
            BoneIndexSize = boneIndexSize;
            MorphIndexSize = morphIndexSize;
            RigidBodyIndexSize = rigidBodyIndexSize;
        }

        public float Version { get; }
        public PmxTextEncoding TextEncoding { get; }
        public byte AdditionalUvCount { get; }
        public byte VertexIndexSize { get; }
        public byte TextureIndexSize { get; }
        public byte MaterialIndexSize { get; }
        public byte BoneIndexSize { get; }
        public byte MorphIndexSize { get; }
        public byte RigidBodyIndexSize { get; }
        public bool IsVersion21 => Version > 2.05f;
    }
}
