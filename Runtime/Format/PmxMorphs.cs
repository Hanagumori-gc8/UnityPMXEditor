using System.Collections.Generic;

namespace Hanagumori.UnityPmx
{
    public abstract class PmxMorphOffset
    {
        protected PmxMorphOffset(long sourceOffset) { SourceOffset = sourceOffset; }
        internal long SourceOffset { get; }
    }

    public sealed class PmxGroupMorphOffset : PmxMorphOffset
    {
        internal PmxGroupMorphOffset(long offset, int morphIndex, float weight) : base(offset)
        { MorphIndex = morphIndex; Weight = weight; }
        public int MorphIndex { get; }
        public float Weight { get; }
    }

    public sealed class PmxVertexMorphOffset : PmxMorphOffset
    {
        internal PmxVertexMorphOffset(long offset, int vertexIndex, PmxVector3 translation) : base(offset)
        { VertexIndex = vertexIndex; Translation = translation; }
        public int VertexIndex { get; }
        public PmxVector3 Translation { get; }
    }

    public sealed class PmxBoneMorphOffset : PmxMorphOffset
    {
        internal PmxBoneMorphOffset(long offset, int boneIndex, PmxVector3 translation, PmxVector4 rotation) : base(offset)
        { BoneIndex = boneIndex; Translation = translation; Rotation = rotation; }
        public int BoneIndex { get; }
        public PmxVector3 Translation { get; }
        public PmxVector4 Rotation { get; }
    }

    public sealed class PmxUvMorphOffset : PmxMorphOffset
    {
        internal PmxUvMorphOffset(long offset, int vertexIndex, PmxVector4 value) : base(offset)
        { VertexIndex = vertexIndex; Value = value; }
        public int VertexIndex { get; }
        public PmxVector4 Value { get; }
    }

    public sealed class PmxMaterialMorphOffset : PmxMorphOffset
    {
        internal PmxMaterialMorphOffset(long offset, int materialIndex, byte rawOperation,
            PmxVector4 diffuse, PmxVector3 specular, float specularStrength, PmxVector3 ambient,
            PmxVector4 edgeColor, float edgeSize, PmxVector4 textureTint,
            PmxVector4 environmentTint, PmxVector4 toonTint) : base(offset)
        {
            MaterialIndex = materialIndex;
            RawOperation = rawOperation;
            Diffuse = diffuse;
            Specular = specular;
            SpecularStrength = specularStrength;
            Ambient = ambient;
            EdgeColor = edgeColor;
            EdgeSize = edgeSize;
            TextureTint = textureTint;
            EnvironmentTint = environmentTint;
            ToonTint = toonTint;
        }
        public int MaterialIndex { get; }
        public byte RawOperation { get; }
        public PmxVector4 Diffuse { get; }
        public PmxVector3 Specular { get; }
        public float SpecularStrength { get; }
        public PmxVector3 Ambient { get; }
        public PmxVector4 EdgeColor { get; }
        public float EdgeSize { get; }
        public PmxVector4 TextureTint { get; }
        public PmxVector4 EnvironmentTint { get; }
        public PmxVector4 ToonTint { get; }
    }

    public sealed class PmxFlipMorphOffset : PmxMorphOffset
    {
        internal PmxFlipMorphOffset(long offset, int morphIndex, float weight) : base(offset)
        { MorphIndex = morphIndex; Weight = weight; }
        public int MorphIndex { get; }
        public float Weight { get; }
    }

    public sealed class PmxImpulseMorphOffset : PmxMorphOffset
    {
        internal PmxImpulseMorphOffset(long offset, int rigidBodyIndex, byte rawLocalFlag,
            PmxVector3 velocity, PmxVector3 torque) : base(offset)
        { RigidBodyIndex = rigidBodyIndex; RawLocalFlag = rawLocalFlag; Velocity = velocity; Torque = torque; }
        public int RigidBodyIndex { get; }
        public byte RawLocalFlag { get; }
        public PmxVector3 Velocity { get; }
        public PmxVector3 Torque { get; }
    }

    public sealed class PmxMorph
    {
        internal PmxMorph(long sourceOffset, string name, string englishName,
            byte rawPanel, byte rawType, List<PmxMorphOffset> offsets)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            RawPanel = rawPanel;
            RawType = rawType;
            Offsets = offsets;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public byte RawPanel { get; }
        public byte RawType { get; }
        public PmxMorphType Type => (PmxMorphType)RawType;
        public IReadOnlyList<PmxMorphOffset> Offsets { get; }
    }
}
