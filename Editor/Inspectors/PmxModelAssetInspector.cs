using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [CustomEditor(typeof(PmxModelAsset))]
    public sealed class PmxModelAssetInspector : UnityEditor.Editor
    {
        private bool showParts = true;
        private bool showBones;
        private bool showMorphs = true;
        private bool showDiagnostics = true;

        public override void OnInspectorGUI()
        {
            var asset = (PmxModelAsset)target;
            EditorGUILayout.LabelField("PMX Model Asset", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Schema Version", asset.SchemaVersion.ToString());
            EditorGUILayout.LabelField("PMX Version", asset.PmxVersion.ToString("0.0"));
            EditorGUILayout.LabelField("Original Name", asset.ModelName ?? string.Empty);
            EditorGUILayout.LabelField("English Name", asset.EnglishModelName ?? string.Empty);
            EditorGUILayout.LabelField("Vertices", asset.VertexCount.ToString());
            EditorGUILayout.LabelField("Surfaces", asset.SurfaceCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Support Status", EditorStyles.boldLabel);
            DrawStatus(asset, PmxFeatureSupportStatus.Supported, new Color(0.25f, 0.65f, 0.35f));
            DrawStatus(asset, PmxFeatureSupportStatus.Approximated, new Color(0.85f, 0.65f, 0.2f));
            DrawStatus(asset, PmxFeatureSupportStatus.Preserved, new Color(0.3f, 0.55f, 0.8f));
            DrawStatus(asset, PmxFeatureSupportStatus.Rejected, new Color(0.8f, 0.25f, 0.25f));
            DrawStatus(asset, PmxFeatureSupportStatus.Unsupported, new Color(0.55f, 0.55f, 0.55f));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preserved Sections", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Bones / IK", asset.BoneMetadata.Length.ToString());
            EditorGUILayout.LabelField("Materials", asset.MaterialMetadata.Length.ToString());
            EditorGUILayout.LabelField("Morphs", asset.MorphMetadata.Length.ToString());
            EditorGUILayout.LabelField("Display Frames", asset.DisplayFrameMetadata.Length.ToString());
            EditorGUILayout.LabelField("Rigid Bodies", asset.RigidBodyMetadata.Length.ToString());
            EditorGUILayout.LabelField("Joints", asset.JointMetadata.Length.ToString());
            EditorGUILayout.LabelField("Soft Bodies", asset.SoftBodyMetadata.Length.ToString());
            EditorGUILayout.LabelField("Physics Import", asset.PhysicsImportMode.ToString());

            EditorGUILayout.Space();
            DrawExportButtons(asset);

            showParts = EditorGUILayout.Foldout(showParts, "Model Parts (Material Submeshes)", true);
            if (showParts) PmxInspectorGui.DrawParts(asset);

            showBones = EditorGUILayout.Foldout(showBones, "Bones", true);
            if (showBones) PmxInspectorGui.DrawBones(asset, null);

            showMorphs = EditorGUILayout.Foldout(showMorphs, "Morph Support", true);
            if (showMorphs)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < asset.MorphMetadata.Length; i++)
                {
                    PmxMorphMetadata morph = asset.MorphMetadata[i];
                    string sourceName = string.IsNullOrEmpty(morph.EnglishName) ? morph.Name : morph.EnglishName;
                    EditorGUILayout.LabelField($"{i:D4} {sourceName}",
                        $"{morph.SupportStatus}  BlendShape={morph.BlendShapeIndex}");
                }
                EditorGUI.indentLevel--;
            }

            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, "Import Diagnostics", true);
            if (showDiagnostics)
            {
                EditorGUI.indentLevel++;
                foreach (PmxImportDiagnostic diagnostic in asset.Diagnostics)
                {
                    MessageType type = diagnostic.Severity == PmxDiagnosticSeverity.Error
                        ? MessageType.Error
                        : diagnostic.Severity == PmxDiagnosticSeverity.Warning
                            ? MessageType.Warning
                            : MessageType.Info;
                    EditorGUILayout.HelpBox(
                        $"[{diagnostic.Status}] {diagnostic.Code}: {diagnostic.Message}", type);
                }
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawExportButtons(PmxModelAsset asset)
        {
            GameObject root = ResolveImportedRoot(asset);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = root != null;
            try
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Export FBX..."))
                    PmxModelExportMenu.ExportWithDialog(root, PmxModelExportFormat.Fbx);
                if (GUILayout.Button("Export OBJ..."))
                    PmxModelExportMenu.ExportWithDialog(root, PmxModelExportFormat.Obj);
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Instantiate Editable Scene Model"))
                {
                    GameObject instance = PmxModelExportMenu.InstantiateInScene(root);
                    Selection.activeGameObject = instance;
                }
            }
            finally
            {
                GUI.enabled = previousEnabled;
            }
        }

        internal static GameObject ResolveImportedRoot(PmxModelAsset asset)
        {
            if (asset == null) return null;
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return null;
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (root != null) return root;
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<GameObject>()
                .FirstOrDefault(value => value.GetComponent<PmxRuntimeController>() != null);
        }

        private static void DrawStatus(PmxModelAsset asset, PmxFeatureSupportStatus status, Color color)
        {
            int count = asset.Diagnostics.Count(value => value.Status == status);
            Color previous = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(status.ToString(), count.ToString(), EditorStyles.helpBox);
            GUI.color = previous;
        }
    }
}
