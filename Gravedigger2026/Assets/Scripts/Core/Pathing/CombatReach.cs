using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// AttackRange edge-gap reach (SPEC_03 §3.12 v0.75.24).
    /// In range when <b>XZ</b> center distance ≤ AttackRange + both BodyRadii
    /// (equiv. footprint-edge gap ≤ AttackRange). Soft-collision contact is always
    /// in reach when AttackRange ≥ 0. Y is ignored — Combat is planar.
    /// </summary>
    public static class CombatReach
    {
        /// <summary>HitConfirm / windup re-check slack (CombatConstantConfig HitConfirmSlack).</summary>
        public static float HitConfirmSlack => CombatRuntimeTuning.HitConfirmSlack;

        /// <summary>Planar XZ distance (Combat movement / AttackRange plane).</summary>
        public static float DistanceXZ(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Max XZ center distance still considered in AttackRange.</summary>
        public static float MaxCenterDistance(
            float attackRange,
            float attackerBodyRadius,
            float targetBodyRadius)
        {
            return Mathf.Max(0f, attackRange)
                   + Mathf.Max(0f, attackerBodyRadius)
                   + Mathf.Max(0f, targetBodyRadius);
        }

        /// <summary>True when XZ center distance is within edge-gap AttackRange (+ optional slack).</summary>
        public static bool IsInAttackRange(
            float centerDistanceXZ,
            float attackRange,
            float attackerBodyRadius,
            float targetBodyRadius,
            float slack = 0f)
        {
            return centerDistanceXZ
                   <= MaxCenterDistance(attackRange, attackerBodyRadius, targetBodyRadius) + slack;
        }

        /// <summary>
        /// In-range hold radius used as AttackSlot closing dest so ArriveEpsilon
        /// still lands inside <see cref="IsInAttackRange"/> (SPEC_03 §3.12 v0.82.57).
        /// </summary>
        public static float ClosingRadius(
            float attackRange,
            float attackerBodyRadius,
            float targetBodyRadius,
            float arriveEpsilon)
        {
            var maxIn = MaxCenterDistance(attackRange, attackerBodyRadius, targetBodyRadius);
            return Mathf.Max(
                0.05f,
                maxIn - Mathf.Max(0.01f, arriveEpsilon) - CombatRuntimeTuning.AttackSlotMargin);
        }

        /// <summary>
        /// Point on the XZ circle of <paramref name="radius"/> around target, on the
        /// attacker→target radial (inside-ring units walk inward, not out to a farther slot).
        /// </summary>
        public static Vector2 ClosingPointXZ(Vector3 attackerPos, Vector3 targetPos, float radius)
        {
            var dx = attackerPos.x - targetPos.x;
            var dz = attackerPos.z - targetPos.z;
            var dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < 1e-4f)
            {
                return new Vector2(targetPos.x + radius, targetPos.z);
            }

            var s = radius / dist;
            return new Vector2(targetPos.x + dx * s, targetPos.z + dz * s);
        }

        /// <summary>
        /// Chase dest: claimed slot if it is closer to the target and still in AttackRange;
        /// otherwise the in-range closing point. Prevents inside-ring units walking away.
        /// </summary>
        public static Vector2 ChaseDestinationXZ(
            Vector3 attackerPos,
            Vector3 targetPos,
            Vector3 slotPos,
            float attackRange,
            float attackerBodyRadius,
            float targetBodyRadius,
            float arriveEpsilon)
        {
            var selfDist = DistanceXZ(attackerPos, targetPos);
            var slotDist = DistanceXZ(slotPos, targetPos);
            var maxIn = MaxCenterDistance(attackRange, attackerBodyRadius, targetBodyRadius);
            if (slotDist + 0.01f < selfDist && slotDist <= maxIn)
            {
                return new Vector2(slotPos.x, slotPos.z);
            }

            return ClosingPointXZ(
                attackerPos,
                targetPos,
                ClosingRadius(attackRange, attackerBodyRadius, targetBodyRadius, arriveEpsilon));
        }

        /// <summary>
        /// PushMap engage detect (SPEC_03 §3.12 / v0.82.55 Approach C):
        /// <c>max(weaponReach, alertRadius)</c> where weaponReach is max AttackRange
        /// + both BodyRadii + arrive ε. Missing/zero AlertRadius keeps weapon reach.
        /// </summary>
        public static float EngageDetectRadius(
            float attackRangeA,
            float attackRangeB,
            float bodyRadiusA,
            float bodyRadiusB,
            float arriveEpsilon,
            float alertRadius = 0f)
        {
            var weaponReach = Mathf.Max(attackRangeA, attackRangeB)
                              + Mathf.Max(0f, bodyRadiusA)
                              + Mathf.Max(0f, bodyRadiusB)
                              + arriveEpsilon;
            return Mathf.Max(weaponReach, Mathf.Max(0f, alertRadius));
        }
    }
}
