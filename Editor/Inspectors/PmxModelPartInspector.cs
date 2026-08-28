using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [CustomEditor(typeof(PmxModelPart))]
    public sealed class PmxModelPartInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var part = (PmxModelPart)target;
            EditorGUILayout.LabelField("PMX Model Part", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Stable Part Index", part.PartIndex.ToString("D6"));
            EditorGUILayout.LabelField("Hierarchy Mode", part.Mode.ToString());
            EditorGUILayout.ObjectField("Renderer", part.TargetRenderer,
                typeof(SkinnedMeshRenderer), true);

            Material current = part.Material;
            bool sceneInstance = part.gameObject.scene.IsValid();
            using (new EditorGUI.DisabledScope(!sceneInstance))
            {
                Material replacement = (Material)EditorGUILayout.ObjectField(
                    "Material", current, typeof(Material), false);
                if (replacement != current)
                {
                    RecordPartObjects(part, "Assign PMX Part Material");
                    part.SetMaterial(replacement);
                    EditorUtility.SetDirty(part.Owner);
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!sceneInstance))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Show Only")) part.ShowOnly();
                if (GUILayout.Button("Show All")) part.ShowAll();
                EditorGUILayout.EndHorizontal();
            }

            if (!sceneInstance)
                EditorGUILayout.HelpBox(
                    "Imported PMX source assets are read-only. Instantiate an editable scene model " +
                    "before changing part materials or visibility.", MessageType.Info);

            if (part.Mode == PmxPartHierarchyMode.ProxyNodes)
                EditorGUILayout.HelpBox(
                    "This selectable node controls one submesh of the shared PMX renderer. " +
                    "Its Transform does not move geometry independently.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(
                    "This node owns an independent SkinnedMeshRenderer for one PMX material part. " +
                    "Moving its Transform moves that rendered part and increases draw-object count.",
                    MessageType.Info);
        }

        private static void RecordPartObjects(PmxModelPart part, string action)
        {
            if (part.Owner != null) Undo.RecordObject(part.Owner, action);
            if (part.TargetRenderer != null) Undo.RecordObject(part.TargetRenderer, action);
        }
    }
}
