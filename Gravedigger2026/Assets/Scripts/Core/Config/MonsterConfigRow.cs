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

        /// <summary>Walk→run delay default (SPEC_04 §9.19).</summary>
        public const float DefaultWalkToRunSeconds = 0.5f;

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
        /// <summary>Walk speed (SPEC_04 §9.19).</summary>
        public float MoveSpeed;
        /// <summary>Run speed; ≤0 → use MoveSpeed at runtime (SPEC_04 §9.19).</summary>
        public float RunSpeed;
        /// <summary>Seconds of continuous locomotion before run; default 0.5 (SPEC_04 §9.19).</summary>
        public float WalkToRunSeconds;
        /// <summary>≥0; × gait speed when ActiveChase; default 1.</summary>
        public float ActiveMoveMult;
        /// <summary>≥0; × gait speed when PassiveChase; default 1.</summary>
        public float PassiveMoveMult;
        public float AttackPower;
        public float AttackSpeed;
        public float AttackRange;
        public float MeleeWindupSeconds;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
        public string Skills;
        /// <summary>Presentation: attack base-name pool `Attack1|Attack2|…`; empty → Attack1 (SPEC_04 §9.19).</summary>
        public string NormalAttackAnims;
        /// <summary>Presentation: walk BlendTree state pool; empty → WalkBT (SPEC_04 §9.19).</summary>
        public string WalkAnims;
        /// <summary>Presentation: run BlendTree state pool; empty → RunBT (SPEC_04 §9.19).</summary>
        public string RunAnims;
        public string LootDrop;

        /// <summary>Run speed with MoveSpeed fallback (SPEC_04 §9.19).</summary>
        public float ResolveRunSpeed()
        {
            return RunSpeed > 0.01f ? RunSpeed : MoveSpeed;
        }

        /// <summary>Walk speed for gait (SPEC_04 §9.19).</summary>
        public float ResolveWalkSpeed()
        {
            return MoveSpeed;
        }

        /// <summary>Gait speed for walk or run (SPEC_04 §9.19).</summary>
        public float ResolveGaitSpeed(bool isRun)
        {
            return isRun ? ResolveRunSpeed() : ResolveWalkSpeed();
        }
    }
}
