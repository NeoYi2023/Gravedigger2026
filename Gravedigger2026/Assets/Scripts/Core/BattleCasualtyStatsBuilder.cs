using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>Aggregates loyal combat casualties for battle settlement UI.</summary>
    public static class BattleCasualtyStatsBuilder
    {
        public static BattleCasualtyStats Build(IEnumerable<DefendCombatWarriorState> warriors)
        {
            var stats = new BattleCasualtyStats();
            if (warriors == null)
            {
                return stats;
            }

            foreach (var w in warriors)
            {
                if (w == null || w.IsRebel)
                {
                    continue;
                }

                if (!w.IsCombatDead && !w.IsPermanentDead && w.RemainingHp > 0f)
                {
                    continue;
                }

                stats.Total++;
                switch (w.BaseClass)
                {
                    case BaseClassKind.Warrior:
                        stats.Warrior++;
                        break;
                    case BaseClassKind.Archer:
                        stats.Archer++;
                        break;
                    case BaseClassKind.Mage:
                        stats.Mage++;
                        break;
                    case BaseClassKind.Thief:
                        stats.Thief++;
                        break;
                }
            }

            return stats;
        }
    }
}
