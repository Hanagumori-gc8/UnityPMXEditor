using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxPhysicsImporterTests
    {
        private const string TestRoot = "Assets/__UnityPMXEditorStage6Tests";
        private const string ModelPath = TestRoot + "/physics-fixture.pmx";

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
        public void PhysicsNone_PreservesMetadataWithoutCreatingComponents()
        {
            ImportFixture();

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            PmxModelAsset metadata = LoadMetadata();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<ConfigurableJoint>(true), Is.Empty);
            Assert.That(root.GetComponent<PmxPhysicsController>(), Is.Null);
            Assert.That(root.GetComponent<SkinnedMeshRenderer>(), Is.Not.Null);
            Assert.That(metadata.RigidBodyMetadata.Length, Is.EqualTo(3));
            Assert.That(metadata.JointMetadata.Length, Is.EqualTo(1));
            Assert.That(metadata.SoftBodyMetadata.Single().SupportStatus,
                Is.EqualTo(PmxFeatureSupportStatus.Unsupported));
            Assert.That(metadata.PhysicsImportMode, Is.EqualTo(PmxPhysicsImportMode.None));
            Assert.That(metadata.Diagnostics.Any(value => value.Code == "PHYSICS_DISABLED"), Is.True);
            Assert.That(metadata.Diagnostics.Any(value => value.Code == "SOFTBODY_UNSUPPORTED" &&
                value.Status == PmxFeatureSupportStatus.Unsupported), Is.True);

            var baked = new Mesh();
            try
            {
                root.GetComponent<SkinnedMeshRenderer>().BakeMesh(baked);
                Assert.That(baked.vertexCount, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void ExperimentalPhysics_MapsShapesBodiesJointAndStableSubAssets()
        {
            ImportFixture();
            EnableExperimentalPhysics();

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            PmxModelAsset metadata = LoadMetadata();
            Assert.That(root.GetComponentsInChildren<SphereCollider>(true).Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<BoxCollider>(true).Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<CapsuleCollider>(true).Length, Is.EqualTo(1));
            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true)
                .OrderBy(value => value.name, StringComparer.Ordinal).ToArray();
            Assert.That(bodies.Length, Is.EqualTo(3));
            Assert.That(bodies[0].name, Is.EqualTo("PMX Rigidbody 000000"));
            Assert.That(bodies[0].isKinematic, Is.True);
            Assert.That(bodies[1].isKinematic, Is.False);
            Assert.That(bodies[2].isKinematic, Is.False);
            ConfigurableJoint joint = root.GetComponentInChildren<ConfigurableJoint>(true);
            Assert.That(joint, Is.Not.Null);
            Assert.That(joint.name, Is.EqualTo("PMX Spring 6DOF 000000"));
            Assert.That(IsFinite(joint.anchor), Is.True);
            Assert.That(joint.xDrive.positionSpring, Is.GreaterThan(0f));
            Assert.That(metadata.PhysicsImportMode,
                Is.EqualTo(PmxPhysicsImportMode.Experimental));
            Assert.That(metadata.Diagnostics.Any(value => value.Code == "PHYSICS_EXPERIMENTAL" &&
                value.Status == PmxFeatureSupportStatus.Approximated), Is.True);

            string[] before = PhysicsMaterialIds();
            Assert.That(before.Length, Is.EqualTo(3));
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            CollectionAssert.AreEqual(before, PhysicsMaterialIds());
        }

        [Test]
        public void ExperimentalPhysics_FilteringAndDisableRestoreAreRepeatable()
        {
            ImportFixture();
            EnableExperimentalPhysics();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                PmxPhysicsController controller = instance.GetComponent<PmxPhysicsController>();
                Collider[] colliders = controller.Colliders;
                controller.ReapplyCollisionFiltering();
                Assert.That(Physics.GetIgnoreCollision(colliders[0], colliders[1]), Is.True);
                controller.ReapplyCollisionFiltering();
                Assert.That(Physics.GetIgnoreCollision(colliders[0], colliders[1]), Is.True);

                Transform physicsBone = instance.GetComponentsInChildren<Transform>(true)
                    .Single(value => value.name == "PMX Bone 000002");
                Vector3 baseline = physicsBone.localPosition;
                physicsBone.localPosition += Vector3.one;
                controller.RigidBodies[2].velocity = Vector3.one;
                controller.RestoreBaseline();
                Assert.That(Vector3.Distance(physicsBone.localPosition, baseline), Is.LessThan(0.00001f));
                Assert.That(controller.RigidBodies[2].velocity, Is.EqualTo(Vector3.zero));
                controller.enabled = false;
                controller.enabled = true;
                controller.ReapplyCollisionFiltering();
                Assert.That(Physics.GetIgnoreCollision(colliders[0], colliders[1]), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ImportFixture()
        {
            File.WriteAllBytes(ToAbsolutePath(ModelPath),
                PmxStaticImportFixtureBuilder.BuildPhysicsFixture());
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void EnableExperimentalPhysics()
        {
            var importer = (PmxScriptedImporter)AssetImporter.GetAtPath(ModelPath);
            importer.Settings.PhysicsMode = PmxPhysicsImportMode.Experimental;
            importer.Settings.PhysicsSettings.EnableOnInstantiate = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static PmxModelAsset LoadMetadata() => AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<PmxModelAsset>().Single();

        private static string[] PhysicsMaterialIds() => AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<PhysicMaterial>()
            .Select(value =>
            {
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    value, out string guid, out long localId), Is.True);
                return guid + ":" + localId;
            })
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        private static bool IsFinite(Vector3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static string ToAbsolutePath(string assetPath)
            => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }
}
