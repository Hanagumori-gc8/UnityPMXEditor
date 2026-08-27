using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hanagumori.UnityPmx.Tests
{
    public sealed class PmxPhysicsLongRunTests
    {
        [UnityTest]
        public IEnumerator ExperimentalPhysics_StaticDynamicAndPhysicsBoneRemainFiniteForLongRun()
        {
            GameObject root = new GameObject("PMX Physics Long Run Root");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PmxPhysicsBuildResult result = null;
            try
            {
                floor.name = "PMX Physics Test Floor";
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(20f, 1f, 20f);
                Transform[] bones = CreateBones(root.transform);
                var settings = new PmxPhysicsSettings
                {
                    JointDamper = 2f,
                    MinimumMass = 0.001f,
                    MaxDepenetrationVelocity = 5f,
                    EnableOnInstantiate = true
                };
                result = new PmxPhysicsBuilder().Build(CreateBodies(), CreateJoints(),
                    root.transform, bones, new PmxCoordinateConverter(0.1f), settings);

                Assert.That(result.UnsupportedShapeCount, Is.Zero);
                Assert.That(result.UnsupportedJointCount, Is.Zero);
                Assert.That(result.Colliders[0], Is.TypeOf<SphereCollider>());
                Assert.That(result.Colliders[1], Is.TypeOf<BoxCollider>());
                Assert.That(result.Colliders[2], Is.TypeOf<CapsuleCollider>());
                CollectionAssert.AreEqual(new byte[] { 0, 1, 2 }, result.Controller.PhysicsModes);
                Assert.That(result.RigidBodies[0].isKinematic, Is.True);
                Assert.That(result.RigidBodies[1].isKinematic, Is.False);
                Assert.That(result.RigidBodies[2].isKinematic, Is.False);
                Assert.That(Physics.GetIgnoreCollision(result.Colliders[0], result.Colliders[1]), Is.True);

                bones[0].position += Vector3.right * 0.05f;
                result.Controller.EvaluateBoneFollow();
                yield return new WaitForFixedUpdate();
                Assert.That(Vector3.Distance(result.RigidBodies[0].position,
                    bones[0].position), Is.LessThan(0.001f));

                const int fixedFrames = 3000;
                for (int frame = 0; frame < fixedFrames; frame++)
                {
                    yield return new WaitForFixedUpdate();
                    if ((frame % 30) != 0) continue;
                    for (int i = 0; i < result.RigidBodies.Length; i++)
                    {
                        Rigidbody body = result.RigidBodies[i];
                        AssertFinite(body.position, $"body {i} position at fixed frame {frame}");
                        AssertFinite(body.rotation, $"body {i} rotation at fixed frame {frame}");
                        AssertFinite(body.velocity, $"body {i} velocity at fixed frame {frame}");
                        AssertFinite(body.angularVelocity,
                            $"body {i} angular velocity at fixed frame {frame}");
                        Assert.That(body.position.sqrMagnitude, Is.LessThan(10000f),
                            $"body {i} escaped the bounded fixture at fixed frame {frame}");
                    }
                }

                result.Controller.EvaluatePhysicsToBone();
                AssertFinite(bones[2].position, "physics+bone transform");
                Vector3 baselineBonePosition = new Vector3(0.2f, 0.3f, 0f);
                result.Controller.enabled = false;
                Assert.That(result.RigidBodies[0].isKinematic, Is.True);
                Assert.That(result.RigidBodies[1].isKinematic, Is.True);
                Assert.That(result.RigidBodies[2].isKinematic, Is.True);
                Assert.That(Vector3.Distance(bones[2].localPosition, baselineBonePosition),
                    Is.LessThan(0.00001f));
                Assert.That(Physics.GetIgnoreCollision(result.Colliders[0], result.Colliders[1]), Is.False);

                result.Controller.enabled = true;
                Assert.That(result.RigidBodies[1].isKinematic, Is.False);
                Assert.That(result.RigidBodies[2].isKinematic, Is.False);
                Assert.That(Physics.GetIgnoreCollision(result.Colliders[0], result.Colliders[1]), Is.True);
            }
            finally
            {
                if (result != null)
                {
                    for (int i = 0; i < result.Materials.Length; i++)
                        if (result.Materials[i] != null) UnityEngine.Object.Destroy(result.Materials[i]);
                }
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(floor);
            }
        }

        private static Transform[] CreateBones(Transform root)
        {
            var positions = new[]
            {
                new Vector3(-0.2f, 0.3f, 0f),
                new Vector3(0f, 0.3f, 0f),
                new Vector3(0.2f, 0.3f, 0f)
            };
            var bones = new Transform[positions.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = new GameObject($"PMX Bone {i:D6}");
                bone.transform.SetParent(root, false);
                bone.transform.localPosition = positions[i];
                bones[i] = bone.transform;
            }
            return bones;
        }

        private static PmxRigidBodyMetadata[] CreateBodies() => new[]
        {
            Body(0, 0, 0x0002, 0, new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-2f, 3f, 0f), 0),
            Body(1, 1, 0, 1, new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0f, 3f, 0f), 1),
            Body(2, 2, 0, 2, new Vector3(0.35f, 0.8f, 0.35f),
                new Vector3(2f, 3f, 0f), 2)
        };

        private static PmxRigidBodyMetadata Body(int bone, byte group, ushort mask,
            byte shape, Vector3 size, Vector3 position, byte mode) => new PmxRigidBodyMetadata
        {
            Name = $"body {bone}",
            EnglishName = $"body {bone}",
            BoneIndex = bone,
            RawCollisionGroup = group,
            RawNonCollisionMask = mask,
            RawShape = shape,
            Size = size,
            Position = position,
            Rotation = Vector3.zero,
            Mass = 1f,
            LinearDamping = 0.05f,
            AngularDamping = 0.1f,
            Restitution = 0.05f,
            Friction = 0.5f,
            RawPhysicsMode = mode
        };

        private static PmxJointMetadata[] CreateJoints() => new[]
        {
            new PmxJointMetadata
            {
                Name = "spring joint",
                EnglishName = "spring joint",
                RawType = 0,
                RigidBodyAIndex = 1,
                RigidBodyBIndex = 2,
                Position = new Vector3(1f, 3f, 0f),
                Rotation = Vector3.zero,
                MinimumPosition = new Vector3(-2f, -0.2f, -0.2f),
                MaximumPosition = new Vector3(2f, 0.2f, 0.2f),
                MinimumRotation = new Vector3(-0.2f, -0.2f, -0.2f),
                MaximumRotation = new Vector3(0.2f, 0.2f, 0.2f),
                PositionSpring = new Vector3(20f, 20f, 20f),
                RotationSpring = new Vector3(5f, 5f, 5f)
            }
        };

        private static void AssertFinite(Vector3 value, string label)
        {
            Assert.That(IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z),
                Is.True, label);
        }

        private static void AssertFinite(Quaternion value, string label)
        {
            Assert.That(IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) &&
                IsFinite(value.w), Is.True, label);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
