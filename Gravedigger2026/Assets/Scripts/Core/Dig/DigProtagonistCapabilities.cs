using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// SPEC_03 §3.10 / §3.16 / SPEC_04 §9.6 — tech AttributeModifiers + Dig-domain EquipEffect (additive).
    /// </summary>
    public sealed class DigProtagonistCapabilities
    {
        public const string GraveSpawnWeightBonusPrefix = "GraveSpawnWeightBonus_";

        public float DigDamage;
        public float DigDurationReductionSum;
        public float DigCursorRadius;
        public HashSet<string> DiggableQualityIds = new HashSet<string>(StringComparer.Ordinal);
        public float DigStageDurationBonus;
        public Dictionary<string, float> GraveSpawnWeightBonus =
            new Dictionary<string, float>(StringComparer.Ordinal);

        public float DigActionDuration => Math.Max(0.1f, 0.8f - DigDurationReductionSum);

        public static DigProtagonistCapabilities CreateDemoDefaults(IEnumerable<string> allQualityIds)
        {
            var caps = FromAttributeSums(
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    ["DigDamage"] = 25f,
                    ["DigCursorRadius"] = 0.6f
                },
                allQualityIds);
            return caps;
        }

        public static DigProtagonistCapabilities FromAttributeSums(
            IReadOnlyDictionary<string, float> sums,
            IEnumerable<string> allQualityIds)
        {
            var caps = new DigProtagonistCapabilities();
            if (sums != null)
            {
                if (sums.TryGetValue("DigDamage", out var digDamage))
                {
                    caps.DigDamage = digDamage;
                }

                if (sums.TryGetValue("DigDurationReductionSum", out var digDur))
                {
                    caps.DigDurationReductionSum = digDur;
                }

                if (sums.TryGetValue("DigCursorRadius", out var cursor))
                {
                    caps.DigCursorRadius = cursor;
                }

                if (sums.TryGetValue("DigStageDurationBonus", out var stageBonus))
                {
                    caps.DigStageDurationBonus = stageBonus;
                }

                foreach (var kv in sums)
                {
                    if (string.IsNullOrEmpty(kv.Key)
                        || !kv.Key.StartsWith(GraveSpawnWeightBonusPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var qualityId = kv.Key.Substring(GraveSpawnWeightBonusPrefix.Length);
                    if (!string.IsNullOrEmpty(qualityId))
                    {
                        caps.GraveSpawnWeightBonus[qualityId] = kv.Value;
                    }
                }
            }

            FillDiggableQualities(caps, allQualityIds);
            return caps;
        }

        public float GetGraveSpawnWeightBonus(string qualityId)
        {
            if (string.IsNullOrEmpty(qualityId) || GraveSpawnWeightBonus == null)
            {
                return 0f;
            }

            return GraveSpawnWeightBonus.TryGetValue(qualityId, out var value) ? value : 0f;
        }

        private static void FillDiggableQualities(DigProtagonistCapabilities caps, IEnumerable<string> allQualityIds)
        {
            if (allQualityIds != null)
            {
                foreach (var id in allQualityIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        caps.DiggableQualityIds.Add(id);
                    }
                }
            }

            if (caps.DiggableQualityIds.Count == 0)
            {
                caps.DiggableQualityIds.Add("Q1");
                caps.DiggableQualityIds.Add("Q2");
            }
        }
    }
}
