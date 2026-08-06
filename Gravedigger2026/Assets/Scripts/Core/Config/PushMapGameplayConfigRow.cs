namespace Gravedigger2026.Core.Config
{
    /// <summary>PushMap gameplay config row (SPEC_04 §9.22).</summary>
    public sealed class PushMapGameplayConfigRow
    {
        public string GameplayConfigId;
        public string MapId;
        public string DisplayName;
        public int StageExpReward;
        public string CaptureLoot;
        public string DungeonUnlockIds;
        public float CaptureSeconds;
    }
}
