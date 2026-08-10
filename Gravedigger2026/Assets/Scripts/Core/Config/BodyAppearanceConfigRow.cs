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

        /// <summary>SoftCollision shove strength default (SPEC_04 §9.13).</summary>
        public const float DefaultPushCoefficient = 1f;

        /// <summary>SoftCollision per-body repulsion default (SPEC_04 §9.13).</summary>
        public const float DefaultRepulsionScale = 1f;

        public string AppearanceId;
        public int AppearanceLevel;
        public string RaceId;
        public string ClassAffinity;
        public string Description;
        public bool IsFallback;
        public float BodyRadius;
        /// <summary>SoftCollision shove strength; neighbor contrib × this when resolving others.</summary>
        public float PushCoefficient;
        /// <summary>SoftCollision per-body repulsion; effective = global × this.</summary>
        public float RepulsionScale;
        /// <summary>0|1; presentation-only (SPEC_04 §15.5).</summary>
        public int FacingYawFlip;
    }
}
