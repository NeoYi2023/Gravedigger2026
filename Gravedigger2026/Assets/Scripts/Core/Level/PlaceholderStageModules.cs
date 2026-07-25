using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    public sealed class DigPlaceholderStageModule : IStageModule
    {
        public GameplayState HandledState => GameplayState.Dig;

        public void Enter(LevelStageContext context)
        {
            Debug.Log(
                $"[Stage:Dig] Enter Level={context.LevelId} Stage={context.StageNumber} ConfigId={context.GameplayConfigId} MapId={context.ResolvedMapId} Prefab={context.ResolvedMapPrefabPath}");
        }

        public void Exit(LevelStageContext context)
        {
            Debug.Log($"[Stage:Dig] Exit Level={context.LevelId} Stage={context.StageNumber}");
        }
    }

    public sealed class UpgradeManufacturePlaceholderStageModule : IStageModule
    {
        public GameplayState HandledState => GameplayState.UpgradeManufacture;

        public void Enter(LevelStageContext context)
        {
            Debug.Log(
                $"[Stage:UM] Enter Level={context.LevelId} Stage={context.StageNumber} ConfigIdIgnored={context.GameplayConfigId} (no Dig/Defend lookup)");
        }

        public void Exit(LevelStageContext context)
        {
            Debug.Log($"[Stage:UM] Exit Level={context.LevelId} Stage={context.StageNumber}");
        }
    }

    public sealed class DefendPlaceholderStageModule : IStageModule
    {
        public GameplayState HandledState => GameplayState.Defend;

        public void Enter(LevelStageContext context)
        {
            Debug.Log(
                $"[Stage:Defend] Enter Level={context.LevelId} Stage={context.StageNumber} ConfigId={context.GameplayConfigId} MapId={context.ResolvedMapId} Prefab={context.ResolvedMapPrefabPath}");
        }

        public void Exit(LevelStageContext context)
        {
            Debug.Log($"[Stage:Defend] Exit Level={context.LevelId} Stage={context.StageNumber}");
        }
    }
}
