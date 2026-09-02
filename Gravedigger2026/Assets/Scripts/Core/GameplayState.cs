namespace Gravedigger2026.Core
{
    /// <summary>
    /// In-session main gameplay state (SPEC_03 §3.1 / §3.7).
    /// </summary>
    public enum GameplayState
    {
        Dig = 0,
        UpgradeManufacture = 1,
        Defend = 2,
        PushMap = 3,
        /// <summary>Mode2 Dig→AutoManufacture→UM pipeline (SPEC_03 §3.15).</summary>
        AutoManufacture = 4,
        /// <summary>Mode2 shop stage (SPEC_03 §3.5 / §3.9 / D-075).</summary>
        Shop = 5,
        /// <summary>Mode2 SearchExtract SubLevel (SPEC_03 §3.19 / D-087).</summary>
        SearchExtract = 6
    }
}
