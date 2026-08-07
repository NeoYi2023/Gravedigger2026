using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PM-10: stagger PushMap spawn positions by BodyRadius footprint circles on NavMesh
    /// (SPEC_03 §3.14 / SPEC_04 §9.23). Ring first, then spiral outward; avoids existing living
    /// footprints; SamplePosition failures may shrink ring; final fallback samples basePos.
    /// </summary>
    public static class PushMapSpawnSpread
    {
        private const float MinRadius = 0.05f;
        private const float OverlapEpsilon = 0.02f;
        private const float NavMeshSampleDistance = 12f;
        private const int MaxSpiralSteps = 24;
        private const int ShrinkAttempts = 6;

        public readonly struct Footprint
        {
            public readonly Vector3 Position;
            public readonly float Radius;

            public Footprint(Vector3 position, float radius)
            {
                Position = position;
                Radius = Mathf.Max(MinRadius, radius);
            }
        }

        /// <summary>
        /// Computes <paramref name="count"/> walkable positions around <paramref name="basePos"/>
        /// that do not overlap each other or <paramref name="occupied"/> footprints.
        /// </summary>
        public static void ComputePositions(
            Vector3 basePos,
            int count,
            float bodyRadius,
            IReadOnlyList<Footprint> occupied,
            List<Vector3> results)
        {
            results.Clear();
            if (count <= 0)
            {
                return;
            }

            var radius = Mathf.Max(MinRadius, bodyRadius);
            var accepted = new List<Footprint>(count + (occupied?.Count ?? 0));
            if (occupied != null)
            {
                for (var i = 0; i < occupied.Count; i++)
                {
                    accepted.Add(occupied[i]);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var pos = ResolveOne(basePos, i, count, radius, accepted);
                results.Add(pos);
                accepted.Add(new Footprint(pos, radius));
            }
        }

        private static Vector3 ResolveOne(
            Vector3 basePos,
            int index,
            int total,
            float radius,
            List<Footprint> accepted)
        {
            if (TryPlaceAt(basePos, radius, accepted, out var atBase) && total == 1)
            {
                return atBase;
            }

            // Prefer a ring large enough that neighboring same-batch circles clear each other.
            var ringRadius = total <= 1
                ? 0f
                : radius * 2f / Mathf.Max(0.2f, 2f * Mathf.Sin(Mathf.PI / total));

            for (var shrink = 0; shrink < ShrinkAttempts; shrink++)
            {
                var scale = 1f - shrink * 0.12f;
                var r = ringRadius * scale;
                if (TryCandidate(basePos, index, total, r, radius, accepted, out var hit))
                {
                    return hit;
                }
            }

            // Spiral outward from base.
            for (var step = 0; step < MaxSpiralSteps; step++)
            {
                var dist = radius * (1.2f + step * 0.55f);
                var angle = index * 2.399963f + step * 0.7f; // golden-angle-ish
                var candidate = basePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                if (TryPlaceAt(candidate, radius, accepted, out var spiralHit))
                {
                    return spiralHit;
                }
            }

            // Final fallback: sample base even if overlapping.
            if (NavMesh.SamplePosition(basePos, out var navHit, NavMeshSampleDistance, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            return basePos;
        }

        private static bool TryCandidate(
            Vector3 basePos,
            int index,
            int total,
            float ringRadius,
            float bodyRadius,
            List<Footprint> accepted,
            out Vector3 placed)
        {
            Vector3 candidate;
            if (total <= 1 || ringRadius <= 0.001f)
            {
                candidate = basePos;
            }
            else
            {
                var angle = index * (Mathf.PI * 2f / total) - Mathf.PI * 0.5f;
                candidate = basePos + new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
            }

            return TryPlaceAt(candidate, bodyRadius, accepted, out placed);
        }

        private static bool TryPlaceAt(
            Vector3 candidate,
            float bodyRadius,
            List<Footprint> accepted,
            out Vector3 placed)
        {
            placed = candidate;
            if (!NavMesh.SamplePosition(candidate, out var hit, NavMeshSampleDistance, NavMesh.AllAreas))
            {
                return false;
            }

            placed = hit.position;
            return !OverlapsAny(placed, bodyRadius, accepted);
        }

        private static bool OverlapsAny(Vector3 pos, float radius, List<Footprint> accepted)
        {
            for (var i = 0; i < accepted.Count; i++)
            {
                var other = accepted[i];
                var minDist = radius + other.Radius - OverlapEpsilon;
                var dx = pos.x - other.Position.x;
                var dz = pos.z - other.Position.z;
                if (dx * dx + dz * dz < minDist * minDist)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
