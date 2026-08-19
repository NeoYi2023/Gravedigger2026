using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Combat
{
    public enum BondConditionKind
    {
        DeployClassCount,
        DeployRaceCount,
        DeployTotalCount,
        DeployPrimaryStatCount
    }

    /// <summary>
    /// Parsed FormationBondConfig.ActivationCondition DSL (SPEC_03 §3.17).
    /// </summary>
    public sealed class BondActivationCondition
    {
        public BondConditionKind Kind;
        public string ClassId;
        public string BaseClass;
        public string RaceId;
        public StatKind PrimaryStat;
        public int Min;

        public static bool TryParse(string dsl, out BondActivationCondition result, out string error)
        {
            result = null;
            error = null;
            if (string.IsNullOrWhiteSpace(dsl))
            {
                error = "ActivationCondition is empty.";
                return false;
            }

            var parts = dsl.Split('|');
            if (parts.Length < 2)
            {
                error = $"ActivationCondition '{dsl}' must contain Kind and at least one Key=Value.";
                return false;
            }

            var kindText = parts[0].Trim();
            if (!TryParseKind(kindText, out var kind))
            {
                error = $"Unknown ActivationCondition Kind '{kindText}'.";
                return false;
            }

            var parsed = new BondActivationCondition { Kind = kind };
            var kv = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 1; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                var eq = segment.IndexOf('=');
                if (eq <= 0 || eq >= segment.Length - 1)
                {
                    error = $"Invalid segment '{segment}' in ActivationCondition.";
                    return false;
                }

                var key = segment.Substring(0, eq).Trim();
                var value = segment.Substring(eq + 1).Trim();
                if (kv.ContainsKey(key))
                {
                    error = $"Duplicate key '{key}' in ActivationCondition.";
                    return false;
                }

                kv[key] = value;
            }

            if (!kv.TryGetValue("Min", out var minText)
                || !int.TryParse(minText, out var min)
                || min < 1)
            {
                error = "ActivationCondition requires Min≥1.";
                return false;
            }

            parsed.Min = min;

            switch (kind)
            {
                case BondConditionKind.DeployClassCount:
                    var hasClassId = kv.TryGetValue("ClassId", out parsed.ClassId);
                    var hasBaseClass = kv.TryGetValue("BaseClass", out parsed.BaseClass);
                    if (hasClassId == hasBaseClass)
                    {
                        error = "DeployClassCount requires exactly one of ClassId or BaseClass.";
                        return false;
                    }

                    if (hasClassId && string.IsNullOrWhiteSpace(parsed.ClassId))
                    {
                        error = "DeployClassCount ClassId is empty.";
                        return false;
                    }

                    if (hasBaseClass && string.IsNullOrWhiteSpace(parsed.BaseClass))
                    {
                        error = "DeployClassCount BaseClass is empty.";
                        return false;
                    }

                    break;

                case BondConditionKind.DeployRaceCount:
                    if (!kv.TryGetValue("RaceId", out parsed.RaceId) || string.IsNullOrWhiteSpace(parsed.RaceId))
                    {
                        error = "DeployRaceCount requires RaceId.";
                        return false;
                    }

                    break;

                case BondConditionKind.DeployTotalCount:
                    break;

                case BondConditionKind.DeployPrimaryStatCount:
                    if (!kv.TryGetValue("Stat", out var statText) || !TryParseStat(statText, out parsed.PrimaryStat))
                    {
                        error = "DeployPrimaryStatCount requires Stat=Strength|Agility|Intelligence.";
                        return false;
                    }

                    break;

                default:
                    error = $"Unhandled Kind '{kind}'.";
                    return false;
            }

            foreach (var pair in kv)
            {
                if (IsAllowedKey(kind, pair.Key))
                {
                    continue;
                }

                error = $"Unexpected key '{pair.Key}' for Kind '{kindText}'.";
                return false;
            }

            result = parsed;
            return true;
        }

        private static bool TryParseKind(string kindText, out BondConditionKind kind)
        {
            if (string.Equals(kindText, nameof(BondConditionKind.DeployClassCount), StringComparison.Ordinal))
            {
                kind = BondConditionKind.DeployClassCount;
                return true;
            }

            if (string.Equals(kindText, nameof(BondConditionKind.DeployRaceCount), StringComparison.Ordinal))
            {
                kind = BondConditionKind.DeployRaceCount;
                return true;
            }

            if (string.Equals(kindText, nameof(BondConditionKind.DeployTotalCount), StringComparison.Ordinal))
            {
                kind = BondConditionKind.DeployTotalCount;
                return true;
            }

            if (string.Equals(kindText, nameof(BondConditionKind.DeployPrimaryStatCount), StringComparison.Ordinal))
            {
                kind = BondConditionKind.DeployPrimaryStatCount;
                return true;
            }

            kind = default;
            return false;
        }

        private static bool TryParseStat(string statText, out StatKind stat)
        {
            if (Enum.TryParse(statText, true, out stat)
                && (stat == StatKind.Strength || stat == StatKind.Agility || stat == StatKind.Intelligence))
            {
                return true;
            }

            stat = default;
            return false;
        }

        private static bool IsAllowedKey(BondConditionKind kind, string key)
        {
            switch (kind)
            {
                case BondConditionKind.DeployClassCount:
                    return key == "Min" || key == "ClassId" || key == "BaseClass";
                case BondConditionKind.DeployRaceCount:
                    return key == "Min" || key == "RaceId";
                case BondConditionKind.DeployTotalCount:
                    return key == "Min";
                case BondConditionKind.DeployPrimaryStatCount:
                    return key == "Min" || key == "Stat";
                default:
                    return false;
            }
        }
    }
}
