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
        /// <summary>Tips middle icon; empty = hide.</summary>
        public string IconAssetId2;
        /// <summary>Dig TipMessages encoding; empty = no message row.</summary>
        public string TipMessages;
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
        /// <summary>UI display name from Operation LevelName; empty → View falls back to LevelId.</summary>
        public string LevelName;
        public bool Visible;
        /// <summary>Optional route-map asset id; empty → legacy Stage-row layout.</summary>
        public string RouteMapAssetId;
        /// <summary>
        /// One-shot: option just cleared when returning to RouteSelect (map clear-return camera ceremony).
        /// Empty on open / tab switch / other publishes.
        /// </summary>
        public string JustClearedOptionId;
        public LevelRouteStageSnapshot[] Stages = Array.Empty<LevelRouteStageSnapshot>();
    }
}
