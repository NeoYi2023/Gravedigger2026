using System;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_ClassConfig (SPEC_04 §9.9b).
    /// </summary>
    public sealed class ClassConfigRow
    {
        /// <summary>Chase move-speed mult default (SPEC_04 §9.9b).</summary>
        public const float DefaultChaseMoveSpeedMult = 1f;

        /// <summary>Soldier base move speed when ClassConfig.BaseMoveSpeed missing/≤0 (SPEC_04 §9.9b).</summary>
        public const float DefaultBaseMoveSpeed = 3.5f;

        /// <summary>Monster death knockback distance mult default (SPEC_04 §9.9b).</summary>
        public const float DefaultDeathKnockbackMult = 1f;

        public string ClassId;
        public string ClassName;
        /// <summary>
        /// Base class family (SPEC_04 §9.9b). CSV Chinese 战士/射手/法师/刺客 (legacy 盗贼 accepted).
        /// Empty/illegal → Unspecified. Reserved; not used in naming/combat this slice.
        /// </summary>
        public BaseClassKind BaseClass;
        /// <summary>
        /// Optional promote-class text (SPEC_04 §9.9b). Empty = none.
        /// Fillable this slice; not used in naming/combat; application TBD.
        /// </summary>
        public string PromoteClass;
        /// <summary>Display-only grade (UI-016 Lv.N). Missing → 0. Not used in combat math.</summary>
        public int ClassLevel;
        public StatKind PrimaryStat;
        public string CombatConvertCoeffs;
        public float AttackRange;
        public float MeleeWindupSeconds;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
        /// <summary>≥0; soldier MoveSpeed Base (SPEC_04 §9.9b); missing/≤0 → DefaultBaseMoveSpeed.</summary>
        public float BaseMoveSpeed;
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
        /// <summary>
        /// Parsed DefaultSkillIds (SPEC_04 §9.9b). Never null; empty = none.
        /// Duplicates keep first. Unknown SkillId kept (warn at grant, not load).
        /// </summary>
        public string[] DefaultSkillIds = Array.Empty<string>();

        public float ResolveBaseMoveSpeed()
        {
            return BaseMoveSpeed > 0.01f ? BaseMoveSpeed : DefaultBaseMoveSpeed;
        }
    }
}
