using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    internal static class PmxBoneGizmoDrawer
    {
        private static readonly Color BoneColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        private static readonly Color JointColor = new Color(1f, 0.72f, 0.18f, 0.95f);

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawBones(PmxRuntimeController controller, GizmoType gizmoType)
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
                    if (parentIndex >= 0 && parentIndex < bones.Count && bones[parentIndex] != null)
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
    }
}
