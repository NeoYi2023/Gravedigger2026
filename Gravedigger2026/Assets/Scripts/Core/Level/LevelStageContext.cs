using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Resolved stage payload for enter/leave hooks. Map instantiate is deferred to Dig/Defend slices.
    /// </summary>
    public sealed class LevelStageContext
    {
        public string LevelId;
        public int StageNumber;
        public GameplayState GameplayType;
        public string GameplayConfigId;
        public bool GameplayConfigIgnored;
        public DigGameplayConfigRow DigConfig;
        public DefendGameplayConfigRow DefendConfig;
        public PushMapGameplayConfigRow PushMapConfig;
        public string ResolvedMapId;
        public string ResolvedMapPrefabPath;
        public string MapResolveNote;
    }
}
