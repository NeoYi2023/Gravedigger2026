using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Resolves BattleMapId for UM FormationEditor (next Defend/PushMap/SearchExtract option; fallback Ground_01).
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
                if (row == null || row.StageNumber <= currentStageNumber || row.GameplayOptionIds == null)
                {
                    continue;
                }

                for (var j = 0; j < row.GameplayOptionIds.Length; j++)
                {
                    var oid = row.GameplayOptionIds[j];
                    if (string.IsNullOrEmpty(oid) || !configs.TryGetSubLevel(oid, out var sub) || sub == null)
                    {
                        continue;
                    }

                    if (sub.GameplayType == GameplayState.Defend
                        && configs.TryGetDefend(sub.GameplayConfigId, out var defend)
                        && defend != null
                        && !string.IsNullOrEmpty(defend.BattleMapId))
                    {
                        return defend.BattleMapId;
                    }

                    if (sub.GameplayType == GameplayState.PushMap
                        && configs.TryGetPushMap(sub.GameplayConfigId, out var push)
                        && push != null
                        && !string.IsNullOrEmpty(push.MapId))
                    {
                        return push.MapId;
                    }

                    if (sub.GameplayType == GameplayState.SearchExtract
                        && configs.TryGetSearchExtract(sub.GameplayConfigId, out var search)
                        && search != null
                        && !string.IsNullOrEmpty(search.MapId))
                    {
                        return search.MapId;
                    }
                }
            }

            return FallbackMapId;
        }
    }
}
