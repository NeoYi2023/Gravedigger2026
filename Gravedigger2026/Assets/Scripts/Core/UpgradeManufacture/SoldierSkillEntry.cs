using System;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// One baked soldier-skill slot on a <see cref="WarriorInstance"/> (SPEC_04 §9.9).
    /// Public fields required for JsonUtility WarriorPool roundtrip.
    /// </summary>
    [Serializable]
    public sealed class SoldierSkillEntry
    {
        public string SkillId;
        public int SkillLevel;
    }
}
