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
        AutoManufacture = 4
    }
}
