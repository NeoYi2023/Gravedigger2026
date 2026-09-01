using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Applied snapshot of CombatConstantConfig P1/P2 (and shared pathing) tunables.
    /// Filled by <see cref="ApplyFromRepository"/> after CSV load; missing keys use Safety.
    /// </summary>
    public static class CombatRuntimeTuning
    {
        public static int AttackSlotMeleeCount { get; private set; } =
            Mathf.RoundToInt(CombatConstantKeys.Safety.AttackSlotMeleeCount);

        public static int AttackSlotRangedCount { get; private set; } =
            Mathf.RoundToInt(CombatConstantKeys.Safety.AttackSlotRangedCount);

        public static float AttackSlotMargin { get; private set; } =
            CombatConstantKeys.Safety.AttackSlotMargin;

        public static float AttackSlotMinRingRadius { get; private set; } =
            CombatConstantKeys.Safety.AttackSlotMinRingRadius;

        public static float AttackSlotReclaimMoveThreshold { get; private set; } =
            CombatConstantKeys.Safety.AttackSlotReclaimMoveThreshold;

        public static float AttackSlotDefaultTargetBodyRadius { get; private set; } =
            CombatConstantKeys.Safety.AttackSlotDefaultTargetBodyRadius;

        public static float HitConfirmSlack { get; private set; } =
            CombatConstantKeys.Safety.HitConfirmSlack;

        public static float SurroundGapDegrees { get; private set; } =
            CombatConstantKeys.Safety.SurroundGapDegrees;

        public static float StuckDetectWindowSeconds { get; private set; } =
            CombatConstantKeys.Safety.StuckDetectWindowSeconds;

        public static float StuckDisplacementEpsilon { get; private set; } =
            CombatConstantKeys.Safety.StuckDisplacementEpsilon;

        public static float StuckHoldSeconds { get; private set; } =
            CombatConstantKeys.Safety.StuckHoldSeconds;

        public static float ProjectileDefaultHitRadius { get; private set; } =
            CombatConstantKeys.Safety.ProjectileDefaultHitRadius;

        public static long DefendVictoryStageExp { get; private set; } =
            (long)CombatConstantKeys.Safety.DefendVictoryStageExp;

        public static float DeathKnockbackRatioCoeff { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackRatioCoeff;

        public static float DeathKnockbackMinDistance { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackMinDistance;

        public static float DeathKnockbackMaxDistance { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackMaxDistance;

        public static float DeathDie2KnockbackThreshold { get; private set; } =
            CombatConstantKeys.Safety.DeathDie2KnockbackThreshold;

        public static float DeathKnockbackPeakHeight { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackPeakHeight;

        public static float DeathCorpseSmashDamageMul { get; private set; } =
            CombatConstantKeys.Safety.DeathCorpseSmashDamageMul;

        public static float DeathCorpseSmashHitRadius { get; private set; } =
            CombatConstantKeys.Safety.DeathCorpseSmashHitRadius;

        public static float DeathKnockbackSeconds { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackSeconds;

        public static float DeathKnockbackShadowAlphaMul { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackShadowAlphaMul;

        public static float DeathKnockbackShadowScaleMin { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackShadowScaleMin;

        public static float DeathKnockbackShadowBaseRadiusMul { get; private set; } =
            CombatConstantKeys.Safety.DeathKnockbackShadowBaseRadiusMul;

        public static float DeathDefendCorpseAlphaMul { get; private set; } =
            CombatConstantKeys.Safety.DeathDefendCorpseAlphaMul;

        public static float DeathCorpseDarkenMul { get; private set; } =
            CombatConstantKeys.Safety.DeathCorpseDarkenMul;

        public static float DeathFakeDeathCorpseDarkenMul { get; private set; } =
            CombatConstantKeys.Safety.DeathFakeDeathCorpseDarkenMul;

        public static float FlowFieldDefaultCellSize { get; private set; } =
            CombatConstantKeys.Safety.FlowFieldDefaultCellSize;

        public static float FlowFieldMinCellSize { get; private set; } =
            CombatConstantKeys.Safety.FlowFieldMinCellSize;

        public static float FlowFieldMaxCellSize { get; private set; } =
            CombatConstantKeys.Safety.FlowFieldMaxCellSize;

        public static int MassMoveMaxRecalcPerFrame { get; private set; } =
            Mathf.RoundToInt(CombatConstantKeys.Safety.MassMoveMaxRecalcPerFrame);

        public static float MassMoveDefaultAgentRadius { get; private set; } =
            CombatConstantKeys.Safety.MassMoveDefaultAgentRadius;

        public static float MassMoveArriveEpsilon { get; private set; } =
            CombatConstantKeys.Safety.MassMoveArriveEpsilon;

        public static float MassMoveDefaultObjectiveArriveRadius { get; private set; } =
            CombatConstantKeys.Safety.MassMoveDefaultObjectiveArriveRadius;

        public static float MassMoveAttackSlotSeparationScale { get; private set; } =
            CombatConstantKeys.Safety.MassMoveAttackSlotSeparationScale;

        public static float SoftCollisionMaxCorrectionSpeed { get; private set; } =
            CombatConstantKeys.Safety.SoftCollisionMaxCorrectionSpeed;

        public static float LocalDetourProbeLength { get; private set; } =
            CombatConstantKeys.Safety.LocalDetourProbeLength;

        public static float LocalDetourSoftSeparationStrength { get; private set; } =
            CombatConstantKeys.Safety.LocalDetourSoftSeparationStrength;

        public static float LocalDetourDetourBias { get; private set; } =
            CombatConstantKeys.Safety.LocalDetourDetourBias;

        public static float LocalDetourForwardConeHalfAngleDeg { get; private set; } =
            CombatConstantKeys.Safety.LocalDetourForwardConeHalfAngleDeg;

        public static float BossAdvanceArriveRadius { get; private set; } =
            CombatConstantKeys.Safety.BossAdvanceArriveRadius;

        public static float EngageStickHysteresisMargin { get; private set; } =
            CombatConstantKeys.Safety.EngageStickHysteresisMargin;

        public static float PushMapSpawnMinSampleDistance { get; private set; } =
            CombatConstantKeys.Safety.PushMapSpawnMinSampleDistance;

        public static float PushMapSpawnSampleDistanceBodyMul { get; private set; } =
            CombatConstantKeys.Safety.PushMapSpawnSampleDistanceBodyMul;

        public static float PushMapSpawnLeashSlack { get; private set; } =
            CombatConstantKeys.Safety.PushMapSpawnLeashSlack;

        public static float PushMapSpawnAbsoluteLeashFloor { get; private set; } =
            CombatConstantKeys.Safety.PushMapSpawnAbsoluteLeashFloor;

        public static float PushMapSpawnAbsoluteLeashBodyMul { get; private set; } =
            CombatConstantKeys.Safety.PushMapSpawnAbsoluteLeashBodyMul;

        public static void ApplyFromRepository(ConfigCsvRepository configs)
        {
            if (configs == null)
            {
                return;
            }

            float F(string key, float safety) => configs.GetCombatConstantOrFallback(key, safety);
            int I(string key, float safety) => Mathf.Max(1, Mathf.RoundToInt(F(key, safety)));

            AttackSlotMeleeCount = I(
                CombatConstantKeys.AttackSlotMeleeCount,
                CombatConstantKeys.Safety.AttackSlotMeleeCount);
            AttackSlotRangedCount = I(
                CombatConstantKeys.AttackSlotRangedCount,
                CombatConstantKeys.Safety.AttackSlotRangedCount);
            AttackSlotMargin = Mathf.Max(0f, F(
                CombatConstantKeys.AttackSlotMargin,
                CombatConstantKeys.Safety.AttackSlotMargin));
            AttackSlotMinRingRadius = Mathf.Max(0.01f, F(
                CombatConstantKeys.AttackSlotMinRingRadius,
                CombatConstantKeys.Safety.AttackSlotMinRingRadius));
            AttackSlotReclaimMoveThreshold = Mathf.Max(0f, F(
                CombatConstantKeys.AttackSlotReclaimMoveThreshold,
                CombatConstantKeys.Safety.AttackSlotReclaimMoveThreshold));
            AttackSlotDefaultTargetBodyRadius = Mathf.Max(0.01f, F(
                CombatConstantKeys.AttackSlotDefaultTargetBodyRadius,
                CombatConstantKeys.Safety.AttackSlotDefaultTargetBodyRadius));
            HitConfirmSlack = Mathf.Max(0f, F(
                CombatConstantKeys.HitConfirmSlack,
                CombatConstantKeys.Safety.HitConfirmSlack));
            SurroundGapDegrees = Mathf.Clamp(
                F(CombatConstantKeys.SurroundGapDegrees, CombatConstantKeys.Safety.SurroundGapDegrees),
                0f,
                360f);
            StuckDetectWindowSeconds = Mathf.Max(0.01f, F(
                CombatConstantKeys.StuckDetectWindowSeconds,
                CombatConstantKeys.Safety.StuckDetectWindowSeconds));
            StuckDisplacementEpsilon = Mathf.Max(0f, F(
                CombatConstantKeys.StuckDisplacementEpsilon,
                CombatConstantKeys.Safety.StuckDisplacementEpsilon));
            StuckHoldSeconds = Mathf.Max(0.01f, F(
                CombatConstantKeys.StuckHoldSeconds,
                CombatConstantKeys.Safety.StuckHoldSeconds));
            ProjectileDefaultHitRadius = Mathf.Max(0.05f, F(
                CombatConstantKeys.ProjectileDefaultHitRadius,
                CombatConstantKeys.Safety.ProjectileDefaultHitRadius));
            DefendVictoryStageExp = (long)Mathf.Max(0f, F(
                CombatConstantKeys.DefendVictoryStageExp,
                CombatConstantKeys.Safety.DefendVictoryStageExp));
            DeathKnockbackRatioCoeff = Mathf.Max(0f, F(
                CombatConstantKeys.DeathKnockbackRatioCoeff,
                CombatConstantKeys.Safety.DeathKnockbackRatioCoeff));
            DeathKnockbackMinDistance = Mathf.Max(0f, F(
                CombatConstantKeys.DeathKnockbackMinDistance,
                CombatConstantKeys.Safety.DeathKnockbackMinDistance));
            DeathKnockbackMaxDistance = Mathf.Max(
                DeathKnockbackMinDistance,
                F(
                    CombatConstantKeys.DeathKnockbackMaxDistance,
                    CombatConstantKeys.Safety.DeathKnockbackMaxDistance));
            DeathDie2KnockbackThreshold = Mathf.Max(0f, F(
                CombatConstantKeys.DeathDie2KnockbackThreshold,
                CombatConstantKeys.Safety.DeathDie2KnockbackThreshold));
            DeathKnockbackPeakHeight = Mathf.Max(0f, F(
                CombatConstantKeys.DeathKnockbackPeakHeight,
                CombatConstantKeys.Safety.DeathKnockbackPeakHeight));
            DeathCorpseSmashDamageMul = Mathf.Max(0f, F(
                CombatConstantKeys.DeathCorpseSmashDamageMul,
                CombatConstantKeys.Safety.DeathCorpseSmashDamageMul));
            DeathCorpseSmashHitRadius = Mathf.Max(0.05f, F(
                CombatConstantKeys.DeathCorpseSmashHitRadius,
                CombatConstantKeys.Safety.DeathCorpseSmashHitRadius));
            DeathKnockbackSeconds = Mathf.Max(0.01f, F(
                CombatConstantKeys.DeathKnockbackSeconds,
                CombatConstantKeys.Safety.DeathKnockbackSeconds));
            DeathKnockbackShadowAlphaMul = Mathf.Clamp01(F(
                CombatConstantKeys.DeathKnockbackShadowAlphaMul,
                CombatConstantKeys.Safety.DeathKnockbackShadowAlphaMul));
            DeathKnockbackShadowScaleMin = Mathf.Clamp01(F(
                CombatConstantKeys.DeathKnockbackShadowScaleMin,
                CombatConstantKeys.Safety.DeathKnockbackShadowScaleMin));
            DeathKnockbackShadowBaseRadiusMul = Mathf.Max(0.01f, F(
                CombatConstantKeys.DeathKnockbackShadowBaseRadiusMul,
                CombatConstantKeys.Safety.DeathKnockbackShadowBaseRadiusMul));
            DeathDefendCorpseAlphaMul = Mathf.Clamp01(F(
                CombatConstantKeys.DeathDefendCorpseAlphaMul,
                CombatConstantKeys.Safety.DeathDefendCorpseAlphaMul));
            DeathCorpseDarkenMul = Mathf.Clamp01(F(
                CombatConstantKeys.DeathCorpseDarkenMul,
                CombatConstantKeys.Safety.DeathCorpseDarkenMul));
            DeathFakeDeathCorpseDarkenMul = Mathf.Clamp01(F(
                CombatConstantKeys.DeathFakeDeathCorpseDarkenMul,
                CombatConstantKeys.Safety.DeathFakeDeathCorpseDarkenMul));

            FlowFieldMinCellSize = Mathf.Max(0.05f, F(
                CombatConstantKeys.FlowFieldMinCellSize,
                CombatConstantKeys.Safety.FlowFieldMinCellSize));
            FlowFieldMaxCellSize = Mathf.Max(FlowFieldMinCellSize, F(
                CombatConstantKeys.FlowFieldMaxCellSize,
                CombatConstantKeys.Safety.FlowFieldMaxCellSize));
            FlowFieldDefaultCellSize = Mathf.Clamp(
                F(CombatConstantKeys.FlowFieldDefaultCellSize, CombatConstantKeys.Safety.FlowFieldDefaultCellSize),
                FlowFieldMinCellSize,
                FlowFieldMaxCellSize);

            MassMoveMaxRecalcPerFrame = I(
                CombatConstantKeys.MassMoveMaxRecalcPerFrame,
                CombatConstantKeys.Safety.MassMoveMaxRecalcPerFrame);
            MassMoveDefaultAgentRadius = Mathf.Max(0.01f, F(
                CombatConstantKeys.MassMoveDefaultAgentRadius,
                CombatConstantKeys.Safety.MassMoveDefaultAgentRadius));
            MassMoveArriveEpsilon = Mathf.Max(0.01f, F(
                CombatConstantKeys.MassMoveArriveEpsilon,
                CombatConstantKeys.Safety.MassMoveArriveEpsilon));
            MassMoveDefaultObjectiveArriveRadius = Mathf.Max(0.01f, F(
                CombatConstantKeys.MassMoveDefaultObjectiveArriveRadius,
                CombatConstantKeys.Safety.MassMoveDefaultObjectiveArriveRadius));
            MassMoveAttackSlotSeparationScale = Mathf.Max(0f, F(
                CombatConstantKeys.MassMoveAttackSlotSeparationScale,
                CombatConstantKeys.Safety.MassMoveAttackSlotSeparationScale));
            SoftCollisionMaxCorrectionSpeed = Mathf.Max(0.01f, F(
                CombatConstantKeys.SoftCollisionMaxCorrectionSpeed,
                CombatConstantKeys.Safety.SoftCollisionMaxCorrectionSpeed));
            LocalDetourProbeLength = Mathf.Max(0.01f, F(
                CombatConstantKeys.LocalDetourProbeLength,
                CombatConstantKeys.Safety.LocalDetourProbeLength));
            LocalDetourSoftSeparationStrength = Mathf.Max(0f, F(
                CombatConstantKeys.LocalDetourSoftSeparationStrength,
                CombatConstantKeys.Safety.LocalDetourSoftSeparationStrength));
            LocalDetourDetourBias = Mathf.Clamp01(F(
                CombatConstantKeys.LocalDetourDetourBias,
                CombatConstantKeys.Safety.LocalDetourDetourBias));
            LocalDetourForwardConeHalfAngleDeg = Mathf.Clamp(
                F(
                    CombatConstantKeys.LocalDetourForwardConeHalfAngleDeg,
                    CombatConstantKeys.Safety.LocalDetourForwardConeHalfAngleDeg),
                1f,
                89f);
            BossAdvanceArriveRadius = Mathf.Max(0.01f, F(
                CombatConstantKeys.BossAdvanceArriveRadius,
                CombatConstantKeys.Safety.BossAdvanceArriveRadius));
            EngageStickHysteresisMargin = Mathf.Max(0f, F(
                CombatConstantKeys.EngageStickHysteresisMargin,
                CombatConstantKeys.Safety.EngageStickHysteresisMargin));
            PushMapSpawnMinSampleDistance = Mathf.Max(0.01f, F(
                CombatConstantKeys.PushMapSpawnMinSampleDistance,
                CombatConstantKeys.Safety.PushMapSpawnMinSampleDistance));
            PushMapSpawnSampleDistanceBodyMul = Mathf.Max(0.01f, F(
                CombatConstantKeys.PushMapSpawnSampleDistanceBodyMul,
                CombatConstantKeys.Safety.PushMapSpawnSampleDistanceBodyMul));
            PushMapSpawnLeashSlack = Mathf.Max(0f, F(
                CombatConstantKeys.PushMapSpawnLeashSlack,
                CombatConstantKeys.Safety.PushMapSpawnLeashSlack));
            PushMapSpawnAbsoluteLeashFloor = Mathf.Max(0.01f, F(
                CombatConstantKeys.PushMapSpawnAbsoluteLeashFloor,
                CombatConstantKeys.Safety.PushMapSpawnAbsoluteLeashFloor));
            PushMapSpawnAbsoluteLeashBodyMul = Mathf.Max(0.01f, F(
                CombatConstantKeys.PushMapSpawnAbsoluteLeashBodyMul,
                CombatConstantKeys.Safety.PushMapSpawnAbsoluteLeashBodyMul));
        }
    }
}
