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
