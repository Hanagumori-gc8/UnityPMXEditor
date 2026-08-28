using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    internal static class PmxInspectorGui
    {
        public static void DrawParts(PmxModelAsset asset,
            PmxModelPartsController controller = null)
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
                PmxModelPart scenePart = controller != null && i < controller.Parts.Length
                    ? controller.Parts[i]
                    : null;
                Material material = controller != null && i < controller.Materials.Length
                    ? controller.Materials[i]
                    : i < asset.Materials.Length ? asset.Materials[i] : null;
                Material replacement = (Material)EditorGUILayout.ObjectField(
                    "Material", material, typeof(Material), false);
                if (controller != null && replacement != material)
                {
                    Undo.RecordObject(controller, "Assign PMX Part Material");
                    if (scenePart != null && scenePart.TargetRenderer != null)
                        Undo.RecordObject(scenePart.TargetRenderer, "Assign PMX Part Material");
                    controller.SetPartMaterial(i, replacement);
                    EditorUtility.SetDirty(controller);
                }
                if (scenePart != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select Part"))
                    {
                        Selection.activeGameObject = scenePart.gameObject;
                        SceneView.FrameLastActiveSceneView();
                    }
                    if (GUILayout.Button("Show Only")) controller.ShowOnlyPart(i);
                    if (GUILayout.Button("Show All")) controller.ShowAllParts();
                    EditorGUILayout.EndHorizontal();
                }
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
