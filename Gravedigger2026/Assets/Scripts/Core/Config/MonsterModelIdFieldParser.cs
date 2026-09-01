using System.Collections.Generic;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Parses Defend_MonsterConfig.ModelId weighted pool (SPEC_04 §9.19).
    /// Encoding: ModelId;Weight|ModelId;Weight|…
    /// </summary>
    public static class MonsterModelIdFieldParser
    {
        public static List<WeightedFieldParser.WeightedId> Parse(string encoded)
        {
            var parsed = WeightedFieldParser.ParseGraveSpawnWeights(encoded);
            if (parsed.Count > 0)
            {
                return parsed;
            }

            if (string.IsNullOrWhiteSpace(encoded))
            {
                return parsed;
            }

            var trimmed = encoded.Trim();
            if (trimmed.IndexOf('|') >= 0 || trimmed.IndexOf(';') >= 0)
            {
                return parsed;
            }

            parsed.Add(new WeightedFieldParser.WeightedId(trimmed, 1f));
            return parsed;
        }

        public static IEnumerable<string> EnumerateModelIds(string encoded)
        {
            var pool = Parse(encoded);
            for (var i = 0; i < pool.Count; i++)
            {
                var id = pool[i].Id;
                if (!string.IsNullOrEmpty(id))
                {
                    yield return id;
                }
            }
        }

        public static string PickSpawnModelId(IReadOnlyList<WeightedFieldParser.WeightedId> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            var roll = UnityEngine.Random.value;
            var total = 0f;
            for (var i = 0; i < pool.Count; i++)
            {
                total += pool[i].Weight;
            }

            if (total <= 0f)
            {
                return null;
            }

            var threshold = roll * total;
            var acc = 0f;
            for (var i = 0; i < pool.Count; i++)
            {
                acc += pool[i].Weight;
                if (threshold <= acc)
                {
                    return pool[i].Id;
                }
            }

            return pool[pool.Count - 1].Id;
        }
    }
}
