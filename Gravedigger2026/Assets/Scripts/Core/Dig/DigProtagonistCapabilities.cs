using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// SPEC_03 §3.10 / SPEC_04 §9.6 — derived from TechEffect AttributeModifiers (Approach A / UI-012).
    /// </summary>
    public sealed class DigProtagonistCapabilities
    {
        public float DigDamage;
        public float DigDurationReductionSum;
        public float DigCursorRadius;
        public HashSet<string> DiggableQualityIds = new HashSet<string>(StringComparer.Ordinal);
        public float DigStageDurationBonus;

        public float DigActionDuration => Math.Max(0.1f, 0.8f - DigDurationReductionSum);

        public static DigProtagonistCapabilities CreateDemoDefaults(IEnumerable<string> allQualityIds)
        {
            var caps = FromAttributeSums(
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    ["DigDamage"] = 25f,
                    ["DigCursorRadius"] = 1.6f
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
            }

            FillDiggableQualities(caps, allQualityIds);
            return caps;
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
