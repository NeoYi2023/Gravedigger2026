namespace Gravedigger2026.Core.Config
{
    public sealed class MonsterConfigRow
    {
        public string MonsterId;
        public string ModelId;
        public string DisplayName;
        public TargetSelect TargetSelect;
        public AttackMode AttackMode;
        public AggroMode AggroMode;
        public float AlertRadius;
        public float BodyRadius;
        /// <summary>0|1; presentation-only (SPEC_04 §15.5).</summary>
        public int FacingYawFlip;
        public float MaxHP;
        public float MoveSpeed;
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
