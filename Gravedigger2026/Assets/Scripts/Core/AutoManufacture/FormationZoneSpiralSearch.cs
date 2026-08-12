using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// In-zone spiral/ring slot search with BodyRadius separation (SPEC_03 §3.15 AM-06 Approach A).
    /// Operates in map-center-relative XZ; zone test is local OBB (RotationYDegrees).
    /// </summary>
    public static class FormationZoneSpiralSearch
    {
        public const float MinRadius = 0.05f;
        public const float OverlapEpsilon = 0.02f;
        public const int MaxRings = 16;
        public const int BasePointsPerRing = 6;

        public readonly struct Footprint
        {
            public readonly float X;
            public readonly float Z;
            public readonly float Radius;

            public Footprint(float x, float z, float radius)
            {
                X = x;
                Z = z;
                Radius = Mathf.Max(MinRadius, radius);
            }
        }

        /// <summary>
        /// Finds first free slot in zone; center must lie in OBB shrunk by bodyRadius.
        /// </summary>
        public static bool TryFindSlot(
            FormationClassZoneSnapshot zone,
            float bodyRadius,
            IReadOnlyList<Footprint> occupied,
            out float relX,
            out float relZ)
        {
            relX = 0f;
            relZ = 0f;
            var radius = Mathf.Max(MinRadius, bodyRadius);
            var step = Mathf.Max(radius * 1.75f, 0.12f);

            if (TryAccept(zone, zone.CenterRelX, zone.CenterRelZ, radius, occupied))
            {
                relX = zone.CenterRelX;
                relZ = zone.CenterRelZ;
                return true;
            }

            for (var ring = 1; ring <= MaxRings; ring++)
            {
                var ringRadius = step * ring;
                var points = BasePointsPerRing + (ring - 1) * 2;
                for (var i = 0; i < points; i++)
                {
                    var angle = (Mathf.PI * 2f) * i / points;
                    var x = zone.CenterRelX + Mathf.Cos(angle) * ringRadius;
                    var z = zone.CenterRelZ + Mathf.Sin(angle) * ringRadius;
                    if (!TryAccept(zone, x, z, radius, occupied))
                    {
                        continue;
                    }

                    relX = x;
                    relZ = z;
                    return true;
                }
            }

            return false;
        }

        private static bool TryAccept(
            FormationClassZoneSnapshot zone,
            float x,
            float z,
            float radius,
            IReadOnlyList<Footprint> occupied)
        {
            if (!IsCenterInsideSafeBox(zone, x, z, radius))
            {
                return false;
            }

            if (occupied == null)
            {
                return true;
            }

            for (var i = 0; i < occupied.Count; i++)
            {
                var other = occupied[i];
                var dx = x - other.X;
                var dz = z - other.Z;
                var minDist = radius + other.Radius + OverlapEpsilon;
                if (dx * dx + dz * dz < minDist * minDist)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsCenterInsideSafeBox(
            FormationClassZoneSnapshot zone,
            float x,
            float z,
            float radius)
        {
            ToLocalXZ(zone, x, z, out var localX, out var localZ);

            var hx = Mathf.Max(0f, zone.HalfExtentX - radius);
            var hz = Mathf.Max(0f, zone.HalfExtentZ - radius);
            if (hx < 1e-4f || hz < 1e-4f)
            {
                // Zone too small to shrink — only allow exact center if it is inside raw OBB.
                return Mathf.Abs(localX) <= zone.HalfExtentX
                       && Mathf.Abs(localZ) <= zone.HalfExtentZ
                       && Mathf.Abs(localX) < 1e-3f
                       && Mathf.Abs(localZ) < 1e-3f;
            }

            return Mathf.Abs(localX) <= hx && Mathf.Abs(localZ) <= hz;
        }

        /// <summary>
        /// World/map-relative XZ → zone-local XZ (inverse of Unity Y euler).
        /// </summary>
        public static void ToLocalXZ(
            FormationClassZoneSnapshot zone,
            float x,
            float z,
            out float localX,
            out float localZ)
        {
            var dx = x - zone.CenterRelX;
            var dz = z - zone.CenterRelZ;
            var rad = zone.RotationYDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            localX = dx * cos - dz * sin;
            localZ = dx * sin + dz * cos;
        }
    }
}
