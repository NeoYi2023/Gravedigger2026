using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// Authored deploy-block polygons (SPEC_03 §3.21, D-103).
    /// Inside a polygon and outside every activated capture circle, deploy is blocked.
    /// Does not read the fog grid.
    /// </summary>
    public sealed class CocDeployBlockArea
    {
        public readonly struct ActivatedCircle
        {
            public readonly float X;
            public readonly float Z;
            public readonly float Radius;

            public ActivatedCircle(float x, float z, float radius)
            {
                X = x;
                Z = z;
                Radius = radius;
            }
        }

        private readonly List<Vector2[]> _polygons;

        private CocDeployBlockArea(List<Vector2[]> polygons)
        {
            _polygons = polygons ?? new List<Vector2[]>(0);
        }

        public bool HasCoverage => _polygons.Count > 0;

        public IReadOnlyList<Vector2[]> Polygons => _polygons;

        public static CocDeployBlockArea Build(IReadOnlyList<Vector2[]> polygons)
        {
            var copy = new List<Vector2[]>(polygons != null ? polygons.Count : 0);
            if (polygons != null)
            {
                for (var i = 0; i < polygons.Count; i++)
                {
                    var poly = polygons[i];
                    if (poly == null || poly.Length < 3)
                    {
                        continue;
                    }

                    copy.Add(poly);
                }
            }

            return new CocDeployBlockArea(copy);
        }

        /// <summary>
        /// True when the point is inside a polygon and not inside any activated capture circle.
        /// </summary>
        public bool BlocksDeploy(float worldX, float worldZ, IReadOnlyList<ActivatedCircle> activatedCircles)
        {
            if (!ContainsAny(worldX, worldZ))
            {
                return false;
            }

            if (activatedCircles != null)
            {
                for (var i = 0; i < activatedCircles.Count; i++)
                {
                    var circle = activatedCircles[i];
                    if (circle.Radius <= 0f)
                    {
                        continue;
                    }

                    var dx = worldX - circle.X;
                    var dz = worldZ - circle.Z;
                    if (dx * dx + dz * dz <= circle.Radius * circle.Radius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ContainsAny(float x, float z)
        {
            for (var i = 0; i < _polygons.Count; i++)
            {
                if (Contains(_polygons[i], x, z))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(Vector2[] poly, float x, float z)
        {
            var inside = false;
            var j = poly.Length - 1;
            for (var i = 0; i < poly.Length; j = i++)
            {
                var yi = poly[i].y;
                var yj = poly[j].y;
                if ((yi > z) == (yj > z))
                {
                    continue;
                }

                var xi = poly[i].x;
                var xj = poly[j].x;
                var cross = (xj - xi) * (z - yi) / (yj - yi) + xi;
                if (x < cross)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
