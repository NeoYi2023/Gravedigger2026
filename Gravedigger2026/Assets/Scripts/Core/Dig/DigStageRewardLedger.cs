using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>One aggregated Dig-stage reward row for DigStageSummary (UI-011).</summary>
    public readonly struct DigStageSummaryEntry
    {
        public DigStageSummaryEntry(string rewardId, string displayName, float amount, bool isBodyPart)
        {
            RewardId = rewardId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Amount = amount;
            IsBodyPart = isBodyPart;
        }

        public string RewardId { get; }
        public string DisplayName { get; }
        public float Amount { get; }
        public bool IsBodyPart { get; }
    }

    /// <summary>
    /// Aggregates rewards credited this Dig stage for DigStageSummary (no extra grants).
    /// </summary>
    public sealed class DigStageRewardLedger
    {
        private readonly Dictionary<string, float> _amounts = new Dictionary<string, float>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, float> Amounts => _amounts;

        public void Clear()
        {
            _amounts.Clear();
        }

        public void Add(string rewardId, float amount)
        {
            if (string.IsNullOrEmpty(rewardId) || amount <= 0f)
            {
                return;
            }

            _amounts.TryGetValue(rewardId, out var current);
            _amounts[rewardId] = current + amount;
        }

        public IReadOnlyList<DigStageSummaryEntry> BuildSummaryEntries(ConfigCsvRepository configs)
        {
            if (_amounts.Count == 0)
            {
                return Array.Empty<DigStageSummaryEntry>();
            }

            var list = new List<DigStageSummaryEntry>(_amounts.Count);
            foreach (var kv in _amounts)
            {
                list.Add(FormatEntry(kv.Key, kv.Value, configs));
            }

            return list;
        }

        private static DigStageSummaryEntry FormatEntry(string rewardId, float amount, ConfigCsvRepository configs)
        {
            if (configs != null && configs.TryGetBodyPart(rewardId, out var part))
            {
                var name = string.IsNullOrEmpty(part.DisplayName) ? part.BodyPartId : part.DisplayName;
                return new DigStageSummaryEntry(rewardId, name, amount, isBodyPart: true);
            }

            return new DigStageSummaryEntry(rewardId, rewardId, amount, isBodyPart: false);
        }

        public static string FormatAmount(float value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.001f)
            {
                return ((int)Math.Round(value)).ToString();
            }

            return value.ToString("0.##");
        }
    }
}
