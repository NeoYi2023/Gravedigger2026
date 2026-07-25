namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_ExtraEquipmentConfig (SPEC_04 §9.14).
    /// </summary>
    public sealed class ExtraEquipmentConfigRow
    {
        public string EquipId;
        public EquipSlot EquipSlot;
        public string NamePrefix;
        public float SpiritCost;
        public float ControlPowerCost;
        public StatBlock EquipStats;
        public string Skills;
    }
}
