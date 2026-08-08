namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_BodyAppearanceConfig (SPEC_04 §9.13).
    /// AppearanceId is a Prefab logical name → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab.
    /// </summary>
    public sealed class BodyAppearanceConfigRow
    {
        /// <summary>Load default and monster Bind clamp baseline (SPEC_04 §9.13).</summary>
        public const float DefaultBodyRadius = 0.1f;

        public string AppearanceId;
        public int AppearanceLevel;
        public string RaceId;
        public string ClassAffinity;
        public string Description;
        public bool IsFallback;
        public float BodyRadius;
        /// <summary>0|1; presentation-only (SPEC_04 §15.5).</summary>
        public int FacingYawFlip;
    }
}
