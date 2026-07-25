using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Soldier static snapshot written at manufacture (SPEC_04 §9.9 / SPEC_03 §3.11).
    /// <see cref="EquipStats"/> and <see cref="BodyLife"/> are the manufacture-locked Equip layer
    /// and BodyLife = Base(MaxHP) + Equip(MaxHP).
    /// </summary>
    public sealed class WarriorInstance
    {
        public string Id;
        public string WarriorName;
        public float RemainingHP;
        public string RaceId;
        public StatBlock RaceAdjustCoeff;
        public StatBlock BaseStats;
        public string AppearanceId;
        public string SoulId;
        public string ClassId;
        public AttackMode AttackMode;
        public readonly List<string> LockedEquipIds = new List<string>();
        public readonly List<string> GemIds = new List<string>();
        public StatBlock GemMult;
        public float ControlPowerCost;
        public StatBlock EquipStats;
        public float BodyLife;
    }
}
