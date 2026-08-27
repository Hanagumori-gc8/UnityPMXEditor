using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxBinaryReader : IDisposable
    {
        private const byte HeaderGlobalCount = 8;
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly PmxReadLimits _limits;
        private readonly byte[] _scratch = new byte[4];
        private readonly long _startOffset;
        private long _offset;
        private long _totalCollectionItems;
        private string _section = "Header";
        private Encoding _textEncoding;
        private PmxHeader _header;
        private bool _hasReadDocument;

        public PmxBinaryReader(Stream stream, PmxReadLimits limits = null, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
            {
                throw new ArgumentException("The PMX stream must be readable.", nameof(stream));
            }

            _limits = (limits ?? PmxReadLimits.Default).CloneValidated();
            _leaveOpen = leaveOpen;
            _offset = stream.CanSeek ? stream.Position : 0;
            _startOffset = _offset;

            if (stream.CanSeek)
            {
                long remaining = stream.Length - stream.Position;
                if (remaining < 0)
                {
                    throw new ArgumentException("The PMX stream position is beyond its length.", nameof(stream));
                }

                if (remaining > _limits.MaxFileBytes)
                {
                    throw Error("Header", _offset,
                        $"The stream has {remaining} remaining bytes, exceeding MaxFileBytes {_limits.MaxFileBytes}.");
                }
            }
        }

        public long ByteOffset => _offset;
        public string Section => _section;

        public PmxDocument ReadDocument()
        {
            if (_hasReadDocument)
            {
                throw new InvalidOperationException("A PmxBinaryReader can read only one document.");
            }

            _hasReadDocument = true;
            try
            {
                _header = ReadHeader();

                SetSection("ModelInfo");
                string name = ReadText();
                string englishName = ReadText();
                string comment = ReadText();
                string englishComment = ReadText();

                List<PmxVertex> vertices = ReadVertices();
                List<long> surfaceOffsets;
                List<int> surfaces = ReadSurfaces(out surfaceOffsets);
                List<PmxTexture> textures = ReadTextures();
                List<PmxMaterial> materials = ReadMaterials();
                List<PmxBone> bones = ReadBones();
                List<PmxMorph> morphs = ReadMorphs();
                List<PmxDisplayFrame> displayFrames = ReadDisplayFrames();
                List<PmxRigidBody> rigidBodies = ReadRigidBodies();
                List<PmxJoint> joints = ReadJoints();
                List<PmxSoftBody> softBodies = _header.IsVersion21
                    ? ReadSoftBodies()
                    : new List<PmxSoftBody>();

                var document = new PmxDocument(_header, name, englishName, comment, englishComment,
                    vertices, surfaces, surfaceOffsets, textures, materials, bones, morphs,
                    displayFrames, rigidBodies, joints, softBodies);

                PmxDocumentValidator.Validate(document);
                EnsureEndOfStream();
                return document;
            }
            catch (PmxFormatException)
            {
                throw;
            }
            catch (IOException exception)
            {
                throw Error(_section, _offset, exception.Message, exception);
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }

        private PmxHeader ReadHeader()
        {
            SetSection("Header");
            long signatureOffset = _offset;
            byte p = ReadByteValue();
            byte m = ReadByteValue();
            byte x = ReadByteValue();
            byte space = ReadByteValue();
            if (p != 0x50 || m != 0x4D || x != 0x58 || space != 0x20)
            {
                throw Error("Header", signatureOffset,
                    "The signature must be the four bytes 'PMX ' (50 4D 58 20)." );
            }

            long versionOffset = _offset;
            float version = ReadSingle();
            if (version != 2.0f && version != 2.1f)
            {
                throw Error("Header", versionOffset,
                    $"Unsupported PMX version {version}. Only 2.0 and 2.1 are supported.");
            }

            long globalCountOffset = _offset;
            byte globalCount = ReadByteValue();
            if (globalCount != HeaderGlobalCount)
            {
                throw Error("Header", globalCountOffset,
                    $"The global byte count must be {HeaderGlobalCount}, but was {globalCount}.");
            }

            long globalsOffset = _offset;
            byte encoding = ReadByteValue();
            byte additionalUvCount = ReadByteValue();
            byte vertexIndexSize = ReadByteValue();
            byte textureIndexSize = ReadByteValue();
            byte materialIndexSize = ReadByteValue();
            byte boneIndexSize = ReadByteValue();
            byte morphIndexSize = ReadByteValue();
            byte rigidBodyIndexSize = ReadByteValue();

            if (encoding > 1)
            {
                throw Error("Header", globalsOffset, $"Unknown text encoding global value {encoding}.");
            }

            if (additionalUvCount > 4)
            {
                throw Error("Header", globalsOffset + 1,
                    $"Additional UV count must be between 0 and 4, but was {additionalUvCount}.");
            }

            ValidateIndexSize(vertexIndexSize, "vertex", globalsOffset + 2);
            ValidateIndexSize(textureIndexSize, "texture", globalsOffset + 3);
            ValidateIndexSize(materialIndexSize, "material", globalsOffset + 4);
            ValidateIndexSize(boneIndexSize, "bone", globalsOffset + 5);
            ValidateIndexSize(morphIndexSize, "morph", globalsOffset + 6);
            ValidateIndexSize(rigidBodyIndexSize, "rigid body", globalsOffset + 7);

            _textEncoding = encoding == 0
                ? new UnicodeEncoding(false, false, true)
                : new UTF8Encoding(false, true);

            return new PmxHeader(version, (PmxTextEncoding)encoding, additionalUvCount,
                vertexIndexSize, textureIndexSize, materialIndexSize, boneIndexSize,
                morphIndexSize, rigidBodyIndexSize);
        }

        private List<PmxVertex> ReadVertices()
        {
            SetSection("Vertex");
            int minimumBytes = 37 + (_header.AdditionalUvCount * 16) + _header.BoneIndexSize;
            int count = ReadCount(_limits.MaxVertices, "vertex", minimumBytes);
            var values = NewList<PmxVertex>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                PmxVector3 position = ReadVector3();
                PmxVector3 normal = ReadVector3();
                PmxVector2 uv = ReadVector2();
                var additionalUvs = new List<PmxVector4>(_header.AdditionalUvCount);
                for (int uvIndex = 0; uvIndex < _header.AdditionalUvCount; uvIndex++)
                {
                    additionalUvs.Add(ReadVector4());
                }

                PmxVertexDeform deform = ReadVertexDeform();
                float edgeScale = ReadSingle();
                values.Add(new PmxVertex(itemOffset, position, normal, uv, additionalUvs, deform, edgeScale));
            }

            return values;
        }

        private PmxVertexDeform ReadVertexDeform()
        {
            long typeOffset = _offset;
            byte rawType = ReadByteValue();
            var bones = new List<int>(4);
            var weights = new List<float>(4);
            PmxVector3? c = null;
            PmxVector3? r0 = null;
            PmxVector3? r1 = null;

            switch ((PmxVertexWeightType)rawType)
            {
                case PmxVertexWeightType.Bdef1:
                    bones.Add(ReadSignedIndex(_header.BoneIndexSize));
                    weights.Add(1f);
                    break;
                case PmxVertexWeightType.Bdef2:
                    bones.Add(ReadSignedIndex(_header.BoneIndexSize));
                    bones.Add(ReadSignedIndex(_header.BoneIndexSize));
                    float bdef2Weight = ReadSingle();
                    weights.Add(bdef2Weight);
                    weights.Add(1f - bdef2Weight);
                    break;
                case PmxVertexWeightType.Bdef4:
                    ReadFourBoneDeform(bones, weights);
                    break;
                case PmxVertexWeightType.Sdef:
                    bones.Add(ReadSignedIndex(_header.BoneIndexSize));
                    bones.Add(ReadSignedIndex(_header.BoneIndexSize));
                    float sdefWeight = ReadSingle();
                    weights.Add(sdefWeight);
                    weights.Add(1f - sdefWeight);
                    c = ReadVector3();
                    r0 = ReadVector3();
                    r1 = ReadVector3();
                    break;
                case PmxVertexWeightType.Qdef:
                    if (!_header.IsVersion21)
                    {
                        throw Error("Vertex", typeOffset, "QDEF is available only in PMX 2.1.");
                    }
                    ReadFourBoneDeform(bones, weights);
                    break;
                default:
                    throw Error("Vertex", typeOffset, $"Unknown vertex weight type {rawType}.");
            }

            return new PmxVertexDeform(rawType, bones, weights, c, r0, r1);
        }

        private void ReadFourBoneDeform(List<int> bones, List<float> weights)
        {
            for (int i = 0; i < 4; i++) bones.Add(ReadSignedIndex(_header.BoneIndexSize));
            for (int i = 0; i < 4; i++) weights.Add(ReadSingle());
        }

        private List<int> ReadSurfaces(out List<long> offsets)
        {
            SetSection("Surface");
            int count = ReadCount(_limits.MaxSurfaceIndices, "surface index", _header.VertexIndexSize);
            if (count % 3 != 0)
            {
                throw Error("Surface", _offset - 4,
                    $"Surface index count {count} is not divisible by three.");
            }

            var values = NewList<int>(count);
            offsets = NewList<long>(count);
            for (int i = 0; i < count; i++)
            {
                offsets.Add(_offset);
                values.Add(ReadVertexIndex());
            }
            return values;
        }

        private List<PmxTexture> ReadTextures()
        {
            SetSection("Texture");
            int count = ReadCount(_limits.MaxTextures, "texture", 4);
            var values = NewList<PmxTexture>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                values.Add(new PmxTexture(itemOffset, ReadText()));
            }
            return values;
        }

        private List<PmxMaterial> ReadMaterials()
        {
            SetSection("Material");
            int minimumBytes = 84 + (_header.TextureIndexSize * 2);
            int count = ReadCount(_limits.MaxMaterials, "material", minimumBytes);
            var values = NewList<PmxMaterial>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                string name = ReadText();
                string englishName = ReadText();
                PmxVector4 diffuse = ReadVector4();
                PmxVector3 specular = ReadVector3();
                float specularStrength = ReadSingle();
                PmxVector3 ambient = ReadVector3();
                byte flags = ReadByteValue();
                PmxVector4 edgeColor = ReadVector4();
                float edgeSize = ReadSingle();
                int textureIndex = ReadSignedIndex(_header.TextureIndexSize);
                int environmentTextureIndex = ReadSignedIndex(_header.TextureIndexSize);
                byte environmentMode = ReadByteValue();
                long toonReferenceOffset = _offset;
                byte toonReference = ReadByteValue();
                int toonTextureIndex;
                if (toonReference == 0)
                {
                    toonTextureIndex = ReadSignedIndex(_header.TextureIndexSize);
                }
                else if (toonReference == 1)
                {
                    toonTextureIndex = ReadByteValue();
                }
                else
                {
                    throw Error("Material", toonReferenceOffset,
                        $"Unknown toon reference flag {toonReference}; payload width cannot be determined.");
                }

                string metadata = ReadText();
                long surfaceCountOffset = _offset;
                int surfaceIndexCount = ReadInt32();
                if (surfaceIndexCount < 0 || surfaceIndexCount % 3 != 0)
                {
                    throw Error("Material", surfaceCountOffset,
                        $"Material surface index count {surfaceIndexCount} must be non-negative and divisible by three.");
                }

                values.Add(new PmxMaterial(itemOffset, name, englishName, diffuse, specular,
                    specularStrength, ambient, flags, edgeColor, edgeSize, textureIndex,
                    environmentTextureIndex, environmentMode, toonReference, toonTextureIndex,
                    metadata, surfaceIndexCount));
            }
            return values;
        }

        private List<PmxBone> ReadBones()
        {
            SetSection("Bone");
            int minimumBytes = 22 + (_header.BoneIndexSize * 2);
            int count = ReadCount(_limits.MaxBones, "bone", minimumBytes);
            var values = NewList<PmxBone>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                string name = ReadText();
                string englishName = ReadText();
                PmxVector3 position = ReadVector3();
                int parentBoneIndex = ReadSignedIndex(_header.BoneIndexSize);
                int deformLayer = ReadInt32();
                ushort rawFlags = ReadUInt16();
                var flags = (PmxBoneFlags)rawFlags;
                int? tailBoneIndex = null;
                PmxVector3? tailOffset = null;
                if ((flags & PmxBoneFlags.IndexedTail) != 0)
                    tailBoneIndex = ReadSignedIndex(_header.BoneIndexSize);
                else
                    tailOffset = ReadVector3();

                int? inheritParent = null;
                float? inheritWeight = null;
                if ((flags & (PmxBoneFlags.InheritRotation | PmxBoneFlags.InheritTranslation)) != 0)
                {
                    inheritParent = ReadSignedIndex(_header.BoneIndexSize);
                    inheritWeight = ReadSingle();
                }

                PmxVector3? fixedAxis = null;
                if ((flags & PmxBoneFlags.FixedAxis) != 0) fixedAxis = ReadVector3();

                PmxVector3? localAxisX = null;
                PmxVector3? localAxisZ = null;
                if ((flags & PmxBoneFlags.LocalCoordinates) != 0)
                {
                    localAxisX = ReadVector3();
                    localAxisZ = ReadVector3();
                }

                int? externalParentKey = null;
                if ((flags & PmxBoneFlags.ExternalParent) != 0) externalParentKey = ReadInt32();

                PmxBoneIk inverseKinematics = null;
                if ((flags & PmxBoneFlags.InverseKinematics) != 0)
                {
                    inverseKinematics = ReadBoneIk();
                }

                values.Add(new PmxBone(itemOffset, name, englishName, position, parentBoneIndex,
                    deformLayer, rawFlags, tailBoneIndex, tailOffset, inheritParent, inheritWeight,
                    fixedAxis, localAxisX, localAxisZ, externalParentKey, inverseKinematics));
            }
            return values;
        }

        private PmxBoneIk ReadBoneIk()
        {
            long sourceOffset = _offset;
            int target = ReadSignedIndex(_header.BoneIndexSize);
            long loopOffset = _offset;
            int loopCount = ReadInt32();
            if (loopCount < 0)
                throw Error("Bone", loopOffset, $"IK loop count cannot be negative, but was {loopCount}.");
            float angleLimit = ReadSingle();
            int linkCount = ReadCount(_limits.MaxIkLinks, "IK link", _header.BoneIndexSize + 1);
            var links = NewList<PmxBoneIkLink>(linkCount);
            for (int i = 0; i < linkCount; i++)
            {
                long linkOffset = _offset;
                int boneIndex = ReadSignedIndex(_header.BoneIndexSize);
                long limitOffset = _offset;
                byte limitFlag = ReadByteValue();
                PmxVector3? minimum = null;
                PmxVector3? maximum = null;
                if (limitFlag == 1)
                {
                    minimum = ReadVector3();
                    maximum = ReadVector3();
                }
                else if (limitFlag != 0)
                {
                    throw Error("Bone", limitOffset,
                        $"Unknown IK angle-limit flag {limitFlag}; payload length cannot be determined.");
                }
                links.Add(new PmxBoneIkLink(linkOffset, boneIndex, limitFlag, minimum, maximum));
            }
            return new PmxBoneIk(sourceOffset, target, loopCount, angleLimit, links);
        }

        private List<PmxMorph> ReadMorphs()
        {
            SetSection("Morph");
            int count = ReadCount(_limits.MaxMorphs, "morph", 14);
            var values = NewList<PmxMorph>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                string name = ReadText();
                string englishName = ReadText();
                byte panel = ReadByteValue();
                long typeOffset = _offset;
                byte rawType = ReadByteValue();
                if (rawType > (byte)PmxMorphType.Impulse)
                    throw Error("Morph", typeOffset, $"Unknown morph type {rawType}.");
                if (!_header.IsVersion21 && rawType >= (byte)PmxMorphType.Flip)
                    throw Error("Morph", typeOffset, $"Morph type {rawType} requires PMX 2.1.");
                int offsetCount = ReadCount(_limits.MaxMorphOffsets, "morph offset", MinimumMorphOffsetBytes(rawType));
                var offsets = NewList<PmxMorphOffset>(offsetCount);
                for (int offsetIndex = 0; offsetIndex < offsetCount; offsetIndex++)
                    offsets.Add(ReadMorphOffset((PmxMorphType)rawType));
                values.Add(new PmxMorph(itemOffset, name, englishName, panel, rawType, offsets));
            }
            return values;
        }

        private int MinimumMorphOffsetBytes(byte rawType)
        {
            switch ((PmxMorphType)rawType)
            {
                case PmxMorphType.Group:
                case PmxMorphType.Flip: return _header.MorphIndexSize + 4;
                case PmxMorphType.Vertex: return _header.VertexIndexSize + 12;
                case PmxMorphType.Bone: return _header.BoneIndexSize + 28;
                case PmxMorphType.Uv:
                case PmxMorphType.AdditionalUv1:
                case PmxMorphType.AdditionalUv2:
                case PmxMorphType.AdditionalUv3:
                case PmxMorphType.AdditionalUv4: return _header.VertexIndexSize + 16;
                case PmxMorphType.Material: return _header.MaterialIndexSize + 113;
                case PmxMorphType.Impulse: return _header.RigidBodyIndexSize + 25;
                default: return 1;
            }
        }

        private PmxMorphOffset ReadMorphOffset(PmxMorphType type)
        {
            long sourceOffset = _offset;
            switch (type)
            {
                case PmxMorphType.Group:
                    return new PmxGroupMorphOffset(sourceOffset,
                        ReadSignedIndex(_header.MorphIndexSize), ReadSingle());
                case PmxMorphType.Vertex:
                    return new PmxVertexMorphOffset(sourceOffset, ReadVertexIndex(), ReadVector3());
                case PmxMorphType.Bone:
                    return new PmxBoneMorphOffset(sourceOffset,
                        ReadSignedIndex(_header.BoneIndexSize), ReadVector3(), ReadVector4());
                case PmxMorphType.Uv:
                case PmxMorphType.AdditionalUv1:
                case PmxMorphType.AdditionalUv2:
                case PmxMorphType.AdditionalUv3:
                case PmxMorphType.AdditionalUv4:
                    return new PmxUvMorphOffset(sourceOffset, ReadVertexIndex(), ReadVector4());
                case PmxMorphType.Material:
                    return new PmxMaterialMorphOffset(sourceOffset,
                        ReadSignedIndex(_header.MaterialIndexSize), ReadByteValue(), ReadVector4(),
                        ReadVector3(), ReadSingle(), ReadVector3(), ReadVector4(), ReadSingle(),
                        ReadVector4(), ReadVector4(), ReadVector4());
                case PmxMorphType.Flip:
                    return new PmxFlipMorphOffset(sourceOffset,
                        ReadSignedIndex(_header.MorphIndexSize), ReadSingle());
                case PmxMorphType.Impulse:
                    return new PmxImpulseMorphOffset(sourceOffset,
                        ReadSignedIndex(_header.RigidBodyIndexSize), ReadByteValue(), ReadVector3(), ReadVector3());
                default:
                    throw Error("Morph", sourceOffset, $"Unsupported morph type {(byte)type}.");
            }
        }

        private List<PmxDisplayFrame> ReadDisplayFrames()
        {
            SetSection("DisplayFrame");
            int count = ReadCount(_limits.MaxDisplayFrames, "display frame", 13);
            var values = NewList<PmxDisplayFrame>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                string name = ReadText();
                string englishName = ReadText();
                byte specialFlag = ReadByteValue();
                int elementCount = ReadCount(_limits.MaxDisplayFrameElements, "display frame element", 2);
                var elements = NewList<PmxDisplayFrameElement>(elementCount);
                for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
                {
                    long elementOffset = _offset;
                    byte type = ReadByteValue();
                    int index;
                    if (type == 0) index = ReadSignedIndex(_header.BoneIndexSize);
                    else if (type == 1) index = ReadSignedIndex(_header.MorphIndexSize);
                    else throw Error("DisplayFrame", elementOffset, $"Unknown display element type {type}.");
                    elements.Add(new PmxDisplayFrameElement(elementOffset, type, index));
                }
                values.Add(new PmxDisplayFrame(itemOffset, name, englishName, specialFlag, elements));
            }
            return values;
        }

        private List<PmxRigidBody> ReadRigidBodies()
        {
            SetSection("RigidBody");
            int minimumBytes = 69 + _header.BoneIndexSize;
            int count = ReadCount(_limits.MaxRigidBodies, "rigid body", minimumBytes);
            var values = NewList<PmxRigidBody>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                values.Add(new PmxRigidBody(itemOffset, ReadText(), ReadText(),
                    ReadSignedIndex(_header.BoneIndexSize), ReadByteValue(), ReadUInt16(),
                    ReadByteValue(), ReadVector3(), ReadVector3(), ReadVector3(), ReadSingle(),
                    ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadByteValue()));
            }
            return values;
        }

        private List<PmxJoint> ReadJoints()
        {
            SetSection("Joint");
            int minimumBytes = 105 + (_header.RigidBodyIndexSize * 2);
            int count = ReadCount(_limits.MaxJoints, "joint", minimumBytes);
            var values = NewList<PmxJoint>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                string name = ReadText();
                string englishName = ReadText();
                long typeOffset = _offset;
                byte rawType = ReadByteValue();
                if (!_header.IsVersion21 && rawType != 0)
                    throw Error("Joint", typeOffset, $"Joint type {rawType} requires PMX 2.1.");
                values.Add(new PmxJoint(itemOffset, name, englishName, rawType,
                    ReadSignedIndex(_header.RigidBodyIndexSize), ReadSignedIndex(_header.RigidBodyIndexSize),
                    ReadVector3(), ReadVector3(), ReadVector3(), ReadVector3(), ReadVector3(),
                    ReadVector3(), ReadVector3(), ReadVector3()));
            }
            return values;
        }

        private List<PmxSoftBody> ReadSoftBodies()
        {
            SetSection("SoftBody");
            int minimumBytes = 133 + _header.MaterialIndexSize;
            int count = ReadCount(_limits.MaxSoftBodies, "soft body", minimumBytes);
            var values = NewList<PmxSoftBody>(count);
            for (int i = 0; i < count; i++)
            {
                long itemOffset = _offset;
                string name = ReadText();
                string englishName = ReadText();
                byte shape = ReadByteValue();
                int materialIndex = ReadSignedIndex(_header.MaterialIndexSize);
                byte collisionGroup = ReadByteValue();
                ushort nonCollisionMask = ReadUInt16();
                byte flags = ReadByteValue();
                long bLinkOffset = _offset;
                int bLinkDistance = ReadInt32();
                if (bLinkDistance < 0) throw Error("SoftBody", bLinkOffset, "B-link distance cannot be negative.");
                long clusterCountOffset = _offset;
                int clusterCount = ReadInt32();
                if (clusterCount < 0) throw Error("SoftBody", clusterCountOffset, "Cluster count cannot be negative.");
                float totalMass = ReadSingle();
                float collisionMargin = ReadSingle();
                int aerodynamicsModel = ReadInt32();
                var config = new PmxSoftBodyConfig(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                    ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                    ReadSingle(), ReadSingle());
                var cluster = new PmxSoftBodyCluster(ReadSingle(), ReadSingle(), ReadSingle(),
                    ReadSingle(), ReadSingle(), ReadSingle());
                var iteration = new PmxSoftBodyIteration(ReadNonNegativeInt32("velocity iteration"),
                    ReadNonNegativeInt32("position iteration"), ReadNonNegativeInt32("drift iteration"),
                    ReadNonNegativeInt32("cluster iteration"));
                var material = new PmxSoftBodyMaterial(ReadSingle(), ReadSingle(), ReadSingle());
                int anchorCount = ReadCount(_limits.MaxSoftBodyAnchors, "soft body anchor",
                    _header.RigidBodyIndexSize + _header.VertexIndexSize + 1);
                var anchors = NewList<PmxSoftBodyAnchor>(anchorCount);
                for (int anchorIndex = 0; anchorIndex < anchorCount; anchorIndex++)
                {
                    long anchorOffset = _offset;
                    anchors.Add(new PmxSoftBodyAnchor(anchorOffset,
                        ReadSignedIndex(_header.RigidBodyIndexSize), ReadVertexIndex(), ReadByteValue()));
                }
                int pinCount = ReadCount(_limits.MaxSoftBodyPins, "soft body pin", _header.VertexIndexSize);
                var pins = NewList<int>(pinCount);
                var pinOffsets = NewList<long>(pinCount);
                for (int pinIndex = 0; pinIndex < pinCount; pinIndex++)
                {
                    pinOffsets.Add(_offset);
                    pins.Add(ReadVertexIndex());
                }
                values.Add(new PmxSoftBody(itemOffset, name, englishName, shape, materialIndex,
                    collisionGroup, nonCollisionMask, flags, bLinkDistance, clusterCount,
                    totalMass, collisionMargin, aerodynamicsModel, config, cluster, iteration,
                    material, anchors, pins, pinOffsets));
            }
            return values;
        }

        private int ReadNonNegativeInt32(string field)
        {
            long valueOffset = _offset;
            int value = ReadInt32();
            if (value < 0) throw Error(_section, valueOffset, $"{field} cannot be negative, but was {value}.");
            return value;
        }

        private string ReadText()
        {
            long lengthOffset = _offset;
            int length = ReadInt32();
            if (length < 0)
                throw Error(_section, lengthOffset, $"String byte length cannot be negative, but was {length}.");
            if (length > _limits.MaxStringBytes)
                throw Error(_section, lengthOffset,
                    $"String byte length {length} exceeds MaxStringBytes {_limits.MaxStringBytes}.");
            if (_header.TextEncoding == PmxTextEncoding.Utf16LittleEndian && (length & 1) != 0)
                throw Error(_section, lengthOffset, $"UTF-16LE string byte length {length} must be even.");
            EnsureRemaining(length, "string payload");
            if (length == 0) return string.Empty;
            var bytes = new byte[length];
            long payloadOffset = _offset;
            ReadExact(bytes, 0, bytes.Length);
            try
            {
                return _textEncoding.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw Error(_section, payloadOffset, "String bytes are invalid for the selected encoding.", exception);
            }
        }

        private int ReadCount(int maximum, string name, int minimumItemBytes)
        {
            long countOffset = _offset;
            int count = ReadInt32();
            if (count < 0) throw Error(_section, countOffset, $"{name} count cannot be negative, but was {count}.");
            if (count > maximum)
                throw Error(_section, countOffset, $"{name} count {count} exceeds the configured limit {maximum}.");
            ReserveCollectionItems(count, name, countOffset);
            EnsureItemBytes(count, minimumItemBytes, name, countOffset);
            return count;
        }

        private void ReserveCollectionItems(int count, string name, long countOffset)
        {
            if (_totalCollectionItems > _limits.MaxTotalCollectionItems - count)
                throw Error(_section, countOffset,
                    $"Reading {count} {name} entries would exceed MaxTotalCollectionItems {_limits.MaxTotalCollectionItems}.");
            _totalCollectionItems += count;
        }

        private void EnsureItemBytes(int count, int minimumItemBytes, string name, long countOffset)
        {
            if (!_stream.CanSeek || count == 0 || minimumItemBytes <= 0) return;
            long required = (long)count * minimumItemBytes;
            long remaining = _stream.Length - _stream.Position;
            if (required > remaining)
                throw Error(_section, countOffset,
                    $"{name} count {count} requires at least {required} bytes, but only {remaining} remain.");
        }

        private void EnsureRemaining(long required, string field)
        {
            if (required < 0) throw Error(_section, _offset, $"Invalid negative byte requirement for {field}.");
            if (_stream.CanSeek && required > _stream.Length - _stream.Position)
                throw Error(_section, _offset,
                    $"Truncated {field}: {required} bytes are required, but only {_stream.Length - _stream.Position} remain.");
            long consumed = _offset - _startOffset;
            if (consumed > _limits.MaxFileBytes - required)
                throw Error(_section, _offset, $"Reading {field} would exceed MaxFileBytes {_limits.MaxFileBytes}.");
        }

        private int ReadVertexIndex()
        {
            long indexOffset = _offset;
            uint value;
            switch (_header.VertexIndexSize)
            {
                case 1: value = ReadByteValue(); break;
                case 2: value = ReadUInt16(); break;
                case 4: value = ReadUInt32(); break;
                default: throw Error(_section, indexOffset, "Invalid vertex index width in the header.");
            }
            if (value > int.MaxValue)
                throw Error(_section, indexOffset,
                    $"Unsigned vertex index {value} exceeds the supported range {int.MaxValue}.");
            return (int)value;
        }

        private int ReadSignedIndex(byte size)
        {
            long indexOffset = _offset;
            switch (size)
            {
                case 1: return unchecked((sbyte)ReadByteValue());
                case 2: return unchecked((short)ReadUInt16());
                case 4: return ReadInt32();
                default: throw Error(_section, indexOffset, $"Invalid signed index width {size}.");
            }
        }

        private PmxVector2 ReadVector2() => new PmxVector2(ReadSingle(), ReadSingle());
        private PmxVector3 ReadVector3() => new PmxVector3(ReadSingle(), ReadSingle(), ReadSingle());
        private PmxVector4 ReadVector4() => new PmxVector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        private float ReadSingle()
        {
            ReadExact(_scratch, 0, 4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(_scratch, 0, 4);
            return BitConverter.ToSingle(_scratch, 0);
        }

        private int ReadInt32() => unchecked((int)ReadUInt32());

        private uint ReadUInt32()
        {
            ReadExact(_scratch, 0, 4);
            return (uint)(_scratch[0] | (_scratch[1] << 8) | (_scratch[2] << 16) | (_scratch[3] << 24));
        }

        private ushort ReadUInt16()
        {
            byte low = ReadByteValue();
            byte high = ReadByteValue();
            return (ushort)(low | (high << 8));
        }

        private byte ReadByteValue()
        {
            EnsureRemaining(1, "byte");
            int value = _stream.ReadByte();
            if (value < 0) throw Error(_section, _offset, "Unexpected end of stream while reading one byte.");
            _offset++;
            return (byte)value;
        }

        private void ReadExact(byte[] buffer, int offset, int count)
        {
            EnsureRemaining(count, "binary value");
            int total = 0;
            while (total < count)
            {
                int read = _stream.Read(buffer, offset + total, count - total);
                if (read <= 0)
                    throw Error(_section, _offset, $"Unexpected end of stream; {count - total} bytes are missing.");
                total += read;
                _offset += read;
            }
        }

        private void EnsureEndOfStream()
        {
            SetSection("EndOfFile");
            if (_stream.CanSeek)
            {
                long remaining = _stream.Length - _stream.Position;
                if (remaining != 0)
                    throw Error("EndOfFile", _offset, $"The document has {remaining} trailing bytes.");
                return;
            }

            int trailing = _stream.ReadByte();
            if (trailing >= 0)
                throw Error("EndOfFile", _offset, "The document contains trailing bytes.");
        }

        private void ValidateIndexSize(byte size, string name, long valueOffset)
        {
            if (size != 1 && size != 2 && size != 4)
                throw Error("Header", valueOffset, $"The {name} index width must be 1, 2, or 4, but was {size}.");
        }

        private void SetSection(string section) => _section = section;

        private static List<T> NewList<T>(int count) => new List<T>(Math.Min(count, 4096));

        private static PmxFormatException Error(string section, long offset, string message, Exception inner = null)
            => new PmxFormatException(section, offset, message, inner);
    }
}
