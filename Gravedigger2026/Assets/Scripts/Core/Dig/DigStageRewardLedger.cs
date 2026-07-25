using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Dig
{
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

        public string BuildSummaryText()
        {
            if (_amounts.Count == 0)
            {
                return "本阶段未获得奖励。";
            }

            var lines = new List<string> { "Dig 阶段汇总（已入账）：" };
            foreach (var kv in _amounts)
            {
                lines.Add($"  {kv.Key} × {FormatAmount(kv.Value)}");
            }

            return string.Join("\n", lines);
        }

        private static string FormatAmount(float value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.001f)
            {
                return ((int)Math.Round(value)).ToString();
            }

            return value.ToString("0.##");
        }
    }
}
