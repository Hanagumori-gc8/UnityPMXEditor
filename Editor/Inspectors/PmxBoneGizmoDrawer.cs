using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [InitializeOnLoad]
    internal static class PmxBoneGizmoDrawer
    {
        private const float BonePickRadius = 12f;
        private static readonly Color BoneColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        private static readonly Color JointColor = new Color(1f, 0.72f, 0.18f, 0.95f);

        static PmxBoneGizmoDrawer()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            if (!sceneView.drawGizmos) return;
            PmxRuntimeController controller = ResolveController(Selection.activeTransform);
            if (controller == null) return;
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && !current.alt &&
                TrySelectBoneAtGuiPosition(controller, current.mousePosition))
                current.Use();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        private static void DrawGizmo(PmxRuntimeController controller, GizmoType gizmoType)
        {
            if (!ShouldDraw(controller, Selection.activeTransform)) return;
            DrawBones(controller);
        }

        private static void DrawBones(PmxRuntimeController controller)
        {
            PmxModelAsset asset = controller.ModelAsset;
            IReadOnlyList<Transform> bones = controller.BoneController?.Bones;
            if (asset == null || bones == null || bones.Count != asset.BoneMetadata.Length) return;

            Color previousColor = Handles.color;
            UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;
            try
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                for (int i = 0; i < bones.Count; i++)
                {
                    Transform bone = bones[i];
                    if (bone == null) continue;
                    int parentIndex = asset.BoneMetadata[i].ParentBoneIndex;
                    if (parentIndex >= 0 && parentIndex < bones.Count &&
                        bones[parentIndex] != null)
                    {
                        Handles.color = BoneColor;
                        Handles.DrawAAPolyLine(3f, bones[parentIndex].position, bone.position);
                    }

                    Handles.color = JointColor;
                    float size = HandleUtility.GetHandleSize(bone.position) * 0.035f;
                    Handles.SphereHandleCap(0, bone.position, Quaternion.identity,
                        size, EventType.Repaint);
                }
            }
            finally
            {
                Handles.color = previousColor;
                Handles.zTest = previousZTest;
            }
        }

        internal static bool TrySelectBoneAtGuiPosition(PmxRuntimeController controller,
            Vector2 mousePosition)
        {
            IReadOnlyList<Transform> bones = controller?.BoneController?.Bones;
            if (bones == null) return false;

            Transform closest = null;
            float closestDistance = BonePickRadius;
            for (int i = 0; i < bones.Count; i++)
            {
                Transform bone = bones[i];
                if (bone == null) continue;
                Vector3 guiPoint = HandleUtility.WorldToGUIPointWithDepth(bone.position);
                if (guiPoint.z < 0f) continue;
                float distance = Vector2.Distance(mousePosition,
                    new Vector2(guiPoint.x, guiPoint.y));
                if (distance > closestDistance) continue;
                closestDistance = distance;
                closest = bone;
            }
            if (closest == null) return false;

            Selection.activeTransform = closest;
            EditorGUIUtility.PingObject(closest);
            SceneView.RepaintAll();
            return true;
        }

        internal static bool ShouldDraw(PmxRuntimeController controller, Transform selection)
        {
            return controller != null && ResolveController(selection) == controller;
        }

        internal static PmxRuntimeController ResolveController(Transform selection)
        {
            if (selection == null || !selection.gameObject.scene.IsValid()) return null;
            PmxRuntimeController controller =
                selection.GetComponentInParent<PmxRuntimeController>();
            if (controller == null)
                controller = selection.GetComponentInChildren<PmxRuntimeController>(true);
            return controller;
        }
    }
}
