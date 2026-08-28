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
    public sealed class PmxModelExporterTests
    {
        private const string TestFolder = "Assets/__UnityPMXEditorExportTests";
        private const string PmxPath = TestFolder + "/fixture.pmx";
        private const string ObjPath = TestFolder + "/fixture.obj";
        private const string MtlPath = TestFolder + "/fixture.mtl";
        private const string FbxPath = TestFolder + "/fixture.fbx";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestFolder);
            Directory.CreateDirectory(ToFullPath(TestFolder));
            File.WriteAllBytes(ToFullPath(PmxPath), PmxStaticImportFixtureBuilder.Build());
            AssetDatabase.ImportAsset(PmxPath, ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void ObjExport_WritesMaterialPartsAndRoundTripsThroughUnityImporter()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PmxPath);
            Assert.That(source, Is.Not.Null);

            PmxModelExportResult result = PmxModelExporter.Export(source,
                ToFullPath(ObjPath), PmxModelExportFormat.Obj);

            Assert.That(result.ModelPath, Is.EqualTo(ToFullPath(ObjPath)));
            Assert.That(result.MaterialPath, Is.EqualTo(ToFullPath(MtlPath)));
            Assert.That(result.VertexCount, Is.EqualTo(4));
            Assert.That(result.TriangleCount, Is.EqualTo(2));
            Assert.That(result.PartCount, Is.EqualTo(2));
            string obj = File.ReadAllText(ToFullPath(ObjPath));
            string mtl = File.ReadAllText(ToFullPath(MtlPath));
            Assert.That(Regex.Matches(obj, "^v ", RegexOptions.Multiline).Count, Is.EqualTo(4));
            Assert.That(Regex.Matches(obj, "^vt ", RegexOptions.Multiline).Count, Is.EqualTo(4));
            Assert.That(Regex.Matches(obj, "^vn ", RegexOptions.Multiline).Count, Is.EqualTo(4));
            Assert.That(Regex.Matches(obj, "^f ", RegexOptions.Multiline).Count, Is.EqualTo(2));
            Assert.That(Regex.Matches(obj, "^g part_", RegexOptions.Multiline).Count, Is.EqualTo(2));
            Assert.That(Regex.Matches(obj, "^usemtl ", RegexOptions.Multiline).Count, Is.EqualTo(2));
            Assert.That(Regex.Matches(mtl, "^newmtl ", RegexOptions.Multiline).Count, Is.EqualTo(2));
            string[] materialNames = mtl.Split(new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(value => value.StartsWith("newmtl ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(materialNames.Distinct().Count(), Is.EqualTo(2));

            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ObjPath);
            Assert.That(imported, Is.Not.Null);
            MeshFilter[] filters = imported.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters.Length, Is.GreaterThan(0));
            int importedVertexCount = filters.Sum(value => value.sharedMesh.vertexCount);
            Assert.That(importedVertexCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(filters.Sum(value => value.sharedMesh.subMeshCount), Is.EqualTo(2));
            Assert.That(filters.Sum(value => value.sharedMesh.triangles.Length / 3), Is.EqualTo(2));
            Assert.That(filters.Sum(value => value.sharedMesh.uv.Length),
                Is.EqualTo(importedVertexCount));
        }

        [Test]
        public void FbxExport_PreservesSkinnedMeshAndBoneHierarchyOnReimport()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PmxPath);
            SkinnedMeshRenderer sourceRenderer =
                source.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(sourceRenderer, Is.Not.Null);
            Assert.That(sourceRenderer.bones.Length, Is.EqualTo(1));

            PmxModelExportResult result = PmxModelExporter.Export(source,
                ToFullPath(FbxPath), PmxModelExportFormat.Fbx);

            Assert.That(result.PartCount, Is.EqualTo(2));
            Assert.That(File.Exists(ToFullPath(FbxPath)), Is.True);
            Assert.That(new FileInfo(ToFullPath(FbxPath)).Length, Is.GreaterThan(0));
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            Assert.That(imported, Is.Not.Null);
            SkinnedMeshRenderer renderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMesh.vertexCount, Is.EqualTo(4));
            Assert.That(renderer.sharedMesh.subMeshCount, Is.EqualTo(2));
            Assert.That(renderer.bones.Length, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Export_RejectsMismatchedFileExtension()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PmxPath);
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                PmxModelExporter.Export(source, ToFullPath(TestFolder + "/wrong.obj"),
                    PmxModelExportFormat.Fbx));
            Assert.That(exception.Message, Does.Contain(".fbx"));
            Assert.That(File.Exists(ToFullPath(TestFolder + "/wrong.obj")), Is.False);
        }

        [Test]
        public void ImportedPmx_ExposesPartAndBoneInspectors()
        {
            PmxModelAsset metadata = AssetDatabase.LoadAllAssetsAtPath(PmxPath)
                .OfType<PmxModelAsset>().Single();
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PmxPath);
            PmxRuntimeController controller = root.GetComponent<PmxRuntimeController>();
            Assert.That(metadata.MaterialMetadata.Length, Is.EqualTo(2));
            Assert.That(metadata.MaterialMetadata.Sum(value => value.SurfaceIndexCount), Is.EqualTo(6));
            Assert.That(metadata.BoneMetadata.Length, Is.EqualTo(1));
            Assert.That(controller.ModelAsset, Is.SameAs(metadata));
            Assert.That(controller.BoneController.Bones.Count, Is.EqualTo(1));
            Assert.That(PmxModelAssetInspector.ResolveImportedRoot(metadata), Is.SameAs(root));
            Assert.That(typeof(PmxBoneGizmoDrawer)
                .GetCustomAttributes(typeof(InitializeOnLoadAttribute), false).Length,
                Is.EqualTo(1));

            GameObject instance = UnityEngine.Object.Instantiate(root);
            var unrelated = new GameObject("Unrelated");
            try
            {
                PmxRuntimeController instanceController =
                    instance.GetComponent<PmxRuntimeController>();
                Transform bone = instanceController.BoneController.Bones[0];
                Transform meshTransform = instance
                    .GetComponentInChildren<SkinnedMeshRenderer>(true).transform;
                Assert.That(PmxModelExportMenu.FindPmxRoot(bone.gameObject), Is.SameAs(instance));
                Assert.That(PmxBoneGizmoDrawer.ShouldDraw(instanceController,
                    instance.transform), Is.True);
                Assert.That(PmxBoneGizmoDrawer.ShouldDraw(instanceController,
                    meshTransform), Is.True);
                Assert.That(PmxBoneGizmoDrawer.ShouldDraw(instanceController, bone), Is.True);
                Assert.That(PmxBoneGizmoDrawer.ShouldDraw(instanceController,
                    unrelated.transform), Is.False);
                Assert.That(PmxBoneGizmoDrawer.ResolveController(bone),
                    Is.SameAs(instanceController));

                GameObject editable = PmxModelExportMenu.InstantiateInScene(root);
                try
                {
                    Assert.That(editable.GetComponentInChildren<SkinnedMeshRenderer>(true),
                        Is.Not.Null);
                    Assert.That(editable.transform.Find("PMX Mesh"), Is.Not.Null);
                    Assert.That(editable.transform.Find("PMX Skeleton"), Is.Not.Null);
                    Assert.That(editable.GetComponentsInChildren<Transform>(true)
                        .All(value => value.gameObject.hideFlags == HideFlags.None), Is.True);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(editable);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unrelated);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            UnityEditor.Editor metadataInspector = UnityEditor.Editor.CreateEditor(metadata);
            UnityEditor.Editor controllerInspector = UnityEditor.Editor.CreateEditor(controller);
            try
            {
                Assert.That(metadataInspector, Is.TypeOf<PmxModelAssetInspector>());
                Assert.That(controllerInspector, Is.TypeOf<PmxRuntimeControllerInspector>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metadataInspector);
                UnityEngine.Object.DestroyImmediate(controllerInspector);
            }
        }

        [UnityTest]
        public IEnumerator BoneSceneInteraction_SelectsBoneAtExactGuiPosition()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PmxPath);
            GameObject instance = UnityEngine.Object.Instantiate(source);
            UnityEngine.Object previousSelection = Selection.activeObject;
            SceneView sceneView = SceneView.lastActiveSceneView ??
                                  EditorWindow.GetWindow<SceneView>();
            bool attempted = false;
            bool selected = false;
            Action<SceneView> callback = null;
            try
            {
                PmxRuntimeController controller = instance.GetComponent<PmxRuntimeController>();
                Transform targetBone = controller.BoneController.Bones[0];
                callback = current =>
                {
                    if (attempted || current != sceneView ||
                        Event.current.type != EventType.Repaint) return;
                    Vector2 position = HandleUtility.WorldToGUIPoint(targetBone.position);
                    selected = PmxBoneGizmoDrawer.TrySelectBoneAtGuiPosition(
                        controller, position);
                    attempted = true;
                };
                SceneView.duringSceneGui += callback;
                Selection.activeGameObject = instance;
                sceneView.drawGizmos = true;
                sceneView.FrameSelected(true);
                sceneView.Repaint();

                double deadline = EditorApplication.timeSinceStartup + 5d;
                while (!attempted && EditorApplication.timeSinceStartup < deadline)
                {
                    sceneView.Repaint();
                    yield return null;
                }
                Assert.That(attempted, Is.True);
                Assert.That(selected, Is.True);
                Assert.That(Selection.activeTransform, Is.EqualTo(targetBone));
                Assert.That(PmxBoneGizmoDrawer.ShouldDraw(controller, targetBone), Is.True);
            }
            finally
            {
                if (callback != null) SceneView.duringSceneGui -= callback;
                Selection.activeObject = previousSelection;
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
