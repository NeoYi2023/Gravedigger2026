using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// SPEC_03 §3.10 / SPEC_04 §9.6 — Demo defaults until TechTree slice.
    /// </summary>
    public sealed class DigProtagonistCapabilities
    {
        public float DigDamage = 25f;
        public float DigDurationReductionSum;
        public float DigCursorRadius = 1.6f;
        public HashSet<string> DiggableQualityIds = new HashSet<string>(StringComparer.Ordinal);
        public float DigStageDurationBonus;

        public float DigActionDuration => Math.Max(0.1f, 0.8f - DigDurationReductionSum);

        public static DigProtagonistCapabilities CreateDemoDefaults(IEnumerable<string> allQualityIds)
        {
            var caps = new DigProtagonistCapabilities();
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

            return caps;
        }
    }
}
