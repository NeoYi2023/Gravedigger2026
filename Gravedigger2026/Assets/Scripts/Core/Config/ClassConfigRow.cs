namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_ClassConfig (SPEC_04 §9.9b).
    /// </summary>
    public sealed class ClassConfigRow
    {
        /// <summary>Chase move-speed mult default (SPEC_04 §9.9b).</summary>
        public const float DefaultChaseMoveSpeedMult = 1f;

        /// <summary>Monster death knockback distance mult default (SPEC_04 §9.9b).</summary>
        public const float DefaultDeathKnockbackMult = 1f;

        public string ClassId;
        public string ClassName;
        public StatKind PrimaryStat;
        public string CombatConvertCoeffs;
        public float AttackRange;
        public float MeleeWindupSeconds;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
        /// <summary>≥0; × FinalStat(MoveSpeed) only when GoalKind=AttackSlot; default 1.</summary>
        public float ChaseMoveSpeedMult;
        /// <summary>≥0; scales monster death knockback displacement (T−M); default 1.</summary>
        public float DeathKnockbackMult;
        /// <summary>Mode2 no-soul AttackMode (SPEC_04 §9.9b). Missing → Melee.</summary>
        public AttackMode AttackMode;
        /// <summary>
        /// Mode2 auto-deploy ascending order. Missing/empty → <see cref="DefaultPlacementOrderMissing"/>.
        /// </summary>
        public int PlacementOrder;
        /// <summary>Missing PlacementOrder sentinel (post-order).</summary>
        public const int DefaultPlacementOrderMissing = 9999;
        /// <summary>Mode2 appearance fallback Id; empty → race IsFallback path.</summary>
        public string DefaultAppearanceId;
    }
}
