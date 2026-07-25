namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_BodyAppearanceConfig (SPEC_04 §9.13).
    /// AppearanceId is a Prefab logical name → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab.
    /// </summary>
    public sealed class BodyAppearanceConfigRow
    {
        public string AppearanceId;
        public int AppearanceLevel;
        public string RaceId;
        public string ClassAffinity;
        public string Description;
        public bool IsFallback;
    }
}
