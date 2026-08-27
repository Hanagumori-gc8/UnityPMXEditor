using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(11000)]
    public sealed class PmxPhysicsController : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] rigidBodies = Array.Empty<Rigidbody>();
        [SerializeField] private Collider[] colliders = Array.Empty<Collider>();
        [SerializeField] private byte[] physicsModes = Array.Empty<byte>();
        [SerializeField] private byte[] collisionGroups = Array.Empty<byte>();
        [SerializeField] private ushort[] nonCollisionMasks = Array.Empty<ushort>();
        [SerializeField] private Transform[] relatedBones = Array.Empty<Transform>();

        [NonSerialized] private bool configured;
        [NonSerialized] private Vector3[] initialBodyPositions;
        [NonSerialized] private Quaternion[] initialBodyRotations;
        [NonSerialized] private Vector3[] baselineBonePositions;
        [NonSerialized] private Quaternion[] baselineBoneRotations;
        [NonSerialized] private Vector3[] boneToBodyPositions;
        [NonSerialized] private Quaternion[] boneToBodyRotations;
        [NonSerialized] private bool[] activeKinematicStates;

        public Rigidbody[] RigidBodies => rigidBodies;
        public Collider[] Colliders => colliders;
        public byte[] PhysicsModes => physicsModes;

        internal void Configure(Rigidbody[] bodies, Collider[] bodyColliders, byte[] modes,
            byte[] groups, ushort[] masks, Transform[] bones, bool enableOnInstantiate)
        {
            rigidBodies = bodies ?? Array.Empty<Rigidbody>();
            colliders = bodyColliders ?? Array.Empty<Collider>();
            physicsModes = modes ?? Array.Empty<byte>();
            collisionGroups = groups ?? Array.Empty<byte>();
            nonCollisionMasks = masks ?? Array.Empty<ushort>();
            relatedBones = bones ?? Array.Empty<Transform>();
            InitializeState();
            enabled = enableOnInstantiate;
            if (enabled) ApplyCollisionFiltering(true);
        }

        public void RestoreBaseline()
        {
            if (!configured) InitializeState();
            for (int i = 0; i < rigidBodies.Length; i++)
            {
                Rigidbody body = rigidBodies[i];
                if (body == null) continue;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = initialBodyPositions[i];
                body.rotation = initialBodyRotations[i];
                Transform bone = relatedBones[i];
                if (bone != null)
                {
                    bone.localPosition = baselineBonePositions[i];
                    bone.localRotation = baselineBoneRotations[i];
                }
            }
        }

        public void ReapplyCollisionFiltering()
        {
            if (!configured) InitializeState();
            ApplyCollisionFiltering(true);
        }

        private void OnEnable()
        {
            if (!configured) InitializeState();
            if (rigidBodies.Length == 0) return;
            SetPhysicsActive(true);
            ApplyCollisionFiltering(true);
        }

        private void OnDisable()
        {
            if (!configured) return;
            ApplyCollisionFiltering(false);
            RestoreBaseline();
            SetPhysicsActive(false);
        }

        private void Start()
        {
            if (!configured) InitializeState();
            if (rigidBodies.Length > 0) ApplyCollisionFiltering(true);
        }

        private void FixedUpdate()
        {
            if (!configured) InitializeState();
            for (int i = 0; i < rigidBodies.Length; i++)
            {
                if (physicsModes[i] != 0 || relatedBones[i] == null) continue;
                Transform bone = relatedBones[i];
                Rigidbody body = rigidBodies[i];
                Vector3 position = bone.TransformPoint(boneToBodyPositions[i]);
                Quaternion rotation = bone.rotation * boneToBodyRotations[i];
                body.MovePosition(position);
                body.MoveRotation(rotation);
            }
        }

        private void LateUpdate()
        {
            if (!configured) InitializeState();
            for (int i = 0; i < rigidBodies.Length; i++)
            {
                if (physicsModes[i] != 2 || relatedBones[i] == null) continue;
                Rigidbody body = rigidBodies[i];
                Transform bone = relatedBones[i];
                Quaternion boneRotation = body.rotation * Quaternion.Inverse(boneToBodyRotations[i]);
                bone.rotation = boneRotation;
                bone.position = body.position - boneRotation * boneToBodyPositions[i];
            }
        }

        public void EvaluateBoneFollow()
        {
            FixedUpdate();
        }

        public void EvaluatePhysicsToBone()
        {
            LateUpdate();
        }

        private void InitializeState()
        {
            rigidBodies = rigidBodies ?? Array.Empty<Rigidbody>();
            colliders = colliders ?? Array.Empty<Collider>();
            physicsModes = physicsModes ?? Array.Empty<byte>();
            collisionGroups = collisionGroups ?? Array.Empty<byte>();
            nonCollisionMasks = nonCollisionMasks ?? Array.Empty<ushort>();
            relatedBones = relatedBones ?? Array.Empty<Transform>();
            int count = rigidBodies.Length;
            if (colliders.Length != count || physicsModes.Length != count ||
                collisionGroups.Length != count || nonCollisionMasks.Length != count ||
                relatedBones.Length != count)
                throw new InvalidOperationException("PMX physics controller arrays have inconsistent lengths.");

            initialBodyPositions = new Vector3[count];
            initialBodyRotations = new Quaternion[count];
            baselineBonePositions = new Vector3[count];
            baselineBoneRotations = new Quaternion[count];
            boneToBodyPositions = new Vector3[count];
            boneToBodyRotations = new Quaternion[count];
            activeKinematicStates = new bool[count];
            for (int i = 0; i < count; i++)
            {
                Rigidbody body = rigidBodies[i];
                if (body == null) throw new InvalidOperationException($"PMX Rigidbody {i} is missing.");
                initialBodyPositions[i] = body.position;
                initialBodyRotations[i] = body.rotation;
                activeKinematicStates[i] = body.isKinematic;
                Transform bone = relatedBones[i];
                if (bone == null) continue;
                baselineBonePositions[i] = bone.localPosition;
                baselineBoneRotations[i] = bone.localRotation;
                boneToBodyPositions[i] = bone.InverseTransformPoint(body.position);
                boneToBodyRotations[i] = Quaternion.Inverse(bone.rotation) * body.rotation;
            }
            configured = true;
        }

        private void SetPhysicsActive(bool active)
        {
            for (int i = 0; i < rigidBodies.Length; i++)
            {
                Rigidbody body = rigidBodies[i];
                if (body == null) continue;
                body.isKinematic = active ? activeKinematicStates[i] : true;
            }
        }

        private void ApplyCollisionFiltering(bool ignore)
        {
            for (int left = 0; left < colliders.Length; left++)
            {
                if (colliders[left] == null) continue;
                int leftGroup = collisionGroups[left] & 0x0F;
                for (int right = left + 1; right < colliders.Length; right++)
                {
                    if (colliders[right] == null) continue;
                    int rightGroup = collisionGroups[right] & 0x0F;
                    bool filtered = ((nonCollisionMasks[left] >> rightGroup) & 1) != 0 ||
                                    ((nonCollisionMasks[right] >> leftGroup) & 1) != 0;
                    if (filtered) Physics.IgnoreCollision(colliders[left], colliders[right], ignore);
                }
            }
        }
    }
}
