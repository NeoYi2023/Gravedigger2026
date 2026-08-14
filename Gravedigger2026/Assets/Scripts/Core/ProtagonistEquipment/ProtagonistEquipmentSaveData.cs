using System;

namespace Gravedigger2026.Core.ProtagonistEquipment
{
    /// <summary>
    /// PlayerPrefs JSON DTO for ProtagonistEquipmentWarehouse (SPEC_04 §6 / §9.25).
    /// EquipCommonExp is stored as a separate int prefs key.
    /// </summary>
    [Serializable]
    public sealed class ProtagonistEquipmentSaveData
    {
        public OwnedEquip[] Equips = Array.Empty<OwnedEquip>();
    }
}
