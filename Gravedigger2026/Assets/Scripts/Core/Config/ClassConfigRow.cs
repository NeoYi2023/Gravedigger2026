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
        public const float DefaultBaseMoveSpeed = 2f;

        /// <summary>Retired; knockback uses CombatConstantConfig (SPEC_04 §15.5). Kept for load compat.</summary>
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
        /// <summary>1-based Attack clip hold frame while ranged windup remains; ≤0 = no hold (SPEC_04 §9.9b).</summary>
        public int RangedWindupHoldFrame;
        /// <summary>
        /// Weighted normal-attack bases <c>id;weight|...</c> (SPEC_04 §9.9b).
        /// Empty → Attack1. Presentation only.
        /// For <c>AttackMode=Parabola</c>, this is the ranged (straight/arc) pool.
        /// </summary>
        public string NormalAttackAnims;
        /// <summary>
        /// Parabola temporary-melee attack bases <c>id;weight|...</c> (SPEC_04 §9.9b).
        /// Empty → Attack1. Presentation only. Unread for non-Parabola rows.
        /// </summary>
        public string ParabolaMeleeAttackAnims;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
        /// <summary>XZ center distance; forward-arc enemy closer than this forces temporary melee. Missing → 0.25.</summary>
        public const float DefaultParabolaMeleeRange = 0.25f;
        /// <summary>Target XZ center distance at which an arc and hit-rate roll start. Missing → 1.25.</summary>
        public const float DefaultParabolaArcMinDistance = 1.25f;
        /// <summary>Arc hit chance rolled once at fire. Missing → 0.6.</summary>
        public const float DefaultParabolaHitRate = 0.6f;
        /// <summary>Miss landing distance behind the target along aim. Missing → 0.8.</summary>
        public const float DefaultParabolaMissOvershoot = 0.8f;
        /// <summary>Seconds a miss stays on the ground. Missing → 1.5.</summary>
        public const float DefaultParabolaMissLingerSeconds = 1.5f;
        public float ParabolaMeleeRange = DefaultParabolaMeleeRange;
        public float ParabolaArcMinDistance = DefaultParabolaArcMinDistance;
        public float ParabolaHitRate = DefaultParabolaHitRate;
        public float ParabolaMissOvershoot = DefaultParabolaMissOvershoot;
        public float ParabolaMissLingerSeconds = DefaultParabolaMissLingerSeconds;
        /// <summary>≥0; soldier MoveSpeed Base (SPEC_04 §9.9b); missing/≤0 → DefaultBaseMoveSpeed.</summary>
        public float BaseMoveSpeed;
        /// <summary>≥0; × FinalStat(MoveSpeed) only when GoalKind=AttackSlot; default 1.</summary>
        public float ChaseMoveSpeedMult;
        /// <summary>Retired optional CSV column; unused in gameplay (kept for load compat).</summary>
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
        /// <summary>
        /// UI-033 CombatIndicator silhouette filename (SPEC_04 §9.9b).
        /// Runtime Resources/UI/Icons/{Id}; missing/empty → empty frame. Not used in combat math.
        /// </summary>
        public string SilhouetteIconAssetId;
        /// <summary>
        /// In-memory CSV data-row order (0-based) for stable same-level indicator sort.
        /// Not a CSV column (SPEC_04 §9.9b).
        /// </summary>
        public int TableOrder;

        public float ResolveBaseMoveSpeed()
        {
            return BaseMoveSpeed > 0.01f ? BaseMoveSpeed : DefaultBaseMoveSpeed;
        }
    }
}
