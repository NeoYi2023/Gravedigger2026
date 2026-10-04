using Gravedigger2026.Core.Pathing;
using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Idle fallback when a member does not seek <see cref="GoalKind.FormationSlot"/>
    /// (SPEC_04 §9.7 <c>KeepFormationWhileEngage=0</c>).
    /// </summary>
    public enum TacticalFormationIdleFallback
    {
        Objective = 0,
        FormationHome = 1
    }

    /// <summary>
    /// Shared PushMap/Defend member GoalKind resolve (SPEC_03 §3.18 / TF-04b Approach A).
    /// Stage still owns AttackSlot claim; this type only answers idle / leash / overflow.
    /// </summary>
    public static class TacticalFormationCombatGoalPolicy
    {
        public static bool TryResolveIdleGoal(
            TacticalFormationRuntimeService runtime,
            string warriorId,
            TacticalFormationIdleFallback fallback,
            Vector2 fallbackHomeXZ,
            out GoalKind kind,
            out Vector2 destXZ)
        {
            kind = default;
            destXZ = default;
            if (!IsMember(runtime, warriorId))
            {
                return false;
            }

            if (runtime.TryGetMoveParams(warriorId, out var moveParams)
                && !moveParams.KeepFormationWhileEngage)
            {
                return TryFallback(fallback, fallbackHomeXZ, out kind, out destXZ);
            }

            return TrySlot(runtime, warriorId, out kind, out destXZ);
        }

        /// <summary>
        /// Enemy center is beyond leash and the member cannot hit from here → keep slot
        /// (SPEC_03 §3.18 超 leash 不追 / 保持槽位).
        /// </summary>
        public static bool TryResolveBeyondLeashHold(
            TacticalFormationRuntimeService runtime,
            string warriorId,
            out GoalKind kind,
            out Vector2 destXZ)
        {
            kind = default;
            destXZ = default;
            return IsMember(runtime, warriorId) && TrySlot(runtime, warriorId, out kind, out destXZ);
        }

        /// <summary>
        /// No free AttackSlot: <c>KeepFormationWhileEngage</c> members return to slot;
        /// otherwise Stage keeps the existing overflow path.
        /// </summary>
        public static bool TryResolveOverflow(
            TacticalFormationRuntimeService runtime,
            string warriorId,
            TacticalFormationIdleFallback fallback,
            Vector2 fallbackHomeXZ,
            out GoalKind kind,
            out Vector2 destXZ)
        {
            kind = default;
            destXZ = default;
            if (!IsMember(runtime, warriorId))
            {
                return false;
            }

            if (runtime.TryGetMoveParams(warriorId, out var moveParams)
                && !moveParams.KeepFormationWhileEngage)
            {
                return false;
            }

            return TrySlot(runtime, warriorId, out kind, out destXZ)
                   || TryFallback(fallback, fallbackHomeXZ, out kind, out destXZ);
        }

        public static bool IsEnemyInsideLeash(
            TacticalFormationRuntimeService runtime,
            string warriorId,
            Vector2 enemyWorldXZ)
        {
            return runtime != null && runtime.TryIsWorldInsideLeash(warriorId, enemyWorldXZ);
        }

        /// <summary>
        /// Enemy center is inside the group-facing forward arc (D-094).
        /// Yaw 0 faces +Z. Arc is the full angle (default 90 → 45° each side).
        /// </summary>
        public static bool IsInsideFrontArc(
            Vector2 soldierXZ,
            Vector2 enemyXZ,
            float facingYawDegrees,
            float arcDegrees)
        {
            var toEnemy = enemyXZ - soldierXZ;
            if (toEnemy.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            var forward = TacticalFormationRuntimeService.RotateYaw(new Vector2(0f, 1f), facingYawDegrees);
            if (forward.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            var arc = arcDegrees > 0f
                ? arcDegrees
                : TacticalFormationMoveParams.DefaultFrontArcDegrees;
            return Vector2.Angle(forward, toEnemy) <= arc * 0.5f + 0.01f;
        }

        /// <summary>
        /// Soft-collision body contact: XZ center distance ≤ both BodyRadii (D-096).
        /// </summary>
        public static bool IsBodyContact(
            float centerDistanceXZ,
            float soldierBodyRadius,
            float enemyBodyRadius)
        {
            return centerDistanceXZ
                   <= Mathf.Max(0f, soldierBodyRadius) + Mathf.Max(0f, enemyBodyRadius) + 0.01f;
        }

        /// <summary>
        /// Hold-swing eligibility: front arc + AttackRange, or body contact (D-096).
        /// Caller keeps <see cref="GoalKind.FormationSlot"/> (in-place; no chase).
        /// </summary>
        public static bool IsEligibleHoldSwingTarget(
            Vector2 soldierXZ,
            Vector2 enemyXZ,
            float facingYawDegrees,
            float frontArcDegrees,
            float centerDistanceXZ,
            float attackRange,
            float soldierBodyRadius,
            float enemyBodyRadius)
        {
            if (IsBodyContact(centerDistanceXZ, soldierBodyRadius, enemyBodyRadius))
            {
                return true;
            }

            return IsInsideFrontArc(soldierXZ, enemyXZ, facingYawDegrees, frontArcDegrees)
                   && CombatReach.IsInAttackRange(
                       centerDistanceXZ,
                       attackRange,
                       soldierBodyRadius,
                       enemyBodyRadius);
        }

        public static Vector2 ClampAttackSlot(
            TacticalFormationRuntimeService runtime,
            string warriorId,
            Vector2 attackSlotWorldXZ)
        {
            if (runtime != null
                && runtime.TryClampMemberAttackSlot(warriorId, attackSlotWorldXZ, out var clamped))
            {
                return clamped;
            }

            return attackSlotWorldXZ;
        }

        private static bool IsMember(TacticalFormationRuntimeService runtime, string warriorId)
        {
            return runtime != null && runtime.IsMember(warriorId);
        }

        private static bool TrySlot(
            TacticalFormationRuntimeService runtime,
            string warriorId,
            out GoalKind kind,
            out Vector2 destXZ)
        {
            kind = GoalKind.FormationSlot;
            destXZ = default;
            if (!runtime.TryGetSlotWorldXZ(warriorId, out destXZ))
            {
                return false;
            }

            return true;
        }

        private static bool TryFallback(
            TacticalFormationIdleFallback fallback,
            Vector2 fallbackHomeXZ,
            out GoalKind kind,
            out Vector2 destXZ)
        {
            if (fallback == TacticalFormationIdleFallback.FormationHome)
            {
                kind = GoalKind.FormationHome;
                destXZ = fallbackHomeXZ;
                return true;
            }

            kind = GoalKind.Objective;
            destXZ = default;
            return true;
        }
    }
}
