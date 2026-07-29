using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Resolves BattleMapId for UM FormationEditor (next Defend stage; fallback Ground_01).
    /// </summary>
    public static class FormationMapResolver
    {
        public const string FallbackMapId = "Ground_01";

        public static string ResolveUmBattleMapId(
            ConfigCsvRepository configs,
            string levelId,
            int currentStageNumber)
        {
            if (configs == null || string.IsNullOrEmpty(levelId))
            {
                return FallbackMapId;
            }

            var stages = configs.GetStagesForLevel(levelId);
            for (var i = 0; i < stages.Count; i++)
            {
                var row = stages[i];
                if (row == null || row.StageNumber <= currentStageNumber)
                {
                    continue;
                }

                if (row.GameplayType != GameplayState.Defend)
                {
                    continue;
                }

                if (configs.TryGetDefend(row.GameplayConfigId, out var defend)
                    && defend != null
                    && !string.IsNullOrEmpty(defend.BattleMapId))
                {
                    return defend.BattleMapId;
                }
            }

            return FallbackMapId;
        }
    }
}
