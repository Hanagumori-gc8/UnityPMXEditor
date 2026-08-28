using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxRuntimeControllerTests
    {
        private const string TestRoot = "Assets/__UnityPMXEditorStage5Tests";
        private const string ModelPath = TestRoot + "/runtime-fixture.pmx";
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
        public void MultipleMorphsComposeAndResetRestoresEvaluatedBaseline()
        {
            GameObject instance = ImportAndInstantiate(
                PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            try
            {
                PmxRuntimeController runtime = instance.GetComponent<PmxRuntimeController>();
                PmxMorphController morph = runtime.MorphController;
                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true);

                float ikDistanceBefore = Vector3.Distance(
                    renderer.bones[3].position, renderer.bones[0].position);
                runtime.EvaluateFrame();
                float ikDistanceAfter = Vector3.Distance(
                    renderer.bones[3].position, renderer.bones[0].position);
                Assert.That(ikDistanceAfter, Is.LessThan(ikDistanceBefore));
                Vector2 baselineUv = renderer.sharedMesh.uv[0];
                Color baselineColor = ReadMaterialColor(renderer, 0);
                Vector3[] baselinePositions = renderer.bones.Select(value => value.localPosition).ToArray();
                Quaternion[] baselineRotations = renderer.bones.Select(value => value.localRotation).ToArray();

                morph.SetMorphWeight(0, 0.25f);
                morph.SetMorphWeight(3, 0.5f);
                morph.SetMorphWeight(7, 0.5f);
                morph.SetMorphWeight(4, 0.5f);
                morph.SetMorphWeight(5, 0.5f);
                morph.SetMorphWeight(6, 0.25f);
                runtime.EvaluateFrame();

                Assert.That(morph.GetEffectiveMorphWeight(0), Is.EqualTo(0.875f).Within(0.00001f));
                Assert.That(renderer.GetBlendShapeWeight(0), Is.EqualTo(87.5f).Within(0.0001f));
                Assert.That(renderer.sharedMesh.uv[0],
                    Is.EqualTo(baselineUv + new Vector2(0.05f, -0.1f)));
                Color morphedColor = ReadMaterialColor(renderer, 0);
                Assert.That(morphedColor.r, Is.EqualTo(baselineColor.r + 0.25f).Within(0.0001f));
                Assert.That(morphedColor.g, Is.EqualTo(baselineColor.g + 0.25f).Within(0.0001f));

                Vector3 expectedBoneMorph = new Vector3(0.05f, 0.1f, -0.15f);
                Assert.That(renderer.bones[1].localPosition,
                    Is.EqualTo(baselinePositions[1] + expectedBoneMorph));
                Assert.That(renderer.bones[2].localPosition,
                    Is.EqualTo(baselinePositions[2] + expectedBoneMorph * 0.5f));
                Assert.That(runtime.BoneController.LastAppliedFrame,
                    Is.EqualTo(runtime.EvaluatedFrameCount - 1));

                morph.ResetAllMorphWeights();
                runtime.EvaluateFrame();
                Assert.That(renderer.GetBlendShapeWeight(0), Is.Zero);
                Assert.That(renderer.sharedMesh.uv[0], Is.EqualTo(baselineUv));
                Assert.That(ReadMaterialColor(renderer, 0), Is.EqualTo(baselineColor));
                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    Assert.That(renderer.bones[i].localPosition, Is.EqualTo(baselinePositions[i]));
                    Assert.That(Quaternion.Angle(renderer.bones[i].localRotation, baselineRotations[i]),
                        Is.LessThan(0.0001f));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void FrameOrderAndRepeatedEvaluationAreDeterministic()
        {
            CollectionAssert.AreEqual(new[]
            {
                PmxFrameUpdateStage.MorphDependencies,
                PmxFrameUpdateStage.VertexUvMaterialMorphs,
                PmxFrameUpdateStage.BoneMorphs,
                PmxFrameUpdateStage.BoneGrant,
                PmxFrameUpdateStage.InverseKinematics
            }, PmxRuntimeController.DeterministicUpdateOrder);

            GameObject instance = ImportAndInstantiate(
                PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            try
            {
                PmxRuntimeController runtime = instance.GetComponent<PmxRuntimeController>();
                runtime.MorphController.SetMorphWeight(4, 0.6f);
                runtime.MorphController.SetMorphWeight(5, 0.25f);
                runtime.MorphController.SetMorphWeight(6, 0.2f);
                runtime.EvaluateFrame();
                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Vector3[] positions = renderer.bones.Select(value => value.localPosition).ToArray();
                Quaternion[] rotations = renderer.bones.Select(value => value.localRotation).ToArray();
                Vector2 uv = renderer.sharedMesh.uv[0];
                Color color = ReadMaterialColor(renderer, 0);
                float blendWeight = renderer.GetBlendShapeWeight(0);

                runtime.EvaluateFrame();
                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    Assert.That(renderer.bones[i].localPosition, Is.EqualTo(positions[i]));
                    Assert.That(Quaternion.Angle(renderer.bones[i].localRotation, rotations[i]),
                        Is.LessThan(0.0001f));
                }
                Assert.That(renderer.sharedMesh.uv[0], Is.EqualTo(uv));
                Assert.That(ReadMaterialColor(renderer, 0), Is.EqualTo(color));
                Assert.That(renderer.GetBlendShapeWeight(0), Is.EqualTo(blendWeight));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void RuntimeEvaluationToggle_PreservesAndRecapturesManualBonePose()
        {
            GameObject instance = ImportAndInstantiate(PmxStaticImportFixtureBuilder.Build());
            var restMesh = new Mesh();
            var posedMesh = new Mesh();
            try
            {
                PmxRuntimeController runtime = instance.GetComponent<PmxRuntimeController>();
                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Transform bone = runtime.BoneController.Bones[0];
                renderer.BakeMesh(restMesh);

                runtime.SetRuntimeEvaluationEnabled(false);
                Assert.That(runtime.RuntimeEvaluationEnabled, Is.False);
                bone.localPosition += new Vector3(0.2f, 0.1f, 0f);
                Vector3 manualPosition = bone.localPosition;
                renderer.BakeMesh(posedMesh);
                Assert.That(Vector3.Distance(restMesh.vertices[0], posedMesh.vertices[0]),
                    Is.GreaterThan(0.1f));

                runtime.SetRuntimeEvaluationEnabled(true);
                Assert.That(runtime.RuntimeEvaluationEnabled, Is.True);
                runtime.EvaluateFrame();
                Assert.That(bone.localPosition, Is.EqualTo(manualPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restMesh);
                UnityEngine.Object.DestroyImmediate(posedMesh);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void RuntimeMorphDependencyCycleIsRejectedWithoutRecursion()
        {
            Import(PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            PmxModelAsset asset = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<PmxModelAsset>().Single();
            asset.MorphMetadata[3].Offsets[0].MorphIndex = 3;
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject instance = UnityEngine.Object.Instantiate(source);
            try
            {
                PmxRuntimeController runtime = instance.GetComponent<PmxRuntimeController>();
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => runtime.EvaluateFrame());
                Assert.That(exception.Message, Does.Contain("cycle"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void MmdCompatibleRejectsOrExplicitlyDowngradesWithoutAdvancedBackend()
        {
            GameObject instance = ImportAndInstantiate(
                PmxStaticImportFixtureBuilder.BuildAdvancedDeformFixture());
            try
            {
                PmxRuntimeController runtime = instance.GetComponent<PmxRuntimeController>();
                InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() =>
                    runtime.SetCapability(PmxRuntimeCapabilityPath.MmdCompatible,
                        PmxMmdCompatibilityFallback.Reject));
                Assert.That(rejected.Message, Does.Contain("dedicated SDEF/QDEF backend"));

                PmxCompatibilityReport downgraded = runtime.SetCapability(
                    PmxRuntimeCapabilityPath.MmdCompatible,
                    PmxMmdCompatibilityFallback.DowngradeToStandardApproximate);
                Assert.That(downgraded.RequestedPath,
                    Is.EqualTo(PmxRuntimeCapabilityPath.MmdCompatible));
                Assert.That(downgraded.ActivePath,
                    Is.EqualTo(PmxRuntimeCapabilityPath.StandardApproximate));
                Assert.That(downgraded.Status,
                    Is.EqualTo(PmxFeatureSupportStatus.Approximated));
                Assert.That(downgraded.Message, Does.Contain("downgraded"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void MmdCompatibleWithoutAdvancedDeformsRemainsHonestlyApproximated()
        {
            GameObject instance = ImportAndInstantiate(
                PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            try
            {
                PmxCompatibilityReport report = instance.GetComponent<PmxRuntimeController>()
                    .SetCapability(PmxRuntimeCapabilityPath.MmdCompatible,
                        PmxMmdCompatibilityFallback.Reject);
                Assert.That(report.ActivePath,
                    Is.EqualTo(PmxRuntimeCapabilityPath.MmdCompatible));
                Assert.That(report.Status, Is.EqualTo(PmxFeatureSupportStatus.Approximated));
                Assert.That(report.Message, Does.Contain("semantic differences"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void WarmRuntimeEvaluationAllocatesNoManagedMemoryPerFrame()
        {
            GameObject instance = ImportAndInstantiate(
                PmxStaticImportFixtureBuilder.BuildMorphMetadataFixture());
            try
            {
                PmxRuntimeController runtime = instance.GetComponent<PmxRuntimeController>();
                PmxMorphController morph = runtime.MorphController;
                morph.SetMorphWeight(3, 0.4f);
                morph.SetMorphWeight(4, 0.3f);
                morph.SetMorphWeight(5, 0.2f);
                morph.SetMorphWeight(6, 0.1f);
                for (int i = 0; i < 16; i++) runtime.EvaluateFrame();

                SkinnedMeshRenderer renderer =
                    instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                int meshId = renderer.sharedMesh.GetInstanceID();
                Material[] materials = renderer.sharedMaterials;
                int materialId = materials[0].GetInstanceID();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) runtime.EvaluateFrame();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

                Assert.That(allocated, Is.Zero,
                    $"Warm EvaluateFrame allocated {allocated} managed bytes over 1000 frames.");
                Assert.That(renderer.sharedMesh.GetInstanceID(), Is.EqualTo(meshId));
                Assert.That(renderer.sharedMaterials[0].GetInstanceID(), Is.EqualTo(materialId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static GameObject ImportAndInstantiate(byte[] bytes)
        {
            Import(bytes);
            return UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        }

        private static void Import(byte[] bytes)
        {
            File.WriteAllBytes(ToAbsolutePath(ModelPath), bytes);
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Color ReadMaterialColor(SkinnedMeshRenderer renderer, int materialIndex)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, materialIndex);
            return block.GetColor(BaseColorId);
        }

        private static string ToAbsolutePath(string assetPath)
            => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }
}
