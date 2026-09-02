using System;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Combat_TacticalFormationConfig (SPEC_04 §9.30).
    /// </summary>
    public sealed class TacticalFormationConfigRow
    {
        public string FormationId;
        public string DisplayName;
        public string IconAssetId;
        public string Description;
        /// <summary>FK → SkillConfig.SkillId; granted into SoldierSkills on MagicBook hit.</summary>
        public string FormationSkillId;
        /// <summary>≥ 1.</summary>
        public int MinMemberCount;
        /// <summary>≥ MinMemberCount.</summary>
        public int MaxMemberCount;
        public string PrefabId;
        /// <summary>Raw Stat=…|Mul=… overlay encoding; empty = none.</summary>
        public string StatModifiers;
        /// <summary>Runtime overlay SkillIds. Never null.</summary>
        public string[] ExclusiveSkillIds = Array.Empty<string>();
        /// <summary>Runtime overlay SkillEffectIds. Never null.</summary>
        public string[] ExclusiveSkillEffectIds = Array.Empty<string>();
    }
}
