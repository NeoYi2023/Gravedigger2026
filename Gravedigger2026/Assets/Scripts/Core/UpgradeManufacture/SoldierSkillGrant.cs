using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Shared manufacture grant for class DefaultSkillIds @ Lv1 (SPEC_03 §3.11 / SPEC_04 §9.9b).
    /// Mode1: ManufactureService after ClassId is final. Mode2 reuses this then SoldierSkillLevelAdd (SS-04).
    /// </summary>
    public static class SoldierSkillGrant
    {
        public const int DefaultGrantLevel = 1;

        /// <summary>
        /// Clears <paramref name="target"/> then writes each DefaultSkillId as { SkillId, SkillLevel=1 }.
        /// Missing (SkillId, 1) row → skip + Warning. Duplicate Ids keep first. Empty column → empty list.
        /// </summary>
        public static void GrantDefaultSkillsAtLevel1(
            string classId,
            ConfigCsvRepository configs,
            List<SoldierSkillEntry> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            if (configs == null || string.IsNullOrEmpty(classId))
            {
                return;
            }

            if (!configs.TryGetClass(classId, out var classRow) || classRow == null)
            {
                Debug.LogWarning($"[SoldierSkillGrant] ClassId '{classId}' not found; SoldierSkills empty.");
                return;
            }

            var ids = classRow.DefaultSkillIds;
            if (ids == null || ids.Length == 0)
            {
                return;
            }

            for (var i = 0; i < ids.Length; i++)
            {
                var skillId = ids[i];
                if (string.IsNullOrEmpty(skillId) || ContainsSkillId(target, skillId))
                {
                    continue;
                }

                if (!configs.TryGetSkill(skillId, DefaultGrantLevel, out var skillRow) || skillRow == null)
                {
                    Debug.LogWarning(
                        $"[SoldierSkillGrant] Skip '{skillId}' for {classId}: missing SkillConfig ({skillId}, {DefaultGrantLevel}).");
                    continue;
                }

                target.Add(new SoldierSkillEntry
                {
                    SkillId = skillId,
                    SkillLevel = DefaultGrantLevel
                });
            }
        }

        public static void GrantDefaultSkillsAtLevel1(WarriorInstance instance, ConfigCsvRepository configs)
        {
            if (instance == null)
            {
                return;
            }

            GrantDefaultSkillsAtLevel1(instance.ClassId, configs, instance.SoldierSkills);
        }

        /// <summary>
        /// ΣSkillBonus from baked SoldierSkills only (SPEC_03 §3.11). Soul/Gem Skills parallel still TBD.
        /// Missing SkillConfig row contributes 0.
        /// </summary>
        public static float SumLossOfControlChanceBonus(
            IReadOnlyList<SoldierSkillEntry> skills,
            ConfigCsvRepository configs)
        {
            if (skills == null || skills.Count == 0 || configs == null)
            {
                return 0f;
            }

            var sum = 0f;
            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                {
                    continue;
                }

                if (configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out var row) && row != null)
                {
                    sum += row.LossOfControlChanceBonus;
                }
            }

            return sum;
        }

        /// <summary>e.g. Skill_01@1 or Skill_01@1,Skill_02@1. Empty list → empty string.</summary>
        public static string FormatSummary(IReadOnlyList<SoldierSkillEntry> skills)
        {
            if (skills == null || skills.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(',');
                }

                sb.Append(entry.SkillId);
                sb.Append('@');
                sb.Append(entry.SkillLevel);
            }

            return sb.ToString();
        }

        private static bool ContainsSkillId(List<SoldierSkillEntry> skills, string skillId)
        {
            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry != null && string.Equals(entry.SkillId, skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
