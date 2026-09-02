using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// SPEC_04 §9.31 GatherPointRewards: <c>N:ItemId;Count|…</c>.
    /// <c>|</c> splits; a token starting with <c>N:</c> begins a new gather point.
    /// </summary>
    public static class GatherPointRewardsParser
    {
        private static readonly Regex OrderPrefix = new Regex(
            @"^(\d+):(.*)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static Dictionary<int, List<LootDropEntry>> Parse(
            string encoded,
            Action<string> onIgnored = null)
        {
            var result = new Dictionary<int, List<LootDropEntry>>();
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return result;
            }

            var currentOrder = 0;
            var segments = encoded.Split('|');
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i].Trim();
                if (seg.Length == 0)
                {
                    continue;
                }

                var itemText = seg;
                var match = OrderPrefix.Match(seg);
                if (match.Success)
                {
                    if (!int.TryParse(
                            match.Groups[1].Value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var order)
                        || order < 1)
                    {
                        onIgnored?.Invoke($"GatherPointRewards ignored (bad Order): '{seg}'");
                        currentOrder = 0;
                        continue;
                    }

                    currentOrder = order;
                    itemText = match.Groups[2].Value.Trim();
                    if (!result.ContainsKey(currentOrder))
                    {
                        result[currentOrder] = new List<LootDropEntry>();
                    }
                }
                else if (currentOrder < 1)
                {
                    onIgnored?.Invoke($"GatherPointRewards ignored (missing N: prefix): '{seg}'");
                    continue;
                }

                if (itemText.Length == 0)
                {
                    continue;
                }

                var parsed = LootDropParser.ParseIdSemicolonCount(
                    itemText,
                    msg => onIgnored?.Invoke(msg));
                if (parsed.Count == 0)
                {
                    continue;
                }

                if (!result.TryGetValue(currentOrder, out var list))
                {
                    list = new List<LootDropEntry>();
                    result[currentOrder] = list;
                }

                list.AddRange(parsed);
            }

            return result;
        }
    }
}
