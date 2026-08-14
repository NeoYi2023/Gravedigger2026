using System;

namespace Gravedigger2026.Core.ProtagonistEquipment
{
    /// <summary>
    /// One owned protagonist gear entry (SPEC_03 §3.16 / SPEC_04 §9.25).
    /// </summary>
    [Serializable]
    public sealed class OwnedEquip
    {
        public string EquipId;
        public int Level = 1;
        public int CurrentExp;
    }
}
