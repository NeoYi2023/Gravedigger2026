using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>What a Parabola soldier does when windup ends (SPEC_03 §3.12).</summary>
    public enum ParabolaShotKind
    {
        NoShot = 0,
        Melee = 1,
        Blocked = 2,
        Straight = 3,
        ArcHit = 4,
        ArcMiss = 5
    }

    /// <summary>One other body sampled for the forward-arc gates.</summary>
    public struct ParabolaBody
    {
        public string Id;
        public Vector3 Position;
        public bool Friendly;
    }

    public struct ParabolaShotChoice
    {
        public ParabolaShotKind Kind;
        public string MeleeTargetId;
    }

    /// <summary>
    /// Pure Parabola fire gates. Distances are XZ center distances.
    /// Melee gate outranks the friendly block. A friendly blocks only when closer than the target.
    /// The hit roll is supplied by the caller.
    /// </summary>
    public static class ParabolaFirePolicy
    {
        public static bool InForwardArc(
            Vector3 origin,
            Vector3 aimDirXZ,
            Vector3 point,
            float fullArcDegrees)
        {
            var to = point - origin;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            var aim = aimDirXZ;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var half = Mathf.Clamp(fullArcDegrees, 0f, 360f) * 0.5f;
            var angle = Vector3.Angle(aim, to);
            return angle <= half + 0.01f;
        }

        public static ParabolaShotChoice Choose(
            Vector3 origin,
            Vector3 targetPosition,
            bool targetInEngageRange,
            float attackRange,
            float meleeRange,
            float arcMinDistance,
            float hitRate,
            float fullArcDegrees,
            float roll01,
            Vector3 aimDirXZ,
            IReadOnlyList<ParabolaBody> bodies)
        {
            var targetDist = DistanceXZ(origin, targetPosition);
            var meleeId = (string)null;
            var meleeDist = float.MaxValue;
            var blocked = false;
            if (bodies != null)
            {
                for (var i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];
                    if (!InForwardArc(origin, aimDirXZ, body.Position, fullArcDegrees))
                    {
                        continue;
                    }

                    var dist = DistanceXZ(origin, body.Position);
                    if (!body.Friendly && dist < meleeRange && dist < meleeDist)
                    {
                        meleeDist = dist;
                        meleeId = body.Id;
                    }
                    else if (body.Friendly && dist < attackRange && dist + 0.02f < targetDist)
                    {
                        blocked = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(meleeId))
            {
                return new ParabolaShotChoice
                {
                    Kind = ParabolaShotKind.Melee,
                    MeleeTargetId = meleeId
                };
            }

            if (!targetInEngageRange)
            {
                return new ParabolaShotChoice { Kind = ParabolaShotKind.NoShot };
            }

            if (blocked)
            {
                return new ParabolaShotChoice { Kind = ParabolaShotKind.Blocked };
            }

            if (targetDist < arcMinDistance)
            {
                return new ParabolaShotChoice { Kind = ParabolaShotKind.Straight };
            }

            var chance = Mathf.Clamp01(hitRate);
            var hit = chance >= 1f || roll01 < chance;
            return new ParabolaShotChoice
            {
                Kind = hit ? ParabolaShotKind.ArcHit : ParabolaShotKind.ArcMiss
            };
        }

        public static float DistanceXZ(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
