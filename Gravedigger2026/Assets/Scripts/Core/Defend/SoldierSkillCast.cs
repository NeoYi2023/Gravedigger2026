using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Skill_03 burst lookup + Skill_01 block hook + Skill_02 Comfort outgoing mul
    /// (SPEC_03 §3.12 SkillCast / D-069 Approach C + SC-02 B + SC-03 A).
    /// Hard-maps SkillEffect_03_* → 3 scheme-D hits, SkillEffect_01_* → block chance,
    /// SkillEffect_02_* → full-HP outgoing bonus; not a generic effect parser.
    /// </summary>
    public static class SoldierSkillCast
    {
        public const string Skill01Id = "Skill_01";
        public const string Skill02Id = "Skill_02";
        public const string Skill03Id = "Skill_03";
        public const string CastTargetSelf = "Self";
        public const string CastTargetEnemySingle = "EnemySingle";
        public const string CooldownMode2 = "Mode2";
        public const string ExtraConditionEnemyNormalHitSelf = "敌人普攻命中Self";
        public const string ExtraConditionSelfHpFull = "自身血量=100%";
        public const int BurstHitCount = 3;

        public static bool TryResolveSkill03(
            IReadOnlyList<SoldierSkillEntry> skills,
            ConfigCsvRepository configs,
            out SkillConfigRow row)
        {
            row = null;
            if (skills == null || configs == null)
            {
                return false;
            }

            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry == null ||
                    !string.Equals(entry.SkillId, Skill03Id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out row) || row == null)
                {
                    return false;
                }

                return IsCastableSkill03(row);
            }

            return false;
        }

        public static bool IsCastableSkill03(SkillConfigRow row)
        {
            if (row == null)
            {
                return false;
            }

            if (!string.Equals(row.CooldownMode, CooldownMode2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(row.CastTarget, CastTargetEnemySingle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(row.ExtraActivationCondition);
        }

        public static bool TryResolveSkill01(
            IReadOnlyList<SoldierSkillEntry> skills,
            ConfigCsvRepository configs,
            out SkillConfigRow row)
        {
            row = null;
            if (skills == null || configs == null)
            {
                return false;
            }

            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry == null ||
                    !string.Equals(entry.SkillId, Skill01Id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out row) || row == null)
                {
                    return false;
                }

                if (!IsPassiveSkill01(row))
                {
                    row = null;
                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool IsPassiveSkill01(SkillConfigRow row)
        {
            if (row == null)
            {
                return false;
            }

            if (!string.Equals(row.CooldownMode, CooldownMode2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(row.CastTarget, CastTargetSelf, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var extra = row.ExtraActivationCondition != null
                ? row.ExtraActivationCondition.Trim()
                : string.Empty;
            return string.Equals(extra, ExtraConditionEnemyNormalHitSelf, StringComparison.Ordinal);
        }

        /// <summary>
        /// Lv1–5 hard-map aligned with SkillConfig Description / SkillEffect_01_* Notes.
        /// Unknown level → 0 (no block).
        /// </summary>
        public static float BlockChanceForSkillLevel(int skillLevel)
        {
            switch (skillLevel)
            {
                case 1: return 0.10f;
                case 2: return 0.15f;
                case 3: return 0.20f;
                case 4: return 0.25f;
                case 5: return 0.30f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Independent on-hit hook. Returns true when this incoming enemy AA should deal 0.
        /// Does not occupy the AA channel, start CD, or fire extra LOC roll.
        /// </summary>
        public static bool TryRollSkill01Block(
            IReadOnlyList<SoldierSkillEntry> skills,
            ConfigCsvRepository configs,
            out SkillConfigRow row,
            out float chance)
        {
            chance = 0f;
            if (!TryResolveSkill01(skills, configs, out row) || row == null)
            {
                return false;
            }

            chance = BlockChanceForSkillLevel(row.SkillLevel);
            if (chance <= 0f)
            {
                return false;
            }

            return UnityEngine.Random.value < chance;
        }

        public static bool TryResolveSkill02(
            IReadOnlyList<SoldierSkillEntry> skills,
            ConfigCsvRepository configs,
            out SkillConfigRow row)
        {
            row = null;
            if (skills == null || configs == null)
            {
                return false;
            }

            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry == null ||
                    !string.Equals(entry.SkillId, Skill02Id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out row) || row == null)
                {
                    return false;
                }

                if (!IsPassiveSkill02(row))
                {
                    row = null;
                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool IsPassiveSkill02(SkillConfigRow row)
        {
            if (row == null)
            {
                return false;
            }

            if (!string.Equals(row.CooldownMode, CooldownMode2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(row.CastTarget, CastTargetSelf, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var extra = row.ExtraActivationCondition != null
                ? row.ExtraActivationCondition.Trim()
                : string.Empty;
            return string.Equals(extra, ExtraConditionSelfHpFull, StringComparison.Ordinal);
        }

        /// <summary>
        /// Lv1–5 hard-map aligned with SkillConfig Description / SkillEffect_02_* Notes.
        /// Unknown level → 0 (no bonus).
        /// </summary>
        public static float OutgoingDamageBonusForSkillLevel(int skillLevel)
        {
            switch (skillLevel)
            {
                case 1: return 0.05f;
                case 2: return 0.10f;
                case 3: return 0.15f;
                case 4: return 0.20f;
                case 5: return 0.25f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Independent outgoing-mul hook. Returns true when this hit should apply Comfort.
        /// Does not occupy the AA channel, start CD, or fire extra LOC roll.
        /// <paramref name="row"/> is set whenever Skill_02 resolves (even if not full HP).
        /// </summary>
        public static bool TryGetSkill02OutgoingBonus(
            IReadOnlyList<SoldierSkillEntry> skills,
            ConfigCsvRepository configs,
            float remainingHp,
            float maxHp,
            out SkillConfigRow row,
            out float bonus)
        {
            bonus = 0f;
            if (!TryResolveSkill02(skills, configs, out row) || row == null)
            {
                return false;
            }

            bonus = OutgoingDamageBonusForSkillLevel(row.SkillLevel);
            if (bonus <= 0f)
            {
                return false;
            }

            return remainingHp >= maxHp;
        }
    }
}
