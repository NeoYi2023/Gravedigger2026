using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Batch-local soldier draft (SPEC_03 §3.15). Finalize Appearance / StaticStat / name before flush.
    /// </summary>
    public sealed class AutoCraftDraft
    {
        public string TempId;
        public string ClassId;
        public string ClassName;
        public AttackMode AttackMode;
        public string RaceId;
        public StatBlock BaseStats;
        public StatBlock RaceAdjustCoeff;
        public StatBlock EquipStats;
        public StatBlock GemMult;
        public float BodyLife;
        public int MaxHP;
        public string AppearanceId;
        public string WarriorName;
        /// <summary>Always 0 in Mode2 AutoManufacture.</summary>
        public float ControlPowerCost;
        /// <summary>Always empty in Mode2 AutoManufacture (no SoulId write).</summary>
        public string SoulId;
        public readonly List<string> ConsumedBodyPartIds = new List<string>();
        public readonly List<float> BodyLevels = new List<float>();
    }
}
