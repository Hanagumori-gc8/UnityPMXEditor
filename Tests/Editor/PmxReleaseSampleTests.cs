using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxReleaseSampleTests
    {
        private const string FixtureSha256 =
            "F802533FBBC698579B118E4C2CB78B4CA5AEA67F08D2A39C4E58B3BB1CD4C817";

        [Test]
        public void MinimalPackageFixture_IsSelfContainedValidPmx20AndFullyConsumed()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(PmxScriptedImporter).Assembly);
            Assert.That(package, Is.Not.Null);
            string path = Path.Combine(package.resolvedPath, "Samples~", "MinimalPmxFixture",
                "MinimalFixture.pmx");
            byte[] bytes = File.ReadAllBytes(path);
            using (var sha256 = SHA256.Create())
                Assert.That(BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty),
                    Is.EqualTo(FixtureSha256));

            using (var stream = new MemoryStream(bytes))
            using (var reader = new PmxBinaryReader(stream, leaveOpen: true))
            {
                PmxDocument document = reader.ReadDocument();
                Assert.That(document.Header.Version, Is.EqualTo(2.0f));
                Assert.That(document.Header.TextEncoding, Is.EqualTo(PmxTextEncoding.Utf8));
                Assert.That(document.Name, Is.EqualTo("UnityPMXEditor Minimal Fixture"));
                Assert.That(document.Vertices.Count, Is.EqualTo(3));
                Assert.That(document.SurfaceVertexIndices.Count, Is.EqualTo(3));
                Assert.That(document.Textures, Is.Empty);
                Assert.That(document.Materials.Count, Is.EqualTo(1));
                Assert.That(document.Materials[0].SurfaceIndexCount, Is.EqualTo(3));
                Assert.That(document.Bones.Count, Is.EqualTo(1));
                Assert.That(document.Morphs, Is.Empty);
                Assert.That(document.DisplayFrames, Is.Empty);
                Assert.That(document.RigidBodies, Is.Empty);
                Assert.That(document.Joints, Is.Empty);
                Assert.That(reader.ByteOffset, Is.EqualTo(bytes.Length));
                Assert.That(stream.Position, Is.EqualTo(stream.Length));
            }
        }
    }
}
