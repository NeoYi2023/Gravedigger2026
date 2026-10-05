using UnityEngine;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Authored fog face (SPEC_04 §9.35). Child transforms in sibling order are the corners.
    /// Slice 02 places the marker only; drawing is slice 04.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocFogPolygon : MonoBehaviour
    {
        public int PointCount => transform.childCount;

        private void OnDrawGizmos()
        {
            var count = transform.childCount;
            if (count < 2)
            {
                return;
            }

            Gizmos.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            for (var i = 0; i < count; i++)
            {
                var a = transform.GetChild(i).position;
                var b = transform.GetChild((i + 1) % count).position;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
