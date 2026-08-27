using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxBinaryReaderTests
    {
        [Test]
        public void FormatAssembly_HasNoUnityReferences()
        {
            string[] unityReferences = typeof(PmxBinaryReader).Assembly.GetReferencedAssemblies()
                .Where(name => name.Name.StartsWith("Unity", StringComparison.Ordinal))
                .Select(name => name.Name)
                .ToArray();

            Assert.That(unityReferences, Is.Empty);
        }

        [TestCase((byte)0)]
        [TestCase((byte)1)]
        public void ReadDocument_SupportsBothTextEncodings(byte encoding)
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(2.1f, encoding, 1);

            PmxDocument document = Read(fixture.Bytes);

            Assert.That(document.Header.TextEncoding, Is.EqualTo((PmxTextEncoding)encoding));
            Assert.That(document.Name, Is.EqualTo("モデル"));
            Assert.That(document.Textures[0].Path, Is.EqualTo("tex/表情.png"));
        }

        [TestCase((byte)1)]
        [TestCase((byte)2)]
        [TestCase((byte)4)]
        public void ReadDocument_SupportsEveryIndexWidth(byte indexSize)
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(2.1f, 1, indexSize);

            PmxDocument document = Read(fixture.Bytes);

            Assert.That(document.Header.VertexIndexSize, Is.EqualTo(indexSize));
            Assert.That(document.Header.TextureIndexSize, Is.EqualTo(indexSize));
            Assert.That(document.Header.MaterialIndexSize, Is.EqualTo(indexSize));
            Assert.That(document.Header.BoneIndexSize, Is.EqualTo(indexSize));
            Assert.That(document.Header.MorphIndexSize, Is.EqualTo(indexSize));
            Assert.That(document.Header.RigidBodyIndexSize, Is.EqualTo(indexSize));
            Assert.That(document.Materials[0].EnvironmentTextureIndex, Is.EqualTo(-1));
        }

        [Test]
        public void ReadDocument_ReadsEverySectionAndConsumesTheStream()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(2.1f, 1, 4);
            using (var stream = new MemoryStream(fixture.Bytes))
            using (var reader = new PmxBinaryReader(stream, leaveOpen: true))
            {
                PmxDocument document = reader.ReadDocument();

                Assert.That(document.Vertices.Count, Is.EqualTo(5));
                Assert.That(document.SurfaceVertexIndices.Count, Is.EqualTo(3));
                Assert.That(document.Surfaces.Count, Is.EqualTo(1));
                Assert.That(document.Surfaces[0].VertexC, Is.EqualTo(2));
                Assert.That(document.Textures.Count, Is.EqualTo(1));
                Assert.That(document.Materials.Count, Is.EqualTo(1));
                Assert.That(document.Bones.Count, Is.EqualTo(4));
                Assert.That(document.Morphs.Count, Is.EqualTo(11));
                Assert.That(document.DisplayFrames.Count, Is.EqualTo(1));
                Assert.That(document.RigidBodies.Count, Is.EqualTo(1));
                Assert.That(document.Joints.Count, Is.EqualTo(1));
                Assert.That(document.SoftBodies.Count, Is.EqualTo(1));
                Assert.That(reader.ByteOffset, Is.EqualTo(fixture.Bytes.Length));
                Assert.That(stream.Position, Is.EqualTo(stream.Length));
            }
        }

        [Test]
        public void ReadDocument_ReadsAllVertexDeformsAndMorphTypes()
        {
            PmxDocument document = Read(PmxFixtureBuilder.Build().Bytes);

            CollectionAssert.AreEqual(
                new[] { PmxVertexWeightType.Bdef1, PmxVertexWeightType.Bdef2,
                    PmxVertexWeightType.Bdef4, PmxVertexWeightType.Sdef, PmxVertexWeightType.Qdef },
                document.Vertices.Select(vertex => vertex.Deform.Type).ToArray());
            CollectionAssert.AreEqual(
                Enumerable.Range(0, 11).Select(value => (PmxMorphType)value).ToArray(),
                document.Morphs.Select(morph => morph.Type).ToArray());
            Assert.That(document.Vertices[3].Deform.SdefC.HasValue, Is.True);
            Assert.That(document.SoftBodies[0].Anchors.Count, Is.EqualTo(1));
            Assert.That(document.SoftBodies[0].PinnedVertexIndices[0], Is.EqualTo(1));
        }

        [Test]
        public void ReadDocument_SupportsPmx20WithoutVersion21Payloads()
        {
            PmxDocument document = Read(PmxFixtureBuilder.Build(2.0f).Bytes);

            Assert.That(document.Header.Version, Is.EqualTo(2.0f));
            Assert.That(document.Vertices.Any(value => value.Deform.Type == PmxVertexWeightType.Qdef), Is.False);
            Assert.That(document.Morphs.Count, Is.EqualTo(9));
            Assert.That(document.SoftBodies, Is.Empty);
        }

        [Test]
        public void ReadDocument_PreservesUnknownFlagBits()
        {
            PmxDocument document = Read(PmxFixtureBuilder.Build().Bytes);

            Assert.That(document.Materials[0].RawFlags, Is.EqualTo(0xE1));
            Assert.That(document.Bones[0].RawFlags, Is.EqualTo(0xC001));
            Assert.That(document.SoftBodies[0].RawFlags, Is.EqualTo(0x81));
            Assert.That(document.Joints[0].RawType, Is.EqualTo(5));
        }

        [Test]
        public void ReadDocument_RejectsWrongSignatureWithSectionAndOffset()
        {
            byte[] bytes = PmxFixtureBuilder.Build().CloneBytes();
            bytes[0] = 0;

            PmxFormatException exception = AssertFormatError(bytes, "Header");

            Assert.That(exception.ByteOffset, Is.EqualTo(0));
            Assert.That(exception.Message, Does.Contain("signature"));
        }

        [Test]
        public void ReadDocument_RejectsUnsupportedVersion()
        {
            byte[] bytes = PmxFixtureBuilder.Build().CloneBytes();
            byte[] version = BitConverter.GetBytes(2.2f);
            if (!BitConverter.IsLittleEndian) Array.Reverse(version);
            Buffer.BlockCopy(version, 0, bytes, 4, 4);

            PmxFormatException exception = AssertFormatError(bytes, "Header");

            Assert.That(exception.ByteOffset, Is.EqualTo(4));
            Assert.That(exception.Message, Does.Contain("Only 2.0 and 2.1"));
        }

        [Test]
        public void ReadDocument_RejectsNegativeStringLength()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build();
            byte[] bytes = fixture.CloneBytes();
            PmxFixture.PatchInt32(bytes, fixture.ModelNameLengthOffset, -1);

            PmxFormatException exception = AssertFormatError(bytes, "ModelInfo");

            Assert.That(exception.ByteOffset, Is.EqualTo(fixture.ModelNameLengthOffset));
            Assert.That(exception.Message, Does.Contain("negative"));
        }

        [Test]
        public void ReadDocument_RejectsTruncatedStream()
        {
            byte[] source = PmxFixtureBuilder.Build().Bytes;
            var truncated = new byte[source.Length - 1];
            Buffer.BlockCopy(source, 0, truncated, 0, truncated.Length);

            PmxFormatException exception = AssertFormatError(truncated, "SoftBody");

            Assert.That(exception.ByteOffset, Is.GreaterThan(0));
            Assert.That(exception.Message, Does.Contain("remain").Or.Contain("missing").Or.Contain("end of stream"));
        }

        [Test]
        public void ReadDocument_RejectsCountAboveConfiguredLimitBeforeAllocation()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build();
            byte[] bytes = fixture.CloneBytes();
            PmxFixture.PatchInt32(bytes, fixture.VertexCountOffset, 17);
            var limits = PmxReadLimits.Default;
            limits.MaxVertices = 16;

            PmxFormatException exception = AssertFormatError(bytes, "Vertex", limits);

            Assert.That(exception.ByteOffset, Is.EqualTo(fixture.VertexCountOffset));
            Assert.That(exception.Message, Does.Contain("configured limit"));
        }

        [Test]
        public void ReadDocument_RejectsNegativeSectionCount()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build();
            byte[] bytes = fixture.CloneBytes();
            PmxFixture.PatchInt32(bytes, fixture.VertexCountOffset, -1);

            PmxFormatException exception = AssertFormatError(bytes, "Vertex");

            Assert.That(exception.ByteOffset, Is.EqualTo(fixture.VertexCountOffset));
            Assert.That(exception.Message, Does.Contain("count cannot be negative"));
        }

        [Test]
        public void ReadDocument_TreatsOneByteVertexIndexAsUnsigned()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(indexSize: 1);
            byte[] bytes = fixture.CloneBytes();
            bytes[fixture.FirstSurfaceIndexOffset] = 0xFF;

            PmxFormatException exception = AssertFormatError(bytes, "Surface");

            Assert.That(exception.ByteOffset, Is.EqualTo(fixture.FirstSurfaceIndexOffset));
            Assert.That(exception.Message, Does.Contain("255"));
            Assert.That(exception.Message, Does.Contain("unsigned vertex"));
        }

        [Test]
        public void ReadDocument_RejectsSignedIndexBelowMinusOne()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(indexSize: 1);
            byte[] bytes = fixture.CloneBytes();
            bytes[fixture.MaterialTextureIndexOffset] = 0xFE;

            PmxFormatException exception = AssertFormatError(bytes, "Material");

            Assert.That(exception.Message, Does.Contain("-2"));
            Assert.That(exception.Message, Does.Contain("[-1"));
        }

        [Test]
        public void ReadDocument_RejectsBoneParentCycle()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(indexSize: 1);
            byte[] bytes = fixture.CloneBytes();
            bytes[fixture.Bone0ParentIndexOffset] = 0;

            PmxFormatException exception = AssertFormatError(bytes, "Bone");

            Assert.That(exception.Message, Does.Contain("cycle"));
        }

        [Test]
        public void ReadDocument_RejectsGroupMorphCycle()
        {
            PmxFixture fixture = PmxFixtureBuilder.Build(indexSize: 1);
            byte[] bytes = fixture.CloneBytes();
            bytes[fixture.GroupMorphTargetOffset] = 0;

            PmxFormatException exception = AssertFormatError(bytes, "Morph");

            Assert.That(exception.Message, Does.Contain("cycle"));
        }

        [Test]
        public void ReadDocument_RejectsInvalidIndexWidthGlobal()
        {
            byte[] bytes = PmxFixtureBuilder.Build().CloneBytes();
            bytes[11] = 3;

            PmxFormatException exception = AssertFormatError(bytes, "Header");

            Assert.That(exception.ByteOffset, Is.EqualTo(11));
            Assert.That(exception.Message, Does.Contain("1, 2, or 4"));
        }

        [Test]
        public void ReadDocument_RejectsTrailingBytes()
        {
            byte[] source = PmxFixtureBuilder.Build().Bytes;
            var bytes = new byte[source.Length + 1];
            Buffer.BlockCopy(source, 0, bytes, 0, source.Length);
            bytes[bytes.Length - 1] = 0x7F;

            PmxFormatException exception = AssertFormatError(bytes, "EndOfFile");

            Assert.That(exception.ByteOffset, Is.EqualTo(source.Length));
            Assert.That(exception.Message, Does.Contain("trailing"));
        }

        private static PmxDocument Read(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new PmxBinaryReader(stream))
            {
                return reader.ReadDocument();
            }
        }

        private static PmxFormatException AssertFormatError(
            byte[] bytes, string section, PmxReadLimits limits = null)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new PmxBinaryReader(stream, limits))
            {
                PmxFormatException exception = Assert.Throws<PmxFormatException>(() => reader.ReadDocument());
                Assert.That(exception.Section, Is.EqualTo(section));
                Assert.That(exception.ByteOffset, Is.GreaterThanOrEqualTo(0));
                Assert.That(exception.Message, Does.Contain("byte offset"));
                return exception;
            }
        }
    }
}
