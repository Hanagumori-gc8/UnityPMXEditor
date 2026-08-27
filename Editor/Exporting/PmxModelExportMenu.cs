using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public static class PmxModelExportMenu
    {
        private const string FbxMenu = "Assets/UnityPMXEditor/Export Selected as FBX...";
        private const string ObjMenu = "Assets/UnityPMXEditor/Export Selected as OBJ...";

        [MenuItem(FbxMenu, false, 2000)]
        private static void ExportSelectedFbx() =>
            ExportWithDialog(ResolveSelectedRoot(), PmxModelExportFormat.Fbx);

        [MenuItem(FbxMenu, true)]
        private static bool ValidateExportSelectedFbx() => ResolveSelectedRoot() != null;

        [MenuItem(ObjMenu, false, 2001)]
        private static void ExportSelectedObj() =>
            ExportWithDialog(ResolveSelectedRoot(), PmxModelExportFormat.Obj);

        [MenuItem(ObjMenu, true)]
        private static bool ValidateExportSelectedObj() => ResolveSelectedRoot() != null;

        public static void ExportWithDialog(GameObject root, PmxModelExportFormat format)
        {
            if (root == null)
            {
                EditorUtility.DisplayDialog("UnityPMXEditor Export",
                    "Select an imported PMX model asset, its PmxModelAsset sub-asset, " +
                    "or a PMX model instance.", "OK");
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(root);
            string directory = string.IsNullOrEmpty(sourcePath)
                ? Application.dataPath
                : Path.GetDirectoryName(Path.GetFullPath(sourcePath));
            string extension = format == PmxModelExportFormat.Fbx ? "fbx" : "obj";
            string path = EditorUtility.SaveFilePanel(
                "Export PMX model as " + extension.ToUpperInvariant(), directory,
                SanitizeFileName(root.name), extension);
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                PmxModelExportResult result = PmxModelExporter.Export(root, path, format);
                EditorUtility.DisplayDialog("UnityPMXEditor Export",
                    $"Exported {result.PartCount} parts, {result.VertexCount} vertices and " +
                    $"{result.TriangleCount} triangles to:\n{result.ModelPath}", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("UnityPMXEditor Export Failed", exception.Message, "OK");
            }
        }

        internal static GameObject ResolveSelectedRoot()
        {
            UnityEngine.Object selected = Selection.activeObject;
            if (selected is GameObject gameObject) return FindPmxRoot(gameObject);
            if (selected is Component component) return FindPmxRoot(component.gameObject);
            if (selected is PmxModelAsset metadata)
            {
                string path = AssetDatabase.GetAssetPath(metadata);
                return AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
            }
            return null;
        }

        internal static GameObject FindPmxRoot(GameObject value)
        {
            if (value == null) return null;
            PmxRuntimeController controller = value.GetComponentInParent<PmxRuntimeController>();
            if (controller == null) controller = value.GetComponentInChildren<PmxRuntimeController>(true);
            return controller != null ? controller.gameObject : null;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "PMX_Model";
            char[] invalid = Path.GetInvalidFileNameChars();
            var characters = value.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (Array.IndexOf(invalid, characters[i]) >= 0) characters[i] = '_';
            }
            return new string(characters);
        }
    }
}
