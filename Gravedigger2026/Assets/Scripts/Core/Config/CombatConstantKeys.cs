namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Keys for Combat_CombatConstantConfig (SPEC_04 §9.20b).
    /// Combat formula + P0 camera/Dig + P1 combat tune + P2 pathing/perf.
    /// </summary>
    public static class CombatConstantKeys
    {
        public const string NormalAttackPrimaryMult = "NormalAttackPrimaryMult";
        public const string AttackSpeedBase = "AttackSpeedBase";
        public const string AttackSpeedAgiDiv = "AttackSpeedAgiDiv";
        public const string SkillCdIntDiv = "SkillCdIntDiv";
        public const string SkillCdFloor = "SkillCdFloor";
        public const string MaxHpStrengthMult = "MaxHpStrengthMult";

        public const string CameraHeightY = "CameraHeightY";
        public const string CameraOrthoSizeMargin = "CameraOrthoSizeMargin";
        public const string PushMapCameraOrthoSize = "PushMapCameraOrthoSize";
        public const string CameraNearClip = "CameraNearClip";
        public const string CameraFarClip = "CameraFarClip";
        public const string CameraFollowDeadzone = "CameraFollowDeadzone";
        public const string CameraFollowSmoothTime = "CameraFollowSmoothTime";
        public const string CameraZoomStepPerNotch = "CameraZoomStepPerNotch";
        public const string CameraOrthoSizeMin = "CameraOrthoSizeMin";
        public const string CameraOrthoSizeMax = "CameraOrthoSizeMax";
        public const string CameraDragThresholdPixels = "CameraDragThresholdPixels";
        public const string PushMapCameraIntroSpeed = "PushMapCameraIntroSpeed";
        public const string PushMapCameraIntroWaypointDwellSeconds = "PushMapCameraIntroWaypointDwellSeconds";

        public const string DigTriggerDwellSeconds = "DigTriggerDwellSeconds";
        public const string BaseDigDuration = "BaseDigDuration";
        public const string DigActionDurationFloor = "DigActionDurationFloor";

        // P1 — combat tune
        public const string AttackSlotMeleeCount = "AttackSlotMeleeCount";
        public const string AttackSlotRangedCount = "AttackSlotRangedCount";
        public const string AttackSlotMargin = "AttackSlotMargin";
        public const string AttackSlotMinRingRadius = "AttackSlotMinRingRadius";
        public const string AttackSlotReclaimMoveThreshold = "AttackSlotReclaimMoveThreshold";
        public const string AttackSlotDefaultTargetBodyRadius = "AttackSlotDefaultTargetBodyRadius";
        public const string HitConfirmSlack = "HitConfirmSlack";
        public const string SurroundGapDegrees = "SurroundGapDegrees";
        public const string StuckDetectWindowSeconds = "StuckDetectWindowSeconds";
        public const string StuckDisplacementEpsilon = "StuckDisplacementEpsilon";
        public const string StuckHoldSeconds = "StuckHoldSeconds";
        public const string ProjectileDefaultHitRadius = "ProjectileDefaultHitRadius";
        public const string DefendVictoryStageExp = "DefendVictoryStageExp";
        public const string NewSaveInitialSpiritCount = "NewSaveInitialSpiritCount";

        // P2 — pathing / perf
        public const string FlowFieldDefaultCellSize = "FlowFieldDefaultCellSize";
        public const string FlowFieldMinCellSize = "FlowFieldMinCellSize";
        public const string FlowFieldMaxCellSize = "FlowFieldMaxCellSize";
        public const string MassMoveMaxRecalcPerFrame = "MassMoveMaxRecalcPerFrame";
        public const string MassMoveDefaultAgentRadius = "MassMoveDefaultAgentRadius";
        public const string MassMoveArriveEpsilon = "MassMoveArriveEpsilon";
        public const string MassMoveDefaultObjectiveArriveRadius = "MassMoveDefaultObjectiveArriveRadius";
        public const string MassMoveAttackSlotSeparationScale = "MassMoveAttackSlotSeparationScale";
        public const string SoftCollisionMaxCorrectionSpeed = "SoftCollisionMaxCorrectionSpeed";
        public const string LocalDetourProbeLength = "LocalDetourProbeLength";
        public const string LocalDetourSoftSeparationStrength = "LocalDetourSoftSeparationStrength";
        public const string LocalDetourDetourBias = "LocalDetourDetourBias";
        public const string LocalDetourForwardConeHalfAngleDeg = "LocalDetourForwardConeHalfAngleDeg";
        public const string BossAdvanceArriveRadius = "BossAdvanceArriveRadius";
        public const string EngageStickHysteresisMargin = "EngageStickHysteresisMargin";
        public const string PushMapSpawnMinSampleDistance = "PushMapSpawnMinSampleDistance";
        public const string PushMapSpawnSampleDistanceBodyMul = "PushMapSpawnSampleDistanceBodyMul";
        public const string PushMapSpawnLeashSlack = "PushMapSpawnLeashSlack";
        public const string PushMapSpawnAbsoluteLeashFloor = "PushMapSpawnAbsoluteLeashFloor";
        public const string PushMapSpawnAbsoluteLeashBodyMul = "PushMapSpawnAbsoluteLeashBodyMul";

        /// <summary>Safety only when table key missing — not business authority.</summary>
        public static class Safety
        {
            public const float CameraHeightY = 18f;
            public const float CameraOrthoSizeMargin = 1.5f;
            public const float PushMapCameraOrthoSize = 2f;
            public const float CameraNearClip = 0.1f;
            public const float CameraFarClip = 100f;
            public const float CameraFollowDeadzone = 0.15f;
            public const float CameraFollowSmoothTime = 0.25f;
            public const float CameraZoomStepPerNotch = 0.5f;
            public const float CameraOrthoSizeMin = 0.5f;
            public const float CameraOrthoSizeMax = 20f;
            public const float CameraDragThresholdPixels = 4f;
            public const float PushMapCameraIntroSpeed = 1.5f;
            public const float PushMapCameraIntroWaypointDwellSeconds = 0.5f;
            public const float DigTriggerDwellSeconds = 0.2f;
            public const float BaseDigDuration = 0.8f;
            public const float DigActionDurationFloor = 0.1f;

            public const float AttackSlotMeleeCount = 12f;
            public const float AttackSlotRangedCount = 8f;
            public const float AttackSlotMargin = 0.05f;
            public const float AttackSlotMinRingRadius = 0.05f;
            public const float AttackSlotReclaimMoveThreshold = 0.5f;
            public const float AttackSlotDefaultTargetBodyRadius = 0.35f;
            public const float HitConfirmSlack = 0.05f;
            public const float SurroundGapDegrees = 60f;
            public const float StuckDetectWindowSeconds = 0.5f;
            public const float StuckDisplacementEpsilon = 0.2f;
            public const float StuckHoldSeconds = 1f;
            public const float ProjectileDefaultHitRadius = 0.55f;
            public const float DefendVictoryStageExp = 100f;
            public const float NewSaveInitialSpiritCount = 30f;

            public const float FlowFieldDefaultCellSize = 0.5f;
            public const float FlowFieldMinCellSize = 0.25f;
            public const float FlowFieldMaxCellSize = 0.5f;
            public const float MassMoveMaxRecalcPerFrame = 50f;
            public const float MassMoveDefaultAgentRadius = 0.1f;
            public const float MassMoveArriveEpsilon = 0.08f;
            public const float MassMoveDefaultObjectiveArriveRadius = 2f;
            public const float MassMoveAttackSlotSeparationScale = 0.35f;
            public const float SoftCollisionMaxCorrectionSpeed = 2f;
            public const float LocalDetourProbeLength = 1f;
            public const float LocalDetourSoftSeparationStrength = 0.15f;
            public const float LocalDetourDetourBias = 0.85f;
            public const float LocalDetourForwardConeHalfAngleDeg = 50f;
            public const float BossAdvanceArriveRadius = 0.35f;
            public const float EngageStickHysteresisMargin = 0.15f;
            public const float PushMapSpawnMinSampleDistance = 0.75f;
            public const float PushMapSpawnSampleDistanceBodyMul = 2.5f;
            public const float PushMapSpawnLeashSlack = 0.35f;
            public const float PushMapSpawnAbsoluteLeashFloor = 3f;
            public const float PushMapSpawnAbsoluteLeashBodyMul = 10f;
        }
    }
}
