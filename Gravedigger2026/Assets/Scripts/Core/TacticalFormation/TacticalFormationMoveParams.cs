namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Snapshot of Pattern move tunables after documented fallbacks (SPEC_04 §9.30).
    /// </summary>
    public readonly struct TacticalFormationMoveParams
    {
        public const float DefaultLeashRadius = 3f;
        public const float DefaultSlotArriveEpsilon = 0.15f;
        public const float DefaultCenterMoveSpeedMul = 1f;
        public const float DefaultFacingTurnRate = 180f;
        public const bool DefaultKeepFormationWhileEngage = true;

        public readonly float LeashRadius;
        public readonly float SlotArriveEpsilon;
        public readonly float CenterMoveSpeedMul;
        public readonly float FacingTurnRate;
        public readonly bool KeepFormationWhileEngage;

        public TacticalFormationMoveParams(
            float leashRadius,
            float slotArriveEpsilon,
            float centerMoveSpeedMul,
            float facingTurnRate,
            bool keepFormationWhileEngage)
        {
            LeashRadius = leashRadius > 0f ? leashRadius : DefaultLeashRadius;
            SlotArriveEpsilon = slotArriveEpsilon > 0f
                ? slotArriveEpsilon
                : DefaultSlotArriveEpsilon;
            CenterMoveSpeedMul = centerMoveSpeedMul > 0f
                ? centerMoveSpeedMul
                : DefaultCenterMoveSpeedMul;
            FacingTurnRate = facingTurnRate < 0f ? DefaultFacingTurnRate : facingTurnRate;
            KeepFormationWhileEngage = keepFormationWhileEngage;
        }

        public static TacticalFormationMoveParams CreateDefault()
        {
            return new TacticalFormationMoveParams(
                DefaultLeashRadius,
                DefaultSlotArriveEpsilon,
                DefaultCenterMoveSpeedMul,
                DefaultFacingTurnRate,
                DefaultKeepFormationWhileEngage);
        }
    }
}
