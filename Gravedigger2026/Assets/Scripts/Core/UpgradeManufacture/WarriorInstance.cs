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

        /// <summary>Non-empty slot ItemIds consumed at manufacture (recipe for remake).</summary>
        public readonly List<string> SourceItemIds = new List<string>();

        /// <summary>Spirit cost paid at manufacture (recipe gate for remake).</summary>
        public float SourceSpiritCost;

        /// <summary>
        /// Baked soldier skills (SPEC_04 §9.9). Mode1/Mode2 grant DefaultSkillIds @ Lv1;
        /// Mode2 may then SoldierSkillLevelAdd (SS-04).
        /// </summary>
        public readonly List<SoldierSkillEntry> SoldierSkills = new List<SoldierSkillEntry>();

        /// <summary>
        /// Mode2 AllIn1 preset baked on MagicBook token hit (SPEC_03 §3.15 6b). Empty = Prefab default.
        /// </summary>
        public string VisualStyleId;

        /// <summary>Winning book's VisualPriority; 0 if none.</summary>
        public int VisualPriority;

        /// <summary>Stacked VisualIntensityAdd on the winning style.</summary>
        public float VisualIntensity;

        /// <summary>
        /// Model scale k (SPEC_03 §3.15 6b / D-082). Default 1; on MagicBook hit ×PerHit,
        /// optional Style_ScaleModel ×IntensityAdd, then clamp Max.
        /// </summary>
        public float VisualModelScale = 1f;
    }
}
