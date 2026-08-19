namespace Gravedigger2026.Core.Combat
{
    /// <summary>SPEC_04 §9.21b TriggerHook enum strings.</summary>
    public static class SkillEffectTriggerHook
    {
        public const string OnOutgoingDamageSettle = "OnOutgoingDamageSettle";
        public const string OnIncomingDamageSettle = "OnIncomingDamageSettle";
        public const string OnWarriorAaHitConfirm = "OnWarriorAaHitConfirm";
        public const string OnWarriorTargetAcquired = "OnWarriorTargetAcquired";
        public const string OnWarriorWouldDie = "OnWarriorWouldDie";
        public const string OnProjectileHit = "OnProjectileHit";
        public const string OnSkillInternalCooldown = "OnSkillInternalCooldown";
    }
}
