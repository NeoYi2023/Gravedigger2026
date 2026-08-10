using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// AttackRange edge-gap reach (SPEC_03 §3.12 v0.75.24).
    /// In range when center distance ≤ AttackRange + both BodyRadii
    /// (equiv. footprint-edge gap ≤ AttackRange). Soft-collision contact is always
    /// in reach when AttackRange ≥ 0.
    /// </summary>
    public static class CombatReach
    {
        /// <summary>HitConfirm / windup re-check slack (matches prior +0.05f). </summary>
        public const float HitConfirmSlack = 0.05f;

        /// <summary>Max center distance still considered in AttackRange.</summary>
        public static float MaxCenterDistance(
            float attackRange,
            float attackerBodyRadius,
            float targetBodyRadius)
        {
            return Mathf.Max(0f, attackRange)
                   + Mathf.Max(0f, attackerBodyRadius)
                   + Mathf.Max(0f, targetBodyRadius);
        }

        /// <summary>True when center distance is within edge-gap AttackRange (+ optional slack).</summary>
        public static bool IsInAttackRange(
            float centerDistance,
            float attackRange,
            float attackerBodyRadius,
            float targetBodyRadius,
            float slack = 0f)
        {
            return centerDistance
                   <= MaxCenterDistance(attackRange, attackerBodyRadius, targetBodyRadius) + slack;
        }

        /// <summary>
        /// PushMap engage detect radius (MP-05): max weapon reach + both footprints + arrive ε.
        /// </summary>
        public static float EngageDetectRadius(
            float attackRangeA,
            float attackRangeB,
            float bodyRadiusA,
            float bodyRadiusB,
            float arriveEpsilon)
        {
            return Mathf.Max(attackRangeA, attackRangeB)
                   + Mathf.Max(0f, bodyRadiusA)
                   + Mathf.Max(0f, bodyRadiusB)
                   + arriveEpsilon;
        }
    }
}
