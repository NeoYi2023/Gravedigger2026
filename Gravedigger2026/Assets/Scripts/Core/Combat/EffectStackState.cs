namespace Gravedigger2026.Core.Combat
{
    /// <summary>Per-warrior stack accumulator for timed outgoing-bonus effects (e.g. Skill_09).</summary>
    public sealed class EffectStackState
    {
        /// <summary>Total additive outgoing bonus (e.g. 0.6 = +60%).</summary>
        public float CurrentBonus;
    }
}
