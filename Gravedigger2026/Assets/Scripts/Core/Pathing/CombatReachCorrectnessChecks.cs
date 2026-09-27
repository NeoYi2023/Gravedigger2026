using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free checks for chase dest vs ArriveEpsilon (SPEC_03 §3.12 v0.84.53).
    /// </summary>
    public static class CombatReachCorrectnessChecks
    {
        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckOuterRingShellDoesNotArriveOutOfRange(sb);
            CheckSafeSlotIsKept(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        /// <summary>
        /// Sample tuning: ring = maxIn − margin (0.05), ArriveEpsilon 0.08.
        /// Standing just outside that ring must get a closer dest whose arrive ball
        /// is still inside AttackRange, and that dest must be farther than epsilon
        /// so steer does not zero.
        /// </summary>
        private static void CheckOuterRingShellDoesNotArriveOutOfRange(StringBuilder sb)
        {
            const float attackRange = 1f;
            const float attackerBody = 0.16f;
            const float targetBody = 0.15f;
            const float epsilon = 0.08f;
            var margin = CombatRuntimeTuning.AttackSlotMargin;
            var maxIn = CombatReach.MaxCenterDistance(attackRange, attackerBody, targetBody);
            var slotDist = maxIn - margin;
            var target = Vector3.zero;
            var slot = new Vector3(slotDist, 0f, 0f);
            var attacker = new Vector3(slotDist + 0.06f, 0f, 0f);

            var dest = CombatReach.ChaseDestinationXZ(
                attacker,
                target,
                slot,
                attackRange,
                attackerBody,
                targetBody,
                epsilon);
            var destDist = dest.magnitude;
            if (destDist + epsilon > maxIn + 0.0001f)
            {
                sb.AppendLine(
                    $"OuterShell: dest {destDist:F3} + epsilon {epsilon:F3} exceeds maxIn {maxIn:F3}.");
            }

            var gap = attacker.x - destDist;
            if (gap <= epsilon)
            {
                sb.AppendLine(
                    $"OuterShell: attacker-to-dest {gap:F3} <= ArriveEpsilon {epsilon:F3} (steer would zero out of range).");
            }

            if (CombatReach.IsInAttackRange(attacker.x, attackRange, attackerBody, targetBody))
            {
                sb.AppendLine("OuterShell: fixture attacker is already in range; test no longer covers the stuck shell.");
            }
        }

        private static void CheckSafeSlotIsKept(StringBuilder sb)
        {
            const float attackRange = 1f;
            const float attackerBody = 0.16f;
            const float targetBody = 0.15f;
            const float epsilon = 0.08f;
            var maxIn = CombatReach.MaxCenterDistance(attackRange, attackerBody, targetBody);
            var safe = CombatReach.ClosingRadius(attackRange, attackerBody, targetBody, epsilon);
            if (safe + epsilon > maxIn + 0.0001f)
            {
                sb.AppendLine($"SafeSlot: closing radius {safe:F3} + epsilon exceeds maxIn {maxIn:F3}.");
            }

            var target = Vector3.zero;
            var slot = new Vector3(safe, 0f, 0f);
            var attacker = new Vector3(safe + 2f, 0f, 0f);
            var dest = CombatReach.ChaseDestinationXZ(
                attacker,
                target,
                slot,
                attackRange,
                attackerBody,
                targetBody,
                epsilon);
            if ((dest - new Vector2(slot.x, slot.z)).sqrMagnitude > 0.0001f)
            {
                sb.AppendLine($"SafeSlot: expected slot {slot.x:F3}, got {dest}.");
            }
        }
    }
}
