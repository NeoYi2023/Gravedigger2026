namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_SoulConfig (SPEC_04 §9.9). Skills are not cast in Demo v1.
    /// </summary>
    public sealed class SoulConfigRow
    {
        public string SoulId;
        public string ClassId;
        public AttackMode AttackMode;
        public string Skills;
        public string AttackPriority;
        public string MoveStyle;
        public float SpiritCost;
        public float ControlPowerCost;
    }
}
