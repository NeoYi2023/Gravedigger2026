using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PM-10 / v0.73.9 / v0.84.122 / v0.84.137: stagger spawn positions by BodyRadius footprint
    /// circles on NavMesh (SPEC_03 §3.14 / SPEC_04 §9.23). Snaps marker basePos onto NavMesh first
    /// (tall vertical recover, local XZ). Filled golden-angle disk from the spawn center
    /// (no equal-angle hollow ring) plus small jitter; then spiral search. Avoids existing living
    /// footprints. SamplePosition is local-only and leashed to basePos so hits cannot snap across
    /// AirWalls onto empty outer-diamond NavMesh; packed batches prefer overlap at base.
    /// Shared by PushMap, SearchExtract, and COC.
    /// </summary>
    public static class PushMapSpawnSpread
    {
        private const float MinRadius = 0.05f;
        private const float OverlapEpsilon = 0.02f;
        private const int MaxSpiralSteps = 24;
        private const float PackMul = 1.85f;
        private const float GoldenAngle = 2.399963f;
        private const float JitterAngle = 0.35f;
        private const float JitterRadiusMin = 0.9f;
        private const float JitterRadiusMax = 1.1f;

        private static float MinSampleDistance => CombatRuntimeTuning.PushMapSpawnMinSampleDistance;
        private static float SampleDistanceBodyMul => CombatRuntimeTuning.PushMapSpawnSampleDistanceBodyMul;
        private static float LeashSlack => CombatRuntimeTuning.PushMapSpawnLeashSlack;
        private static float AbsoluteLeashFloor => CombatRuntimeTuning.PushMapSpawnAbsoluteLeashFloor;
        private static float AbsoluteLeashBodyMul => CombatRuntimeTuning.PushMapSpawnAbsoluteLeashBodyMul;

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
            var sampleDistance = Mathf.Max(MinSampleDistance, radius * SampleDistanceBodyMul);
            var absoluteLeash = Mathf.Max(AbsoluteLeashFloor, radius * AbsoluteLeashBodyMul);
            // v0.84.137: recover Y-misauthored markers before filled-disk search.
            basePos = SnapBaseOntoNavMesh(basePos, sampleDistance, absoluteLeash);
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
                var pos = ResolveOne(basePos, i, count, radius, sampleDistance, absoluteLeash, accepted);
                results.Add(pos);
                accepted.Add(new Footprint(pos, radius));
            }
        }

        /// <summary>
        /// Projects <paramref name="basePos"/> onto walkable NavMesh. Uses a tall sample for Y
        /// recover when the marker sits below/above the mesh, but rejects hits that stray beyond
        /// the local XZ sample radius (no lateral AirWall snap).
        /// </summary>
        private static Vector3 SnapBaseOntoNavMesh(
            Vector3 basePos,
            float sampleDistance,
            float absoluteLeash)
        {
            if (TrySampleNear(basePos, basePos, sampleDistance, sampleDistance, out var localHit))
            {
                return localHit;
            }

            var tall = Mathf.Max(sampleDistance, absoluteLeash);
            if (!SpecialMoveNavMesh.SampleWalkable(
                    basePos,
                    tall,
                    SpecialMoveNavMesh.DefaultAgentTypeId,
                    out var hit))
            {
                return basePos;
            }

            var dx = hit.position.x - basePos.x;
            var dz = hit.position.z - basePos.z;
            if (dx * dx + dz * dz > sampleDistance * sampleDistance)
            {
                return basePos;
            }

            return hit.position;
        }

        private static Vector3 ResolveOne(
            Vector3 basePos,
            int index,
            int total,
            float radius,
            float sampleDistance,
            float absoluteLeash,
            List<Footprint> accepted)
        {
            if ((index == 0 || total == 1) &&
                TryPlaceAt(basePos, radius, basePos, absoluteLeash, sampleDistance, accepted, out var atBase))
            {
                return atBase;
            }

            if (index > 0)
            {
                var packAngle = index * GoldenAngle + Random.Range(-JitterAngle, JitterAngle);
                var packDist = radius * PackMul * Mathf.Sqrt(index) *
                    Random.Range(JitterRadiusMin, JitterRadiusMax);
                if (packDist <= absoluteLeash)
                {
                    var packed = basePos + new Vector3(
                        Mathf.Cos(packAngle) * packDist,
                        0f,
                        Mathf.Sin(packAngle) * packDist);
                    var packLeash = Mathf.Min(absoluteLeash, packDist + radius + LeashSlack);
                    if (TryPlaceAt(packed, radius, basePos, packLeash, sampleDistance, accepted, out var packHit))
                    {
                        return packHit;
                    }
                }
            }

            // Local spiral search if the packed slot is blocked; never beyond absolute leash.
            for (var step = 0; step < MaxSpiralSteps; step++)
            {
                var dist = radius * (1.2f + step * 0.55f);
                if (dist > absoluteLeash)
                {
                    break;
                }

                var angle = index * GoldenAngle + step * 0.7f;
                var candidate = basePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                var leash = Mathf.Min(absoluteLeash, dist + radius + LeashSlack);
                if (TryPlaceAt(candidate, radius, basePos, leash, sampleDistance, accepted, out var spiralHit))
                {
                    return spiralHit;
                }
            }

            // Final fallback: sample base even if overlapping (prefer pile-up over outer snap).
            if (TrySampleNear(basePos, basePos, absoluteLeash, sampleDistance, out var navHit))
            {
                return navHit;
            }

            return basePos;
        }

        private static bool TryPlaceAt(
            Vector3 candidate,
            float bodyRadius,
            Vector3 basePos,
            float leashFromBase,
            float sampleDistance,
            List<Footprint> accepted,
            out Vector3 placed)
        {
            placed = candidate;
            if (!TrySampleNear(candidate, basePos, leashFromBase, sampleDistance, out placed))
            {
                return false;
            }

            return !OverlapsAny(placed, bodyRadius, accepted);
        }

        /// <summary>
        /// Local SamplePosition around <paramref name="candidate"/>; reject hits that stray
        /// beyond <paramref name="leashFromBase"/> XZ from the spawn base.
        /// </summary>
        private static bool TrySampleNear(
            Vector3 candidate,
            Vector3 basePos,
            float leashFromBase,
            float sampleDistance,
            out Vector3 placed)
        {
            placed = candidate;
            if (!SpecialMoveNavMesh.SampleWalkable(
                    candidate,
                    sampleDistance,
                    SpecialMoveNavMesh.DefaultAgentTypeId,
                    out var hit))
            {
                return false;
            }

            placed = hit.position;
            var dx = placed.x - basePos.x;
            var dz = placed.z - basePos.z;
            var leash = Mathf.Max(0.01f, leashFromBase);
            return dx * dx + dz * dz <= leash * leash;
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
