using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Recomputes Combat derived stats from WarriorInstance + StatMul overlay
    /// without mutating <c>WarriorInstance.BaseStats</c> (SPEC_03 §3.18 / TF-05).
    /// </summary>
    public static class WarriorCombatDerivedStats
    {
        public static void Refresh(
            DefendCombatWarriorState state,
            WarriorInstance warrior,
            ClassConfigRow classRow,
            CombatStatMulBuff combatBuff,
            ConfigCsvRepository configs)
        {
            if (state == null || warrior == null)
            {
                return;
            }

            var battleStats = WarriorCombatMath.ComputeBattleStats(
                warrior,
                WarriorCombatMath.ResolveClassBaseMoveSpeed(classRow));
            var bodyLife = warrior.BodyLife > 0f
                ? warrior.BodyLife
                : WarriorStatMath.ComputeBodyLife(warrior.BaseStats, warrior.EquipStats);
            bodyLife = combatBuff.ApplyToBodyLife(bodyLife);
            combatBuff.ApplyToBattleStats(ref battleStats);
            var coeffDefaults = configs != null
                ? configs.GetCombatConvertCoeffDefaults()
                : CombatConvertCoeffs.SafetyDefaults;
            var coeffs = CombatConvertCoeffs.Parse(
                classRow != null ? classRow.CombatConvertCoeffs : null,
                coeffDefaults);
            var primaryKind = classRow != null ? classRow.PrimaryStat : StatKind.Strength;
            var primary = WarriorCombatMath.ResolvePrimary(battleStats, primaryKind);
            var maxHpMult = configs != null
                ? configs.GetMaxHpStrengthMult()
                : CombatConvertCoeffs.SafetyMaxHpStrengthMult;
            var maxHp = WarriorStatMath.ComputeMaxHP(bodyLife, battleStats.Strength, maxHpMult);
            state.MaxHp = maxHp;
            state.RemainingHp = Math.Min(Math.Max(0f, state.RemainingHp), maxHp);
            state.NormalAttackPower = WarriorCombatMath.ComputeNormalAttackPower(primary, coeffs);
            state.AttackSpeed = WarriorCombatMath.ComputeAttackSpeed(battleStats.Agility, coeffs);
            state.MoveSpeed = Math.Max(0.1f, battleStats.MoveSpeed > 0.01f ? battleStats.MoveSpeed : 3.5f);
        }
    }
}
