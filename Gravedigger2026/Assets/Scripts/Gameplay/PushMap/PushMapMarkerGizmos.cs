using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Shared editor gizmos for PushMap map markers (SPEC_04 §9.22).
    /// </summary>
    public static class PushMapMarkerGizmos
    {
        public static void DrawCircleXZ(Vector3 center, float radius, Color color, int segments = 48)
        {
            if (radius < 0.01f || segments < 3)
            {
                return;
            }

            Gizmos.color = color;
            var prev = center + new Vector3(radius, 0f, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var ang = i * Mathf.PI * 2f / segments;
                var next = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
