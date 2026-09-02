namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Level_LevelOperationConfig row — Stage mounts up to 5 gameplay options (SPEC_04 §9.1 / D-086).
    /// </summary>
    public sealed class LevelOperationConfigRow
    {
        public string LevelId;
        public int StageNumber;
        public string[] GameplayOptionIds = System.Array.Empty<string>();
    }
}
