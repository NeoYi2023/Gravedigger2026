using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Shared race finalize helpers (SPEC_03 §3.11 / §3.15): same-race else Undead;
    /// optional weight-1 pick for Mode2 Restore MagicBook.
    /// </summary>
    public static class RaceResolve
    {
        public const string UndeadRaceId = "Race_Undead";
        public const string RaceWeightPickPayload = "RaceWeightPick";

        /// <summary>
        /// Default: all filled BodyPart RaceIds identical → that race; else Race_Undead.
        /// </summary>
        public static string ResolveDefaultRace(IReadOnlyList<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var first = candidates[0];
            for (var i = 1; i < candidates.Count; i++)
            {
                if (!string.Equals(candidates[i], first, StringComparison.Ordinal))
                {
                    return UndeadRaceId;
                }
            }

            return first;
        }

        /// <summary>Legacy weight-1 pick among part RaceIds (each list entry weight 1).</summary>
        public static string PickWeighted(IReadOnlyList<string> candidates, Random rng)
        {
            if (candidates == null || candidates.Count == 0 || rng == null)
            {
                return null;
            }

            return candidates[rng.Next(candidates.Count)];
        }
    }
}
