using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Soldier nearest-band pick with TargetFocus free-first (SPEC_03 §3.12).
    /// Pure C#: bandMax = dMin×(1+relative)+slack; sticky within same free/engaged tier.
    /// </summary>
    public static class MonsterNearestWarriorPicker
    {
        public readonly struct Candidate
        {
            public readonly string Id;
            public readonly Vector3 Position;

            public Candidate(string id, Vector3 position)
            {
                Id = id;
                Position = position;
            }
        }

        /// <summary>
        /// Pick best soldier id among <paramref name="candidates"/>, or null.
        /// Candidates must already be legality-filtered (active/loyal/detect gate).
        /// </summary>
        public static string Pick(
            Vector3 monsterPos,
            IReadOnlyList<Candidate> candidates,
            TargetFocusRegistry focus,
            string attackerId,
            string currentFocusId,
            float bandRelative,
            float bandSlack)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var relative = Mathf.Max(0f, bandRelative);
            var slack = Mathf.Max(0f, bandSlack);

            var dMin = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (string.IsNullOrEmpty(c.Id))
                {
                    continue;
                }

                var d = Vector3.Distance(monsterPos, c.Position);
                if (d < dMin)
                {
                    dMin = d;
                }
            }

            if (dMin >= float.MaxValue)
            {
                return null;
            }

            var bandMax = dMin * (1f + relative) + slack;

            string bestFreeId = null;
            var bestFreeDist = float.MaxValue;
            string bestEngagedId = null;
            var bestEngagedDist = float.MaxValue;

            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (string.IsNullOrEmpty(c.Id))
                {
                    continue;
                }

                var d = Vector3.Distance(monsterPos, c.Position);
                if (d > bandMax)
                {
                    continue;
                }

                var otherFocus = focus != null
                    ? focus.GetFocusCountExcluding(c.Id, attackerId)
                    : 0;
                if (otherFocus <= 0)
                {
                    if (d < bestFreeDist)
                    {
                        bestFreeDist = d;
                        bestFreeId = c.Id;
                    }
                }
                else if (d < bestEngagedDist)
                {
                    bestEngagedDist = d;
                    bestEngagedId = c.Id;
                }
            }

            var preferFree = !string.IsNullOrEmpty(bestFreeId);
            var picked = preferFree ? bestFreeId : bestEngagedId;
            if (string.IsNullOrEmpty(picked))
            {
                return null;
            }

            // Sticky: keep current focus if still in band and same free/engaged tier.
            if (!string.IsNullOrEmpty(currentFocusId))
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    if (c.Id != currentFocusId)
                    {
                        continue;
                    }

                    var d = Vector3.Distance(monsterPos, c.Position);
                    if (d > bandMax)
                    {
                        break;
                    }

                    var curOther = focus != null
                        ? focus.GetFocusCountExcluding(currentFocusId, attackerId)
                        : 0;
                    var curFree = curOther <= 0;
                    if (curFree == preferFree)
                    {
                        return currentFocusId;
                    }

                    break;
                }
            }

            return picked;
        }
    }
}
