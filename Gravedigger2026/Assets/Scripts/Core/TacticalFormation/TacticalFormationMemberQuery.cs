using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Who may join a new tactical group (SPEC_03 §3.18 / TFG-02).
    /// Class soft-preference is applied at slot assignment in LayoutService, not here.
    /// </summary>
    public static class TacticalFormationMemberQuery
    {
        public readonly struct EligibleMember
        {
            public readonly string WarriorId;
            public readonly int DeployIndex;
            public readonly BattleFormationEntry Entry;

            public EligibleMember(string warriorId, int deployIndex, BattleFormationEntry entry)
            {
                WarriorId = warriorId;
                DeployIndex = deployIndex;
                Entry = entry;
            }
        }

        /// <summary>
        /// Deployed, not already grouped, and any <c>SoldierSkills</c> row whose
        /// <c>SkillConfig.FormationId</c> equals <paramref name="formationId"/>.
        /// Sorted by deploy order, then <c>WarriorId</c>.
        /// </summary>
        public static void CollectEligible(
            string formationId,
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            HashSet<string> groupedWarriorIds,
            List<EligibleMember> into)
        {
            if (into == null)
            {
                return;
            }

            into.Clear();
            if (string.IsNullOrEmpty(formationId) || formation == null || pool == null || configs == null)
            {
                return;
            }

            var entries = formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (groupedWarriorIds != null && groupedWarriorIds.Contains(entry.WarriorId))
                {
                    continue;
                }

                if (!pool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    continue;
                }

                if (!HasFormationSkill(warrior, configs, formationId))
                {
                    continue;
                }

                into.Add(new EligibleMember(entry.WarriorId, i, entry));
            }

            into.Sort(CompareDeployOrder);
        }

        /// <summary>
        /// Undeployed pool soldiers, in pool order, with the formation skill and in no group.
        /// </summary>
        public static void CollectUndeployed(
            string formationId,
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            HashSet<string> groupedWarriorIds,
            List<string> into)
        {
            if (into == null)
            {
                return;
            }

            into.Clear();
            if (string.IsNullOrEmpty(formationId) || formation == null || pool == null || configs == null)
            {
                return;
            }

            var warriors = pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var warrior = warriors[i];
                if (warrior == null || string.IsNullOrEmpty(warrior.Id))
                {
                    continue;
                }

                if (formation.IsDeployed(warrior.Id))
                {
                    continue;
                }

                if (groupedWarriorIds != null && groupedWarriorIds.Contains(warrior.Id))
                {
                    continue;
                }

                if (!HasFormationSkill(warrior, configs, formationId))
                {
                    continue;
                }

                into.Add(warrior.Id);
            }
        }

        public static bool HasFormationSkill(
            WarriorInstance warrior,
            ConfigCsvRepository configs,
            string formationId)
        {
            if (warrior?.SoldierSkills == null || configs == null || string.IsNullOrEmpty(formationId))
            {
                return false;
            }

            for (var i = 0; i < warrior.SoldierSkills.Count; i++)
            {
                var skill = warrior.SoldierSkills[i];
                if (skill == null || string.IsNullOrEmpty(skill.SkillId))
                {
                    continue;
                }

                if (!TryResolveSkillRow(configs, skill.SkillId, skill.SkillLevel, out var row) || row == null)
                {
                    continue;
                }

                if (string.Equals(row.FormationId, formationId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveSkillRow(
            ConfigCsvRepository configs,
            string skillId,
            int skillLevel,
            out SkillConfigRow row)
        {
            var level = skillLevel < 1 ? 1 : skillLevel;
            if (configs.TryGetSkill(skillId, level, out row) && row != null)
            {
                return true;
            }

            if (configs.TryGetSkillLevelRange(skillId, out var min, out _)
                && configs.TryGetSkill(skillId, min, out row)
                && row != null)
            {
                return true;
            }

            row = null;
            return false;
        }

        private static int CompareDeployOrder(EligibleMember a, EligibleMember b)
        {
            var byIndex = a.DeployIndex.CompareTo(b.DeployIndex);
            return byIndex != 0 ? byIndex : string.CompareOrdinal(a.WarriorId, b.WarriorId);
        }
    }
}
