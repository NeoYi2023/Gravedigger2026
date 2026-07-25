using System;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Static-layer soldier math (SPEC_03 §3.11). Runtime SkillBuff (FinalStat) is added by combat.
    /// </summary>
    public static class WarriorStatMath
    {
        public static float ComputeStaticStat(
            StatKind kind,
            in StatBlock baseStats,
            in StatBlock equip,
            in StatBlock gemMult,
            in StatBlock raceAdjust)
        {
            var b = baseStats.Get(kind);
            var raw = b + equip.Get(kind) + b * gemMult.Get(kind) + b * raceAdjust.Get(kind);
            return Math.Max(0f, raw);
        }

        public static StatBlock ComputeStaticStats(
            in StatBlock baseStats,
            in StatBlock equip,
            in StatBlock gemMult,
            in StatBlock raceAdjust)
        {
            var result = new StatBlock();
            for (var kind = StatKind.MaxHP; kind <= StatKind.Intelligence; kind++)
            {
                result.Set(kind, ComputeStaticStat(kind, baseStats, equip, gemMult, raceAdjust));
            }

            return result;
        }

        /// <summary>
        /// HP-dim exception: MaxHP = ceil(BodyLife + Str × 3); BodyLife = Base(MaxHP) + Equip(MaxHP).
        /// </summary>
        public static int ComputeMaxHP(float bodyLife, float strength)
        {
            return (int)Math.Ceiling(bodyLife + strength * 3f);
        }

        public static float ComputeBodyLife(in StatBlock baseStats, in StatBlock equip)
        {
            return baseStats.MaxHP + equip.MaxHP;
        }
    }
}
