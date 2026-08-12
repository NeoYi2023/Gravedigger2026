using System;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// PlayerPrefs JSON DTO for last AutoManufacture batch Ids (SPEC_03 §3.15 / SPEC_04 §6 / D-054).
    /// </summary>
    [Serializable]
    public sealed class AutoManufactureBatchSaveData
    {
        public string[] WarriorIds = Array.Empty<string>();
    }
}
