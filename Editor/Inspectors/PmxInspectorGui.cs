using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    internal static class PmxInspectorGui
    {
        public static void DrawParts(PmxModelAsset asset)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < asset.MaterialMetadata.Length; i++)
            {
                PmxMaterialMetadata part = asset.MaterialMetadata[i];
                string displayName = string.IsNullOrWhiteSpace(part.EnglishName)
                    ? part.Name
                    : part.EnglishName;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Part {i:D4}", displayName ?? string.Empty,
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Triangles", (part.SurfaceIndexCount / 3).ToString());
                Material material = i < asset.Materials.Length ? asset.Materials[i] : null;
                EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawBones(PmxModelAsset asset, IReadOnlyList<Transform> instanceBones)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < asset.BoneMetadata.Length; i++)
            {
                PmxBoneMetadata bone = asset.BoneMetadata[i];
                string displayName = string.IsNullOrWhiteSpace(bone.EnglishName)
                    ? bone.Name
                    : bone.EnglishName;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i:D4} {displayName}",
                    $"Parent={bone.ParentBoneIndex}  Layer={bone.Layer}");
                Transform transform = instanceBones != null && i < instanceBones.Count
                    ? instanceBones[i]
                    : null;
                using (new EditorGUI.DisabledScope(transform == null))
                {
                    if (GUILayout.Button("Select", GUILayout.Width(52f)))
                    {
                        Selection.activeTransform = transform;
                        SceneView.FrameLastActiveSceneView();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
}
