using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Local L/R detour + soft separation (SPEC_03 §3.12 / SPEC_04 §9.7 Approach B).
    /// Pure C#: no Transform/Animator/NavMeshObstacle. Stage wiring is MP-04/05.
    /// </summary>
    public sealed class LocalDetourSolver
    {
        public const float ProbeLength = 1.0f;
        public const float SoftSeparationStrength = 0.15f;
        public const float DetourBias = 0.85f;

        /// <summary>Forward half-angle (degrees). Cos cached for hot path.</summary>
        public const float ForwardConeHalfAngleDeg = 50f;

        private static readonly float ForwardConeCos =
            Mathf.Cos(ForwardConeHalfAngleDeg * Mathf.Deg2Rad);

        /// <summary>
        /// Steer in XZ: default follow <paramref name="desiredDir"/>; if a friendly blocks the
        /// forward cone, bias toward the clearer L/R probe. Optional soft separation scaled by
        /// <paramref name="separationScale"/> (pass &lt;1 in engage bubble).
        /// </summary>
        public Vector2 Steer(
            Vector2 desiredDir,
            in LocalDetourAgent self,
            List<SpatialHashEntry> neighbors,
            float separationScale = 1f)
        {
            if (desiredDir.sqrMagnitude < 1e-8f)
            {
                return ApplySeparation(Vector2.zero, self, neighbors, separationScale);
            }

            var desired = desiredDir.normalized;
            var blocked = HasForwardBlocker(desired, self, neighbors);

            Vector2 steer;
            if (!blocked)
            {
                steer = desired;
            }
            else
            {
                var left = new Vector2(-desired.y, desired.x);
                var right = new Vector2(desired.y, -desired.x);
                var leftClear = ScoreProbeClearance(desired, left, self, neighbors);
                var rightClear = ScoreProbeClearance(desired, right, self, neighbors);

                // Prefer clearer side; on near-tie keep desired (stable).
                if (Mathf.Abs(leftClear - rightClear) < 1e-4f)
                {
                    steer = desired;
                }
                else if (leftClear > rightClear)
                {
                    steer = (desired + left * DetourBias).normalized;
                }
                else
                {
                    steer = (desired + right * DetourBias).normalized;
                }
            }

            return ApplySeparation(steer, self, neighbors, separationScale);
        }

        private static bool HasForwardBlocker(
            Vector2 desired,
            in LocalDetourAgent self,
            List<SpatialHashEntry> neighbors)
        {
            if (neighbors == null || neighbors.Count == 0)
            {
                return false;
            }

            var blockDist = ProbeLength + self.Radius;

            for (var i = 0; i < neighbors.Count; i++)
            {
                var n = neighbors[i];
                if (n.Id == self.Id)
                {
                    continue;
                }

                var dx = n.Position.x - self.Position.x;
                var dy = n.Position.y - self.Position.y;
                var distSq = dx * dx + dy * dy;
                if (distSq < 1e-8f)
                {
                    return true;
                }

                var combined = blockDist + n.Radius;
                if (distSq > combined * combined)
                {
                    continue;
                }

                var invDist = 1f / Mathf.Sqrt(distSq);
                var dot = (dx * desired.x + dy * desired.y) * invDist;
                if (dot >= ForwardConeCos)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Higher = clearer. Samples along a forward+side biased probe segment.
        /// </summary>
        private static float ScoreProbeClearance(
            Vector2 desired,
            Vector2 side,
            in LocalDetourAgent self,
            List<SpatialHashEntry> neighbors)
        {
            var probeDir = (desired + side * DetourBias).normalized;
            var probeEnd = self.Position + probeDir * ProbeLength;
            var minClear = ProbeLength;

            if (neighbors == null)
            {
                return minClear;
            }

            for (var i = 0; i < neighbors.Count; i++)
            {
                var n = neighbors[i];
                if (n.Id == self.Id)
                {
                    continue;
                }

                var dist = DistancePointToSegment(n.Position, self.Position, probeEnd);
                var clear = dist - n.Radius - self.Radius;
                if (clear < minClear)
                {
                    minClear = clear;
                }
            }

            return minClear;
        }

        private static Vector2 ApplySeparation(
            Vector2 baseSteer,
            in LocalDetourAgent self,
            List<SpatialHashEntry> neighbors,
            float separationScale)
        {
            var scale = SoftSeparationStrength * Mathf.Max(0f, separationScale);
            if (scale <= 1e-6f || neighbors == null || neighbors.Count == 0)
            {
                return baseSteer.sqrMagnitude > 1e-8f ? baseSteer.normalized : baseSteer;
            }

            var sep = Vector2.zero;
            for (var i = 0; i < neighbors.Count; i++)
            {
                var n = neighbors[i];
                if (n.Id == self.Id)
                {
                    continue;
                }

                var dx = self.Position.x - n.Position.x;
                var dy = self.Position.y - n.Position.y;
                var distSq = dx * dx + dy * dy;
                var minDist = self.Radius + n.Radius;
                if (minDist < 1e-4f)
                {
                    continue;
                }

                if (distSq >= minDist * minDist || distSq < 1e-8f)
                {
                    continue;
                }

                var dist = Mathf.Sqrt(distSq);
                var push = (minDist - dist) / minDist;
                sep.x += dx / dist * push;
                sep.y += dy / dist * push;
            }

            var combined = baseSteer + sep * scale;
            if (combined.sqrMagnitude < 1e-8f)
            {
                return baseSteer.sqrMagnitude > 1e-8f ? baseSteer.normalized : Vector2.zero;
            }

            return combined.normalized;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var abx = b.x - a.x;
            var aby = b.y - a.y;
            var abLenSq = abx * abx + aby * aby;
            if (abLenSq < 1e-8f)
            {
                var dx = p.x - a.x;
                var dy = p.y - a.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            var t = ((p.x - a.x) * abx + (p.y - a.y) * aby) / abLenSq;
            if (t < 0f)
            {
                t = 0f;
            }
            else if (t > 1f)
            {
                t = 1f;
            }

            var qx = a.x + abx * t;
            var qy = a.y + aby * t;
            var ex = p.x - qx;
            var ey = p.y - qy;
            return Mathf.Sqrt(ex * ex + ey * ey);
        }
    }

    /// <summary>Self agent sample for <see cref="LocalDetourSolver.Steer"/> (XZ).</summary>
    public readonly struct LocalDetourAgent
    {
        public readonly int Id;
        public readonly Vector2 Position;
        public readonly float Radius;

        public LocalDetourAgent(int id, Vector2 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }
    }
}
