using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

    public readonly struct LootDropWeightedEntry
    {
        public readonly string Id;
        public readonly int Weight;
        public readonly int Count;

        public LootDropWeightedEntry(string id, int weight, int count)
        {
            Id = id;
            Weight = weight;
            Count = count;
        }
    }

    /// <summary>
    /// SPEC_04 §9.3 Dig LootDrop: Id;Weight;Count|... with DropMode.
    /// Monster LootDrop + CaptureLoot (and other item reward strings) use ParseIdSemicolonCount (Id;Count|...).
    /// Dig "settled" / encoded loot lists use underscore encoding via Parse (Id_Count|...).
    /// </summary>
    public static class LootDropParser
    {
        public const string SpiritId = "Spirit";
        public const int DropModeIndependent = 1;
        public const int DropModeWeightedPickOne = 2;
        public const int PerTenThousand = 10000;
        private const char WeightedFieldSeparator = ';';

        /// <summary>Legacy underscore encoding: Id_Count|Id_Count|...</summary>
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

        /// <summary>Monster LootDrop + CaptureLoot / item reward encoding: Id;Count|Id;Count|...</summary>
        public static List<LootDropEntry> ParseIdSemicolonCount(string encoded, Action<string> onIgnored = null)
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

                var sep = seg.LastIndexOf(WeightedFieldSeparator);
                if (sep <= 0 || sep >= seg.Length - 1)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (missing Id;Count): '{seg}'");
                    continue;
                }

                var id = seg.Substring(0, sep);
                var countText = seg.Substring(sep + 1);
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

        /// <summary>Convenience alias: MonsterConfig.LootDrop parser.</summary>
        public static List<LootDropEntry> ParseMonsterLootDrop(string encoded, Action<string> onIgnored = null)
        {
            return ParseIdSemicolonCount(encoded, onIgnored);
        }

        /// <summary>Dig GraveQuality encoding: Id;Weight;Count|... (two ';' from the right).</summary>
        public static List<LootDropWeightedEntry> ParseWeighted(string encoded, Action<string> onIgnored = null)
        {
            var result = new List<LootDropWeightedEntry>();
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

                var countSep = seg.LastIndexOf(WeightedFieldSeparator);
                if (countSep <= 0 || countSep >= seg.Length - 1)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (missing Id;Weight;Count): '{seg}'");
                    continue;
                }

                var weightSep = seg.LastIndexOf(WeightedFieldSeparator, countSep - 1);
                if (weightSep <= 0 || weightSep >= countSep - 1)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (missing Id;Weight;Count): '{seg}'");
                    continue;
                }

                var id = seg.Substring(0, weightSep);
                var weightText = seg.Substring(weightSep + 1, countSep - weightSep - 1);
                var countText = seg.Substring(countSep + 1);
                if (string.IsNullOrEmpty(id))
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (empty Id): '{seg}'");
                    continue;
                }

                if (!int.TryParse(weightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) ||
                    weight < 0)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (bad Weight): '{seg}'");
                    continue;
                }

                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ||
                    count < 1)
                {
                    onIgnored?.Invoke($"LootDrop segment ignored (bad Count): '{seg}'");
                    continue;
                }

                result.Add(new LootDropWeightedEntry(id, weight, count));
            }

            return result;
        }

        public static List<LootDropEntry> Resolve(
            string encoded,
            int dropMode,
            Random rng,
            Action<string> onIgnored = null)
        {
            var weighted = ParseWeighted(encoded, onIgnored);
            var result = new List<LootDropEntry>();
            if (dropMode == DropModeIndependent)
            {
                ResolveIndependent(weighted, rng, result);
                return result;
            }

            if (dropMode == DropModeWeightedPickOne)
            {
                ResolveWeightedPickOne(weighted, rng, result);
                return result;
            }

            onIgnored?.Invoke($"LootDrop DropMode {dropMode} unimplemented — no loot.");
            return result;
        }

        public static string Encode(IReadOnlyList<LootDropEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (string.IsNullOrEmpty(entry.Id) || entry.Count < 1)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append('|');
                }

                sb.Append(entry.Id);
                sb.Append('_');
                sb.Append(entry.Count.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static void ResolveIndependent(
            List<LootDropWeightedEntry> weighted,
            Random rng,
            List<LootDropEntry> result)
        {
            for (var i = 0; i < weighted.Count; i++)
            {
                var entry = weighted[i];
                if (entry.Weight <= 0)
                {
                    continue;
                }

                if (entry.Weight >= PerTenThousand || rng.Next(0, PerTenThousand) < entry.Weight)
                {
                    result.Add(new LootDropEntry(entry.Id, entry.Count));
                }
            }
        }

        private static void ResolveWeightedPickOne(
            List<LootDropWeightedEntry> weighted,
            Random rng,
            List<LootDropEntry> result)
        {
            var total = 0L;
            for (var i = 0; i < weighted.Count; i++)
            {
                if (weighted[i].Weight > 0)
                {
                    total += weighted[i].Weight;
                }
            }

            if (total <= 0)
            {
                return;
            }

            var roll = rng.NextDouble() * total;
            var acc = 0.0;
            LootDropWeightedEntry picked = default;
            var found = false;
            for (var i = 0; i < weighted.Count; i++)
            {
                var entry = weighted[i];
                if (entry.Weight <= 0)
                {
                    continue;
                }

                acc += entry.Weight;
                if (roll <= acc)
                {
                    picked = entry;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                for (var i = weighted.Count - 1; i >= 0; i--)
                {
                    if (weighted[i].Weight > 0)
                    {
                        picked = weighted[i];
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                result.Add(new LootDropEntry(picked.Id, picked.Count));
            }
        }
    }
}
