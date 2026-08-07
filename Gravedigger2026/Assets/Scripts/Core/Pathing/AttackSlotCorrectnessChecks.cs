using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free correctness checks for MP-02 acceptance (SPEC_04 §9.7 AttackSlot).
    /// Call <see cref="RunAll"/> from Editor/console or a future EditMode test.
    /// </summary>
    public static class AttackSlotCorrectnessChecks
    {
        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckDistinctSlotsSameTarget(sb);
            CheckRingRadiusDistance(sb);
            CheckReleaseThenReclaim(sb);
            CheckTargetMoveRecompute(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckDistinctSlotsSameTarget(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            var target = new Vector3(0f, 0f, 0f);
            var claimed = new List<Vector3>();
            const float attackRange = 1.0f;

            for (var i = 0; i < AttackSlotService.MeleeSlotCount; i++)
            {
                if (!svc.TryClaim(
                        $"a{i}",
                        "t1",
                        attackRange,
                        target,
                        out var pos,
                        AttackMode.Melee,
                        attackerPos: new Vector3(2f, 0f, i * 0.01f),
                        targetBodyRadius: 0.1f))
                {
                    sb.AppendLine($"DistinctSlots: claim {i} failed.");
                    return;
                }

                claimed.Add(pos);
            }

            if (svc.GetOccupiedCount("t1") != AttackSlotService.MeleeSlotCount)
            {
                sb.AppendLine(
                    $"DistinctSlots: occupied {svc.GetOccupiedCount("t1")} != N.");
            }

            for (var i = 0; i < claimed.Count; i++)
            {
                for (var j = i + 1; j < claimed.Count; j++)
                {
                    var d = claimed[i] - claimed[j];
                    if (d.sqrMagnitude < 1e-6f)
                    {
                        sb.AppendLine($"DistinctSlots: duplicate world pos at {i}/{j}.");
                    }
                }
            }

            if (svc.TryClaim(
                    "overflow",
                    "t1",
                    attackRange,
                    target,
                    out _,
                    AttackMode.Melee,
                    targetBodyRadius: 0.1f))
            {
                sb.AppendLine("DistinctSlots: overflow claim should fail when full.");
            }
        }

        private static void CheckRingRadiusDistance(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            const float attackRange = 1.25f;
            var expected = AttackSlotService.ComputeRingRadius(attackRange);
            var target = new Vector3(3f, 0f, -1f);

            if (!svc.TryClaim(
                    "archer",
                    "boss",
                    attackRange,
                    target,
                    out var pos,
                    AttackMode.Ranged,
                    attackerPos: new Vector3(5f, 0f, -1f),
                    targetBodyRadius: 0.1f))
            {
                sb.AppendLine("RingRadius: claim failed.");
                return;
            }

            var dx = pos.x - target.x;
            var dz = pos.z - target.z;
            var dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (Mathf.Abs(dist - expected) > 0.02f)
            {
                sb.AppendLine(
                    $"RingRadius: dist {dist:F3} != ring {expected:F3}.");
            }

            if (AttackSlotService.SlotCountFor(AttackMode.Ranged) !=
                AttackSlotService.RangedSlotCount)
            {
                sb.AppendLine("RingRadius: ranged N mismatch.");
            }
        }

        private static void CheckReleaseThenReclaim(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            var target = Vector3.zero;
            const float attackRange = 0.8f;

            if (!svc.TryClaim("a", "t", attackRange, target, out var first,
                    AttackMode.Melee, targetBodyRadius: 0.1f))
            {
                sb.AppendLine("ReleaseReclaim: first claim failed.");
                return;
            }

            svc.Release("a");
            if (svc.TryGetClaim("a", out _))
            {
                sb.AppendLine("ReleaseReclaim: claim still present after Release.");
            }

            if (svc.GetOccupiedCount("t") != 0)
            {
                sb.AppendLine("ReleaseReclaim: target still occupied after Release.");
            }

            if (!svc.TryClaim("b", "t", attackRange, target, out var second,
                    AttackMode.Melee, targetBodyRadius: 0.1f))
            {
                sb.AppendLine("ReleaseReclaim: reclaim failed.");
                return;
            }

            // Freed slot may be reused; distance to ring must still hold.
            var ring = AttackSlotService.ComputeRingRadius(attackRange);
            var dx = second.x - target.x;
            var dz = second.z - target.z;
            var dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (Mathf.Abs(dist - ring) > 0.02f)
            {
                sb.AppendLine($"ReleaseReclaim: reclaimed dist {dist:F3} != {ring:F3}.");
            }

            // Explicit: same attacker re-claims after release.
            svc.Release("b");
            if (!svc.TryClaim("a", "t", attackRange, target, out _,
                    AttackMode.Melee, targetBodyRadius: 0.1f))
            {
                sb.AppendLine("ReleaseReclaim: original attacker could not reclaim.");
            }

            _ = first;
        }

        private static void CheckTargetMoveRecompute(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            var targetA = new Vector3(0f, 0f, 0f);
            const float attackRange = 1.0f;

            if (!svc.TryClaim(
                    "chaser",
                    "prey",
                    attackRange,
                    targetA,
                    out var posA,
                    AttackMode.Melee,
                    attackerPos: new Vector3(2f, 0f, 0f),
                    targetBodyRadius: 0.1f))
            {
                sb.AppendLine("MoveRecompute: initial claim failed.");
                return;
            }

            if (!svc.TryGetTargetAnchor("prey", out var anchorA, out _))
            {
                sb.AppendLine("MoveRecompute: missing anchor after claim.");
                return;
            }

            var targetB = new Vector3(2f, 0f, 0f); // > 0.5 threshold
            if (!svc.TryClaim(
                    "chaser",
                    "prey",
                    attackRange,
                    targetB,
                    out var posB,
                    AttackMode.Melee,
                    attackerPos: new Vector3(4f, 0f, 0f),
                    targetBodyRadius: 0.1f))
            {
                sb.AppendLine("MoveRecompute: claim after move failed.");
                return;
            }

            if (!svc.TryGetTargetAnchor("prey", out var anchorB, out var ring))
            {
                sb.AppendLine("MoveRecompute: missing anchor after move.");
                return;
            }

            var anchorDelta = anchorB - anchorA;
            if (anchorDelta.sqrMagnitude <
                AttackSlotService.SlotReclaimMoveThreshold *
                AttackSlotService.SlotReclaimMoveThreshold * 0.9f)
            {
                sb.AppendLine("MoveRecompute: anchor did not update after large move.");
            }

            var dx = posB.x - targetB.x;
            var dz = posB.z - targetB.z;
            var dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (Mathf.Abs(dist - ring) > 0.02f)
            {
                sb.AppendLine(
                    $"MoveRecompute: slot dist {dist:F3} != ring {ring:F3} after move.");
            }

            var shift = posB - posA;
            if (shift.sqrMagnitude < 0.2f)
            {
                sb.AppendLine(
                    $"MoveRecompute: slot barely moved ({shift.magnitude:F3}) after target +2.");
            }
        }
    }
}
