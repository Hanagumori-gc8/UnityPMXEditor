using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public enum PmxDiagnosticSeverity { Info = 0, Warning = 1, Error = 2 }

    [Serializable]
    public sealed class PmxImportDiagnostic
    {
        [SerializeField] private PmxDiagnosticSeverity severity;
        [SerializeField] private PmxFeatureSupportStatus status;
        [SerializeField] private string code;
        [SerializeField] private string message;
        [SerializeField] private string section;
        [SerializeField] private long byteOffset = -1;

        internal PmxImportDiagnostic(PmxDiagnosticSeverity severity,
            PmxFeatureSupportStatus status, string code, string message,
            string section = "Import", long byteOffset = -1)
        { this.severity = severity; this.status = status; this.code = code; this.message = message; this.section = section; this.byteOffset = byteOffset; }

        public PmxDiagnosticSeverity Severity => severity;
        public PmxFeatureSupportStatus Status => status;
        public string Code => code;
        public string Message => message;
        public string Section => section;
        public long ByteOffset => byteOffset;
    }

    [Serializable]
    public sealed class PmxHeaderMetadata
    {
        public float Version;
        public PmxTextEncoding TextEncoding;
        public byte AdditionalUvCount;
        public byte VertexIndexSize;
        public byte TextureIndexSize;
        public byte MaterialIndexSize;
        public byte BoneIndexSize;
        public byte MorphIndexSize;
        public byte RigidBodyIndexSize;
    }

    [Serializable]
    public sealed class PmxIkLinkMetadata
    {
        public int BoneIndex;
        public byte RawLimitFlag;
        public bool HasLimits;
        public Vector3 MinimumAngle;
        public Vector3 MaximumAngle;
    }

    [Serializable]
    public sealed class PmxIkMetadata
    {
        public int TargetBoneIndex;
        public int LoopCount;
        public float AngleLimit;
        public PmxIkLinkMetadata[] Links = Array.Empty<PmxIkLinkMetadata>();
    }

    [Serializable]
    public sealed class PmxBoneMetadata
    {
        public string Name;
        public string EnglishName;
        public Vector3 Position;
        public int ParentBoneIndex;
        public int Layer;
        public ushort RawFlags;
        public bool HasTailBoneIndex;
        public int TailBoneIndex;
        public Vector3 TailOffset;
        public bool HasInheritParent;
        public int InheritParentBoneIndex;
        public float InheritWeight;
        public bool HasFixedAxis;
        public Vector3 FixedAxis;
        public bool HasLocalAxes;
        public Vector3 LocalAxisX;
        public Vector3 LocalAxisZ;
        public bool HasExternalParentKey;
        public int ExternalParentKey;
        public PmxIkMetadata InverseKinematics;
    }

    [Serializable]
    public sealed class PmxMaterialMetadata
    {
        public string Name;
        public string EnglishName;
        public Vector4 Diffuse;
        public Vector3 Specular;
        public float SpecularStrength;
        public Vector3 Ambient;
        public byte RawFlags;
        public Vector4 EdgeColor;
        public float EdgeSize;
        public int TextureIndex;
        public int EnvironmentTextureIndex;
        public byte RawEnvironmentBlendMode;
        public byte RawToonReference;
        public int ToonTextureIndex;
        public string Metadata;
        public int SurfaceIndexCount;
        public PmxFeatureSupportStatus SupportStatus;
    }

    [Serializable]
    public sealed class PmxMorphOffsetMetadata
    {
        public PmxMorphType MorphType;
        public int MorphIndex = -1;
        public int VertexIndex = -1;
        public int BoneIndex = -1;
        public int MaterialIndex = -1;
        public int RigidBodyIndex = -1;
        public float Weight;
        public Vector3 Translation;
        public Vector4 Rotation;
        public Vector4 UvDelta;
        public byte RawOperation;
        public Vector4 Diffuse;
        public Vector3 Specular;
        public float SpecularStrength;
        public Vector3 Ambient;
        public Vector4 EdgeColor;
        public float EdgeSize;
        public Vector4 TextureTint;
        public Vector4 EnvironmentTint;
        public Vector4 ToonTint;
        public byte RawLocalFlag;
        public Vector3 Velocity;
        public Vector3 Torque;
    }

    [Serializable]
    public sealed class PmxMorphMetadata
    {
        public string Name;
        public string EnglishName;
        public byte RawPanel;
        public byte RawType;
        public PmxFeatureSupportStatus SupportStatus;
        public int BlendShapeIndex = -1;
        public string StableBlendShapeName;
        public PmxMorphOffsetMetadata[] Offsets = Array.Empty<PmxMorphOffsetMetadata>();
    }

    [Serializable]
    public sealed class PmxDisplayFrameElementMetadata
    {
        public byte RawType;
        public int Index;
    }

    [Serializable]
    public sealed class PmxDisplayFrameMetadata
    {
        public string Name;
        public string EnglishName;
        public byte RawSpecialFlag;
        public PmxDisplayFrameElementMetadata[] Elements = Array.Empty<PmxDisplayFrameElementMetadata>();
    }

    [Serializable]
    public sealed class PmxRigidBodyMetadata
    {
        public string Name;
        public string EnglishName;
        public int BoneIndex;
        public byte RawCollisionGroup;
        public ushort RawNonCollisionMask;
        public byte RawShape;
        public Vector3 Size;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Mass;
        public float LinearDamping;
        public float AngularDamping;
        public float Restitution;
        public float Friction;
        public byte RawPhysicsMode;
    }

    [Serializable]
    public sealed class PmxJointMetadata
    {
        public string Name;
        public string EnglishName;
        public byte RawType;
        public int RigidBodyAIndex;
        public int RigidBodyBIndex;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 MinimumPosition;
        public Vector3 MaximumPosition;
        public Vector3 MinimumRotation;
        public Vector3 MaximumRotation;
        public Vector3 PositionSpring;
        public Vector3 RotationSpring;
    }

    [Serializable]
    public sealed class PmxSoftBodyAnchorMetadata
    {
        public int RigidBodyIndex;
        public int VertexIndex;
        public byte RawNearMode;
    }

    [Serializable]
    public sealed class PmxSoftBodyMetadata
    {
        public PmxFeatureSupportStatus SupportStatus;
        public string Name;
        public string EnglishName;
        public byte RawShape;
        public int MaterialIndex;
        public byte RawCollisionGroup;
        public ushort RawNonCollisionMask;
        public byte RawFlags;
        public int BLinkDistance;
        public int ClusterCount;
        public float TotalMass;
        public float CollisionMargin;
        public int RawAerodynamicsModel;
        public float[] Config = Array.Empty<float>();
        public float[] Cluster = Array.Empty<float>();
        public int[] Iteration = Array.Empty<int>();
        public float[] Material = Array.Empty<float>();
        public PmxSoftBodyAnchorMetadata[] Anchors = Array.Empty<PmxSoftBodyAnchorMetadata>();
        public int[] PinnedVertexIndices = Array.Empty<int>();
    }

    internal static class PmxMetadataFactory
    {
        public static PmxHeaderMetadata Header(PmxHeader value) => new PmxHeaderMetadata
        {
            Version = value.Version, TextEncoding = value.TextEncoding,
            AdditionalUvCount = value.AdditionalUvCount, VertexIndexSize = value.VertexIndexSize,
            TextureIndexSize = value.TextureIndexSize, MaterialIndexSize = value.MaterialIndexSize,
            BoneIndexSize = value.BoneIndexSize, MorphIndexSize = value.MorphIndexSize,
            RigidBodyIndexSize = value.RigidBodyIndexSize
        };

        public static PmxBoneMetadata[] Bones(IReadOnlyList<PmxBone> values)
        {
            var result = new PmxBoneMetadata[values.Count];
            for (int i = 0; i < result.Length; i++) result[i] = Bone(values[i]);
            return result;
        }

        public static PmxMaterialMetadata[] Materials(IReadOnlyList<PmxMaterial> values)
        {
            var result = new PmxMaterialMetadata[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                PmxMaterial v = values[i];
                result[i] = new PmxMaterialMetadata
                {
                    Name = v.Name, EnglishName = v.EnglishName, Diffuse = V4(v.Diffuse),
                    Specular = V3(v.Specular), SpecularStrength = v.SpecularStrength,
                    Ambient = V3(v.Ambient), RawFlags = v.RawFlags, EdgeColor = V4(v.EdgeColor),
                    EdgeSize = v.EdgeSize, TextureIndex = v.TextureIndex,
                    EnvironmentTextureIndex = v.EnvironmentTextureIndex,
                    RawEnvironmentBlendMode = v.RawEnvironmentBlendMode,
                    RawToonReference = v.RawToonReference, ToonTextureIndex = v.ToonTextureIndex,
                    Metadata = v.Metadata, SurfaceIndexCount = v.SurfaceIndexCount,
                    SupportStatus = PmxFeatureSupportStatus.Approximated
                };
            }
            return result;
        }

        public static PmxMorphMetadata[] Morphs(IReadOnlyList<PmxMorph> values, int[] mapping)
        {
            var result = new PmxMorphMetadata[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                PmxMorph v = values[i];
                int blendShape = mapping != null && i < mapping.Length ? mapping[i] : -1;
                var offsets = new PmxMorphOffsetMetadata[v.Offsets.Count];
                for (int j = 0; j < offsets.Length; j++) offsets[j] = MorphOffset(v.Type, v.Offsets[j]);
                result[i] = new PmxMorphMetadata
                {
                    Name = v.Name, EnglishName = v.EnglishName, RawPanel = v.RawPanel,
                    RawType = v.RawType,
                    SupportStatus = MorphSupport(v.Type),
                    BlendShapeIndex = blendShape,
                    StableBlendShapeName = blendShape >= 0 ? $"PMX Vertex Morph {i:D6}" : string.Empty,
                    Offsets = offsets
                };
            }
            return result;
        }

        internal static PmxFeatureSupportStatus MorphSupport(PmxMorphType type)
        {
            switch (type)
            {
                case PmxMorphType.Vertex:
                    return PmxFeatureSupportStatus.Supported;
                case PmxMorphType.Group:
                case PmxMorphType.Bone:
                case PmxMorphType.Uv:
                case PmxMorphType.Material:
                case PmxMorphType.Flip:
                    return PmxFeatureSupportStatus.Approximated;
                default:
                    return PmxFeatureSupportStatus.Preserved;
            }
        }

        public static PmxDisplayFrameMetadata[] DisplayFrames(IReadOnlyList<PmxDisplayFrame> values)
        {
            var result = new PmxDisplayFrameMetadata[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                PmxDisplayFrame value = values[i];
                var elements = new PmxDisplayFrameElementMetadata[value.Elements.Count];
                for (int j = 0; j < elements.Length; j++)
                    elements[j] = new PmxDisplayFrameElementMetadata { RawType = value.Elements[j].RawType, Index = value.Elements[j].Index };
                result[i] = new PmxDisplayFrameMetadata
                { Name = value.Name, EnglishName = value.EnglishName, RawSpecialFlag = value.RawSpecialFlag, Elements = elements };
            }
            return result;
        }

        public static PmxRigidBodyMetadata[] RigidBodies(IReadOnlyList<PmxRigidBody> values)
        {
            var result = new PmxRigidBodyMetadata[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                PmxRigidBody v = values[i];
                result[i] = new PmxRigidBodyMetadata
                { Name = v.Name, EnglishName = v.EnglishName, BoneIndex = v.BoneIndex,
                    RawCollisionGroup = v.RawCollisionGroup, RawNonCollisionMask = v.RawNonCollisionMask,
                    RawShape = v.RawShape, Size = V3(v.Size), Position = V3(v.Position), Rotation = V3(v.Rotation),
                    Mass = v.Mass, LinearDamping = v.LinearDamping, AngularDamping = v.AngularDamping,
                    Restitution = v.Restitution, Friction = v.Friction, RawPhysicsMode = v.RawPhysicsMode };
            }
            return result;
        }

        public static PmxJointMetadata[] Joints(IReadOnlyList<PmxJoint> values)
        {
            var result = new PmxJointMetadata[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                PmxJoint v = values[i];
                result[i] = new PmxJointMetadata
                { Name = v.Name, EnglishName = v.EnglishName, RawType = v.RawType,
                    RigidBodyAIndex = v.RigidBodyAIndex, RigidBodyBIndex = v.RigidBodyBIndex,
                    Position = V3(v.Position), Rotation = V3(v.Rotation),
                    MinimumPosition = V3(v.MinimumPosition), MaximumPosition = V3(v.MaximumPosition),
                    MinimumRotation = V3(v.MinimumRotation), MaximumRotation = V3(v.MaximumRotation),
                    PositionSpring = V3(v.PositionSpring), RotationSpring = V3(v.RotationSpring) };
            }
            return result;
        }

        public static PmxSoftBodyMetadata[] SoftBodies(IReadOnlyList<PmxSoftBody> values)
        {
            var result = new PmxSoftBodyMetadata[values.Count];
            for (int i = 0; i < result.Length; i++) result[i] = SoftBody(values[i]);
            return result;
        }

        private static PmxBoneMetadata Bone(PmxBone v)
        {
            var result = new PmxBoneMetadata
            { Name = v.Name, EnglishName = v.EnglishName, Position = V3(v.Position),
                ParentBoneIndex = v.ParentBoneIndex, Layer = v.DeformLayer, RawFlags = v.RawFlags,
                HasTailBoneIndex = v.TailBoneIndex.HasValue,
                TailBoneIndex = v.TailBoneIndex ?? -1, TailOffset = V3(v.TailOffset),
                HasInheritParent = v.InheritParentBoneIndex.HasValue,
                InheritParentBoneIndex = v.InheritParentBoneIndex ?? -1,
                InheritWeight = v.InheritWeight ?? 0f, HasFixedAxis = v.FixedAxis.HasValue,
                FixedAxis = V3(v.FixedAxis), HasLocalAxes = v.LocalAxisX.HasValue && v.LocalAxisZ.HasValue,
                LocalAxisX = V3(v.LocalAxisX), LocalAxisZ = V3(v.LocalAxisZ),
                HasExternalParentKey = v.ExternalParentKey.HasValue,
                ExternalParentKey = v.ExternalParentKey ?? 0 };
            if (v.InverseKinematics != null)
            {
                var links = new PmxIkLinkMetadata[v.InverseKinematics.Links.Count];
                for (int i = 0; i < links.Length; i++)
                {
                    PmxBoneIkLink link = v.InverseKinematics.Links[i];
                    links[i] = new PmxIkLinkMetadata { BoneIndex = link.BoneIndex,
                        RawLimitFlag = link.RawLimitFlag, HasLimits = link.RawLimitFlag == 1,
                        MinimumAngle = V3(link.MinimumAngle), MaximumAngle = V3(link.MaximumAngle) };
                }
                result.InverseKinematics = new PmxIkMetadata
                { TargetBoneIndex = v.InverseKinematics.TargetBoneIndex,
                    LoopCount = v.InverseKinematics.LoopCount,
                    AngleLimit = v.InverseKinematics.AngleLimit, Links = links };
            }
            return result;
        }

        private static PmxMorphOffsetMetadata MorphOffset(PmxMorphType type, PmxMorphOffset value)
        {
            var r = new PmxMorphOffsetMetadata { MorphType = type };
            if (value is PmxGroupMorphOffset group) { r.MorphIndex = group.MorphIndex; r.Weight = group.Weight; }
            else if (value is PmxVertexMorphOffset vertex) { r.VertexIndex = vertex.VertexIndex; r.Translation = V3(vertex.Translation); }
            else if (value is PmxBoneMorphOffset bone) { r.BoneIndex = bone.BoneIndex; r.Translation = V3(bone.Translation); r.Rotation = V4(bone.Rotation); }
            else if (value is PmxUvMorphOffset uv) { r.VertexIndex = uv.VertexIndex; r.UvDelta = V4(uv.Value); }
            else if (value is PmxMaterialMorphOffset material)
            { r.MaterialIndex = material.MaterialIndex; r.RawOperation = material.RawOperation;
                r.Diffuse = V4(material.Diffuse); r.Specular = V3(material.Specular);
                r.SpecularStrength = material.SpecularStrength; r.Ambient = V3(material.Ambient);
                r.EdgeColor = V4(material.EdgeColor); r.EdgeSize = material.EdgeSize;
                r.TextureTint = V4(material.TextureTint); r.EnvironmentTint = V4(material.EnvironmentTint);
                r.ToonTint = V4(material.ToonTint); }
            else if (value is PmxFlipMorphOffset flip) { r.MorphIndex = flip.MorphIndex; r.Weight = flip.Weight; }
            else if (value is PmxImpulseMorphOffset impulse)
            { r.RigidBodyIndex = impulse.RigidBodyIndex; r.RawLocalFlag = impulse.RawLocalFlag;
                r.Velocity = V3(impulse.Velocity); r.Torque = V3(impulse.Torque); }
            return r;
        }

        private static PmxSoftBodyMetadata SoftBody(PmxSoftBody v)
        {
            var anchors = new PmxSoftBodyAnchorMetadata[v.Anchors.Count];
            for (int i = 0; i < anchors.Length; i++) anchors[i] = new PmxSoftBodyAnchorMetadata
            { RigidBodyIndex = v.Anchors[i].RigidBodyIndex, VertexIndex = v.Anchors[i].VertexIndex,
                RawNearMode = v.Anchors[i].RawNearMode };
            return new PmxSoftBodyMetadata
            { SupportStatus = PmxFeatureSupportStatus.Unsupported,
                Name = v.Name, EnglishName = v.EnglishName, RawShape = v.RawShape,
                MaterialIndex = v.MaterialIndex, RawCollisionGroup = v.RawCollisionGroup,
                RawNonCollisionMask = v.RawNonCollisionMask, RawFlags = v.RawFlags,
                BLinkDistance = v.BLinkDistance, ClusterCount = v.ClusterCount,
                TotalMass = v.TotalMass, CollisionMargin = v.CollisionMargin,
                RawAerodynamicsModel = v.RawAerodynamicsModel,
                Config = new[] { v.Config.Vcf, v.Config.Dp, v.Config.Dg, v.Config.Lf, v.Config.Pr, v.Config.Vc,
                    v.Config.Df, v.Config.Mt, v.Config.Chr, v.Config.Khr, v.Config.Shr, v.Config.Ahr },
                Cluster = new[] { v.Cluster.Srhr, v.Cluster.Skhr, v.Cluster.Sshr,
                    v.Cluster.SrSplit, v.Cluster.SkSplit, v.Cluster.SsSplit },
                Iteration = new[] { v.Iteration.Velocity, v.Iteration.Position, v.Iteration.Drift, v.Iteration.Cluster },
                Material = new[] { v.Material.LinearStiffness, v.Material.AngularStiffness, v.Material.VolumeStiffness },
                Anchors = anchors, PinnedVertexIndices = Copy(v.PinnedVertexIndices) };
        }

        private static int[] Copy(IReadOnlyList<int> value)
        { var result = new int[value.Count]; for (int i = 0; i < result.Length; i++) result[i] = value[i]; return result; }
        private static Vector3 V3(PmxVector3 value) => new Vector3(value.X, value.Y, value.Z);
        private static Vector3 V3(PmxVector3? value) => value.HasValue ? V3(value.Value) : Vector3.zero;
        private static Vector4 V4(PmxVector4 value) => new Vector4(value.X, value.Y, value.Z, value.W);
    }
}
