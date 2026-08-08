using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Combat move mode layered on GoalKind (SPEC_03 §3.12 Approach B+ / SPEC_04 §9.7).
    /// <b>No Follow</b> — protagonist stickiness / ArmyRadius is explicitly out of scope.
    /// <see cref="Sweep"/> is P2: enum value kept, intentionally unwired.
    /// </summary>
    public enum CombatMoveMode
    {
        /// <summary>Straight (+ LocalDetour) toward the claimed slot; default mode.</summary>
        Chase = 0,

        /// <summary>AttackSlot ring claim that skips the surround-gap sector (SC-02).</summary>
        Surround = 1,

        /// <summary>P2 reserved (boss-wave / charge tangent advance). Not wired in Demo.</summary>
        Sweep = 2,
    }

    /// <summary>
    /// SC-03 derivation policy (SPEC_03 §3.12): GoalKind AttackSlot/ChaseAnchor + Melee →
    /// <see cref="CombatMoveMode.Surround"/> (multi-vs-one leaves a ring gap); everything
    /// else → <see cref="CombatMoveMode.Chase"/>. Objective / FormationHome carry no
    /// independent move mode (FlowField / direct-home + hold separation stay as before).
    /// </summary>
    public static class CombatMoveModePolicy
    {
        public static CombatMoveMode Derive(GoalKind kind, AttackMode attackMode)
        {
            if ((kind == GoalKind.AttackSlot || kind == GoalKind.ChaseAnchor) &&
                attackMode == AttackMode.Melee)
            {
                return CombatMoveMode.Surround;
            }

            return CombatMoveMode.Chase;
        }

        /// <summary>Surround → <see cref="SurroundParams.Default"/>; otherwise null (legacy full ring).</summary>
        public static SurroundParams? SurroundOrNull(CombatMoveMode mode)
        {
            return mode == CombatMoveMode.Surround ? SurroundParams.Default : (SurroundParams?)null;
        }

        /// <summary>One-call helper for TryClaim sites: surround params only for melee chase goals.</summary>
        public static SurroundParams? SurroundFor(GoalKind kind, AttackMode attackMode)
        {
            return SurroundOrNull(Derive(kind, attackMode));
        }
    }
}
