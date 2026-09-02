using System.Collections.Generic;
using Gravedigger2026.Core.Combat;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Read-only Combat overlay for an active tactical-formation member
    /// (SPEC_03 §3.18 / TF-05). Does not mutate <c>WarriorInstance</c>.
    /// </summary>
    public interface ITacticalFormationOverlayLookup
    {
        bool IsOverlayActive(string warriorId);

        bool TryGetStatMul(string warriorId, out CombatStatMulBuff statMul);

        /// <summary>Never null; empty when inactive.</summary>
        IReadOnlyList<string> GetExclusiveSkillIds(string warriorId);

        /// <summary>Never null; empty when inactive.</summary>
        IReadOnlyList<string> GetExclusiveSkillEffectIds(string warriorId);
    }
}
