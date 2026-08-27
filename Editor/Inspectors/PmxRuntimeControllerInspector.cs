using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [CustomEditor(typeof(PmxRuntimeController))]
    public sealed class PmxRuntimeControllerInspector : UnityEditor.Editor
    {
        private bool showParts = true;
        private bool showBones = true;

        public override void OnInspectorGUI()
        {
            var controller = (PmxRuntimeController)target;
            PmxModelAsset asset = controller.ModelAsset;
            EditorGUILayout.LabelField("PMX Model", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Metadata", asset, typeof(PmxModelAsset), false);
            EditorGUILayout.LabelField("Runtime Capability", controller.ActiveCapability.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export FBX..."))
                PmxModelExportMenu.ExportWithDialog(controller.gameObject, PmxModelExportFormat.Fbx);
            if (GUILayout.Button("Export OBJ..."))
                PmxModelExportMenu.ExportWithDialog(controller.gameObject, PmxModelExportFormat.Obj);
            EditorGUILayout.EndHorizontal();

            if (asset == null)
            {
                EditorGUILayout.HelpBox("The PMX metadata reference is missing.", MessageType.Error);
                return;
            }

            showParts = EditorGUILayout.Foldout(showParts, "Model Parts (Material Submeshes)", true);
            if (showParts) PmxInspectorGui.DrawParts(asset);

            showBones = EditorGUILayout.Foldout(showBones, "Bones (Scene Gizmos)", true);
            if (showBones)
                PmxInspectorGui.DrawBones(asset, controller.BoneController?.Bones);
        }
    }
}
