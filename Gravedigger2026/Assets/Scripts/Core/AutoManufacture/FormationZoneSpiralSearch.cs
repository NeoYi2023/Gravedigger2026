using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// In-zone spiral/ring slot search with BodyRadius separation (SPEC_03 §3.15 AM-06 / FZ-01).
    /// Operates in map-center-relative XZ; zone test is IsoDiamond.
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
        /// Finds first free slot in zone; center must lie in IsoDiamond shrunk by bodyRadius.
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

        /// <summary>
        /// Randomized candidate traversal inside zone (SPEC_03 D-074):
        /// keep IsoDiamond safe check + Footprint separation, but shuffle candidate points order.
        /// </summary>
        public static bool TryFindRandomSlot(
            FormationClassZoneSnapshot zone,
            float bodyRadius,
            IReadOnlyList<Footprint> occupied,
            System.Random rng,
            out float relX,
            out float relZ)
        {
            relX = 0f;
            relZ = 0f;
            if (zone.Equals(default(FormationClassZoneSnapshot)) && string.IsNullOrEmpty(zone.ClassId))
            {
                return false;
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var radius = Mathf.Max(MinRadius, bodyRadius);
            var step = Mathf.Max(radius * 1.75f, 0.12f);
            var angleOffset = (float)(rng.NextDouble() * Mathf.PI * 2f);

            // Upper bound: center + sum(ring=1..MaxRings) (BasePointsPerRing + (ring-1)*2)
            var candidateCount = 1;
            for (var ring = 1; ring <= MaxRings; ring++)
            {
                candidateCount += BasePointsPerRing + (ring - 1) * 2;
            }

            var candidates = new List<Footprint>(candidateCount);
            candidates.Add(new Footprint(zone.CenterRelX, zone.CenterRelZ, radius));

            for (var ring = 1; ring <= MaxRings; ring++)
            {
                var ringRadius = step * ring;
                var points = BasePointsPerRing + (ring - 1) * 2;
                for (var i = 0; i < points; i++)
                {
                    var angle = angleOffset + (Mathf.PI * 2f) * i / points;
                    var x = zone.CenterRelX + Mathf.Cos(angle) * ringRadius;
                    var z = zone.CenterRelZ + Mathf.Sin(angle) * ringRadius;
                    candidates.Add(new Footprint(x, z, radius));
                }
            }

            // Fisher–Yates shuffle.
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            for (var idx = 0; idx < candidates.Count; idx++)
            {
                var c = candidates[idx];
                if (TryAccept(zone, c.X, c.Z, radius, occupied))
                {
                    relX = c.X;
                    relZ = c.Z;
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
            if (!IsCenterInsideSafeDiamond(zone, x, z, radius))
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

        private static bool IsCenterInsideSafeDiamond(
            FormationClassZoneSnapshot zone,
            float x,
            float z,
            float radius)
        {
            var dx = Mathf.Abs(x - zone.CenterRelX);
            var dz = Mathf.Abs(z - zone.CenterRelZ);
            var hx = Mathf.Max(0f, zone.HalfExtentX - radius);
            var hz = Mathf.Max(0f, zone.HalfExtentZ - radius);
            if (hx < 1e-4f || hz < 1e-4f)
            {
                var rawHx = Mathf.Max(1e-4f, zone.HalfExtentX);
                var rawHz = Mathf.Max(1e-4f, zone.HalfExtentZ);
                return dx / rawHx + dz / rawHz <= 1f
                       && dx < 1e-3f
                       && dz < 1e-3f;
            }

            return dx / hx + dz / hz <= 1f;
        }
    }
}
