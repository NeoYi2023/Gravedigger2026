namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// One deployed soldier slot on BattleMap continuous coordinates (SPEC_03 §3.11 BattleFormation).
    /// </summary>
    public sealed class BattleFormationEntry
    {
        public string WarriorId;
        public float PositionX;
        public float PositionZ;
        public float RemainingHP;
    }
}
