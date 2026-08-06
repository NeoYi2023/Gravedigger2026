using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>Pure geometry for DigHitShape circle ∩ convex hull (SPEC_03 §3.10).</summary>
    public static class DigHitShapeMath
    {
        /// <summary>
        /// Test in local XZ: circle center transformed by subtracting worldOriginXZ.
        /// </summary>
        public static bool CircleIntersectsConvexPolygonLocal(
            Vector2 circleCenterWorldXZ,
            float circleRadius,
            Vector2[] polygonLocalXZ,
            Vector2 worldOriginXZ)
        {
            if (polygonLocalXZ == null || polygonLocalXZ.Length < 3 || circleRadius < 0f)
            {
                return false;
            }

            var localCenter = circleCenterWorldXZ - worldOriginXZ;
            if (PointInConvexPolygon(localCenter, polygonLocalXZ))
            {
                return true;
            }

            var rSq = circleRadius * circleRadius;
            for (var i = 0; i < polygonLocalXZ.Length; i++)
            {
                var a = polygonLocalXZ[i];
                var b = polygonLocalXZ[(i + 1) % polygonLocalXZ.Length];
                if (DistanceSqPointToSegment(localCenter, a, b) <= rSq)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CircleIntersectsCircle(
            Vector2 a,
            float ra,
            Vector2 b,
            float rb)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var min = ra + rb;
            return dx * dx + dy * dy <= min * min;
        }

        public static bool PointInConvexPolygon(Vector2 point, Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return false;
            }

            var sign = 0;
            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                var cross = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
                if (Mathf.Abs(cross) < 1e-6f)
                {
                    continue;
                }

                var s = cross > 0f ? 1 : -1;
                if (sign == 0)
                {
                    sign = s;
                }
                else if (s != sign)
                {
                    return false;
                }
            }

            return true;
        }

        public static float DistanceSqPointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = p - a;
            var abLenSq = ab.sqrMagnitude;
            if (abLenSq < 1e-12f)
            {
                return ap.sqrMagnitude;
            }

            var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abLenSq);
            var closest = a + ab * t;
            return (p - closest).sqrMagnitude;
        }
    }
}
