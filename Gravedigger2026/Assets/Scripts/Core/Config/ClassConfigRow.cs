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
    }
}
