using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxSkinningImporterTests
    {
        private const string TestRoot = "Assets/__UnityPMXEditorStage3Tests";
        private const string ModelPath = TestRoot + "/skinned-fixture.pmx";
        private static readonly string RestImagePath = Path.Combine(
            Path.GetTempPath(), "UnityPMXEditor-Stage3-Rest.png");
        private static readonly string RotatedImagePath = Path.Combine(
            Path.GetTempPath(), "UnityPMXEditor-Stage3-Rotated.png");

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
        public void SkinningConverter_HandlesBdefMinusOneZeroAndNonNormalizedWeights()
        {
            PmxDocument document = Read(PmxStaticImportFixtureBuilder.BuildSkinnedFixture());

            SkinningConversionResult result = new SkinningConverter().Convert(
                document, PmxAdvancedDeformMode.PreserveOnly);

            AssertWeight(result.BoneWeights[0], 1, 1f);
            AssertWeight(result.BoneWeights[1], 0, 1f);
            Assert.That(result.BoneWeights[2].boneIndex0, Is.EqualTo(1));
            Assert.That(result.BoneWeights[2].weight0, Is.EqualTo(0.75f).Within(0.00001f));
            Assert.That(result.BoneWeights[2].boneIndex1, Is.EqualTo(0));
            Assert.That(result.BoneWeights[2].weight1, Is.EqualTo(0.25f).Within(0.00001f));
            AssertWeight(result.BoneWeights[3], 0, 1f);
            Assert.That(result.BoneWeights[4].boneIndex0, Is.EqualTo(1));
            Assert.That(result.BoneWeights[4].weight0, Is.EqualTo(0.75f).Within(0.00001f));
            Assert.That(result.BoneWeights[4].boneIndex1, Is.EqualTo(0));
            Assert.That(result.BoneWeights[4].weight1, Is.EqualTo(0.25f).Within(0.00001f));
            AssertWeight(result.BoneWeights[5], 0, 1f);
            Assert.That(result.FallbackVertexCount, Is.EqualTo(1));
        }

        [Test]
        public void AdvancedDeformModes_AreStrictApproximateOrPreserveOnly()
        {
            PmxDocument document = Read(PmxStaticImportFixtureBuilder.BuildAdvancedDeformFixture());
            var converter = new SkinningConverter();

            InvalidOperationException strict = Assert.Throws<InvalidOperationException>(() =>
                converter.Convert(document, PmxAdvancedDeformMode.Strict));
            Assert.That(strict.Message, Does.Contain("not exactly supported"));

            SkinningConversionResult approximate = converter.Convert(
                document, PmxAdvancedDeformMode.Approximate);
            Assert.That(approximate.AdvancedDeformVertexCount, Is.EqualTo(2));
            Assert.That(approximate.UsedApproximation, Is.True);
            Assert.That(approximate.Warning, Does.Contain("not exact"));
            Assert.That(Sum(approximate.BoneWeights[0]), Is.EqualTo(1f).Within(0.00001f));
            Assert.That(Sum(approximate.BoneWeights[1]), Is.EqualTo(1f).Within(0.00001f));

            SkinningConversionResult preserve = converter.Convert(
                document, PmxAdvancedDeformMode.PreserveOnly);
            Assert.That(preserve.AdvancedDeformVertexCount, Is.EqualTo(2));
            Assert.That(preserve.UsedApproximation, Is.False);
            Assert.That(preserve.UsesPreservationAnchor, Is.True);
            Assert.That(preserve.PreservationAnchorBoneIndex, Is.EqualTo(document.Bones.Count));
            Assert.That(Sum(preserve.BoneWeights[0]), Is.EqualTo(1f));
            Assert.That(Sum(preserve.BoneWeights[1]), Is.EqualTo(1f));
            Assert.That(preserve.BoneWeights[0].boneIndex0, Is.EqualTo(document.Bones.Count));
            Assert.That(preserve.BoneWeights[1].boneIndex0, Is.EqualTo(document.Bones.Count));
        }

        [Test]
        public void SkeletonConverter_RejectsCyclesAndVertexDocumentsWithoutBones()
        {
            PmxFormatException cycle;
            using (var stream = new MemoryStream(
                       PmxStaticImportFixtureBuilder.BuildSkinnedFixture(cyclicHierarchy: true)))
            using (var reader = new PmxBinaryReader(stream))
                cycle = Assert.Throws<PmxFormatException>(() => reader.ReadDocument());
            Assert.That(cycle.Section, Is.EqualTo("Bone"));
            Assert.That(cycle.Message, Does.Contain("cycle"));

            PmxDocument noBones = Read(PmxStaticImportFixtureBuilder.BuildNoBoneFixture());
            var root = new GameObject("Degenerate Root");
            try
            {
                Assert.Throws<InvalidOperationException>(() => new SkeletonConverter().Convert(
                    noBones, root.transform, new PmxCoordinateConverter(0.1f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ScriptedImporter_CreatesLateParentSkeletonBindposesAndStableBoneReferences()
        {
            Import(ModelPath, PmxStaticImportFixtureBuilder.BuildSkinnedFixture());

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(root.GetComponent<Animator>(), Is.Null);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.bones.Length, Is.EqualTo(2));
            Assert.That(renderer.bones[0].name, Does.StartWith("PMX Bone 000000 - "));
            Assert.That(renderer.bones[1].name, Does.StartWith("PMX Bone 000001 - "));
            Assert.That(renderer.bones[0].parent, Is.EqualTo(renderer.bones[1]));
            Assert.That(renderer.bones[0].localPosition,
                Is.EqualTo(new Vector3(0.05f, 0, 0)));
            Assert.That(renderer.bones[1].localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(renderer.rootBone, Is.EqualTo(renderer.bones[1]));
            Assert.That(renderer.sharedMesh.bindposes.Length, Is.EqualTo(2));

            for (int i = 0; i < renderer.bones.Length; i++)
            {
                Matrix4x4 restIdentity = root.transform.worldToLocalMatrix *
                                         renderer.bones[i].localToWorldMatrix *
                                         renderer.sharedMesh.bindposes[i];
                AssertMatrixApproximatelyIdentity(restIdentity);
            }

            PmxModelAsset metadata = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<PmxModelAsset>().Single();
            Assert.That(metadata.Bones.Length, Is.EqualTo(2));
            Assert.That(metadata.RootBone, Is.EqualTo(renderer.rootBone));
            CollectionAssert.AreEqual(renderer.bones, metadata.Bones);

            string[] boneIdsBefore = GetBoneLocalIds(renderer);
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            GameObject reimportedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            SkinnedMeshRenderer reimportedRenderer =
                reimportedRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            CollectionAssert.AreEqual(boneIdsBefore, GetBoneLocalIds(reimportedRenderer));
            PmxModelAsset reimportedMetadata = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<PmxModelAsset>().Single();
            CollectionAssert.AreEqual(reimportedRenderer.bones, reimportedMetadata.Bones);
        }

        [Test]
        public void Importer_PreservesAdvancedDataAndApproximateModeLogsExplicitWarning()
        {
            const string advancedPath = TestRoot + "/advanced-fixture.pmx";
            Import(advancedPath, PmxStaticImportFixtureBuilder.BuildAdvancedDeformFixture());

            PmxModelAsset preserved = AssetDatabase.LoadAllAssetsAtPath(advancedPath)
                .OfType<PmxModelAsset>().Single();
            Assert.That(preserved.AdvancedDeformMode, Is.EqualTo(PmxAdvancedDeformMode.PreserveOnly));
            Assert.That(preserved.AdvancedDeformVertexCount, Is.EqualTo(2));
            Assert.That(preserved.AdvancedDeforms.Length, Is.EqualTo(2));
            Assert.That(preserved.AdvancedDeforms[0].DeformType, Is.EqualTo(PmxVertexWeightType.Sdef));
            Assert.That(preserved.AdvancedDeforms[0].RawSdefC, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(preserved.AdvancedDeforms[1].DeformType, Is.EqualTo(PmxVertexWeightType.Qdef));
            GameObject preservedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(advancedPath);
            SkinnedMeshRenderer preservedRenderer =
                preservedRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(preserved.Bones.Length, Is.EqualTo(2));
            Assert.That(preservedRenderer.bones.Length, Is.EqualTo(3));
            Assert.That(preservedRenderer.bones[2].name, Is.EqualTo("PMX Preserved Deform Anchor"));
            Assert.That(preservedRenderer.sharedMesh.bindposes.Length, Is.EqualTo(3));
            var preservedBake = new Mesh();
            try
            {
                GameObject instance = UnityEngine.Object.Instantiate(preservedRoot);
                try
                {
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true)
                        .BakeMesh(preservedBake);
                    Vector3[] sourceVertices = preservedRenderer.sharedMesh.vertices;
                    Vector3[] bakedVertices = preservedBake.vertices;
                    Assert.That(bakedVertices.Length, Is.EqualTo(sourceVertices.Length));
                    for (int i = 0; i < sourceVertices.Length; i++)
                        Assert.That(Vector3.Distance(sourceVertices[i], bakedVertices[i]),
                            Is.LessThan(0.0001f));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preservedBake);
            }

            var importer = (PmxScriptedImporter)AssetImporter.GetAtPath(advancedPath);
            importer.Settings.AdvancedDeformMode = PmxAdvancedDeformMode.Approximate;
            EditorUtility.SetDirty(importer);
            LogAssert.Expect(LogType.Warning,
                new Regex("Approximated 2 SDEF/QDEF vertices.*not exact", RegexOptions.Singleline));
            importer.SaveAndReimport();

            PmxModelAsset approximated = AssetDatabase.LoadAllAssetsAtPath(advancedPath)
                .OfType<PmxModelAsset>().Single();
            Assert.That(approximated.AdvancedDeformMode, Is.EqualTo(PmxAdvancedDeformMode.Approximate));
            Assert.That(approximated.AdvancedDeformVertexCount, Is.EqualTo(2));
            Assert.That(approximated.AdvancedDeforms.Length, Is.EqualTo(2));
            Assert.That(approximated.AdvancedDeforms[0].RawSdefR1, Is.EqualTo(new Vector3(7, 8, 9)));
            Assert.That(approximated.Bones.Length, Is.EqualTo(2));
            GameObject approximatedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(advancedPath);
            Assert.That(approximatedRoot.GetComponentInChildren<SkinnedMeshRenderer>(true)
                .bones.Length, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RotatingImportedBone_DeformsBakedMeshAndProducesVisualEvidence()
        {
            Import(ModelPath, PmxStaticImportFixtureBuilder.BuildSkinnedFixture());
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject instance = UnityEngine.Object.Instantiate(source);
            var restMesh = new Mesh();
            var rotatedMesh = new Mesh();
            Material visualMaterial = null;
            try
            {
                SetLayerRecursively(instance, 31);
                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                renderer.updateWhenOffscreen = true;
                visualMaterial = CreateVisualMaterial(renderer.sharedMaterial);
                renderer.sharedMaterials = Enumerable.Repeat(
                    visualMaterial, renderer.sharedMesh.subMeshCount).ToArray();

                yield return null;
                renderer.BakeMesh(restMesh);
                for (int i = 0; i < renderer.sharedMesh.vertexCount; i++)
                    Assert.That(Vector3.Distance(
                        renderer.sharedMesh.vertices[i], restMesh.vertices[i]), Is.LessThan(0.0001f));
                Color32[] restPixels = Render(instance, RestImagePath);
                renderer.bones[0].localRotation = Quaternion.Euler(0, 0, 65f);
                yield return null;
                renderer.BakeMesh(rotatedMesh);
                Color32[] rotatedPixels = Render(instance, RotatedImagePath);

                Assert.That(Vector3.Distance(restMesh.vertices[0], rotatedMesh.vertices[0]),
                    Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(restMesh.vertices[1], rotatedMesh.vertices[1]),
                    Is.GreaterThan(0.01f));
                float blendedMovement = Vector3.Distance(restMesh.vertices[2], rotatedMesh.vertices[2]);
                float fullMovement = Vector3.Distance(restMesh.vertices[1], rotatedMesh.vertices[1]);
                Assert.That(blendedMovement, Is.GreaterThan(0f).And.LessThan(fullMovement));
                float bdef4ZeroMovement = Vector3.Distance(restMesh.vertices[3], rotatedMesh.vertices[3]);
                float bdef4BlendedMovement = Vector3.Distance(restMesh.vertices[4], rotatedMesh.vertices[4]);
                Assert.That(bdef4ZeroMovement, Is.EqualTo(fullMovement).Within(0.0001f));
                Assert.That(bdef4BlendedMovement, Is.GreaterThan(0f).And.LessThan(fullMovement));
                int changedPixels = restPixels.Zip(rotatedPixels,
                    (rest, rotated) => ColorDistance(rest, rotated) > 12).Count(changed => changed);
                Assert.That(changedPixels, Is.GreaterThan(100));
                Assert.That(File.Exists(RestImagePath), Is.True);
                Assert.That(File.Exists(RotatedImagePath), Is.True);
            }
            finally
            {
                if (visualMaterial != null) UnityEngine.Object.DestroyImmediate(visualMaterial);
                UnityEngine.Object.DestroyImmediate(restMesh);
                UnityEngine.Object.DestroyImmediate(rotatedMesh);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static Color32[] Render(GameObject model, string outputPath)
        {
            var cameraObject = new GameObject("Stage3 Validation Camera");
            var renderTexture = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                cameraObject.layer = 31;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.04f, 0.04f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 0.12f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.transform.position = new Vector3(0, 0, -1f);
                camera.transform.rotation = Quaternion.identity;
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Camera camera = cameraObject.GetComponent<Camera>();
                if (camera != null) camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Material CreateVisualMaterial(Material fallback)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color") ?? fallback.shader;
            var material = new Material(shader);
            Color color = new Color(0.1f, 0.9f, 0.75f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        private static void Import(string assetPath, byte[] bytes)
        {
            File.WriteAllBytes(ToAbsolutePath(assetPath), bytes);
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static PmxDocument Read(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new PmxBinaryReader(stream))
                return reader.ReadDocument();
        }

        private static void AssertWeight(BoneWeight value, int boneIndex, float weight)
        {
            Assert.That(value.boneIndex0, Is.EqualTo(boneIndex));
            Assert.That(value.weight0, Is.EqualTo(weight).Within(0.00001f));
            Assert.That(Sum(value), Is.EqualTo(1f).Within(0.00001f));
        }

        private static float Sum(BoneWeight value)
            => value.weight0 + value.weight1 + value.weight2 + value.weight3;

        private static void AssertMatrixApproximatelyIdentity(Matrix4x4 value)
        {
            Matrix4x4 identity = Matrix4x4.identity;
            for (int row = 0; row < 4; row++)
                for (int column = 0; column < 4; column++)
                    Assert.That(value[row, column],
                        Is.EqualTo(identity[row, column]).Within(0.0001f));
        }

        private static string[] GetBoneLocalIds(SkinnedMeshRenderer renderer)
        {
            return renderer.bones.Select(bone =>
            {
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    bone, out string guid, out long localId), Is.True);
                return bone.name + ":" + guid + ":" + localId;
            }).ToArray();
        }

        private static void SetLayerRecursively(GameObject value, int layer)
        {
            value.layer = layer;
            for (int i = 0; i < value.transform.childCount; i++)
                SetLayerRecursively(value.transform.GetChild(i).gameObject, layer);
        }

        private static int ColorDistance(Color32 left, Color32 right)
            => Math.Abs(left.r - right.r) + Math.Abs(left.g - right.g) +
               Math.Abs(left.b - right.b) + Math.Abs(left.a - right.a);

        private static string ToAbsolutePath(string assetPath)
            => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }
}
