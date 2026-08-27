using System;
using System.Collections.Generic;

namespace Hanagumori.UnityPmx
{
    internal static class PmxDocumentValidator
    {
        public static void Validate(PmxDocument document)
        {
            ValidateSurfaces(document);
            ValidateVertices(document);
            ValidateMaterials(document);
            ValidateBones(document);
            ValidateMorphs(document);
            ValidateDisplayFrames(document);
            ValidateRigidBodies(document);
            ValidateJoints(document);
            ValidateSoftBodies(document);
        }

        private static void ValidateSurfaces(PmxDocument document)
        {
            for (int i = 0; i < document.SurfaceVertexIndices.Count; i++)
            {
                ValidateVertexIndex(document.SurfaceVertexIndices[i], document.Vertices.Count,
                    "Surface", document.SurfaceIndexOffsets[i], $"surface vertex index {i}");
            }
        }

        private static void ValidateVertices(PmxDocument document)
        {
            for (int i = 0; i < document.Vertices.Count; i++)
            {
                PmxVertex vertex = document.Vertices[i];
                for (int bone = 0; bone < vertex.Deform.BoneIndices.Count; bone++)
                {
                    ValidateSignedIndex(vertex.Deform.BoneIndices[bone], document.Bones.Count,
                        "Vertex", vertex.SourceOffset, $"vertex {i} bone index {bone}");
                }
            }
        }

        private static void ValidateMaterials(PmxDocument document)
        {
            long surfaceTotal = 0;
            for (int i = 0; i < document.Materials.Count; i++)
            {
                PmxMaterial material = document.Materials[i];
                ValidateSignedIndex(material.TextureIndex, document.Textures.Count,
                    "Material", material.SourceOffset, $"material {i} texture index");
                ValidateSignedIndex(material.EnvironmentTextureIndex, document.Textures.Count,
                    "Material", material.SourceOffset, $"material {i} environment texture index");
                if (material.UsesSharedToonTexture)
                {
                    if (material.ToonTextureIndex < 0 || material.ToonTextureIndex > 9)
                    {
                        throw Error("Material", material.SourceOffset,
                            $"Material {i} shared toon index {material.ToonTextureIndex} must be between 0 and 9.");
                    }
                }
                else
                {
                    ValidateSignedIndex(material.ToonTextureIndex, document.Textures.Count,
                        "Material", material.SourceOffset, $"material {i} toon texture index");
                }

                surfaceTotal += material.SurfaceIndexCount;
                if (surfaceTotal > document.SurfaceVertexIndices.Count)
                {
                    throw Error("Material", material.SourceOffset,
                        $"Material surface counts total {surfaceTotal}, exceeding the Surface count {document.SurfaceVertexIndices.Count}.");
                }
            }

            if (surfaceTotal != document.SurfaceVertexIndices.Count)
            {
                long offset = document.Materials.Count == 0 ? 0 : document.Materials[document.Materials.Count - 1].SourceOffset;
                throw Error("Material", offset,
                    $"Material surface counts total {surfaceTotal}, but Surface contains {document.SurfaceVertexIndices.Count} indices.");
            }
        }

        private static void ValidateBones(PmxDocument document)
        {
            int count = document.Bones.Count;
            for (int i = 0; i < count; i++)
            {
                PmxBone bone = document.Bones[i];
                ValidateSignedIndex(bone.ParentBoneIndex, count, "Bone", bone.SourceOffset, $"bone {i} parent");
                if (bone.TailBoneIndex.HasValue)
                    ValidateSignedIndex(bone.TailBoneIndex.Value, count, "Bone", bone.SourceOffset, $"bone {i} tail");
                if (bone.InheritParentBoneIndex.HasValue)
                    ValidateSignedIndex(bone.InheritParentBoneIndex.Value, count, "Bone", bone.SourceOffset, $"bone {i} inherit parent");
                if (bone.InverseKinematics != null)
                {
                    ValidateSignedIndex(bone.InverseKinematics.TargetBoneIndex, count,
                        "Bone", bone.InverseKinematics.SourceOffset, $"bone {i} IK target");
                    for (int link = 0; link < bone.InverseKinematics.Links.Count; link++)
                    {
                        PmxBoneIkLink value = bone.InverseKinematics.Links[link];
                        ValidateSignedIndex(value.BoneIndex, count, "Bone", value.SourceOffset,
                            $"bone {i} IK link {link}");
                    }
                }
            }

            DetectBoneParentCycles(document);
        }

        private static void ValidateMorphs(PmxDocument document)
        {
            for (int i = 0; i < document.Morphs.Count; i++)
            {
                PmxMorph morph = document.Morphs[i];
                if (morph.RawType >= (byte)PmxMorphType.AdditionalUv1 &&
                    morph.RawType <= (byte)PmxMorphType.AdditionalUv4 &&
                    morph.RawType - (byte)PmxMorphType.Uv > document.Header.AdditionalUvCount)
                {
                    throw Error("Morph", morph.SourceOffset,
                        $"Morph {i} targets additional UV set {morph.RawType - (byte)PmxMorphType.Uv}, " +
                        $"but the header declares only {document.Header.AdditionalUvCount}.");
                }

                for (int offsetIndex = 0; offsetIndex < morph.Offsets.Count; offsetIndex++)
                {
                    PmxMorphOffset offset = morph.Offsets[offsetIndex];
                    if (offset is PmxGroupMorphOffset group)
                        ValidateSignedIndex(group.MorphIndex, document.Morphs.Count, "Morph", group.SourceOffset, $"morph {i} group target");
                    else if (offset is PmxFlipMorphOffset flip)
                        ValidateSignedIndex(flip.MorphIndex, document.Morphs.Count, "Morph", flip.SourceOffset, $"morph {i} flip target");
                    else if (offset is PmxVertexMorphOffset vertex)
                        ValidateVertexIndex(vertex.VertexIndex, document.Vertices.Count, "Morph", vertex.SourceOffset, $"morph {i} vertex target");
                    else if (offset is PmxUvMorphOffset uv)
                        ValidateVertexIndex(uv.VertexIndex, document.Vertices.Count, "Morph", uv.SourceOffset, $"morph {i} UV target");
                    else if (offset is PmxBoneMorphOffset bone)
                        ValidateSignedIndex(bone.BoneIndex, document.Bones.Count, "Morph", bone.SourceOffset, $"morph {i} bone target");
                    else if (offset is PmxMaterialMorphOffset material)
                        ValidateSignedIndex(material.MaterialIndex, document.Materials.Count, "Morph", material.SourceOffset, $"morph {i} material target");
                    else if (offset is PmxImpulseMorphOffset impulse)
                        ValidateSignedIndex(impulse.RigidBodyIndex, document.RigidBodies.Count, "Morph", impulse.SourceOffset, $"morph {i} rigid body target");
                }
            }

            DetectMorphCycles(document);
        }

        private static void ValidateDisplayFrames(PmxDocument document)
        {
            for (int frameIndex = 0; frameIndex < document.DisplayFrames.Count; frameIndex++)
            {
                PmxDisplayFrame frame = document.DisplayFrames[frameIndex];
                for (int elementIndex = 0; elementIndex < frame.Elements.Count; elementIndex++)
                {
                    PmxDisplayFrameElement element = frame.Elements[elementIndex];
                    if (element.IsMorph)
                        ValidateSignedIndex(element.Index, document.Morphs.Count, "DisplayFrame", element.SourceOffset, $"frame {frameIndex} morph element");
                    else
                        ValidateSignedIndex(element.Index, document.Bones.Count, "DisplayFrame", element.SourceOffset, $"frame {frameIndex} bone element");
                }
            }
        }

        private static void ValidateRigidBodies(PmxDocument document)
        {
            for (int i = 0; i < document.RigidBodies.Count; i++)
            {
                PmxRigidBody body = document.RigidBodies[i];
                ValidateSignedIndex(body.BoneIndex, document.Bones.Count, "RigidBody", body.SourceOffset, $"rigid body {i} bone");
            }
        }

        private static void ValidateJoints(PmxDocument document)
        {
            for (int i = 0; i < document.Joints.Count; i++)
            {
                PmxJoint joint = document.Joints[i];
                ValidateSignedIndex(joint.RigidBodyAIndex, document.RigidBodies.Count, "Joint", joint.SourceOffset, $"joint {i} rigid body A");
                ValidateSignedIndex(joint.RigidBodyBIndex, document.RigidBodies.Count, "Joint", joint.SourceOffset, $"joint {i} rigid body B");
            }
        }

        private static void ValidateSoftBodies(PmxDocument document)
        {
            for (int i = 0; i < document.SoftBodies.Count; i++)
            {
                PmxSoftBody body = document.SoftBodies[i];
                ValidateSignedIndex(body.MaterialIndex, document.Materials.Count, "SoftBody", body.SourceOffset, $"soft body {i} material");
                for (int anchorIndex = 0; anchorIndex < body.Anchors.Count; anchorIndex++)
                {
                    PmxSoftBodyAnchor anchor = body.Anchors[anchorIndex];
                    ValidateSignedIndex(anchor.RigidBodyIndex, document.RigidBodies.Count,
                        "SoftBody", anchor.SourceOffset, $"soft body {i} anchor rigid body");
                    ValidateVertexIndex(anchor.VertexIndex, document.Vertices.Count,
                        "SoftBody", anchor.SourceOffset, $"soft body {i} anchor vertex");
                }
                for (int pinIndex = 0; pinIndex < body.PinnedVertexIndices.Count; pinIndex++)
                {
                    ValidateVertexIndex(body.PinnedVertexIndices[pinIndex], document.Vertices.Count,
                        "SoftBody", body.PinOffsets[pinIndex], $"soft body {i} pinned vertex");
                }
            }
        }

        private static void DetectBoneParentCycles(PmxDocument document)
        {
            int count = document.Bones.Count;
            var indegree = new int[count];
            for (int i = 0; i < count; i++)
            {
                int parent = document.Bones[i].ParentBoneIndex;
                if (parent >= 0) indegree[parent]++;
            }

            var queue = new Queue<int>();
            for (int i = 0; i < count; i++) if (indegree[i] == 0) queue.Enqueue(i);
            int consumed = 0;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                consumed++;
                int parent = document.Bones[index].ParentBoneIndex;
                if (parent >= 0 && --indegree[parent] == 0) queue.Enqueue(parent);
            }

            if (consumed != count)
            {
                for (int i = 0; i < count; i++)
                    if (indegree[i] > 0) throw Error("Bone", document.Bones[i].SourceOffset, $"Bone parent references contain a cycle involving bone {i}.");
            }
        }

        private static void DetectMorphCycles(PmxDocument document)
        {
            int count = document.Morphs.Count;
            var indegree = new int[count];
            for (int i = 0; i < count; i++)
                ForEachMorphDependency(document.Morphs[i], target => { if (target >= 0) indegree[target]++; });

            var queue = new Queue<int>();
            for (int i = 0; i < count; i++) if (indegree[i] == 0) queue.Enqueue(i);
            int consumed = 0;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                consumed++;
                ForEachMorphDependency(document.Morphs[index], target =>
                {
                    if (target >= 0 && --indegree[target] == 0) queue.Enqueue(target);
                });
            }

            if (consumed != count)
            {
                for (int i = 0; i < count; i++)
                    if (indegree[i] > 0) throw Error("Morph", document.Morphs[i].SourceOffset, $"Group/flip morph references contain a cycle involving morph {i}.");
            }
        }

        private static void ForEachMorphDependency(PmxMorph morph, Action<int> action)
        {
            for (int i = 0; i < morph.Offsets.Count; i++)
            {
                if (morph.Offsets[i] is PmxGroupMorphOffset group) action(group.MorphIndex);
                else if (morph.Offsets[i] is PmxFlipMorphOffset flip) action(flip.MorphIndex);
            }
        }

        private static void ValidateVertexIndex(int index, int count, string section, long offset, string field)
        {
            if (index < 0 || index >= count)
                throw Error(section, offset, $"{field} {index} is outside the unsigned vertex range [0, {count}).");
        }

        private static void ValidateSignedIndex(int index, int count, string section, long offset, string field)
        {
            if (index < -1 || index >= count)
                throw Error(section, offset, $"{field} {index} is outside the allowed range [-1, {count}).");
        }

        private static PmxFormatException Error(string section, long offset, string message)
            => new PmxFormatException(section, Math.Max(0, offset), message);
    }
}
