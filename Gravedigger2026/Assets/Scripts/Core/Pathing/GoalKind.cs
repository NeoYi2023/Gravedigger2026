namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Destination kind from rules layer (SPEC_03 §3.12 / SPEC_04 §9.7).
    /// Move service resolves world DesiredDestination; Views do not invent goals.
    /// </summary>
    public enum GoalKind
    {
        /// <summary>Shared PushMap objective — sample FlowField.</summary>
        Objective = 0,

        /// <summary>Defend return home (MP-06).</summary>
        FormationHome = 1,

        /// <summary>Chase/attack — claimed AttackSlot world point.</summary>
        AttackSlot = 2,

        /// <summary>Optional chase anchor before slot resolve; Demo treats like AttackSlot once claimed.</summary>
        ChaseAnchor = 3,

        /// <summary>Tactical formation member — straight seek center+rotated slot (SPEC_03 §3.18).</summary>
        FormationSlot = 4
    }
}
