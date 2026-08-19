using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// SPEC_04 §9.21b EffectKind registry (D-073 / SE-01 bootstrap).
    /// </summary>
    public static class SkillEffectKind
    {
        public const string OutgoingMulOnNewTargetFirstHit = "OutgoingMulOnNewTargetFirstHit";
        public const string CheatDeathInvincible = "CheatDeathInvincible";
        public const string OnAaHitChanceAoeStun = "OnAaHitChanceAoeStun";
        public const string OnAaHitAoeSlow = "OnAaHitAoeSlow";
        public const string OutgoingMulVsMonsterType = "OutgoingMulVsMonsterType";
        public const string StackingOutgoingMulTimed = "StackingOutgoingMulTimed";
        public const string RangedPierceExtraHits = "RangedPierceExtraHits";
        public const string OnAaHitApplyBurn = "OnAaHitApplyBurn";
        public const string RetargetFarthestTeleportBehind = "RetargetFarthestTeleportBehind";

        public const string BondMaxHpMulForClass = "BondMaxHpMulForClass";

        private static readonly HashSet<string> Registered = new HashSet<string>(StringComparer.Ordinal)
        {
            OutgoingMulOnNewTargetFirstHit,
            CheatDeathInvincible,
            OnAaHitChanceAoeStun,
            OnAaHitAoeSlow,
            OutgoingMulVsMonsterType,
            StackingOutgoingMulTimed,
            RangedPierceExtraHits,
            OnAaHitApplyBurn,
            RetargetFarthestTeleportBehind,
            BondMaxHpMulForClass
        };

        public static bool IsRegistered(string kind)
        {
            return !string.IsNullOrWhiteSpace(kind) && Registered.Contains(kind);
        }
    }
}
