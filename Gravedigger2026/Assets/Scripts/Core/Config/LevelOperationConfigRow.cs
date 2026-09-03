namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Level_LevelOperationConfig row — Stage mounts up to 5 gameplay options (SPEC_04 §9.1 / D-086).
    /// </summary>
    public sealed class LevelOperationConfigRow
    {
        public string LevelId;
        /// <summary>UI display name (UI-031 tabs / Title). Per-Level; first non-empty among rows.</summary>
        public string LevelName;
        public int StageNumber;
        public string[] GameplayOptionIds = System.Array.Empty<string>();
        /// <summary>Optional route-map sprite id (filename without ext). Per-Level; first non-empty among rows.</summary>
        public string RouteMapAssetId;
        /// <summary>
        /// Stage1 only (other Stage rows ignored at resolve). Empty=default unlocked;
        /// known GameplayOptionId=prereq; unknown=never (SPEC_03 §3.9).
        /// </summary>
        public string UnlockLevelId;
    }
}

