using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Soldier combat derives (SPEC_03 §3.12). Demo uses StaticStat (no SkillBuff yet).
    /// </summary>
    public static class WarriorCombatMath
    {
        public static StatBlock ComputeBattleStats(WarriorInstance warrior)
        {
            if (warrior == null)
            {
                throw new ArgumentNullException(nameof(warrior));
            }

            return WarriorStatMath.ComputeStaticStats(
                warrior.BaseStats,
                warrior.EquipStats,
                warrior.GemMult,
                warrior.RaceAdjustCoeff);
        }

        public static float ResolvePrimary(in StatBlock stats, StatKind primaryStat)
        {
            switch (primaryStat)
            {
                case StatKind.Strength:
                    return stats.Strength;
                case StatKind.Agility:
                    return stats.Agility;
                case StatKind.Intelligence:
                    return stats.Intelligence;
                default:
                    return 0f;
            }
        }

        public static float ComputeNormalAttackPower(float primary, in CombatConvertCoeffs coeffs)
        {
            return primary * coeffs.NormalAttackPrimaryMult;
        }

        public static float ComputeAttackSpeed(float agility, in CombatConvertCoeffs coeffs)
        {
            return coeffs.AttackSpeedBase + coeffs.AttackSpeedAgiDiv / Math.Max(agility, 1f);
        }

        public static int ComputeBattleMaxHp(
            WarriorInstance warrior,
            in StatBlock battleStats,
            float maxHpStrengthMult)
        {
            var bodyLife = warrior != null
                ? (warrior.BodyLife > 0f
                    ? warrior.BodyLife
                    : WarriorStatMath.ComputeBodyLife(warrior.BaseStats, warrior.EquipStats))
                : 0f;
            return WarriorStatMath.ComputeMaxHP(bodyLife, battleStats.Strength, maxHpStrengthMult);
        }
    }
}
