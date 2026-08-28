using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxBlendShapeMetadataTests
    {
        private const string TestRoot = "Assets/__UnityPMXEditorStage4Tests";
        private const string ModelPath = TestRoot + "/morph-metadata-fixture.pmx";

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
        public void Importer_CreatesStableSparseEmptyAndAccumulatedVertexBlendShapes()
        {
            Import(PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh mesh = renderer.sharedMesh;

            Assert.That(mesh.blendShapeCount, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[]
            {
                "PMX Vertex Morph 000000",
                "PMX Vertex Morph 000001",
                "PMX Vertex Morph 000002"
            }, Enumerable.Range(0, mesh.blendShapeCount)
                .Select(mesh.GetBlendShapeName).ToArray());

            Vector3[] sparse = ReadDeltaVertices(mesh, 0);
            Assert.That(sparse.Length, Is.EqualTo(mesh.vertexCount));
            Assert.That(sparse[0], Is.EqualTo(Vector3.zero));
            Assert.That(sparse[1], Is.EqualTo(new Vector3(0.1f, 0, 0)));
            Assert.That(sparse[2], Is.EqualTo(Vector3.zero));
            Assert.That(sparse[3], Is.EqualTo(new Vector3(0, 0.1f, 0)));

            Vector3[] empty = ReadDeltaVertices(mesh, 1);
            Assert.That(empty.All(value => value == Vector3.zero), Is.True);

            Vector3[] duplicate = ReadDeltaVertices(mesh, 2);
            Assert.That(duplicate[2], Is.EqualTo(new Vector3(0.1f, 0, 0)));

            GameObject instance = UnityEngine.Object.Instantiate(root);
            var baked = new Mesh();
            try
            {
                SkinnedMeshRenderer instanceRenderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                instanceRenderer.SetBlendShapeWeight(0, 100f);
                instanceRenderer.BakeMesh(baked);
                Assert.That(Vector3.Distance(
                    baked.vertices[1] - mesh.vertices[1], new Vector3(0.1f, 0, 0)),
                    Is.LessThan(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            string[] namesBefore = Enumerable.Range(0, mesh.blendShapeCount)
                .Select(mesh.GetBlendShapeName).ToArray();
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Mesh reimported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)
                .GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
            CollectionAssert.AreEqual(namesBefore, Enumerable.Range(0, reimported.blendShapeCount)
                .Select(reimported.GetBlendShapeName).ToArray());
        }

        [Test]
        public void ModelAsset_VersionAndAllRequestedMetadataArePreserved()
        {
            Import(PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            PmxModelAsset asset = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<PmxModelAsset>().Single();

            Assert.That(asset.SchemaVersion, Is.EqualTo(PmxModelAsset.CurrentSchemaVersion));
            Assert.That(asset.SchemaVersion, Is.EqualTo(5));
            Assert.That(asset.PmxVersion, Is.EqualTo(2.1f));
            Assert.That(asset.Header.TextEncoding, Is.EqualTo(PmxTextEncoding.Utf8));
            Assert.That(asset.ModelName, Is.EqualTo("メタデータモデル"));
            Assert.That(asset.EnglishModelName, Is.EqualTo("Metadata Model"));
            Assert.That(asset.Comment, Is.EqualTo("Original model comment"));
            Assert.That(asset.EnglishComment, Is.EqualTo("Original English comment"));

            Assert.That(asset.BoneMetadata.Length, Is.EqualTo(4));
            Assert.That(asset.BoneMetadata[0].Layer, Is.EqualTo(7));
            Assert.That(asset.BoneMetadata[0].RawFlags, Is.EqualTo(0x2C21));
            Assert.That(asset.BoneMetadata[0].InverseKinematics, Is.Not.Null);
            Assert.That(asset.BoneMetadata[0].InverseKinematics.TargetBoneIndex, Is.EqualTo(3));
            Assert.That(asset.BoneMetadata[0].InverseKinematics.Links.Length, Is.EqualTo(2));
            Assert.That(asset.BoneMetadata[0].InverseKinematics.Links[0].HasLimits, Is.True);
            Assert.That(asset.BoneMetadata[2].HasInheritParent, Is.True);
            Assert.That(asset.BoneMetadata[2].InheritParentBoneIndex, Is.EqualTo(1));
            Assert.That(asset.BoneMetadata[2].InheritWeight, Is.EqualTo(0.5f));

            Assert.That(asset.MaterialMetadata.Length, Is.EqualTo(1));
            Assert.That(asset.MaterialMetadata[0].RawFlags, Is.EqualTo(0xA5));
            Assert.That(asset.MaterialMetadata[0].Metadata, Is.EqualTo("material semantic memo"));
            Assert.That(asset.MaterialMetadata[0].SupportStatus,
                Is.EqualTo(PmxFeatureSupportStatus.Approximated));

            Assert.That(asset.MorphMetadata.Length, Is.EqualTo(9));
            Assert.That(asset.MorphMetadata[0].SupportStatus,
                Is.EqualTo(PmxFeatureSupportStatus.Supported));
            Assert.That(asset.MorphMetadata[0].BlendShapeIndex, Is.EqualTo(0));
            Assert.That(asset.MorphMetadata[0].Offsets.Length, Is.EqualTo(2));
            Assert.That(asset.MorphMetadata[1].Offsets, Is.Empty);
            Assert.That(asset.MorphMetadata[2].Name, Is.EqualTo(asset.MorphMetadata[0].Name));
            Assert.That(asset.MorphMetadata[3].SupportStatus,
                Is.EqualTo(PmxFeatureSupportStatus.Approximated));
            Assert.That(asset.MorphMetadata[3].Offsets[0].MorphIndex, Is.EqualTo(0));
            Assert.That(asset.MorphMetadata[4].Offsets[0].BoneIndex, Is.EqualTo(1));
            Assert.That(asset.MorphMetadata[5].Offsets[0].UvDelta,
                Is.EqualTo(new Vector4(0.1f, 0.2f, 0.3f, 0.4f)));
            Assert.That(asset.MorphMetadata[6].Offsets[0].MaterialIndex, Is.EqualTo(-1));
            Assert.That(asset.MorphMetadata[7].Offsets[0].Weight, Is.EqualTo(0.75f));
            Assert.That(asset.MorphMetadata[8].Offsets[0].RawLocalFlag, Is.EqualTo(1));
            Assert.That(asset.MorphMetadata[8].SupportStatus,
                Is.EqualTo(PmxFeatureSupportStatus.Preserved));

            Assert.That(asset.DisplayFrameMetadata.Length, Is.EqualTo(1));
            Assert.That(asset.DisplayFrameMetadata[0].Elements.Length, Is.EqualTo(2));
            Assert.That(asset.RigidBodyMetadata.Length, Is.EqualTo(1));
            Assert.That(asset.RigidBodyMetadata[0].RawPhysicsMode, Is.EqualTo(2));
            Assert.That(asset.JointMetadata.Length, Is.EqualTo(1));
            Assert.That(asset.JointMetadata[0].RawType, Is.EqualTo(5));
            Assert.That(asset.SoftBodyMetadata.Length, Is.EqualTo(1));
            Assert.That(asset.SoftBodyMetadata[0].RawFlags, Is.EqualTo(0x85));
            Assert.That(asset.SoftBodyMetadata[0].Config.Length, Is.EqualTo(12));
            Assert.That(asset.SoftBodyMetadata[0].Anchors.Length, Is.EqualTo(1));
            Assert.That(asset.SoftBodyMetadata[0].PinnedVertexIndices[0], Is.EqualTo(1));

            Assert.That(asset.Diagnostics.Any(value =>
                value.Status == PmxFeatureSupportStatus.Supported), Is.True);
            Assert.That(asset.Diagnostics.Any(value =>
                value.Status == PmxFeatureSupportStatus.Approximated), Is.True);
            Assert.That(asset.Diagnostics.Any(value =>
                value.Status == PmxFeatureSupportStatus.Preserved), Is.True);
            Assert.That(Enum.GetValues(typeof(PmxFeatureSupportStatus)).Length, Is.EqualTo(5));

            Component[] runtimeComponents = root.GetComponentsInChildren<Component>(true);
            Assert.That(runtimeComponents.All(value =>
                value is Transform || value is SkinnedMeshRenderer ||
                value is PmxMorphController || value is PmxBoneController ||
                value is PmxRuntimeController), Is.True);

            UnityEditor.Editor inspector = UnityEditor.Editor.CreateEditor(asset);
            try
            {
                Assert.That(inspector, Is.TypeOf<PmxModelAssetInspector>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inspector);
            }
        }

        [Test]
        public void InvalidVertexMorphIndex_IsRejectedWithMorphSectionAndOffset()
        {
            byte[] bytes = PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture(
                invalidVertexMorphIndex: true);
            using (var stream = new MemoryStream(bytes))
            using (var reader = new PmxBinaryReader(stream))
            {
                PmxFormatException exception = Assert.Throws<PmxFormatException>(() =>
                    reader.ReadDocument());
                Assert.That(exception.Section, Is.EqualTo("Morph"));
                Assert.That(exception.ByteOffset, Is.GreaterThan(0));
                Assert.That(exception.Message, Does.Contain("vertex target 255"));
            }
        }

        private static Vector3[] ReadDeltaVertices(Mesh mesh, int blendShapeIndex)
        {
            var vertices = new Vector3[mesh.vertexCount];
            var normals = new Vector3[mesh.vertexCount];
            var tangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(blendShapeIndex, 0, vertices, normals, tangents);
            return vertices;
        }

        private static void Import(byte[] bytes)
        {
            File.WriteAllBytes(ToAbsolutePath(ModelPath), bytes);
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static string ToAbsolutePath(string assetPath)
            => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }
}
