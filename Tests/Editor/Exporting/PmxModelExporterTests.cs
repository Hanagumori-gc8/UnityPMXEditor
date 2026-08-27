using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
            SkinnedMeshRenderer sourceRenderer = source.GetComponent<SkinnedMeshRenderer>();
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
            MethodInfo gizmoMethod = typeof(PmxBoneGizmoDrawer).GetMethod("DrawBones",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(gizmoMethod, Is.Not.Null);
            Assert.That(gizmoMethod.GetCustomAttributes(typeof(DrawGizmo), false).Length,
                Is.EqualTo(1));

            GameObject instance = UnityEngine.Object.Instantiate(root);
            try
            {
                Transform bone = instance.GetComponent<PmxRuntimeController>()
                    .BoneController.Bones[0];
                Assert.That(PmxModelExportMenu.FindPmxRoot(bone.gameObject), Is.SameAs(instance));
            }
            finally
            {
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

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
