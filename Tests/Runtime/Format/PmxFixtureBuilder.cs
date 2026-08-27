using System;
using System.IO;
using System.Text;

namespace Hanagumori.UnityPmx.Tests
{
    // Field order follows the PMX 2.1 format description credited in Documentation~/PmxFormat.md.
    // Fixtures are generated independently by this project and contain no third-party model data.
    internal static class PmxFixtureBuilder
    {
        public static PmxFixture Build(float version = 2.1f, byte encoding = 1, byte indexSize = 1)
        {
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(new byte[] { 0x50, 0x4D, 0x58, 0x20 });
                writer.Write(version);
                writer.Write((byte)8);
                writer.Write(encoding);
                writer.Write((byte)4);
                for (int i = 0; i < 6; i++) writer.Write(indexSize);

                var fixture = new PmxFixture();
                fixture.ModelNameLengthOffset = CheckedOffset(stream);
                WriteText(writer, "モデル", encoding);
                WriteText(writer, "Model", encoding);
                WriteText(writer, "fixture", encoding);
                WriteText(writer, "fixture-en", encoding);

                fixture.VertexCountOffset = CheckedOffset(stream);
                int vertexCount = version == 2.1f ? 5 : 4;
                writer.Write(vertexCount);
                for (int deform = 0; deform < vertexCount; deform++)
                    WriteVertex(writer, deform, indexSize);

                writer.Write(3);
                fixture.FirstSurfaceIndexOffset = CheckedOffset(stream);
                WriteVertexIndex(writer, 0, indexSize);
                WriteVertexIndex(writer, 1, indexSize);
                WriteVertexIndex(writer, 2, indexSize);

                writer.Write(1);
                WriteText(writer, "tex/表情.png", encoding);

                writer.Write(1);
                WriteText(writer, "材質", encoding);
                WriteText(writer, "Material", encoding);
                WriteVector4(writer, 1, 1, 1, 1);
                WriteVector3(writer, 0.2f, 0.3f, 0.4f);
                writer.Write(0.5f);
                WriteVector3(writer, 0.1f, 0.1f, 0.1f);
                writer.Write((byte)0xE1);
                WriteVector4(writer, 0, 0, 0, 1);
                writer.Write(1f);
                fixture.MaterialTextureIndexOffset = CheckedOffset(stream);
                WriteSignedIndex(writer, 0, indexSize);
                WriteSignedIndex(writer, -1, indexSize);
                writer.Write((byte)3);
                writer.Write((byte)1);
                writer.Write((byte)0);
                WriteText(writer, "meta", encoding);
                writer.Write(3);

                writer.Write(4);
                WriteBone0(writer, encoding, indexSize, fixture);
                WriteBone1(writer, encoding, indexSize);
                WriteBone2(writer, encoding, indexSize);
                WriteBone3(writer, encoding, indexSize);

                int morphCount = version == 2.1f ? 11 : 9;
                writer.Write(morphCount);
                for (byte type = 0; type < morphCount; type++)
                    WriteMorph(writer, type, encoding, indexSize, fixture);

                writer.Write(1);
                WriteText(writer, "表示", encoding);
                WriteText(writer, "Display", encoding);
                writer.Write((byte)1);
                writer.Write(2);
                writer.Write((byte)0);
                WriteSignedIndex(writer, 0, indexSize);
                writer.Write((byte)1);
                WriteSignedIndex(writer, 1, indexSize);

                writer.Write(1);
                WriteText(writer, "剛体", encoding);
                WriteText(writer, "RigidBody", encoding);
                WriteSignedIndex(writer, 0, indexSize);
                writer.Write((byte)1);
                writer.Write((ushort)0xFFFE);
                writer.Write((byte)0);
                WriteVector3(writer, 1, 1, 1);
                WriteVector3(writer, 0, 0, 0);
                WriteVector3(writer, 0, 0, 0);
                writer.Write(1f);
                writer.Write(0.5f);
                writer.Write(0.5f);
                writer.Write(0.1f);
                writer.Write(0.2f);
                writer.Write((byte)1);

                writer.Write(1);
                WriteText(writer, "ジョイント", encoding);
                WriteText(writer, "Joint", encoding);
                writer.Write(version == 2.1f ? (byte)5 : (byte)0);
                WriteSignedIndex(writer, 0, indexSize);
                WriteSignedIndex(writer, -1, indexSize);
                for (int i = 0; i < 8; i++) WriteVector3(writer, i, i + 1, i + 2);

                if (version == 2.1f)
                {
                    writer.Write(1);
                    WriteSoftBody(writer, encoding, indexSize);
                }

                writer.Flush();
                fixture.Bytes = stream.ToArray();
                return fixture;
            }
        }

        private static void WriteVertex(BinaryWriter writer, int deform, byte indexSize)
        {
            WriteVector3(writer, deform, deform + 1, deform + 2);
            WriteVector3(writer, 0, 1, 0);
            WriteVector2(writer, 0.25f, 0.75f);
            for (int i = 0; i < 4; i++) WriteVector4(writer, i, i, i, i);
            writer.Write((byte)deform);
            switch (deform)
            {
                case 0:
                    WriteSignedIndex(writer, 0, indexSize);
                    break;
                case 1:
                    WriteSignedIndex(writer, 0, indexSize);
                    WriteSignedIndex(writer, 1, indexSize);
                    writer.Write(0.25f);
                    break;
                case 2:
                case 4:
                    for (int i = 0; i < 4; i++) WriteSignedIndex(writer, i, indexSize);
                    for (int i = 0; i < 4; i++) writer.Write(0.25f);
                    break;
                case 3:
                    WriteSignedIndex(writer, 0, indexSize);
                    WriteSignedIndex(writer, 1, indexSize);
                    writer.Write(0.5f);
                    WriteVector3(writer, 1, 2, 3);
                    WriteVector3(writer, 4, 5, 6);
                    WriteVector3(writer, 7, 8, 9);
                    break;
            }
            writer.Write(1f);
        }

        private static void WriteBone0(BinaryWriter writer, byte encoding, byte indexSize, PmxFixture fixture)
        {
            WriteText(writer, "bone0", encoding);
            WriteText(writer, "bone0", encoding);
            WriteVector3(writer, 0, 0, 0);
            fixture.Bone0ParentIndexOffset = CheckedOffset(writer.BaseStream);
            WriteSignedIndex(writer, -1, indexSize);
            writer.Write(0);
            writer.Write((ushort)0xC001);
            WriteSignedIndex(writer, 1, indexSize);
        }

        private static void WriteBone1(BinaryWriter writer, byte encoding, byte indexSize)
        {
            WriteText(writer, "bone1", encoding);
            WriteText(writer, "bone1", encoding);
            WriteVector3(writer, 0, 1, 0);
            WriteSignedIndex(writer, 0, indexSize);
            writer.Write(1);
            writer.Write((ushort)0x0100);
            WriteVector3(writer, 0, 1, 0);
            WriteSignedIndex(writer, 0, indexSize);
            writer.Write(0.5f);
        }

        private static void WriteBone2(BinaryWriter writer, byte encoding, byte indexSize)
        {
            WriteText(writer, "bone2", encoding);
            WriteText(writer, "bone2", encoding);
            WriteVector3(writer, 0, 2, 0);
            WriteSignedIndex(writer, 1, indexSize);
            writer.Write(2);
            writer.Write((ushort)0x2C20);
            WriteVector3(writer, 0, 1, 0);
            WriteVector3(writer, 1, 0, 0);
            WriteVector3(writer, 1, 0, 0);
            WriteVector3(writer, 0, 0, 1);
            writer.Write(42);
            WriteSignedIndex(writer, 1, indexSize);
            writer.Write(2);
            writer.Write(0.5f);
            writer.Write(1);
            WriteSignedIndex(writer, 0, indexSize);
            writer.Write((byte)1);
            WriteVector3(writer, -1, -1, -1);
            WriteVector3(writer, 1, 1, 1);
        }

        private static void WriteBone3(BinaryWriter writer, byte encoding, byte indexSize)
        {
            WriteText(writer, "bone3", encoding);
            WriteText(writer, "bone3", encoding);
            WriteVector3(writer, 0, 3, 0);
            WriteSignedIndex(writer, 2, indexSize);
            writer.Write(3);
            writer.Write((ushort)0);
            WriteVector3(writer, 0, 1, 0);
        }

        private static void WriteMorph(BinaryWriter writer, byte type, byte encoding, byte indexSize, PmxFixture fixture)
        {
            WriteText(writer, "morph" + type, encoding);
            WriteText(writer, "morph" + type, encoding);
            writer.Write((byte)1);
            writer.Write(type);
            writer.Write(1);
            switch (type)
            {
                case 0:
                    fixture.GroupMorphTargetOffset = CheckedOffset(writer.BaseStream);
                    WriteSignedIndex(writer, 1, indexSize);
                    writer.Write(0.5f);
                    break;
                case 1:
                    WriteVertexIndex(writer, 0, indexSize);
                    WriteVector3(writer, 1, 2, 3);
                    break;
                case 2:
                    WriteSignedIndex(writer, 0, indexSize);
                    WriteVector3(writer, 1, 2, 3);
                    WriteVector4(writer, 0, 0, 0, 1);
                    break;
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                    WriteVertexIndex(writer, 0, indexSize);
                    WriteVector4(writer, 1, 2, 3, 4);
                    break;
                case 8:
                    WriteSignedIndex(writer, -1, indexSize);
                    writer.Write((byte)1);
                    WriteVector4(writer, 1, 1, 1, 1);
                    WriteVector3(writer, 1, 1, 1);
                    writer.Write(1f);
                    WriteVector3(writer, 1, 1, 1);
                    WriteVector4(writer, 1, 1, 1, 1);
                    writer.Write(1f);
                    WriteVector4(writer, 1, 1, 1, 1);
                    WriteVector4(writer, 1, 1, 1, 1);
                    WriteVector4(writer, 1, 1, 1, 1);
                    break;
                case 9:
                    WriteSignedIndex(writer, 1, indexSize);
                    writer.Write(0.5f);
                    break;
                case 10:
                    WriteSignedIndex(writer, 0, indexSize);
                    writer.Write((byte)1);
                    WriteVector3(writer, 1, 2, 3);
                    WriteVector3(writer, 4, 5, 6);
                    break;
            }
        }

        private static void WriteSoftBody(BinaryWriter writer, byte encoding, byte indexSize)
        {
            WriteText(writer, "ソフト", encoding);
            WriteText(writer, "SoftBody", encoding);
            writer.Write((byte)0);
            WriteSignedIndex(writer, 0, indexSize);
            writer.Write((byte)2);
            writer.Write((ushort)0xFFFC);
            writer.Write((byte)0x81);
            writer.Write(1);
            writer.Write(1);
            writer.Write(2f);
            writer.Write(0.1f);
            writer.Write(4);
            for (int i = 0; i < 12; i++) writer.Write(i + 0.1f);
            for (int i = 0; i < 6; i++) writer.Write(i + 0.2f);
            writer.Write(1);
            writer.Write(2);
            writer.Write(3);
            writer.Write(4);
            writer.Write(0.5f);
            writer.Write(0.6f);
            writer.Write(0.7f);
            writer.Write(1);
            WriteSignedIndex(writer, 0, indexSize);
            WriteVertexIndex(writer, 0, indexSize);
            writer.Write((byte)1);
            writer.Write(1);
            WriteVertexIndex(writer, 1, indexSize);
        }

        private static void WriteText(BinaryWriter writer, string value, byte encoding)
        {
            Encoding textEncoding = encoding == 0
                ? new UnicodeEncoding(false, false, true)
                : new UTF8Encoding(false, true);
            byte[] bytes = textEncoding.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteSignedIndex(BinaryWriter writer, int value, byte size)
        {
            switch (size)
            {
                case 1: writer.Write(unchecked((sbyte)value)); break;
                case 2: writer.Write(unchecked((short)value)); break;
                case 4: writer.Write(value); break;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        private static void WriteVertexIndex(BinaryWriter writer, uint value, byte size)
        {
            switch (size)
            {
                case 1: writer.Write(unchecked((byte)value)); break;
                case 2: writer.Write(unchecked((ushort)value)); break;
                case 4: writer.Write(value); break;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        private static void WriteVector2(BinaryWriter writer, float x, float y)
        { writer.Write(x); writer.Write(y); }
        private static void WriteVector3(BinaryWriter writer, float x, float y, float z)
        { writer.Write(x); writer.Write(y); writer.Write(z); }
        private static void WriteVector4(BinaryWriter writer, float x, float y, float z, float w)
        { writer.Write(x); writer.Write(y); writer.Write(z); writer.Write(w); }
        private static int CheckedOffset(Stream stream) => checked((int)stream.Position);
    }

    internal sealed class PmxFixture
    {
        public byte[] Bytes;
        public int ModelNameLengthOffset;
        public int VertexCountOffset;
        public int FirstSurfaceIndexOffset;
        public int MaterialTextureIndexOffset;
        public int Bone0ParentIndexOffset;
        public int GroupMorphTargetOffset;

        public byte[] CloneBytes() => (byte[])Bytes.Clone();

        public static void PatchInt32(byte[] bytes, int offset, int value)
        {
            byte[] encoded = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) Array.Reverse(encoded);
            Buffer.BlockCopy(encoded, 0, bytes, offset, 4);
        }
    }
}
