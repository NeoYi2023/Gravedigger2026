using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Slot-change preview (SPEC_03 §3.11 「预览刷新」). Race / appearance are trial rolls.
    /// </summary>
    public sealed class ManufacturePreview
    {
        public StatBlock BaseStats;
        public StatBlock EquipStats;
        public StatBlock GemMult;
        public StatBlock RaceAdjustCoeff;
        public StatBlock StaticStats;
        public float BodyLife;
        public int StaticMaxHP;
        public float TotalSpiritCost;
        public float ControlPowerCost;
        public string TrialRaceId;
        public string TrialRaceDisplayName;
        public string TrialAppearanceId;
        public string ClassId;
        public string ClassName;
        public string TrialWarriorName;
        public bool MinRequirementMet;
        public bool SpiritEnough;
        public bool CanManufacture;
        public string BlockReason;
    }
}
