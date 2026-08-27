using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxScriptedImporterTests
    {
        private const string TestRoot = "Assets/__UnityPMXEditorStage2Tests";
        private const string TexturePath = TestRoot + "/texture.png";
        private const string ModelPath = TestRoot + "/static-fixture.pmx";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TestRoot)) AssetDatabase.DeleteAsset(TestRoot);
            Directory.CreateDirectory(ToAbsolutePath(TestRoot));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TestRoot)) AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void CoordinateConverter_OwnsPositionNormalUvWindingAndScale()
        {
            var converter = new PmxCoordinateConverter(0.1f);
            Vector3 position = converter.ConvertPosition(new PmxVector3(1, 2, 3));
            Vector3 normal = converter.ConvertNormal(new PmxVector3(0, 0, 2));
            Vector2 uv = converter.ConvertUv(new PmxVector2(0.25f, 0.75f));
            var triangle = new int[3];
            converter.ConvertTriangle(1, 2, 3, triangle, 0);

            Assert.That(position, Is.EqualTo(new Vector3(0.1f, 0.2f, -0.3f)));
            Assert.That(normal, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(uv, Is.EqualTo(new Vector2(0.25f, 0.25f)));
            Assert.That(converter.ConvertUvDelta(new Vector4(0.1f, 0.2f, 0.3f, 0.4f)),
                Is.EqualTo(new Vector2(0.1f, -0.2f)));
            CollectionAssert.AreEqual(new[] { 1, 3, 2 }, triangle);
            Assert.That(converter.ConvertScale(2f), Is.EqualTo(0.2f));
        }

        [Test]
        public void Validator_NormalizesPortableProjectTexturePath()
        {
            var validator = new PmxValidator();

            string path = validator.NormalizeTextureAssetPath(
                "Assets/Models/model.pmx", "tex\\body.png");

            Assert.That(path, Is.EqualTo("Assets/Models/tex/body.png"));
            Assert.That(validator.NormalizeTextureAssetPath(
                "Packages/com.example.models/model.pmx", "tex/body.png"),
                Is.EqualTo("Packages/com.example.models/tex/body.png"));
        }

        [TestCase("../outside.png")]
        [TestCase("C:/textures/body.png")]
        [TestCase("/textures/body.png")]
        [TestCase("https://example.invalid/body.png")]
        public void Validator_RejectsTraversalAndNonPortableAbsolutePaths(string texturePath)
        {
            var validator = new PmxValidator();

            Assert.Throws<PmxImportValidationException>(() =>
                validator.NormalizeTextureAssetPath("Assets/Models/model.pmx", texturePath));
        }

        [Test]
        public void ScriptedImporter_ImportsStaticFixtureWithStableAlignedAssetsAndTextureDependency()
        {
            WriteTexture(TexturePath, Color.red, 2);
            AssetDatabase.ImportAsset(TexturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            File.WriteAllBytes(ToAbsolutePath(ModelPath), PmxStaticImportFixtureBuilder.Build());
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Assert.That(root, Is.Not.Null);
            SkinnedMeshRenderer renderer = root.GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = renderer.sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16));
            Assert.That(mesh.subMeshCount, Is.EqualTo(2));
            Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { 0, 2, 1 }, mesh.GetTriangles(0));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, mesh.GetTriangles(1));
            Assert.That(mesh.vertices[1], Is.EqualTo(new Vector3(0.1f, 0, 0)));
            Assert.That(mesh.normals[0], Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(mesh.uv[0], Is.EqualTo(new Vector2(0, 1)));
            Assert.That(mesh.uv[1], Is.EqualTo(new Vector2(1, 1)));
            Assert.That(mesh.uv[2], Is.EqualTo(new Vector2(0, 0)));

            int[] firstTriangle = mesh.GetTriangles(0);
            Vector3 geometricNormal = Vector3.Cross(
                mesh.vertices[firstTriangle[1]] - mesh.vertices[firstTriangle[0]],
                mesh.vertices[firstTriangle[2]] - mesh.vertices[firstTriangle[0]]).normalized;
            Assert.That(Vector3.Dot(geometricNormal, mesh.normals[firstTriangle[0]]), Is.GreaterThan(0.999f));

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            Assert.That(renderer.sharedMaterials[0].mainTexture, Is.EqualTo(texture));
            Assert.That(renderer.sharedMaterials[0].name, Does.Contain("Approximation"));

            PmxModelAsset metadata = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<PmxModelAsset>().Single();
            Assert.That(metadata.Mesh, Is.EqualTo(mesh));
            Assert.That(metadata.Materials.Length, Is.EqualTo(2));
            Assert.That(metadata.TextureAssetPaths[0], Is.EqualTo(TexturePath));
            Assert.That(metadata.UsesApproximateMaterials, Is.True);

            GameObject instance = UnityEngine.Object.Instantiate(root);
            try
            {
                Assert.That(instance.GetComponent<SkinnedMeshRenderer>().sharedMesh, Is.EqualTo(mesh));
                Assert.That(instance.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            string[] localIdsBefore = GetStableSubAssetLocalIds(ModelPath);
            Assert.That(localIdsBefore.Length, Is.EqualTo(4));
            Assert.That(localIdsBefore.Distinct().Count(), Is.EqualTo(4));
            string[] dependencies = AssetDatabase.GetDependencies(ModelPath, true);
            CollectionAssert.Contains(dependencies, TexturePath);
            Hash128 dependencyHashBefore = AssetDatabase.GetAssetDependencyHash(ModelPath);

            WriteTexture(TexturePath, Color.green, 4);
            AssetDatabase.ImportAsset(TexturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Hash128 dependencyHashAfter = AssetDatabase.GetAssetDependencyHash(ModelPath);
            Assert.That(dependencyHashAfter, Is.Not.EqualTo(dependencyHashBefore));
            CollectionAssert.AreEqual(localIdsBefore, GetStableSubAssetLocalIds(ModelPath));
        }

        [Test]
        public void ScriptedImporter_UsesUInt32IndicesAbove65535Vertices()
        {
            const string largePath = TestRoot + "/large-fixture.pmx";
            File.WriteAllBytes(ToAbsolutePath(largePath),
                PmxStaticImportFixtureBuilder.BuildLargeVertexFixture(65536));
            AssetDatabase.ImportAsset(largePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(largePath);
            Assert.That(root, Is.Not.Null);
            Mesh mesh = root.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(65536));
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt32));
        }

        private static string[] GetStableSubAssetLocalIds(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .Where(asset => asset is Mesh || asset is Material || asset is PmxModelAsset)
                .Select(asset =>
                {
                    Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        asset, out string guid, out long localId), Is.True);
                    return asset.GetType().Name + ":" + guid + ":" + localId;
                })
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void WriteTexture(string assetPath, Color color, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                var pixels = Enumerable.Repeat(color, size * size).ToArray();
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(ToAbsolutePath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ToAbsolutePath(string assetPath)
            => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }
}
