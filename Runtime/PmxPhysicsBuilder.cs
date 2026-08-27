using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxPhysicsBuildResult
    {
        internal PmxPhysicsBuildResult(GameObject root, Rigidbody[] rigidBodies,
            Collider[] colliders, ConfigurableJoint[] joints, PhysicMaterial[] materials,
            PmxPhysicsController controller, int unsupportedShapeCount,
            int unsupportedJointCount)
        {
            Root = root;
            RigidBodies = rigidBodies;
            Colliders = colliders;
            Joints = joints;
            Materials = materials;
            Controller = controller;
            UnsupportedShapeCount = unsupportedShapeCount;
            UnsupportedJointCount = unsupportedJointCount;
        }

        public GameObject Root { get; }
        public Rigidbody[] RigidBodies { get; }
        public Collider[] Colliders { get; }
        public ConfigurableJoint[] Joints { get; }
        public PhysicMaterial[] Materials { get; }
        public PmxPhysicsController Controller { get; }
        public int UnsupportedShapeCount { get; }
        public int UnsupportedJointCount { get; }
    }

    public sealed class PmxPhysicsBuilder
    {
        public PmxPhysicsBuildResult Build(PmxRigidBodyMetadata[] rigidBodyMetadata,
            PmxJointMetadata[] jointMetadata, Transform modelRoot, Transform[] bones,
            PmxCoordinateConverter coordinates, PmxPhysicsSettings settings)
        {
            if (rigidBodyMetadata == null) throw new ArgumentNullException(nameof(rigidBodyMetadata));
            if (jointMetadata == null) throw new ArgumentNullException(nameof(jointMetadata));
            if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
            if (bones == null) throw new ArgumentNullException(nameof(bones));
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();

            GameObject physicsRoot = null;
            PhysicMaterial[] materials = null;
            try
            {
            var physicsCoordinates = new PmxPhysicsCoordinateConverter(coordinates);
            physicsRoot = new GameObject("PMX Physics");
            physicsRoot.transform.SetParent(modelRoot, false);
            int count = rigidBodyMetadata.Length;
            var bodies = new Rigidbody[count];
            var colliders = new Collider[count];
            materials = new PhysicMaterial[count];
            var modes = new byte[count];
            var groups = new byte[count];
            var masks = new ushort[count];
            var relatedBones = new Transform[count];
            int unsupportedShapeCount = 0;

            for (int i = 0; i < count; i++)
            {
                PmxRigidBodyMetadata source = rigidBodyMetadata[i] ??
                    throw new ArgumentException($"Rigid body metadata {i} is null.", nameof(rigidBodyMetadata));
                ValidateRigidBody(source, i);
                var bodyObject = new GameObject($"PMX Rigidbody {i:D6}");
                bodyObject.transform.SetParent(physicsRoot.transform, false);
                bodyObject.transform.SetPositionAndRotation(
                    physicsCoordinates.ConvertPosition(source.Position),
                    physicsCoordinates.ConvertEulerRadians(source.Rotation));

                PhysicMaterial material = CreateMaterial(source, i);
                Collider collider = CreateCollider(source, bodyObject, physicsCoordinates, material);
                if (collider == null) unsupportedShapeCount++;
                Rigidbody body = bodyObject.AddComponent<Rigidbody>();
                body.mass = Mathf.Max(source.Mass, settings.MinimumMass);
                body.drag = source.LinearDamping;
                body.angularDrag = source.AngularDamping;
                body.maxDepenetrationVelocity = settings.MaxDepenetrationVelocity;
                body.isKinematic = source.RawPhysicsMode == 0;
                body.useGravity = source.RawPhysicsMode != 0;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;

                bodies[i] = body;
                colliders[i] = collider;
                materials[i] = material;
                modes[i] = source.RawPhysicsMode;
                groups[i] = source.RawCollisionGroup;
                masks[i] = source.RawNonCollisionMask;
                relatedBones[i] = source.BoneIndex >= 0 && source.BoneIndex < bones.Length
                    ? bones[source.BoneIndex]
                    : null;
            }

            var joints = new List<ConfigurableJoint>(jointMetadata.Length);
            int unsupportedJointCount = 0;
            for (int i = 0; i < jointMetadata.Length; i++)
            {
                PmxJointMetadata source = jointMetadata[i] ??
                    throw new ArgumentException($"Joint metadata {i} is null.", nameof(jointMetadata));
                ValidateJoint(source, i, bodies.Length);
                if (source.RawType != 0)
                {
                    unsupportedJointCount++;
                    continue;
                }
                ConfigurableJoint joint = CreateJoint(source, i, bodies, physicsCoordinates, settings);
                if (joint == null) unsupportedJointCount++;
                else joints.Add(joint);
            }

            PmxPhysicsController controller = modelRoot.gameObject.AddComponent<PmxPhysicsController>();
            controller.Configure(bodies, colliders, modes, groups, masks, relatedBones,
                settings.EnableOnInstantiate);
            return new PmxPhysicsBuildResult(physicsRoot, bodies, colliders, joints.ToArray(),
                materials, controller, unsupportedShapeCount, unsupportedJointCount);
            }
            catch
            {
                if (materials != null)
                {
                    for (int i = 0; i < materials.Length; i++)
                        DestroyCreated(materials[i]);
                }
                DestroyCreated(physicsRoot);
                throw;
            }
        }

        private static void DestroyCreated(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        private static PhysicMaterial CreateMaterial(PmxRigidBodyMetadata source, int index)
        {
            var material = new PhysicMaterial($"PMX Physics Material {index:D6}")
            {
                dynamicFriction = Mathf.Clamp01(source.Friction),
                staticFriction = Mathf.Clamp01(source.Friction),
                bounciness = Mathf.Clamp01(source.Restitution),
                frictionCombine = PhysicMaterialCombine.Average,
                bounceCombine = PhysicMaterialCombine.Maximum
            };
            return material;
        }

        private static Collider CreateCollider(PmxRigidBodyMetadata source, GameObject target,
            PmxPhysicsCoordinateConverter coordinates, PhysicMaterial material)
        {
            Collider collider;
            switch (source.RawShape)
            {
                case 0:
                    var sphere = target.AddComponent<SphereCollider>();
                    sphere.radius = coordinates.ConvertLength(Mathf.Abs(source.Size.x));
                    collider = sphere;
                    break;
                case 1:
                    var box = target.AddComponent<BoxCollider>();
                    box.size = coordinates.ConvertBoxHalfExtents(source.Size) * 2f;
                    collider = box;
                    break;
                case 2:
                    var capsule = target.AddComponent<CapsuleCollider>();
                    capsule.direction = 1;
                    capsule.radius = coordinates.ConvertLength(Mathf.Abs(source.Size.x));
                    capsule.height = Mathf.Max(capsule.radius * 2f,
                        coordinates.ConvertLength(Mathf.Abs(source.Size.y)) + capsule.radius * 2f);
                    collider = capsule;
                    break;
                default:
                    return null;
            }
            collider.sharedMaterial = material;
            return collider;
        }

        private static ConfigurableJoint CreateJoint(PmxJointMetadata source, int index,
            Rigidbody[] bodies, PmxPhysicsCoordinateConverter coordinates,
            PmxPhysicsSettings settings)
        {
            Rigidbody owner = ResolveBody(bodies, source.RigidBodyAIndex);
            Rigidbody connected = ResolveBody(bodies, source.RigidBodyBIndex);
            if (owner == null && connected == null) return null;
            if (owner == null)
            {
                owner = connected;
                connected = null;
            }

            ConfigurableJoint joint = owner.gameObject.AddComponent<ConfigurableJoint>();
            joint.name = $"PMX Spring 6DOF {index:D6}";
            joint.connectedBody = connected;
            joint.autoConfigureConnectedAnchor = false;
            Vector3 worldAnchor = coordinates.ConvertPosition(source.Position);
            joint.anchor = owner.transform.InverseTransformPoint(worldAnchor);
            joint.connectedAnchor = connected != null
                ? connected.transform.InverseTransformPoint(worldAnchor)
                : worldAnchor;

            Quaternion worldFrame = coordinates.ConvertEulerRadians(source.Rotation);
            joint.axis = owner.transform.InverseTransformDirection(worldFrame * Vector3.right).normalized;
            joint.secondaryAxis = owner.transform.InverseTransformDirection(worldFrame * Vector3.up).normalized;

            Vector3 minimumPosition = coordinates.ConvertLinearLimit(source.MinimumPosition);
            Vector3 maximumPosition = coordinates.ConvertLinearLimit(source.MaximumPosition);
            float linearLimit = MaxAbs(minimumPosition, maximumPosition);
            joint.xMotion = MotionForRange(minimumPosition.x, maximumPosition.x);
            joint.yMotion = MotionForRange(minimumPosition.y, maximumPosition.y);
            joint.zMotion = MotionForRange(minimumPosition.z, maximumPosition.z);
            joint.linearLimit = new SoftJointLimit { limit = linearLimit };

            Vector3 minimumRotation = coordinates.ConvertAngularLimitDegrees(source.MinimumRotation);
            Vector3 maximumRotation = coordinates.ConvertAngularLimitDegrees(source.MaximumRotation);
            joint.angularXMotion = MotionForRange(minimumRotation.x, maximumRotation.x);
            joint.angularYMotion = MotionForRange(minimumRotation.y, maximumRotation.y);
            joint.angularZMotion = MotionForRange(minimumRotation.z, maximumRotation.z);
            joint.lowAngularXLimit = new SoftJointLimit
                { limit = Mathf.Min(minimumRotation.x, maximumRotation.x) };
            joint.highAngularXLimit = new SoftJointLimit
                { limit = Mathf.Max(minimumRotation.x, maximumRotation.x) };
            joint.angularYLimit = new SoftJointLimit
                { limit = Mathf.Max(Mathf.Abs(minimumRotation.y), Mathf.Abs(maximumRotation.y)) };
            joint.angularZLimit = new SoftJointLimit
                { limit = Mathf.Max(Mathf.Abs(minimumRotation.z), Mathf.Abs(maximumRotation.z)) };

            Vector3 linearSpring = coordinates.ConvertSpring(source.PositionSpring);
            Vector3 angularSpring = coordinates.ConvertSpring(source.RotationSpring);
            joint.xDrive = Drive(linearSpring.x, settings.JointDamper);
            joint.yDrive = Drive(linearSpring.y, settings.JointDamper);
            joint.zDrive = Drive(linearSpring.z, settings.JointDamper);
            joint.angularXDrive = Drive(angularSpring.x, settings.JointDamper);
            joint.angularYZDrive = Drive(Mathf.Max(angularSpring.y, angularSpring.z), settings.JointDamper);
            joint.rotationDriveMode = RotationDriveMode.XYAndZ;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            return joint;
        }

        private static Rigidbody ResolveBody(Rigidbody[] bodies, int index)
            => index >= 0 && index < bodies.Length ? bodies[index] : null;

        private static ConfigurableJointMotion MotionForRange(float minimum, float maximum)
            => Mathf.Approximately(minimum, 0f) && Mathf.Approximately(maximum, 0f)
                ? ConfigurableJointMotion.Locked
                : ConfigurableJointMotion.Limited;

        private static JointDrive Drive(float spring, float damper) => new JointDrive
        {
            positionSpring = Mathf.Max(0f, spring),
            positionDamper = Mathf.Max(0f, damper),
            maximumForce = float.MaxValue
        };

        private static float MaxAbs(Vector3 minimum, Vector3 maximum)
            => Mathf.Max(Mathf.Abs(minimum.x), Mathf.Abs(minimum.y), Mathf.Abs(minimum.z),
                Mathf.Abs(maximum.x), Mathf.Abs(maximum.y), Mathf.Abs(maximum.z));

        private static void ValidateRigidBody(PmxRigidBodyMetadata source, int index)
        {
            ValidateFinite(source.Size, $"rigid body {index} size");
            ValidateFinite(source.Position, $"rigid body {index} position");
            ValidateFinite(source.Rotation, $"rigid body {index} rotation");
            ValidateFiniteNonNegative(source.Mass, $"rigid body {index} mass");
            ValidateFiniteNonNegative(source.LinearDamping, $"rigid body {index} linear damping");
            ValidateFiniteNonNegative(source.AngularDamping, $"rigid body {index} angular damping");
            ValidateFiniteNonNegative(source.Restitution, $"rigid body {index} restitution");
            ValidateFiniteNonNegative(source.Friction, $"rigid body {index} friction");
            if (source.RawCollisionGroup > 15)
                throw new ArgumentOutOfRangeException(nameof(source.RawCollisionGroup),
                    $"Rigid body {index} collision group must be in [0, 15].");
            if (source.RawPhysicsMode > 2)
                throw new ArgumentOutOfRangeException(nameof(source.RawPhysicsMode),
                    $"Rigid body {index} physics mode {source.RawPhysicsMode} is unknown.");
        }

        private static void ValidateJoint(PmxJointMetadata source, int index, int bodyCount)
        {
            if (source.RigidBodyAIndex < -1 || source.RigidBodyAIndex >= bodyCount ||
                source.RigidBodyBIndex < -1 || source.RigidBodyBIndex >= bodyCount)
                throw new ArgumentOutOfRangeException(nameof(source),
                    $"Joint {index} references a rigid body outside [-1, {bodyCount - 1}].");
            ValidateFinite(source.Position, $"joint {index} position");
            ValidateFinite(source.Rotation, $"joint {index} rotation");
            ValidateFinite(source.MinimumPosition, $"joint {index} minimum position");
            ValidateFinite(source.MaximumPosition, $"joint {index} maximum position");
            ValidateFinite(source.MinimumRotation, $"joint {index} minimum rotation");
            ValidateFinite(source.MaximumRotation, $"joint {index} maximum rotation");
            ValidateFinite(source.PositionSpring, $"joint {index} position spring");
            ValidateFinite(source.RotationSpring, $"joint {index} rotation spring");
        }

        private static void ValidateFinite(Vector3 value, string label)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z))
                throw new ArgumentOutOfRangeException(label, $"{label} must be finite.");
        }

        private static void ValidateFiniteNonNegative(float value, string label)
        {
            if (!IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(label, $"{label} must be finite and non-negative.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal sealed class PmxPhysicsCoordinateConverter
    {
        private readonly PmxCoordinateConverter coordinates;

        public PmxPhysicsCoordinateConverter(PmxCoordinateConverter coordinates)
        {
            this.coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
        }

        public Vector3 ConvertPosition(Vector3 value)
            => new Vector3(value.x * coordinates.Scale, value.y * coordinates.Scale,
                -value.z * coordinates.Scale);

        public float ConvertLength(float value) => value * coordinates.Scale;

        public Vector3 ConvertBoxHalfExtents(Vector3 value)
            => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z)) *
               coordinates.Scale;

        public Quaternion ConvertEulerRadians(Vector3 value)
        {
            Quaternion source = Quaternion.Euler(value * Mathf.Rad2Deg);
            return coordinates.ConvertRotation(new Vector4(source.x, source.y, source.z, source.w));
        }

        public Vector3 ConvertLinearLimit(Vector3 value) => ConvertPosition(value);

        public Vector3 ConvertAngularLimitDegrees(Vector3 value)
            => new Vector3(-value.x, -value.y, value.z) * Mathf.Rad2Deg;

        public Vector3 ConvertSpring(Vector3 value)
            => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
