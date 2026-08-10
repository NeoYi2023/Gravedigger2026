using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// AttackSlot claim table (SPEC_03 §3.12 / SPEC_04 §9.7 Approach B).
    /// Pure C#: no Transform/Animator. Chase arrival = ring slot, not target center.
    /// Stage wiring is MP-05; this service only owns claim / release / recompute.
    /// Approach B+ (SC-02): optional <see cref="SurroundParams"/> skips ring slots
    /// inside the gap sector (default = far side vs the attacker centroid), so
    /// melee multi-vs-one no longer packs a solid ring. Absent param → legacy
    /// full-ring behavior, MP-02 semantics unchanged.
    /// </summary>
    public sealed class AttackSlotService
    {
        public const float SlotMargin = 0.05f;
        public const float MinRingRadius = 0.05f;
        public const float SlotReclaimMoveThreshold = 0.5f;
        public const int MeleeSlotCount = 12;
        public const int RangedSlotCount = 8;
        public const float DefaultTargetBodyRadius = 0.35f;

        private readonly Dictionary<string, TargetSlotTable> _byTarget =
            new Dictionary<string, TargetSlotTable>();

        private readonly Dictionary<string, AttackerClaim> _byAttacker =
            new Dictionary<string, AttackerClaim>();

        private IAttackSlotWalkable _walkable = StubAttackSlotFullyWalkable.Instance;

        /// <summary>Optional walkability hook (SamplePosition / mask). Null → stub full walkable.</summary>
        public void SetWalkableHook(IAttackSlotWalkable walkable)
        {
            _walkable = walkable ?? StubAttackSlotFullyWalkable.Instance;
        }

        /// <summary>
        /// Ring radius from target center. AttackRange is edge-gap (SPEC_03 §3.12 v0.75.24+):
        /// <c>AttackRange + attackerBody + targetBody − slotMargin</c>.
        /// Body radii default 0 → legacy <c>AttackRange − slotMargin</c>.
        /// </summary>
        public static float ComputeRingRadius(
            float attackRange,
            float attackerBodyRadius = 0f,
            float targetBodyRadius = 0f)
        {
            return Mathf.Max(
                MinRingRadius,
                attackRange
                + Mathf.Max(0f, attackerBodyRadius)
                + Mathf.Max(0f, targetBodyRadius)
                - SlotMargin);
        }

        public static int SlotCountFor(AttackMode attackMode)
        {
            return attackMode == AttackMode.Ranged ? RangedSlotCount : MeleeSlotCount;
        }

        /// <summary>
        /// Claim or refresh a slot for <paramref name="attackerId"/> on <paramref name="targetId"/>.
        /// ≤1 slot per attacker; frees prior claim on retarget. Returns false if no free walkable slot.
        /// <paramref name="surround"/> (SC-02): when set, slots inside the gap sector are skipped
        /// in both keep and pick paths; gap center derives from the rolling attacker centroid
        /// (only claims with a valid <paramref name="attackerPos"/> feed the centroid).
        /// If the gap covers every slot the claim honestly fails.
        /// </summary>
        public bool TryClaim(
            string attackerId,
            string targetId,
            float attackRange,
            Vector3 targetPos,
            out Vector3 worldPos,
            AttackMode attackMode = AttackMode.Melee,
            Vector3 attackerPos = default,
            float targetBodyRadius = DefaultTargetBodyRadius,
            float attackerBodyRadius = 0f,
            SurroundParams? surround = null)
        {
            worldPos = default;
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(targetId))
            {
                return false;
            }

            var ringRadius = ComputeRingRadius(attackRange, attackerBodyRadius, targetBodyRadius);
            var slotCount = SlotCountFor(attackMode);
            var minDistFromCenter = Mathf.Max(0f, targetBodyRadius * 0.5f);

            // Same target already claimed → refresh / move-threshold recompute, keep slot index.
            if (_byAttacker.TryGetValue(attackerId, out var existing) &&
                existing.TargetId == targetId)
            {
                var table = EnsureTable(targetId, slotCount, ringRadius, targetPos);
                if (existing.SlotIndex >= 0 &&
                    existing.SlotIndex < table.Slots.Length &&
                    table.Slots[existing.SlotIndex].AttackerId == attackerId)
                {
                    MaybeRecomputePositions(table, ringRadius, targetPos);
                    UpdateClaimerPos(table, existing.SlotIndex, attackerPos, targetPos);
                    var kept = table.Slots[existing.SlotIndex].WorldPos;
                    if (IsAcceptableSlot(kept, targetPos, minDistFromCenter) &&
                        !IsSlotInGap(table, kept, targetPos, surround, attackerPos))
                    {
                        worldPos = kept;
                        return true;
                    }

                    // Slot became illegal after move / fell into the gap — free and re-pick.
                    ClearSlot(table, existing.SlotIndex);
                    _byAttacker.Remove(attackerId);
                }
            }
            else if (_byAttacker.ContainsKey(attackerId))
            {
                Release(attackerId);
            }

            var targetTable = EnsureTable(targetId, slotCount, ringRadius, targetPos);
            MaybeRecomputePositions(targetTable, ringRadius, targetPos);

            var preferred = PreferredApproachXZ(attackerPos, targetPos);
            var bestIndex = -1;
            var bestScore = float.NegativeInfinity;

            for (var i = 0; i < targetTable.Slots.Length; i++)
            {
                ref var slot = ref targetTable.Slots[i];
                if (slot.AttackerId != null)
                {
                    continue;
                }

                if (!IsAcceptableSlot(slot.WorldPos, targetPos, minDistFromCenter))
                {
                    continue;
                }

                if (IsSlotInGap(targetTable, slot.WorldPos, targetPos, surround, attackerPos))
                {
                    continue;
                }

                var score = ScoreSlot(slot.WorldPos, targetPos, preferred);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            targetTable.Slots[bestIndex].AttackerId = attackerId;
            worldPos = targetTable.Slots[bestIndex].WorldPos;
            if (IsValidClaimerPos(attackerPos, targetPos))
            {
                targetTable.Slots[bestIndex].ClaimerPos = attackerPos;
                targetTable.Slots[bestIndex].HasClaimerPos = true;
                targetTable.CentroidSumX += attackerPos.x;
                targetTable.CentroidSumZ += attackerPos.z;
            }

            _byAttacker[attackerId] = new AttackerClaim
            {
                TargetId = targetId,
                SlotIndex = bestIndex
            };
            return true;
        }

        /// <summary>Release one attacker's claim (death / retarget handled by caller).</summary>
        public void Release(string attackerId)
        {
            if (string.IsNullOrEmpty(attackerId))
            {
                return;
            }

            if (!_byAttacker.TryGetValue(attackerId, out var claim))
            {
                return;
            }

            _byAttacker.Remove(attackerId);
            if (!_byTarget.TryGetValue(claim.TargetId, out var table))
            {
                return;
            }

            if (claim.SlotIndex >= 0 &&
                claim.SlotIndex < table.Slots.Length &&
                table.Slots[claim.SlotIndex].AttackerId == attackerId)
            {
                ClearSlot(table, claim.SlotIndex);
            }

            if (CountOccupied(table) == 0)
            {
                _byTarget.Remove(claim.TargetId);
            }
        }

        /// <summary>Release every claim on a target (target death).</summary>
        public void ReleaseAllForTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId) || !_byTarget.TryGetValue(targetId, out var table))
            {
                return;
            }

            for (var i = 0; i < table.Slots.Length; i++)
            {
                var id = table.Slots[i].AttackerId;
                if (id == null)
                {
                    continue;
                }

                table.Slots[i].AttackerId = null;
                if (_byAttacker.TryGetValue(id, out var claim) && claim.TargetId == targetId)
                {
                    _byAttacker.Remove(id);
                }
            }

            _byTarget.Remove(targetId);
        }

        public bool TryGetClaim(string attackerId, out Vector3 worldPos)
        {
            worldPos = default;
            if (string.IsNullOrEmpty(attackerId) ||
                !_byAttacker.TryGetValue(attackerId, out var claim) ||
                !_byTarget.TryGetValue(claim.TargetId, out var table) ||
                claim.SlotIndex < 0 ||
                claim.SlotIndex >= table.Slots.Length)
            {
                return false;
            }

            ref var slot = ref table.Slots[claim.SlotIndex];
            if (slot.AttackerId != attackerId)
            {
                return false;
            }

            worldPos = slot.WorldPos;
            return true;
        }

        /// <summary>Current claimed target id for an attacker (if any).</summary>
        public bool TryGetClaimedTargetId(string attackerId, out string targetId)
        {
            targetId = null;
            if (string.IsNullOrEmpty(attackerId) ||
                !_byAttacker.TryGetValue(attackerId, out var claim) ||
                string.IsNullOrEmpty(claim.TargetId))
            {
                return false;
            }

            targetId = claim.TargetId;
            return true;
        }

        /// <summary>Occupied slot count for a target (0 if unknown).</summary>
        public int GetOccupiedCount(string targetId)
        {
            if (string.IsNullOrEmpty(targetId) || !_byTarget.TryGetValue(targetId, out var table))
            {
                return 0;
            }

            return CountOccupied(table);
        }

        /// <summary>Last ring-anchor position used for a target's slot table (for tests / debug).</summary>
        public bool TryGetTargetAnchor(string targetId, out Vector3 anchorPos, out float ringRadius)
        {
            anchorPos = default;
            ringRadius = 0f;
            if (string.IsNullOrEmpty(targetId) || !_byTarget.TryGetValue(targetId, out var table))
            {
                return false;
            }

            anchorPos = table.AnchorPos;
            ringRadius = table.RingRadius;
            return true;
        }

        public void Clear()
        {
            _byTarget.Clear();
            _byAttacker.Clear();
        }

        private TargetSlotTable EnsureTable(
            string targetId,
            int slotCount,
            float ringRadius,
            Vector3 targetPos)
        {
            if (_byTarget.TryGetValue(targetId, out var table))
            {
                if (table.Slots.Length != slotCount)
                {
                    // Capacity / mode change: drop claims on this target and rebuild ring.
                    for (var i = 0; i < table.Slots.Length; i++)
                    {
                        var id = table.Slots[i].AttackerId;
                        if (id != null &&
                            _byAttacker.TryGetValue(id, out var c) &&
                            c.TargetId == targetId)
                        {
                            _byAttacker.Remove(id);
                        }
                    }

                    table = CreateTable(targetId, slotCount, ringRadius, targetPos);
                    _byTarget[targetId] = table;
                }

                return table;
            }

            table = CreateTable(targetId, slotCount, ringRadius, targetPos);
            _byTarget[targetId] = table;
            return table;
        }

        private static TargetSlotTable CreateTable(string targetId, int slotCount, float ringRadius, Vector3 targetPos)
        {
            var table = new TargetSlotTable
            {
                TargetId = targetId,
                AnchorPos = targetPos,
                RingRadius = ringRadius,
                Slots = new SlotEntry[slotCount]
            };
            WriteRingPositions(table);
            return table;
        }

        private void MaybeRecomputePositions(TargetSlotTable table, float ringRadius, Vector3 targetPos)
        {
            var dx = targetPos.x - table.AnchorPos.x;
            var dz = targetPos.z - table.AnchorPos.z;
            var moved = dx * dx + dz * dz >
                        SlotReclaimMoveThreshold * SlotReclaimMoveThreshold;
            var radiusChanged = Mathf.Abs(table.RingRadius - ringRadius) > 1e-4f;
            if (!moved && !radiusChanged)
            {
                return;
            }

            table.AnchorPos = targetPos;
            table.RingRadius = ringRadius;
            WriteRingPositions(table);
        }

        private static void WriteRingPositions(TargetSlotTable table)
        {
            var n = table.Slots.Length;
            var twoPi = Mathf.PI * 2f;
            for (var k = 0; k < n; k++)
            {
                var angle = k * (twoPi / n);
                var ox = Mathf.Cos(angle) * table.RingRadius;
                var oz = Mathf.Sin(angle) * table.RingRadius;
                table.Slots[k].WorldPos = new Vector3(
                    table.AnchorPos.x + ox,
                    table.AnchorPos.y,
                    table.AnchorPos.z + oz);
                // AttackerId preserved across recompute.
            }
        }

        private bool IsAcceptableSlot(Vector3 slotPos, Vector3 targetPos, float minDistFromCenter)
        {
            if (!_walkable.IsSlotWalkable(slotPos.x, slotPos.y, slotPos.z))
            {
                return false;
            }

            if (minDistFromCenter <= 0f)
            {
                return true;
            }

            var dx = slotPos.x - targetPos.x;
            var dz = slotPos.z - targetPos.z;
            return dx * dx + dz * dz >= minDistFromCenter * minDistFromCenter;
        }

        private static Vector2 PreferredApproachXZ(Vector3 attackerPos, Vector3 targetPos)
        {
            var dx = attackerPos.x - targetPos.x;
            var dz = attackerPos.z - targetPos.z;
            var sqr = dx * dx + dz * dz;
            if (sqr < 1e-8f)
            {
                return Vector2.zero;
            }

            var inv = 1f / Mathf.Sqrt(sqr);
            return new Vector2(dx * inv, dz * inv);
        }

        private static float ScoreSlot(Vector3 slotPos, Vector3 targetPos, Vector2 preferredXZ)
        {
            if (preferredXZ.sqrMagnitude < 1e-8f)
            {
                return 0f;
            }

            var dx = slotPos.x - targetPos.x;
            var dz = slotPos.z - targetPos.z;
            var sqr = dx * dx + dz * dz;
            if (sqr < 1e-8f)
            {
                return float.NegativeInfinity;
            }

            var inv = 1f / Mathf.Sqrt(sqr);
            return dx * inv * preferredXZ.x + dz * inv * preferredXZ.y;
        }

        private static void ClearSlot(TargetSlotTable table, int index)
        {
            ref var slot = ref table.Slots[index];
            if (slot.HasClaimerPos)
            {
                table.CentroidSumX -= slot.ClaimerPos.x;
                table.CentroidSumZ -= slot.ClaimerPos.z;
                slot.ClaimerPos = default;
                slot.HasClaimerPos = false;
            }

            slot.AttackerId = null;
        }

        // Rolling centroid of claimer positions (SPEC_04 §9.7: gap = back sector vs
        // "target←attacker centroid"). Refreshed on keep / claim, subtracted on release.
        private static void UpdateClaimerPos(
            TargetSlotTable table, int index, Vector3 attackerPos, Vector3 targetPos)
        {
            if (!IsValidClaimerPos(attackerPos, targetPos))
            {
                return;
            }

            ref var slot = ref table.Slots[index];
            if (slot.HasClaimerPos)
            {
                table.CentroidSumX += attackerPos.x - slot.ClaimerPos.x;
                table.CentroidSumZ += attackerPos.z - slot.ClaimerPos.z;
            }
            else
            {
                table.CentroidSumX += attackerPos.x;
                table.CentroidSumZ += attackerPos.z;
            }

            slot.ClaimerPos = attackerPos;
            slot.HasClaimerPos = true;
        }

        private static bool IsValidClaimerPos(Vector3 attackerPos, Vector3 targetPos)
        {
            var dx = attackerPos.x - targetPos.x;
            var dz = attackerPos.z - targetPos.z;
            return dx * dx + dz * dz > 1e-8f;
        }

        /// <summary>
        /// True when <paramref name="slotPos"/> falls inside the surround gap sector.
        /// No surround / zero gap / degenerate axis → false (legacy full ring).
        /// On an empty table the current claimer's position seeds the axis so the very
        /// first claim already respects the gap. Boundary step exactly at ±half-width
        /// counts as outside (claims stay dense).
        /// </summary>
        private static bool IsSlotInGap(
            TargetSlotTable table,
            Vector3 slotPos,
            Vector3 targetPos,
            SurroundParams? surround,
            Vector3 fallbackPos)
        {
            if (!surround.HasValue)
            {
                return false;
            }

            var gapDeg = Mathf.Clamp(surround.Value.GapDegrees, 0f, 360f);
            if (gapDeg <= 0f)
            {
                return false;
            }

            if (!TryComputeGapCenterDegrees(table, targetPos, surround.Value, fallbackPos, out var centerDeg))
            {
                return false;
            }

            var slotDeg = Mathf.Atan2(slotPos.z - targetPos.z, slotPos.x - targetPos.x) *
                          Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle(slotDeg, centerDeg)) < gapDeg * 0.5f;
        }

        /// <summary>Gap center for tests/debug; false when no centroid can be formed.</summary>
        public bool TryGetGapCenterDegrees(
            string targetId, Vector3 targetPos, SurroundParams surround, out float centerDeg)
        {
            centerDeg = 0f;
            if (string.IsNullOrEmpty(targetId) ||
                !_byTarget.TryGetValue(targetId, out var table))
            {
                return false;
            }

            return TryComputeGapCenterDegrees(table, targetPos, surround, targetPos, out centerDeg);
        }

        private static bool TryComputeGapCenterDegrees(
            TargetSlotTable table,
            Vector3 targetPos,
            SurroundParams surround,
            Vector3 fallbackPos,
            out float centerDeg)
        {
            centerDeg = 0f;
            if (surround.GapDir == SurroundGapDirection.Random)
            {
                // Debug only: deterministic per-target angle, no runtime RNG.
                var h = unchecked((table.TargetId ?? string.Empty).GetHashCode());
                centerDeg = (h & 0xFF) * (360f / 256f);
                return true;
            }

            float centroidX;
            float centroidZ;
            var occupied = CountOccupied(table);
            if (occupied > 0)
            {
                centroidX = table.CentroidSumX / occupied;
                centroidZ = table.CentroidSumZ / occupied;
            }
            else if (IsValidClaimerPos(fallbackPos, targetPos))
            {
                // Empty table: seed the approach axis with this claimer's position.
                centroidX = fallbackPos.x;
                centroidZ = fallbackPos.z;
            }
            else
            {
                return false;
            }
            var axisX = targetPos.x - centroidX; // approach axis: centroid → target
            var axisZ = targetPos.z - centroidZ;
            if (axisX * axisX + axisZ * axisZ < 1e-8f)
            {
                return false;
            }

            var baseDeg = Mathf.Atan2(axisZ, axisX) * Mathf.Rad2Deg;
            switch (surround.GapDir)
            {
                case SurroundGapDirection.Bottom: // far side beyond target (SPEC default)
                    centerDeg = baseDeg;
                    return true;
                case SurroundGapDirection.Top: // toward the attackers
                    centerDeg = baseDeg + 180f;
                    return true;
                case SurroundGapDirection.Left:
                    centerDeg = baseDeg + 90f;
                    return true;
                case SurroundGapDirection.Right:
                    centerDeg = baseDeg - 90f;
                    return true;
                default:
                    return false;
            }
        }

        private static int CountOccupied(TargetSlotTable table)
        {
            var n = 0;
            for (var i = 0; i < table.Slots.Length; i++)
            {
                if (table.Slots[i].AttackerId != null)
                {
                    n++;
                }
            }

            return n;
        }

        private struct AttackerClaim
        {
            public string TargetId;
            public int SlotIndex;
        }

        private sealed class TargetSlotTable
        {
            public string TargetId;
            public Vector3 AnchorPos;
            public float RingRadius;
            public SlotEntry[] Slots;
            public float CentroidSumX;
            public float CentroidSumZ;
        }

        private struct SlotEntry
        {
            public string AttackerId;
            public Vector3 WorldPos;
            public Vector3 ClaimerPos;
            public bool HasClaimerPos;
        }
    }
}
