using UnityEngine;

namespace Gravedigger2026.Gameplay.Maps
{
    /// <summary>
    /// Shared IsoDiamond footprint on XZ (SPEC_03 §3.10 / §3.12, SPEC_04 §9.7 v0.46.2).
    /// Half-extents = PaintRadius*(cellSize.x, cellSize.y); may be anisotropic.
    /// Contains = |dx|/hx + |dz|/hz &lt;= 1.
    /// </summary>
    public static class MapFootprintMath
    {
        /// <summary>Demo Grid cell size matching Ground_* Prefabs (Isometric).</summary>
        public static readonly Vector3 DemoIsoCellSize = new Vector3(1f, 0.5f, 2f);

        /// <summary>Map-scale IsoDiamond (WalkSurface / DigMapBounds / EngageZone).</summary>
        public const float DefaultMinHalfExtent = 0.5f;

        /// <summary>FormationClassZone and other sub-map markers (SPEC_04 §13).</summary>
        public const float MarkerMinHalfExtent = 0.05f;

        /// <summary>
        /// Yaw (degrees) that aligns Transform local +X with the isometric Grid X-axis on XZ
        /// after GroundTilemap RotX 90°. Demo cellSize (1, 0.5) → ≈ -26.57°.
        /// FormationClassZone no longer uses this (identity IsoDiamond, same as WalkSurface).
        /// </summary>
        public static float IsoTileYawYDegrees(Vector3 cellSize)
        {
            var x = Mathf.Max(0.01f, cellSize.x);
            var y = Mathf.Max(0.01f, cellSize.y);
            return -Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        }

        public static Vector2 SanitizeHalfExtents(Vector2 halfExtents)
        {
            return SanitizeHalfExtents(halfExtents, DefaultMinHalfExtent);
        }

        public static Vector2 SanitizeHalfExtents(Vector2 halfExtents, float minExtent)
        {
            var min = Mathf.Max(0.01f, minExtent);
            return new Vector2(
                Mathf.Max(min, halfExtents.x),
                Mathf.Max(min, halfExtents.y));
        }

        /// <summary>
        /// World IsoDiamond half-extents from painted iso cell range.
        /// Unity iso: max |X| = R*cellSize.x, max |Z| = R*cellSize.y (after Grid RotX 90°).
        /// </summary>
        public static Vector2 HalfExtentsFromIsoCell(int paintRadius, Vector3 cellSize)
        {
            var r = Mathf.Max(1, paintRadius);
            return SanitizeHalfExtents(new Vector2(
                r * Mathf.Max(0.01f, cellSize.x),
                r * Mathf.Max(0.01f, cellSize.y)));
        }

        public static bool ContainsXZ(Vector3 center, Vector2 halfExtents, Vector3 worldPosition)
        {
            return ContainsXZ(center, halfExtents, worldPosition, DefaultMinHalfExtent);
        }

        public static bool ContainsXZ(
            Vector3 center,
            Vector2 halfExtents,
            Vector3 worldPosition,
            float minExtent)
        {
            var half = SanitizeHalfExtents(halfExtents, minExtent);
            var dx = Mathf.Abs(worldPosition.x - center.x);
            var dz = Mathf.Abs(worldPosition.z - center.z);
            return dx / half.x + dz / half.y <= 1f;
        }

        /// <summary>
        /// Clock hour 1–12: 12 = +Z, 3 = +X. Point on diamond rim at rimScale (1 = vertex distance).
        /// </summary>
        public static Vector3 PointOnClockHour(
            Vector3 center,
            Vector2 halfExtents,
            int hour,
            float rimScale = 0.9f)
        {
            var half = SanitizeHalfExtents(halfExtents);
            hour = Mathf.Clamp(hour, 1, 12);
            rimScale = Mathf.Max(0.01f, rimScale);

            var angleDeg = (12 - hour) * 30f;
            var rad = angleDeg * Mathf.Deg2Rad;
            var dx = Mathf.Sin(rad);
            var dz = Mathf.Cos(rad);
            var denom = Mathf.Abs(dx) / half.x + Mathf.Abs(dz) / half.y;
            if (denom < 1e-5f)
            {
                return center + new Vector3(0f, 0f, half.y * rimScale);
            }

            var t = rimScale / denom;
            return center + new Vector3(dx * t, 0f, dz * t);
        }

        /// <summary>
        /// Thin XZ Manhattan-diamond mesh (vertices on axes). No Y rotation required.
        /// </summary>
        public static Mesh BuildDiamondMesh(Vector2 halfExtents, float thickness = 0.05f)
        {
            return BuildDiamondMesh(halfExtents, thickness, DefaultMinHalfExtent);
        }

        public static Mesh BuildDiamondMesh(Vector2 halfExtents, float thickness, float minExtent)
        {
            var half = SanitizeHalfExtents(halfExtents, minExtent);
            var y = Mathf.Max(0.01f, thickness) * 0.5f;
            var mesh = new Mesh { name = "IsoDiamondWalkSurface" };

            // Top + bottom diamond (N,E,S,W)
            var verts = new Vector3[8];
            verts[0] = new Vector3(0f, y, half.y);
            verts[1] = new Vector3(half.x, y, 0f);
            verts[2] = new Vector3(0f, y, -half.y);
            verts[3] = new Vector3(-half.x, y, 0f);
            verts[4] = new Vector3(0f, -y, half.y);
            verts[5] = new Vector3(half.x, -y, 0f);
            verts[6] = new Vector3(0f, -y, -half.y);
            verts[7] = new Vector3(-half.x, -y, 0f);

            var tris = new[]
            {
                // Top
                0, 1, 2, 0, 2, 3,
                // Bottom
                4, 6, 5, 4, 7, 6,
                // Sides
                0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3,
                3, 7, 4, 3, 4, 0
            };

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void ApplyWalkSurfaceTransform(Transform walk)
        {
            if (walk == null)
            {
                return;
            }

            walk.localPosition = new Vector3(0f, -0.05f, 0f);
            walk.localRotation = Quaternion.identity;
            walk.localScale = Vector3.one;
        }

        /// <summary>NavMesh box that tightly covers the diamond AABB (not a 45° square).</summary>
        public static Vector3 NavMeshCoverBoxSize(Vector2 halfExtents, float pad = 1.05f, float height = 0.25f)
        {
            var half = SanitizeHalfExtents(halfExtents);
            pad = Mathf.Max(1f, pad);
            return new Vector3(
                Mathf.Max(2f, half.x * 2f * pad),
                Mathf.Max(0.05f, height),
                Mathf.Max(2f, half.y * 2f * pad));
        }

        public static void DrawDiamondGizmo(Vector3 center, Vector2 halfExtents, Color color, float y = 0.05f)
        {
            DrawDiamondGizmo(center, halfExtents, color, y, DefaultMinHalfExtent);
        }

        public static void DrawDiamondGizmo(
            Vector3 center,
            Vector2 halfExtents,
            Color color,
            float y,
            float minExtent)
        {
            var half = SanitizeHalfExtents(halfExtents, minExtent);
            var c = new Vector3(center.x, center.y + y, center.z);
            var n = c + new Vector3(0f, 0f, half.y);
            var e = c + new Vector3(half.x, 0f, 0f);
            var s = c + new Vector3(0f, 0f, -half.y);
            var w = c + new Vector3(-half.x, 0f, 0f);
            Gizmos.color = color;
            Gizmos.DrawLine(n, e);
            Gizmos.DrawLine(e, s);
            Gizmos.DrawLine(s, w);
            Gizmos.DrawLine(w, n);
        }
    }
}
