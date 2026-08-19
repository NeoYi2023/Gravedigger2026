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
            in StatBlock raceAdjust,
            float classBaseMoveSpeed)
        {
            var b = kind == StatKind.MoveSpeed
                ? ResolveMoveSpeedBase(classBaseMoveSpeed)
                : baseStats.Get(kind);
            var raw = b + equip.Get(kind) + b * gemMult.Get(kind) + b * raceAdjust.Get(kind);
            return Math.Max(0f, raw);
        }

        public static StatBlock ComputeStaticStats(
            in StatBlock baseStats,
            in StatBlock equip,
            in StatBlock gemMult,
            in StatBlock raceAdjust,
            float classBaseMoveSpeed)
        {
            var result = new StatBlock();
            for (var kind = StatKind.MaxHP; kind <= StatKind.Intelligence; kind++)
            {
                result.Set(
                    kind,
                    ComputeStaticStat(kind, baseStats, equip, gemMult, raceAdjust, classBaseMoveSpeed));
            }

            return result;
        }

        public static float ResolveMoveSpeedBase(float classBaseMoveSpeed)
        {
            return classBaseMoveSpeed > 0.01f ? classBaseMoveSpeed : ClassConfigRow.DefaultBaseMoveSpeed;
        }

        /// <summary>
        /// HP-dim exception: MaxHP = ceil(BodyLife + Str × MaxHpStrengthMult).
        /// </summary>
        public static int ComputeMaxHP(float bodyLife, float strength, float maxHpStrengthMult)
        {
            return (int)Math.Ceiling(bodyLife + strength * maxHpStrengthMult);
        }

        public static float ComputeBodyLife(in StatBlock baseStats, in StatBlock equip)
        {
            return baseStats.MaxHP + equip.MaxHP;
        }
    }
}
