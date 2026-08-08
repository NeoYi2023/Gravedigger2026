using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free correctness checks for MP-02 acceptance (SPEC_04 §9.7 AttackSlot)
    /// and SC-02 surround gap (SPEC_03 §3.12 Approach B+).
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
            CheckSurroundGapSkipped(sb);
            CheckSurroundTopGapTowardAttackers(sb);
            CheckSurroundReleaseThenReclaim(sb);
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

        // SC-02: attackers approach from +X → centroid +X → default Bottom gap sits on the
        // far side (180°). With 12 melee slots (30° step) a 90° gap strictly covers
        // 150°/180°/210° → exactly 9 claimable slots, none inside the sector.
        private static void CheckSurroundGapSkipped(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            var target = Vector3.zero;
            const float attackRange = 1.0f;
            var surround = new SurroundParams
            {
                GapDir = SurroundGapDirection.Bottom,
                GapDegrees = 90f,
            };

            for (var i = 0; i < 9; i++)
            {
                if (!svc.TryClaim(
                        $"s{i}",
                        "boss",
                        attackRange,
                        target,
                        out var pos,
                        AttackMode.Melee,
                        attackerPos: new Vector3(3f, 0f, i * 0.05f),
                        targetBodyRadius: 0.1f,
                        surround: surround))
                {
                    sb.AppendLine($"SurroundGap: claim {i} failed though 9 slots expected.");
                    return;
                }

                var deg = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 180f)) < 90f * 0.5f - 0.01f)
                {
                    sb.AppendLine($"SurroundGap: claim {i} at {deg:F1}° inside gap sector.");
                }
            }

            if (svc.GetOccupiedCount("boss") != 9)
            {
                sb.AppendLine(
                    $"SurroundGap: occupied {svc.GetOccupiedCount("boss")} != 9 (3 slots in gap).");
            }

            if (!svc.TryGetGapCenterDegrees("boss", target, surround, out var center) ||
                Mathf.Abs(Mathf.DeltaAngle(center, 180f)) > 5f)
            {
                sb.AppendLine($"SurroundGap: gap center {center:F1}° != 180° (far side).");
            }

            if (svc.TryClaim(
                    "overflow",
                    "boss",
                    attackRange,
                    target,
                    out _,
                    AttackMode.Melee,
                    attackerPos: new Vector3(3f, 0f, 1f),
                    targetBodyRadius: 0.1f,
                    surround: surround))
            {
                sb.AppendLine("SurroundGap: overflow claim should fail when non-gap ring full.");
            }
        }

        // Top = gap toward the attacker side (0° here): 0°/±30° slots skipped.
        private static void CheckSurroundTopGapTowardAttackers(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            var target = Vector3.zero;
            var surround = new SurroundParams
            {
                GapDir = SurroundGapDirection.Top,
                GapDegrees = 90f,
            };

            for (var i = 0; i < 9; i++)
            {
                if (!svc.TryClaim(
                        $"t{i}",
                        "prey",
                        1.0f,
                        target,
                        out var pos,
                        AttackMode.Melee,
                        attackerPos: new Vector3(3f, 0f, i * 0.05f),
                        targetBodyRadius: 0.1f,
                        surround: surround))
                {
                    sb.AppendLine($"SurroundTop: claim {i} failed though 9 slots expected.");
                    return;
                }

                var deg = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 0f)) < 90f * 0.5f - 0.01f)
                {
                    sb.AppendLine($"SurroundTop: claim {i} at {deg:F1}° inside gap sector.");
                }
            }

            if (svc.GetOccupiedCount("prey") != 9)
            {
                sb.AppendLine(
                    $"SurroundTop: occupied {svc.GetOccupiedCount("prey")} != 9.");
            }
        }

        private static void CheckSurroundReleaseThenReclaim(StringBuilder sb)
        {
            var svc = new AttackSlotService();
            var target = Vector3.zero;
            const float attackRange = 1.0f;
            var surround = SurroundParams.Default; // Bottom 60° → only the 180° step skipped

            for (var i = 0; i < 4; i++)
            {
                if (!svc.TryClaim(
                        $"r{i}",
                        "t",
                        attackRange,
                        target,
                        out var pos,
                        AttackMode.Melee,
                        attackerPos: new Vector3(3f, 0f, i * 0.05f),
                        targetBodyRadius: 0.1f,
                        surround: surround))
                {
                    sb.AppendLine($"SurroundRelease: claim {i} failed.");
                    return;
                }

                var deg = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 180f)) < 60f * 0.5f - 0.01f)
                {
                    sb.AppendLine($"SurroundRelease: claim {i} at {deg:F1}° inside gap.");
                }
            }

            svc.Release("r1");
            if (svc.GetOccupiedCount("t") != 3)
            {
                sb.AppendLine("SurroundRelease: occupied != 3 after release.");
            }

            // Kept-claim refresh must also respect the gap.
            if (!svc.TryClaim(
                    "r0",
                    "t",
                    attackRange,
                    target,
                    out var kept,
                    AttackMode.Melee,
                    attackerPos: new Vector3(3f, 0f, 0.2f),
                    targetBodyRadius: 0.1f,
                    surround: surround))
            {
                sb.AppendLine("SurroundRelease: refresh of kept claim failed.");
                return;
            }

            var keptDeg = Mathf.Atan2(kept.z, kept.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(keptDeg, 180f)) < 60f * 0.5f - 0.01f)
            {
                sb.AppendLine($"SurroundRelease: kept claim at {keptDeg:F1}° inside gap.");
            }

            if (!svc.TryClaim(
                    "r4",
                    "t",
                    attackRange,
                    target,
                    out var re,
                    AttackMode.Melee,
                    attackerPos: new Vector3(3f, 0f, 0.3f),
                    targetBodyRadius: 0.1f,
                    surround: surround))
            {
                sb.AppendLine("SurroundRelease: reclaim after release failed.");
                return;
            }

            var reDeg = Mathf.Atan2(re.z, re.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(reDeg, 180f)) < 60f * 0.5f - 0.01f)
            {
                sb.AppendLine($"SurroundRelease: reclaimed slot at {reDeg:F1}° inside gap.");
            }

            if (svc.GetOccupiedCount("t") != 4)
            {
                sb.AppendLine("SurroundRelease: occupied != 4 after reclaim.");
            }
        }
    }
}
