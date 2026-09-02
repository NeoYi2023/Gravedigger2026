using System;
using System.Collections.Generic;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Read-only merge of instance SoldierSkills + formation ExclusiveSkillIds (SPEC_03 §3.18 / TF-05).
    /// Never writes the instance list.
    /// </summary>
    public static class TacticalFormationSkillOverlay
    {
        public static IReadOnlyList<SoldierSkillEntry> MergeForCast(
            IReadOnlyList<SoldierSkillEntry> skills,
            ITacticalFormationOverlayLookup overlay,
            string warriorId)
        {
            if (overlay == null || string.IsNullOrEmpty(warriorId) || !overlay.IsOverlayActive(warriorId))
            {
                return skills ?? Array.Empty<SoldierSkillEntry>();
            }

            var extra = overlay.GetExclusiveSkillIds(warriorId);
            if (extra == null || extra.Count == 0)
            {
                return skills ?? Array.Empty<SoldierSkillEntry>();
            }

            var merged = new List<SoldierSkillEntry>(
                (skills != null ? skills.Count : 0) + extra.Count);
            if (skills != null)
            {
                for (var i = 0; i < skills.Count; i++)
                {
                    merged.Add(skills[i]);
                }
            }

            for (var i = 0; i < extra.Count; i++)
            {
                var skillId = extra[i];
                if (string.IsNullOrEmpty(skillId) || ContainsSkillId(merged, skillId))
                {
                    continue;
                }

                merged.Add(new SoldierSkillEntry
                {
                    SkillId = skillId,
                    SkillLevel = 1
                });
            }

            return merged;
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
