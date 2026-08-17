using System;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// PlayerPrefs JSON DTOs for WarriorPool (SPEC_04 §6).
    /// </summary>
    [Serializable]
    public sealed class WarriorPoolSaveData
    {
        public int NextSerial = 1;
        public WarriorSaveDto[] Warriors = Array.Empty<WarriorSaveDto>();
    }

    [Serializable]
    public sealed class WarriorSaveDto
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
        public int AttackMode;
        public string[] LockedEquipIds = Array.Empty<string>();
        public string[] GemIds = Array.Empty<string>();
        public StatBlock GemMult;
        public float ControlPowerCost;
        public StatBlock EquipStats;
        public float BodyLife;
        public string[] SourceItemIds = Array.Empty<string>();
        public float SourceSpiritCost;
        public SoldierSkillEntry[] SoldierSkills = Array.Empty<SoldierSkillEntry>();
        public string VisualStyleId;
        public int VisualPriority;
        public float VisualIntensity;
    }

    [Serializable]
    public sealed class BattleFormationSaveData
    {
        public BattleFormationSaveEntry[] Entries = Array.Empty<BattleFormationSaveEntry>();
    }

    [Serializable]
    public sealed class BattleFormationSaveEntry
    {
        public string WarriorId;
        public float PositionX;
        public float PositionZ;
        public float RemainingHP;
    }
}
