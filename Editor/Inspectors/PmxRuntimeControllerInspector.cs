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
            PmxModelPartsController partsController =
                controller.GetComponent<PmxModelPartsController>();
            EditorGUILayout.LabelField("PMX Model", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Metadata", asset, typeof(PmxModelAsset), false);
            EditorGUILayout.LabelField("Runtime Capability", controller.ActiveCapability.ToString());
            bool evaluationEnabled = EditorGUILayout.Toggle("Runtime Evaluation",
                controller.RuntimeEvaluationEnabled);
            if (evaluationEnabled != controller.RuntimeEvaluationEnabled)
            {
                Undo.RecordObject(controller, "Toggle PMX Runtime Evaluation");
                controller.SetRuntimeEvaluationEnabled(evaluationEnabled);
                EditorUtility.SetDirty(controller);
            }

            SkinnedMeshRenderer renderer = partsController != null
                ? partsController.CanonicalRenderer
                : controller.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null && partsController != null &&
                partsController.Renderers.Length > 0)
                renderer = partsController.Renderers[0];
            EditorGUILayout.LabelField("Part Hierarchy",
                partsController != null ? partsController.Mode.ToString() : "Legacy");
            if (partsController != null && partsController.Parts.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField("Parts Root",
                    partsController.Parts[0].transform.parent,
                    typeof(Transform), true);
                if (GUILayout.Button("Select", GUILayout.Width(52f)))
                    Selection.activeTransform = partsController.Parts[0].transform.parent;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("Mesh Renderer", renderer,
                typeof(SkinnedMeshRenderer), true);
            using (new EditorGUI.DisabledScope(renderer == null))
            {
                if (GUILayout.Button("Select", GUILayout.Width(52f)))
                    Selection.activeGameObject = renderer.gameObject;
            }
            EditorGUILayout.EndHorizontal();

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
            if (showParts) PmxInspectorGui.DrawParts(asset, partsController);

            showBones = EditorGUILayout.Foldout(showBones, "Bones (Scene Gizmos)", true);
            if (showBones)
                PmxInspectorGui.DrawBones(asset, controller.BoneController?.Bones);
        }
    }
}
