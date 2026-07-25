using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// SPEC_04 §9 weighted-field common rules: drop Weight=0; empty effective list abandons draw.
    /// </summary>
    public static class WeightedFieldParser
    {
        public readonly struct WeightedId
        {
            public readonly string Id;
            public readonly float Weight;

            public WeightedId(string id, float weight)
            {
                Id = id;
                Weight = weight;
            }
        }

        public static List<WeightedId> ParseGraveSpawnWeights(string encoded)
        {
            var result = new List<WeightedId>();
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

                var parts = seg.Split(';');
                if (parts.Length != 2)
                {
                    continue;
                }

                var id = parts[0].Trim();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
                {
                    continue;
                }

                if (weight <= 0f)
                {
                    continue;
                }

                result.Add(new WeightedId(id, weight));
            }

            return result;
        }

        public static bool TryParseSpawnRate(string encoded, out float intervalSeconds, out int countPerInterval)
        {
            intervalSeconds = 0f;
            countPerInterval = 0;
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return false;
            }

            var parts = encoded.Split(';');
            if (parts.Length != 2)
            {
                return false;
            }

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out intervalSeconds))
            {
                return false;
            }

            if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out countPerInterval))
            {
                return false;
            }

            return intervalSeconds > 0f && countPerInterval >= 0;
        }

        public static string PickWeighted(IReadOnlyList<WeightedId> effective, Random rng)
        {
            if (effective == null || effective.Count == 0)
            {
                return null;
            }

            var total = 0f;
            for (var i = 0; i < effective.Count; i++)
            {
                total += effective[i].Weight;
            }

            if (total <= 0f)
            {
                return null;
            }

            var roll = (float)(rng.NextDouble() * total);
            var acc = 0f;
            for (var i = 0; i < effective.Count; i++)
            {
                acc += effective[i].Weight;
                if (roll <= acc)
                {
                    return effective[i].Id;
                }
            }

            return effective[effective.Count - 1].Id;
        }
    }
}
