namespace Gravedigger2026.Core.Config
{
    /// <summary>COC gameplay row (SPEC_04 §9.35 / D-099).</summary>
    public sealed class CocGameplayConfigRow
    {
        public string GameplayConfigId;
        public string MapId;
        public float RevealRadius;
        public float UnexploredAlpha;
        public float ExploredAlpha;
        public float CardHoldSeconds;
        public float DeployHoldIntervalSeconds;
    }
}
