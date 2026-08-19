using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    public enum FormationBondDisplayState
    {
        Inactive,
        Active,
        Superseded
    }

    public readonly struct ActiveFormationBond
    {
        public readonly FormationBondConfigRow Row;
        public readonly FormationBondDisplayState State;
        public readonly int CurrentCount;
        public readonly int RequiredCount;

        public bool IsActive => State == FormationBondDisplayState.Active;

        public ActiveFormationBond(
            FormationBondConfigRow row,
            FormationBondDisplayState state,
            int currentCount,
            int requiredCount)
        {
            Row = row;
            State = state;
            CurrentCount = currentCount;
            RequiredCount = requiredCount;
        }
    }

    /// <summary>
    /// Evaluates FormationBond activation from deployed soldiers (SPEC_03 §3.17).
    /// </summary>
    public static class FormationBondEvaluator
    {
        public static IReadOnlyList<ActiveFormationBond> Evaluate(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            var result = new List<ActiveFormationBond>();
            if (formation == null || pool == null || configs == null || !configs.IsLoaded)
            {
                return result;
            }

            var rows = configs.GetAllFormationBondRows();
            if (rows.Count == 0)
            {
                return result;
            }

            var stats = BuildDeployStats(formation, pool, configs);
            var evaluated = new List<(FormationBondConfigRow row, bool meets, int current, int required)>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!configs.TryGetBondActivationCondition(row, out var condition))
                {
                    continue;
                }

                var current = CountForCondition(condition, stats);
                var meets = current >= condition.Min;
                evaluated.Add((row, meets, current, condition.Min));
            }

            var activeLevelByBond = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < evaluated.Count; i++)
            {
                var item = evaluated[i];
                if (!item.meets)
                {
                    continue;
                }

                var bondId = item.row.BondId;
                if (!activeLevelByBond.TryGetValue(bondId, out var existing)
                    || item.row.BondLevel > existing)
                {
                    activeLevelByBond[bondId] = item.row.BondLevel;
                }
            }

            for (var i = 0; i < evaluated.Count; i++)
            {
                var item = evaluated[i];
                FormationBondDisplayState state;
                if (!item.meets)
                {
                    state = FormationBondDisplayState.Inactive;
                }
                else if (activeLevelByBond.TryGetValue(item.row.BondId, out var activeLevel)
                         && item.row.BondLevel == activeLevel)
                {
                    state = FormationBondDisplayState.Active;
                }
                else
                {
                    state = FormationBondDisplayState.Superseded;
                }

                result.Add(new ActiveFormationBond(item.row, state, item.current, item.required));
            }

            result.Sort(CompareBondRows);
            return result;
        }

        public static IReadOnlyList<ActiveFormationBond> EvaluateActiveOnly(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            var all = Evaluate(formation, pool, configs);
            var active = new List<ActiveFormationBond>();
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i].IsActive)
                {
                    active.Add(all[i]);
                }
            }

            return active;
        }

        public static string FormatProgressLabel(ActiveFormationBond bond)
        {
            if (bond.Row == null || !configsTryGetConditionKind(bond, out var kind))
            {
                return $"{bond.CurrentCount}/{bond.RequiredCount}";
            }

            switch (kind)
            {
                case BondConditionKind.DeployClassCount:
                    if (!string.IsNullOrEmpty(bond.Row.ActivationCondition)
                        && bond.Row.ActivationCondition.IndexOf("ClassId=", StringComparison.Ordinal) >= 0)
                    {
                        return $"{bond.CurrentCount}/{bond.RequiredCount} 职业";
                    }

                    return $"{bond.CurrentCount}/{bond.RequiredCount} 基础职业";
                case BondConditionKind.DeployRaceCount:
                    return $"{bond.CurrentCount}/{bond.RequiredCount} 种族";
                case BondConditionKind.DeployTotalCount:
                    return $"{bond.CurrentCount}/{bond.RequiredCount} 上阵";
                case BondConditionKind.DeployPrimaryStatCount:
                    return $"{bond.CurrentCount}/{bond.RequiredCount} 主属性";
                default:
                    return $"{bond.CurrentCount}/{bond.RequiredCount}";
            }
        }

        private static bool configsTryGetConditionKind(ActiveFormationBond bond, out BondConditionKind kind)
        {
            kind = default;
            if (bond.Row == null || string.IsNullOrWhiteSpace(bond.Row.ActivationCondition))
            {
                return false;
            }

            var parts = bond.Row.ActivationCondition.Split('|');
            if (parts.Length == 0)
            {
                return false;
            }

            return Enum.TryParse(parts[0].Trim(), out kind);
        }

        private static int CompareBondRows(ActiveFormationBond a, ActiveFormationBond b)
        {
            var idCompare = string.Compare(a.Row.BondId, b.Row.BondId, StringComparison.Ordinal);
            if (idCompare != 0)
            {
                return idCompare;
            }

            return a.Row.BondLevel.CompareTo(b.Row.BondLevel);
        }

        private sealed class DeployStats
        {
            public int TotalCount;
            public readonly Dictionary<string, int> ClassIdCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly Dictionary<BaseClassKind, int> BaseClassCounts =
                new Dictionary<BaseClassKind, int>();
            public readonly Dictionary<string, int> RaceCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly Dictionary<StatKind, int> PrimaryStatCounts =
                new Dictionary<StatKind, int>();
        }

        private static DeployStats BuildDeployStats(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            var stats = new DeployStats();
            var entries = formation.Entries;
            stats.TotalCount = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!pool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    Debug.LogWarning(
                        $"[FormationBond] Deployed warrior '{entry.WarriorId}' missing from pool — skip count.");
                    continue;
                }

                if (!string.IsNullOrEmpty(warrior.RaceId))
                {
                    Increment(stats.RaceCounts, warrior.RaceId);
                }

                if (string.IsNullOrEmpty(warrior.ClassId)
                    || !configs.TryGetClass(warrior.ClassId, out var classRow)
                    || classRow == null)
                {
                    Debug.LogWarning(
                        $"[FormationBond] Warrior '{entry.WarriorId}' ClassId '{warrior.ClassId}' missing — skip class count.");
                    continue;
                }

                Increment(stats.ClassIdCounts, warrior.ClassId);
                if (classRow.BaseClass != BaseClassKind.Unspecified)
                {
                    Increment(stats.BaseClassCounts, classRow.BaseClass);
                }

                Increment(stats.PrimaryStatCounts, classRow.PrimaryStat);
            }

            return stats;
        }

        private static int CountForCondition(BondActivationCondition condition, DeployStats stats)
        {
            switch (condition.Kind)
            {
                case BondConditionKind.DeployClassCount:
                    if (!string.IsNullOrEmpty(condition.ClassId))
                    {
                        return stats.ClassIdCounts.TryGetValue(condition.ClassId, out var classCount)
                            ? classCount
                            : 0;
                    }

                    if (!TryParseBaseClassToken(condition.BaseClass, out var baseClass)
                        || baseClass == BaseClassKind.Unspecified)
                    {
                        return 0;
                    }

                    return stats.BaseClassCounts.TryGetValue(baseClass, out var baseCount)
                        ? baseCount
                        : 0;

                case BondConditionKind.DeployRaceCount:
                    return stats.RaceCounts.TryGetValue(condition.RaceId, out var raceCount) ? raceCount : 0;

                case BondConditionKind.DeployTotalCount:
                    return stats.TotalCount;

                case BondConditionKind.DeployPrimaryStatCount:
                    return stats.PrimaryStatCounts.TryGetValue(condition.PrimaryStat, out var statCount)
                        ? statCount
                        : 0;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// FormationBond ActivationCondition BaseClass token (SPEC_03 §3.17 CSV Chinese).
        /// </summary>
        private static bool TryParseBaseClassToken(string text, out BaseClassKind kind)
        {
            kind = BaseClassKind.Unspecified;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            switch (text.Trim())
            {
                case "战士":
                    kind = BaseClassKind.Warrior;
                    return true;
                case "射手":
                    kind = BaseClassKind.Archer;
                    return true;
                case "法师":
                    kind = BaseClassKind.Mage;
                    return true;
                case "刺客":
                case "盗贼":
                    kind = BaseClassKind.Thief;
                    return true;
                default:
                    return Enum.TryParse(text.Trim(), true, out kind)
                           && kind != BaseClassKind.Unspecified;
            }
        }

        private static void Increment<TKey>(Dictionary<TKey, int> map, TKey key)
        {
            if (map.TryGetValue(key, out var count))
            {
                map[key] = count + 1;
            }
            else
            {
                map[key] = 1;
            }
        }
    }
}
