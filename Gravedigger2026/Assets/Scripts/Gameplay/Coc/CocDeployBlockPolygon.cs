using UnityEngine;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Authored deploy-block face (SPEC_04 §9.35, D-103).
    /// Child transforms in sibling order are the corners. No fill; the runtime view draws the edge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocDeployBlockPolygon : MonoBehaviour
    {
        public int PointCount => transform.childCount;

        private void OnDrawGizmos()
        {
            var count = transform.childCount;
            if (count < 2)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            for (var i = 0; i < count; i++)
            {
                var a = transform.GetChild(i).position;
                var b = transform.GetChild((i + 1) % count).position;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
