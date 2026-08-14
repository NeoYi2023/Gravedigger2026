namespace Gravedigger2026.Core.Config
{
    public sealed class MonsterConfigRow
    {
        /// <summary>SoftCollision shove strength default (SPEC_04 §9.19).</summary>
        public const float DefaultPushCoefficient = 1f;

        /// <summary>SoftCollision per-body repulsion default (SPEC_04 §9.19).</summary>
        public const float DefaultRepulsionScale = 1f;

        /// <summary>ActiveChase move-speed mult default (SPEC_04 §9.19).</summary>
        public const float DefaultActiveMoveMult = 1f;

        /// <summary>PassiveChase move-speed mult default (SPEC_04 §9.19).</summary>
        public const float DefaultPassiveMoveMult = 1f;

        public string MonsterId;
        public string ModelId;
        public string DisplayName;
        public TargetSelect TargetSelect;
        public AttackMode AttackMode;
        /// <summary>1=Normal / 2=Elite / 3=Boss; default Normal (SPEC_04 §9.19).</summary>
        public MonsterType MonsterType;
        public AggroMode AggroMode;
        public float AlertRadius;
        public float BodyRadius;
        /// <summary>SoftCollision shove strength; default 1 (SPEC_04 §9.19).</summary>
        public float PushCoefficient;
        /// <summary>SoftCollision per-body repulsion; default 1 (SPEC_04 §9.19).</summary>
        public float RepulsionScale;
        /// <summary>0|1; presentation-only (SPEC_04 §15.5).</summary>
        public int FacingYawFlip;
        public float MaxHP;
        public float MoveSpeed;
        /// <summary>≥0; × MoveSpeed when ActiveChase; default 1.</summary>
        public float ActiveMoveMult;
        /// <summary>≥0; × MoveSpeed when PassiveChase; default 1.</summary>
        public float PassiveMoveMult;
        public float AttackPower;
        public float AttackSpeed;
        public float AttackRange;
        public float MeleeWindupSeconds;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
        public string Skills;
        public string LootDrop;
    }
}
