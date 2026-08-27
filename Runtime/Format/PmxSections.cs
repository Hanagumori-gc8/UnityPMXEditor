using System.Collections.Generic;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxSurface
    {
        internal PmxSurface(long sourceOffset, int vertexA, int vertexB, int vertexC)
        {
            SourceOffset = sourceOffset;
            VertexA = vertexA;
            VertexB = vertexB;
            VertexC = vertexC;
        }

        internal long SourceOffset { get; }
        public int VertexA { get; }
        public int VertexB { get; }
        public int VertexC { get; }
    }

    public sealed class PmxTexture
    {
        internal PmxTexture(long sourceOffset, string path)
        {
            SourceOffset = sourceOffset;
            Path = path;
        }

        internal long SourceOffset { get; }
        public string Path { get; }
    }

    public sealed class PmxVertexDeform
    {
        internal PmxVertexDeform(
            byte rawType,
            List<int> boneIndices,
            List<float> weights,
            PmxVector3? sdefC,
            PmxVector3? sdefR0,
            PmxVector3? sdefR1)
        {
            RawType = rawType;
            BoneIndices = boneIndices;
            Weights = weights;
            SdefC = sdefC;
            SdefR0 = sdefR0;
            SdefR1 = sdefR1;
        }

        public byte RawType { get; }
        public PmxVertexWeightType Type => (PmxVertexWeightType)RawType;
        public IReadOnlyList<int> BoneIndices { get; }
        public IReadOnlyList<float> Weights { get; }
        public PmxVector3? SdefC { get; }
        public PmxVector3? SdefR0 { get; }
        public PmxVector3? SdefR1 { get; }
    }

    public sealed class PmxVertex
    {
        internal PmxVertex(long sourceOffset, PmxVector3 position, PmxVector3 normal, PmxVector2 uv,
            List<PmxVector4> additionalUvs, PmxVertexDeform deform, float edgeScale)
        {
            SourceOffset = sourceOffset;
            Position = position;
            Normal = normal;
            Uv = uv;
            AdditionalUvs = additionalUvs;
            Deform = deform;
            EdgeScale = edgeScale;
        }

        internal long SourceOffset { get; }
        public PmxVector3 Position { get; }
        public PmxVector3 Normal { get; }
        public PmxVector2 Uv { get; }
        public IReadOnlyList<PmxVector4> AdditionalUvs { get; }
        public PmxVertexDeform Deform { get; }
        public float EdgeScale { get; }
    }

    public sealed class PmxMaterial
    {
        internal PmxMaterial(long sourceOffset, string name, string englishName, PmxVector4 diffuse,
            PmxVector3 specular, float specularStrength, PmxVector3 ambient, byte rawFlags,
            PmxVector4 edgeColor, float edgeSize, int textureIndex, int environmentTextureIndex,
            byte rawEnvironmentBlendMode, byte rawToonReference, int toonTextureIndex,
            string metadata, int surfaceIndexCount)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            Diffuse = diffuse;
            Specular = specular;
            SpecularStrength = specularStrength;
            Ambient = ambient;
            RawFlags = rawFlags;
            EdgeColor = edgeColor;
            EdgeSize = edgeSize;
            TextureIndex = textureIndex;
            EnvironmentTextureIndex = environmentTextureIndex;
            RawEnvironmentBlendMode = rawEnvironmentBlendMode;
            RawToonReference = rawToonReference;
            ToonTextureIndex = toonTextureIndex;
            Metadata = metadata;
            SurfaceIndexCount = surfaceIndexCount;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public PmxVector4 Diffuse { get; }
        public PmxVector3 Specular { get; }
        public float SpecularStrength { get; }
        public PmxVector3 Ambient { get; }
        public byte RawFlags { get; }
        public PmxMaterialFlags Flags => (PmxMaterialFlags)RawFlags;
        public PmxVector4 EdgeColor { get; }
        public float EdgeSize { get; }
        public int TextureIndex { get; }
        public int EnvironmentTextureIndex { get; }
        public byte RawEnvironmentBlendMode { get; }
        public byte RawToonReference { get; }
        public bool UsesSharedToonTexture => RawToonReference == 1;
        public int ToonTextureIndex { get; }
        public string Metadata { get; }
        public int SurfaceIndexCount { get; }
    }

    public sealed class PmxBoneIkLink
    {
        internal PmxBoneIkLink(long sourceOffset, int boneIndex, byte rawLimitFlag,
            PmxVector3? minimumAngle, PmxVector3? maximumAngle)
        {
            SourceOffset = sourceOffset;
            BoneIndex = boneIndex;
            RawLimitFlag = rawLimitFlag;
            MinimumAngle = minimumAngle;
            MaximumAngle = maximumAngle;
        }

        internal long SourceOffset { get; }
        public int BoneIndex { get; }
        public byte RawLimitFlag { get; }
        public PmxVector3? MinimumAngle { get; }
        public PmxVector3? MaximumAngle { get; }
    }

    public sealed class PmxBoneIk
    {
        internal PmxBoneIk(long sourceOffset, int targetBoneIndex, int loopCount,
            float angleLimit, List<PmxBoneIkLink> links)
        {
            SourceOffset = sourceOffset;
            TargetBoneIndex = targetBoneIndex;
            LoopCount = loopCount;
            AngleLimit = angleLimit;
            Links = links;
        }

        internal long SourceOffset { get; }
        public int TargetBoneIndex { get; }
        public int LoopCount { get; }
        public float AngleLimit { get; }
        public IReadOnlyList<PmxBoneIkLink> Links { get; }
    }

    public sealed class PmxBone
    {
        internal PmxBone(long sourceOffset, string name, string englishName, PmxVector3 position,
            int parentBoneIndex, int deformLayer, ushort rawFlags, int? tailBoneIndex,
            PmxVector3? tailOffset, int? inheritParentBoneIndex, float? inheritWeight,
            PmxVector3? fixedAxis, PmxVector3? localAxisX, PmxVector3? localAxisZ,
            int? externalParentKey, PmxBoneIk inverseKinematics)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            Position = position;
            ParentBoneIndex = parentBoneIndex;
            DeformLayer = deformLayer;
            RawFlags = rawFlags;
            TailBoneIndex = tailBoneIndex;
            TailOffset = tailOffset;
            InheritParentBoneIndex = inheritParentBoneIndex;
            InheritWeight = inheritWeight;
            FixedAxis = fixedAxis;
            LocalAxisX = localAxisX;
            LocalAxisZ = localAxisZ;
            ExternalParentKey = externalParentKey;
            InverseKinematics = inverseKinematics;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public PmxVector3 Position { get; }
        public int ParentBoneIndex { get; }
        public int DeformLayer { get; }
        public ushort RawFlags { get; }
        public PmxBoneFlags Flags => (PmxBoneFlags)RawFlags;
        public int? TailBoneIndex { get; }
        public PmxVector3? TailOffset { get; }
        public int? InheritParentBoneIndex { get; }
        public float? InheritWeight { get; }
        public PmxVector3? FixedAxis { get; }
        public PmxVector3? LocalAxisX { get; }
        public PmxVector3? LocalAxisZ { get; }
        public int? ExternalParentKey { get; }
        public PmxBoneIk InverseKinematics { get; }
    }

    public sealed class PmxDisplayFrameElement
    {
        internal PmxDisplayFrameElement(long sourceOffset, byte rawType, int index)
        {
            SourceOffset = sourceOffset;
            RawType = rawType;
            Index = index;
        }

        internal long SourceOffset { get; }
        public byte RawType { get; }
        public int Index { get; }
        public bool IsMorph => RawType == 1;
    }

    public sealed class PmxDisplayFrame
    {
        internal PmxDisplayFrame(long sourceOffset, string name, string englishName,
            byte rawSpecialFlag, List<PmxDisplayFrameElement> elements)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            RawSpecialFlag = rawSpecialFlag;
            Elements = elements;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public byte RawSpecialFlag { get; }
        public IReadOnlyList<PmxDisplayFrameElement> Elements { get; }
    }

    public sealed class PmxRigidBody
    {
        internal PmxRigidBody(long sourceOffset, string name, string englishName, int boneIndex,
            byte rawCollisionGroup, ushort rawNonCollisionMask, byte rawShape,
            PmxVector3 size, PmxVector3 position, PmxVector3 rotation, float mass,
            float linearDamping, float angularDamping, float restitution, float friction,
            byte rawPhysicsMode)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            BoneIndex = boneIndex;
            RawCollisionGroup = rawCollisionGroup;
            RawNonCollisionMask = rawNonCollisionMask;
            RawShape = rawShape;
            Size = size;
            Position = position;
            Rotation = rotation;
            Mass = mass;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
            Restitution = restitution;
            Friction = friction;
            RawPhysicsMode = rawPhysicsMode;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public int BoneIndex { get; }
        public byte RawCollisionGroup { get; }
        public ushort RawNonCollisionMask { get; }
        public byte RawShape { get; }
        public PmxVector3 Size { get; }
        public PmxVector3 Position { get; }
        public PmxVector3 Rotation { get; }
        public float Mass { get; }
        public float LinearDamping { get; }
        public float AngularDamping { get; }
        public float Restitution { get; }
        public float Friction { get; }
        public byte RawPhysicsMode { get; }
    }

    public sealed class PmxJoint
    {
        internal PmxJoint(long sourceOffset, string name, string englishName, byte rawType,
            int rigidBodyAIndex, int rigidBodyBIndex, PmxVector3 position, PmxVector3 rotation,
            PmxVector3 minimumPosition, PmxVector3 maximumPosition, PmxVector3 minimumRotation,
            PmxVector3 maximumRotation, PmxVector3 positionSpring, PmxVector3 rotationSpring)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            RawType = rawType;
            RigidBodyAIndex = rigidBodyAIndex;
            RigidBodyBIndex = rigidBodyBIndex;
            Position = position;
            Rotation = rotation;
            MinimumPosition = minimumPosition;
            MaximumPosition = maximumPosition;
            MinimumRotation = minimumRotation;
            MaximumRotation = maximumRotation;
            PositionSpring = positionSpring;
            RotationSpring = rotationSpring;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public byte RawType { get; }
        public int RigidBodyAIndex { get; }
        public int RigidBodyBIndex { get; }
        public PmxVector3 Position { get; }
        public PmxVector3 Rotation { get; }
        public PmxVector3 MinimumPosition { get; }
        public PmxVector3 MaximumPosition { get; }
        public PmxVector3 MinimumRotation { get; }
        public PmxVector3 MaximumRotation { get; }
        public PmxVector3 PositionSpring { get; }
        public PmxVector3 RotationSpring { get; }
    }
}
