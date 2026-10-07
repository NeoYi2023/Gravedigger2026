using UnityEngine;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Authored fog face (SPEC_04 §9.35). Child transforms in sibling order are the corners.
    /// Same FogGroupId unions into one island. Slice 02 places the marker only; drawing is slice 04.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocFogPolygon : MonoBehaviour
    {
        [SerializeField] private int fogGroupId;

        public int PointCount => transform.childCount;

        public int FogGroupId => fogGroupId;

        private void OnDrawGizmos()
        {
            var count = transform.childCount;
            if (count < 2)
            {
                return;
            }

            Gizmos.color = GroupGizmoColor(fogGroupId);
            for (var i = 0; i < count; i++)
            {
                var a = transform.GetChild(i).position;
                var b = transform.GetChild((i + 1) % count).position;
                Gizmos.DrawLine(a, b);
            }
        }

        private static Color GroupGizmoColor(int groupId)
        {
            var hue = (groupId * 0.137f) % 1f;
            if (hue < 0f)
            {
                hue += 1f;
            }

            return Color.HSVToRGB(hue, 0.55f, 0.35f);
        }
    }
}
