#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Formation;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Formation
{
    /// <summary>
    /// Draws IsoDiamond authoring frame + child diamonds when the zones root
    /// or any child is in the Scene selection hierarchy (SPEC_04 §13).
    /// </summary>
    public static class FormationClassZonesRootGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        public static void DrawAuthoringFrame(FormationClassZonesRoot root, GizmoType gizmoType)
        {
            if (root == null)
            {
                return;
            }

            var selected = (gizmoType & GizmoType.Selected) != 0;
            var color = selected
                ? new Color(0.25f, 0.75f, 0.95f, 0.95f)
                : new Color(0.25f, 0.75f, 0.95f, 0.6f);
            root.DrawIsoDiamondGizmo(color);
            if (selected)
            {
                root.DrawChildZoneGizmos();
            }
        }
    }
}
#endif
