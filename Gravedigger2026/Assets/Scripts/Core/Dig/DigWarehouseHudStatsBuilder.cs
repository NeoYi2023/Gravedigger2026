using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Dig HUD Warehouse three-row stats (SPEC_03 §3.10 WarehouseHudStats).
    /// </summary>
    public sealed class DigWarehouseHudStats
    {
        public const string RaceUndead = "Race_Undead";
        public const string RaceOrc = "Race_Orc";
        public const string RaceElf = "Race_Elf";
        public const string RaceHuman = "Race_Human";

        public float Spirit;
        public int WreckCount;
        public int UndeadPrimaryHand;
        public int OrcPrimaryHand;
        public int ElfPrimaryHand;
        public int HumanPrimaryHand;
        public int WarriorPrimaryHand;
        public int ArcherPrimaryHand;
        public int MagePrimaryHand;
        public int ThiefPrimaryHand;
    }

    /// <summary>
    /// Aggregates warehouse materials into Dig HUD icon stats (pure C#).
    /// </summary>
    public static class DigWarehouseHudStatsBuilder
    {
        public static DigWarehouseHudStats Build(WarehouseService warehouse, ConfigCsvRepository configs)
        {
            var stats = new DigWarehouseHudStats();
            if (warehouse == null)
            {
                return stats;
            }

            stats.Spirit = warehouse.SpiritEssence;
            if (configs == null)
            {
                return stats;
            }

            foreach (var kv in warehouse.Materials)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value <= 0)
                {
                    continue;
                }

                if (!configs.TryGetBodyPart(kv.Key, out var part) || part == null)
                {
                    continue;
                }

                if (part.IsPrimaryHand != 1)
                {
                    stats.WreckCount += kv.Value;
                    continue;
                }

                switch (part.RaceId)
                {
                    case DigWarehouseHudStats.RaceUndead:
                        stats.UndeadPrimaryHand += kv.Value;
                        break;
                    case DigWarehouseHudStats.RaceOrc:
                        stats.OrcPrimaryHand += kv.Value;
                        break;
                    case DigWarehouseHudStats.RaceElf:
                        stats.ElfPrimaryHand += kv.Value;
                        break;
                    case DigWarehouseHudStats.RaceHuman:
                        stats.HumanPrimaryHand += kv.Value;
                        break;
                }

                switch (part.BaseClass)
                {
                    case BaseClassKind.Warrior:
                        stats.WarriorPrimaryHand += kv.Value;
                        break;
                    case BaseClassKind.Archer:
                        stats.ArcherPrimaryHand += kv.Value;
                        break;
                    case BaseClassKind.Mage:
                        stats.MagePrimaryHand += kv.Value;
                        break;
                    case BaseClassKind.Thief:
                        stats.ThiefPrimaryHand += kv.Value;
                        break;
                }
            }

            return stats;
        }
    }
}
