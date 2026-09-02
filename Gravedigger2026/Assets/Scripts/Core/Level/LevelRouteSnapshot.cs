using System;

namespace Gravedigger2026.Core.Level
{
    public enum LevelRouteOptionUiState
    {
        Locked = 0,
        Selectable = 1,
        Cleared = 2,
        Running = 3
    }

    public sealed class LevelRouteOptionSnapshot
    {
        public string GameplayOptionId;
        public int StageNumber;
        public string Title;
        public string Description;
        public string IconAssetId;
        public string Reward;
        public string UnlockNextOptionIds;
        public GameplayState GameplayType;
        public LevelRouteOptionUiState UiState;
    }

    public sealed class LevelRouteStageSnapshot
    {
        public int StageNumber;
        public LevelRouteOptionSnapshot[] Options = Array.Empty<LevelRouteOptionSnapshot>();
    }

    /// <summary>
    /// Immutable view of route-select state for UI (UI-031).
    /// </summary>
    public sealed class LevelRouteSnapshot
    {
        public string LevelId;
        public bool Visible;
        public LevelRouteStageSnapshot[] Stages = Array.Empty<LevelRouteStageSnapshot>();
    }
}
