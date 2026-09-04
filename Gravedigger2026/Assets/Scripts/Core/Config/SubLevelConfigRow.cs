namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Level_SubLevelConfig row — one gameplay option (SPEC_04 §9.31 / D-086).
    /// </summary>
    public sealed class SubLevelConfigRow
    {
        public string GameplayOptionId;
        public GameplayState GameplayType;
        public string GameplayConfigId;
        public string IconAssetId;
        /// <summary>Tips middle art only; empty = hide (SPEC_04 §9.31).</summary>
        public string IconAssetId2;
        /// <summary>Dig Tips messages: MsgType;StockScale|… (SPEC_04 §9.31).</summary>
        public string TipMessages;
        public string Title;
        public string Description;
        public string Reward;
        public string UnlockNextOptionIds;
        /// <summary>SearchExtract only; 0 = unused / missing column.</summary>
        public int GatherPointCount;
        /// <summary>SearchExtract only; encoding N:ItemId;Count|…</summary>
        public string GatherPointRewards;
    }
}
