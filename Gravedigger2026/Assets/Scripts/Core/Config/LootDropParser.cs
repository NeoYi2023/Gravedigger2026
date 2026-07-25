using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gravedigger2026.Core.Config
{
    public readonly struct LootDropEntry
    {
        public readonly string Id;
        public readonly int Count;

        public LootDropEntry(string id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    /// <summary>
    /// SPEC_04 §9.3 LootDrop: Id_Count|Id_Count|...
    /// </summary>
    public static class LootDropParser
    {
        public const string SpiritId = "Spirit";

        public static List<LootDropEntry> Parse(string encoded, Action<string> onIgnored = null)
        {
            var result = new List<LootDropEntry>();
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return result;
            }

            var segments = encoded.Split('|');
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i].Trim();
                if (seg.Length == 0)
                {
                    continue;
                }

                var underscore = seg.LastIndexOf('_');
                if (underscore <= 0 || underscore >= seg.Length - 1)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (missing Id_Count): '{seg}'");
                    continue;
                }

                var id = seg.Substring(0, underscore);
                var countText = seg.Substring(underscore + 1);
                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ||
                    count < 1)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (bad Count): '{seg}'");
                    continue;
                }

                result.Add(new LootDropEntry(id, count));
            }

            return result;
        }
    }
}
