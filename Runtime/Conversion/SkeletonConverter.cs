using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public sealed class SkeletonConversionResult
    {
        internal SkeletonConversionResult(Transform skeletonRoot, Transform[] bones,
            Transform rootBone, Matrix4x4[] bindposes)
        {
            SkeletonRoot = skeletonRoot;
            Bones = bones;
            RootBone = rootBone;
            Bindposes = bindposes;
            RendererBones = bones;
            RendererBindposes = bindposes;
        }

        public Transform SkeletonRoot { get; }
        public Transform[] Bones { get; }
        public Transform RootBone { get; }
        public Matrix4x4[] Bindposes { get; }
        public Transform[] RendererBones { get; private set; }
        public Matrix4x4[] RendererBindposes { get; private set; }
        public Transform PreservedDeformAnchor { get; private set; }

        internal int EnsurePreservedDeformAnchor(Transform modelRoot)
        {
            if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
            if (PreservedDeformAnchor != null) return Bones.Length;

            var anchorObject = new GameObject("PMX Preserved Deform Anchor");
            PreservedDeformAnchor = anchorObject.transform;
            PreservedDeformAnchor.SetParent(SkeletonRoot, false);
            PreservedDeformAnchor.localPosition = Vector3.zero;
            PreservedDeformAnchor.localRotation = Quaternion.identity;
            PreservedDeformAnchor.localScale = Vector3.one;

            RendererBones = new Transform[Bones.Length + 1];
            Array.Copy(Bones, RendererBones, Bones.Length);
            RendererBones[Bones.Length] = PreservedDeformAnchor;
            RendererBindposes = new Matrix4x4[Bindposes.Length + 1];
            Array.Copy(Bindposes, RendererBindposes, Bindposes.Length);
            RendererBindposes[Bindposes.Length] =
                PreservedDeformAnchor.worldToLocalMatrix * modelRoot.localToWorldMatrix;
            return Bones.Length;
        }
    }

    public sealed class SkeletonConverter
    {
        public SkeletonConversionResult Convert(PmxDocument document, Transform modelRoot,
            PmxCoordinateConverter coordinates)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
            if (document.Vertices.Count > 0 && document.Bones.Count == 0)
                throw new InvalidOperationException("A PMX with vertices cannot be skinned without bones.");

            ValidateParentGraph(document);

            var skeletonObject = new GameObject("PMX Skeleton");
            Transform skeletonRoot = skeletonObject.transform;
            skeletonRoot.SetParent(modelRoot, false);
            skeletonRoot.localPosition = Vector3.zero;
            skeletonRoot.localRotation = Quaternion.identity;
            skeletonRoot.localScale = Vector3.one;

            int count = document.Bones.Count;
            var bones = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var boneObject = new GameObject(CreateBoneHierarchyName(document.Bones[i], i));
                bones[i] = boneObject.transform;
                bones[i].SetParent(skeletonRoot, false);
            }

            int rootCount = 0;
            Transform singleRoot = null;
            for (int i = 0; i < count; i++)
            {
                PmxBone source = document.Bones[i];
                int parentIndex = source.ParentBoneIndex;
                Transform parent = parentIndex >= 0 ? bones[parentIndex] : skeletonRoot;
                bones[i].SetParent(parent, false);

                PmxVector3 localPosition = parentIndex >= 0
                    ? Subtract(source.Position, document.Bones[parentIndex].Position)
                    : source.Position;
                bones[i].localPosition = coordinates.ConvertPosition(localPosition);
                bones[i].localRotation = Quaternion.identity;
                bones[i].localScale = Vector3.one;

                if (parentIndex < 0)
                {
                    rootCount++;
                    singleRoot = bones[i];
                }
            }

            Transform rootBone = rootCount == 1 ? singleRoot : skeletonRoot;
            var bindposes = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
                bindposes[i] = bones[i].worldToLocalMatrix * modelRoot.localToWorldMatrix;

            return new SkeletonConversionResult(skeletonRoot, bones, rootBone, bindposes);
        }

        private static void ValidateParentGraph(PmxDocument document)
        {
            int count = document.Bones.Count;
            var state = new byte[count];
            var path = new List<int>();
            for (int start = 0; start < count; start++)
            {
                if (state[start] == 2) continue;
                path.Clear();
                int current = start;
                while (current >= 0)
                {
                    if (current >= count)
                        throw new InvalidOperationException(
                            $"Bone {path[path.Count - 1]} parent index {current} is outside [0, {count}).");
                    if (state[current] == 1)
                        throw new InvalidOperationException(
                            $"Bone parent hierarchy contains a cycle involving bone {current}.");
                    if (state[current] == 2) break;
                    state[current] = 1;
                    path.Add(current);
                    int parent = document.Bones[current].ParentBoneIndex;
                    if (parent < -1)
                        throw new InvalidOperationException(
                            $"Bone {current} parent index {parent} is below the allowed sentinel -1.");
                    current = parent;
                }

                for (int i = 0; i < path.Count; i++) state[path[i]] = 2;
            }
        }

        private static PmxVector3 Subtract(PmxVector3 value, PmxVector3 parent)
            => new PmxVector3(value.X - parent.X, value.Y - parent.Y, value.Z - parent.Z);

        private static string CreateBoneHierarchyName(PmxBone bone, int index)
        {
            string displayName = !string.IsNullOrWhiteSpace(bone.Name)
                ? bone.Name
                : bone.EnglishName;
            if (string.IsNullOrWhiteSpace(displayName)) return $"PMX Bone {index:D6}";

            var characters = displayName.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (char.IsControl(characters[i])) characters[i] = ' ';
            }
            string cleaned = new string(characters).Trim();
            if (cleaned.Length > 80) cleaned = cleaned.Substring(0, 80);
            return string.IsNullOrEmpty(cleaned)
                ? $"PMX Bone {index:D6}"
                : $"PMX Bone {index:D6} - {cleaned}";
        }
    }
}
