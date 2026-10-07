using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// COC three-axis product targeting (SPEC_03 §3.21 / D-100 Approach A).
    /// PushMap / Defend / SearchExtract do not call this.
    /// </summary>
    public static class CocAttackPriority
    {
        public const float FloorCellWorldSize = 1f;
        public const string TypeEnemyUnit = "敌方单位";
        public const string TypeDestructibleObstacle = "可破坏障碍物";
        public const string DistanceNearestToken = "优先最近";
        public const string DistanceFarthestToken = "优先最远";
        public const string ValueHighToLowToken = "由大到小";
        public const string ValueIgnoreToken = "无视价值";

        public readonly struct Candidate
        {
            public readonly string Id;
            public readonly float XzDistance;
            public readonly float TargetValue;
            public readonly string TypeName;

            public Candidate(string id, float xzDistance, float targetValue, string typeName)
            {
                Id = id ?? string.Empty;
                XzDistance = xzDistance;
                TargetValue = targetValue;
                TypeName = typeName ?? string.Empty;
            }
        }

        public static float ResolveObserveRange(float configured, float revealFallback)
        {
            if (configured > 0f)
            {
                return configured;
            }

            if (revealFallback > 0f)
            {
                return revealFallback;
            }

            return ClassConfigRow.DefaultObserveRange;
        }

        public static bool IsInsideObserveRange(float xzDistance, float observeRange)
        {
            return xzDistance <= observeRange;
        }

        public static int DistanceCells(float xzDistance)
        {
            var cells = Mathf.CeilToInt(Mathf.Max(0f, xzDistance) / FloorCellWorldSize);
            return Mathf.Max(1, cells);
        }

        public static float DistanceScore(CocDistancePriority priority, int cells)
        {
            if (cells < 1)
            {
                cells = 1;
            }

            if (priority == CocDistancePriority.PreferFarthest)
            {
                return cells;
            }

            return (float)Math.Round(1d / cells, 2, MidpointRounding.AwayFromZero);
        }

        public static float ValueScore(CocTargetValueMode mode, float targetValue)
        {
            if (mode == CocTargetValueMode.IgnoreValue)
            {
                return 1f;
            }

            return Mathf.Max(0f, targetValue);
        }

        public static bool TryPickBest(
            ClassConfigRow classRow,
            IReadOnlyList<Candidate> candidates,
            out string bestId)
        {
            bestId = null;
            if (classRow == null ||
                classRow.TargetTypeScores == null ||
                classRow.TargetTypeScores.Count == 0 ||
                candidates == null ||
                candidates.Count == 0)
            {
                return false;
            }

            var bestScore = float.NegativeInfinity;
            var bestValue = float.NegativeInfinity;
            var bestDist = float.MaxValue;
            string bestStable = null;
            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (string.IsNullOrEmpty(c.Id) ||
                    string.IsNullOrEmpty(c.TypeName) ||
                    !classRow.TargetTypeScores.TryGetValue(c.TypeName, out var typeScore))
                {
                    continue;
                }

                var cells = DistanceCells(c.XzDistance);
                var distScore = DistanceScore(classRow.DistancePriority, cells);
                var valueScore = ValueScore(classRow.TargetValueMode, c.TargetValue);
                var finalScore = distScore * valueScore * typeScore;
                var nearer = c.XzDistance < bestDist;
                var id = c.Id;
                var better = false;
                if (bestStable == null)
                {
                    better = true;
                }
                else if (finalScore > bestScore)
                {
                    better = true;
                }
                else if (finalScore == bestScore)
                {
                    if (valueScore > bestValue)
                    {
                        better = true;
                    }
                    else if (valueScore == bestValue)
                    {
                        if (nearer)
                        {
                            better = true;
                        }
                        else if (!nearer &&
                                 c.XzDistance == bestDist &&
                                 string.CompareOrdinal(id, bestStable) < 0)
                        {
                            better = true;
                        }
                    }
                }

                if (!better)
                {
                    continue;
                }

                bestScore = finalScore;
                bestValue = valueScore;
                bestDist = c.XzDistance;
                bestStable = id;
            }

            bestId = bestStable;
            return !string.IsNullOrEmpty(bestId);
        }
    }
}
