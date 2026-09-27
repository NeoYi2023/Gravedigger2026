using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Scene-free correctness checks for TF-04a/04b (SPEC_03 §3.18 / SPEC_04 §9.7).
    /// Call <see cref="RunAll"/> from Editor menu or console.
    /// </summary>
    public static class TacticalFormationRuntimeCorrectnessChecks
    {
        private const float Dt = 1f / 60f;
        private const float Eps = 0.02f;

        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckSlotWorldMatchesPrepareRotation(sb);
            CheckLeashProjectsOutsideAndKeepsInside(sb);
            CheckLeashNonPositiveFallsBackToDefault(sb);
            CheckHoldDoesNotMoveCenter(sb);
            CheckFollowFlowFieldIntegrates(sb);
            CheckFacingTurnsTowardFlowDir(sb);
            CheckSchedulerFormationSlotSeeksDest(sb);
            CheckFormationSlotAnimDistanceIsFinite(sb);
            CheckFormationSlotMoveModeIsChase(sb);
            CheckPolicyIdleKeepTrueSeeksSlot(sb);
            CheckPolicyIdleKeepFalseFallsBack(sb);
            CheckPolicyNonMemberNotHandled(sb);
            CheckPolicyBeyondLeashHoldKeepsSlot(sb);
            CheckPolicyOverflowKeepTrueSeeksSlot(sb);
            CheckPolicyOverflowKeepFalseUnhandled(sb);
            CheckPolicyEnemyLeashAndClamp(sb);
            CheckStatModifiersParseStrengthAndAll(sb);
            CheckOverlayActiveAtStart(sb);
            CheckDissolveBelowMinRemovesOverlay(sb);
            CheckRebelLeavesWithoutDissolvingOthers(sb);
            CheckExclusiveSkillOverlayReadOnly(sb);
            CheckStatMulCombineWithMagicBook(sb);
            CheckFormationLevelLookup(sb);
            CheckTwoGroupsSameFormationIndependentLevel(sb);
            CheckDuplicateFormationCompositeKeyThrows(sb);
            CheckManualGroupSession(sb);
            CheckCatalogFillsFromSoldierBar(sb);
            CheckSearchExtractLayoutKeepsGroupOffset(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckSlotWorldMatchesPrepareRotation(StringBuilder sb)
        {
            var runtime = new TacticalFormationRuntimeService();
            var local = new Vector2(0f, 1f);
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Test",
                        new[] { "w1" },
                        new[] { local },
                        TacticalFormationMoveParams.CreateDefault(),
                        Vector2.zero,
                        90f)
                },
                TacticalFormationCenterMode.Hold,
                0f);

            if (!runtime.TryGetSlotWorldXZ("w1", out var world))
            {
                sb.AppendLine("SlotWorld: missing member w1.");
                return;
            }

            var expected = TacticalFormationRuntimeService.RotateYaw(local, 90f);
            if ((world - expected).sqrMagnitude > Eps * Eps)
            {
                sb.AppendLine($"SlotWorld: got {world} expected {expected} (yaw 90, local +Z).");
            }

            if (Mathf.Abs(expected.x - 1f) > Eps || Mathf.Abs(expected.y) > Eps)
            {
                sb.AppendLine($"SlotWorld: RotateYaw(0,1) @90° expected ~(1,0) got {expected}.");
            }
        }

        private static void CheckLeashProjectsOutsideAndKeepsInside(StringBuilder sb)
        {
            var center = Vector2.zero;
            var outside = new Vector2(10f, 0f);
            var clamped = TacticalFormationRuntimeService.ClampToLeash(center, outside, 3f);
            if (Mathf.Abs(clamped.x - 3f) > Eps || Mathf.Abs(clamped.y) > Eps)
            {
                sb.AppendLine($"LeashOutside: expected (3,0) got {clamped}.");
            }

            var inside = new Vector2(1f, 0f);
            var kept = TacticalFormationRuntimeService.ClampToLeash(center, inside, 3f);
            if ((kept - inside).sqrMagnitude > 1e-8f)
            {
                sb.AppendLine($"LeashInside: expected unchanged {inside} got {kept}.");
            }

            var runtime = new TacticalFormationRuntimeService();
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Test",
                        new[] { "w1" },
                        new[] { Vector2.zero },
                        new TacticalFormationMoveParams(3f, 0.15f, 1f, 0f, true),
                        Vector2.zero,
                        0f)
                },
                TacticalFormationCenterMode.Hold,
                0f);

            if (!runtime.TryClampMemberAttackSlot("w1", outside, out var memberClamped)
                || (memberClamped - clamped).sqrMagnitude > Eps * Eps)
            {
                sb.AppendLine($"LeashMember: TryClampMemberAttackSlot got {memberClamped}.");
            }

            if (runtime.TryIsWorldInsideLeash("w1", outside))
            {
                sb.AppendLine("LeashMember: (10,0) should be outside leash 3.");
            }

            if (!runtime.TryIsWorldInsideLeash("w1", inside))
            {
                sb.AppendLine("LeashMember: (1,0) should be inside leash 3.");
            }
        }

        private static void CheckLeashNonPositiveFallsBackToDefault(StringBuilder sb)
        {
            var clamped = TacticalFormationRuntimeService.ClampToLeash(
                Vector2.zero,
                new Vector2(10f, 0f),
                0f);
            var expected = TacticalFormationMoveParams.DefaultLeashRadius;
            if (Mathf.Abs(clamped.x - expected) > Eps)
            {
                sb.AppendLine($"LeashFallback: expected x={expected} got {clamped}.");
            }
        }

        private static void CheckHoldDoesNotMoveCenter(StringBuilder sb)
        {
            var runtime = new TacticalFormationRuntimeService();
            var start = new Vector2(5f, 7f);
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Test",
                        new[] { "w1" },
                        new[] { Vector2.zero },
                        TacticalFormationMoveParams.CreateDefault(),
                        start,
                        0f)
                },
                TacticalFormationCenterMode.Hold,
                4f);

            runtime.Tick(1f, new Vector2(0f, 1f));
            if (!runtime.TryGetCenterXZ("w1", out var center) || (center - start).sqrMagnitude > 1e-8f)
            {
                sb.AppendLine($"Hold: center moved from {start} to {center}.");
            }
        }

        private static void CheckFollowFlowFieldIntegrates(StringBuilder sb)
        {
            var runtime = new TacticalFormationRuntimeService();
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Test",
                        new[] { "w1" },
                        new[] { Vector2.zero },
                        new TacticalFormationMoveParams(3f, 0.15f, 1f, 0f, true),
                        Vector2.zero,
                        0f)
                },
                TacticalFormationCenterMode.FollowFlowField,
                2f);

            runtime.Tick(0.5f, new Vector2(0f, 1f));
            if (!runtime.TryGetCenterXZ("w1", out var center))
            {
                sb.AppendLine("Follow: missing center.");
                return;
            }

            var expectedZ = 1f;
            if (Mathf.Abs(center.x) > Eps || Mathf.Abs(center.y - expectedZ) > Eps)
            {
                sb.AppendLine($"Follow: expected (0,{expectedZ}) after 0.5s @ speed 2 got {center}.");
            }
        }

        private static void CheckFacingTurnsTowardFlowDir(StringBuilder sb)
        {
            var runtime = new TacticalFormationRuntimeService();
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Test",
                        new[] { "w1" },
                        new[] { Vector2.zero },
                        new TacticalFormationMoveParams(3f, 0.15f, 1f, 180f, true),
                        Vector2.zero,
                        0f)
                },
                TacticalFormationCenterMode.FollowFlowField,
                0f);

            runtime.Tick(0.25f, new Vector2(1f, 0f));
            if (!runtime.TryGetFacingYawDegrees("w1", out var yaw))
            {
                sb.AppendLine("Facing: missing yaw.");
                return;
            }

            if (Mathf.Abs(Mathf.DeltaAngle(yaw, 45f)) > 1f)
            {
                sb.AppendLine($"Facing: expected ~45° after 0.25s @ 180°/s toward +X, got {yaw}.");
            }
        }

        private static void CheckSchedulerFormationSlotSeeksDest(StringBuilder sb)
        {
            var scheduler = new MassMoveScheduler();
            scheduler.Register(1, 0.1f, MassMoveScheduler.DetourGroupLoyal);
            scheduler.SetGoal(1, GoalKind.FormationSlot, new Vector2(10f, 0f));
            var samples = new List<MassMoveSample>
            {
                new MassMoveSample(1, Vector2.zero, 0.1f, true)
            };
            scheduler.Tick(samples, Dt);

            if (!scheduler.TryGetSteer(1, out var steer) || steer.x <= 0.5f || Mathf.Abs(steer.y) > 0.25f)
            {
                sb.AppendLine($"SchedulerSeek: expected +X steer for FormationSlot, got {steer}.");
            }

            if (!scheduler.TryGetGoal(1, out var kind, out _) || kind != GoalKind.FormationSlot)
            {
                sb.AppendLine($"SchedulerSeek: GoalKind {kind} != FormationSlot.");
            }
        }

        private static void CheckFormationSlotAnimDistanceIsFinite(StringBuilder sb)
        {
            var scheduler = new MassMoveScheduler();
            scheduler.Register(1, 0.1f, MassMoveScheduler.DetourGroupLoyal);
            scheduler.SetGoal(1, GoalKind.FormationSlot, new Vector2(4f, 0f));
            var dist = scheduler.GetAnimMoveTargetDistanceXZ(1, Vector2.zero);
            if (float.IsInfinity(dist) || Mathf.Abs(dist - 4f) > Eps)
            {
                sb.AppendLine($"AnimDist: FormationSlot expected 4, got {dist}.");
            }
        }

        private static void CheckFormationSlotMoveModeIsChase(StringBuilder sb)
        {
            var mode = CombatMoveModePolicy.Derive(GoalKind.FormationSlot, AttackMode.Melee);
            if (mode != CombatMoveMode.Chase)
            {
                sb.AppendLine($"MoveMode: FormationSlot+Melee expected Chase, got {mode}.");
            }

            if (CombatMoveModePolicy.SurroundFor(GoalKind.FormationSlot, AttackMode.Melee).HasValue)
            {
                sb.AppendLine("MoveMode: FormationSlot should not use Surround.");
            }
        }

        private static TacticalFormationRuntimeService CreateMemberRuntime(
            bool keepFormation,
            Vector2 center,
            Vector2 slotLocal,
            float leash = 3f)
        {
            var runtime = new TacticalFormationRuntimeService();
            var move = new TacticalFormationMoveParams(leash, 0.15f, 1f, 180f, keepFormation);
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Test",
                        new[] { "w1" },
                        new[] { slotLocal },
                        move,
                        center,
                        0f)
                },
                TacticalFormationCenterMode.Hold,
                0f);
            return runtime;
        }

        private static void CheckPolicyIdleKeepTrueSeeksSlot(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(true, Vector2.zero, new Vector2(0f, 1.2f));
            if (!TacticalFormationCombatGoalPolicy.TryResolveIdleGoal(
                    runtime,
                    "w1",
                    TacticalFormationIdleFallback.Objective,
                    default,
                    out var kind,
                    out var dest))
            {
                sb.AppendLine("PolicyIdleKeepTrue: expected handled.");
                return;
            }

            if (kind != GoalKind.FormationSlot)
            {
                sb.AppendLine($"PolicyIdleKeepTrue: expected FormationSlot got {kind}.");
            }

            if ((dest - new Vector2(0f, 1.2f)).sqrMagnitude > Eps * Eps)
            {
                sb.AppendLine($"PolicyIdleKeepTrue: dest {dest} expected (0,1.2).");
            }
        }

        private static void CheckPolicyIdleKeepFalseFallsBack(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(false, Vector2.zero, new Vector2(0f, 1.2f));
            if (!TacticalFormationCombatGoalPolicy.TryResolveIdleGoal(
                    runtime,
                    "w1",
                    TacticalFormationIdleFallback.Objective,
                    default,
                    out var kind,
                    out _))
            {
                sb.AppendLine("PolicyIdleKeepFalse Objective: expected handled.");
            }
            else if (kind != GoalKind.Objective)
            {
                sb.AppendLine($"PolicyIdleKeepFalse Objective: expected Objective got {kind}.");
            }

            var home = new Vector2(4f, 5f);
            if (!TacticalFormationCombatGoalPolicy.TryResolveIdleGoal(
                    runtime,
                    "w1",
                    TacticalFormationIdleFallback.FormationHome,
                    home,
                    out kind,
                    out var dest))
            {
                sb.AppendLine("PolicyIdleKeepFalse Home: expected handled.");
            }
            else if (kind != GoalKind.FormationHome || (dest - home).sqrMagnitude > Eps * Eps)
            {
                sb.AppendLine($"PolicyIdleKeepFalse Home: expected FormationHome {home} got {kind} {dest}.");
            }
        }

        private static void CheckPolicyNonMemberNotHandled(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(true, Vector2.zero, Vector2.zero);
            if (TacticalFormationCombatGoalPolicy.TryResolveIdleGoal(
                    runtime,
                    "other",
                    TacticalFormationIdleFallback.Objective,
                    default,
                    out _,
                    out _))
            {
                sb.AppendLine("PolicyNonMember: idle should not handle outsiders.");
            }
        }

        private static void CheckPolicyBeyondLeashHoldKeepsSlot(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(false, Vector2.zero, new Vector2(0f, 1f), leash: 3f);
            if (!TacticalFormationCombatGoalPolicy.TryResolveBeyondLeashHold(
                    runtime,
                    "w1",
                    out var kind,
                    out var dest))
            {
                sb.AppendLine("PolicyBeyondLeash: expected slot hold even when Keep=false.");
                return;
            }

            if (kind != GoalKind.FormationSlot || (dest - new Vector2(0f, 1f)).sqrMagnitude > Eps * Eps)
            {
                sb.AppendLine($"PolicyBeyondLeash: expected FormationSlot (0,1) got {kind} {dest}.");
            }
        }

        private static void CheckPolicyOverflowKeepTrueSeeksSlot(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(true, Vector2.zero, new Vector2(1f, 0f));
            if (!TacticalFormationCombatGoalPolicy.TryResolveOverflow(
                    runtime,
                    "w1",
                    TacticalFormationIdleFallback.Objective,
                    default,
                    out var kind,
                    out _))
            {
                sb.AppendLine("PolicyOverflowKeepTrue: expected handled.");
            }
            else if (kind != GoalKind.FormationSlot)
            {
                sb.AppendLine($"PolicyOverflowKeepTrue: expected FormationSlot got {kind}.");
            }
        }

        private static void CheckPolicyOverflowKeepFalseUnhandled(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(false, Vector2.zero, new Vector2(1f, 0f));
            if (TacticalFormationCombatGoalPolicy.TryResolveOverflow(
                    runtime,
                    "w1",
                    TacticalFormationIdleFallback.Objective,
                    default,
                    out _,
                    out _))
            {
                sb.AppendLine("PolicyOverflowKeepFalse: should leave Stage overflow path.");
            }
        }

        private static void CheckPolicyEnemyLeashAndClamp(StringBuilder sb)
        {
            var runtime = CreateMemberRuntime(true, Vector2.zero, Vector2.zero, leash: 3f);
            if (TacticalFormationCombatGoalPolicy.IsEnemyInsideLeash(runtime, "w1", new Vector2(10f, 0f)))
            {
                sb.AppendLine("PolicyLeash: (10,0) should be outside r=3.");
            }

            if (!TacticalFormationCombatGoalPolicy.IsEnemyInsideLeash(runtime, "w1", new Vector2(1f, 0f)))
            {
                sb.AppendLine("PolicyLeash: (1,0) should be inside r=3.");
            }

            var clamped = TacticalFormationCombatGoalPolicy.ClampAttackSlot(
                runtime,
                "w1",
                new Vector2(10f, 0f));
            if (Mathf.Abs(clamped.x - 3f) > Eps || Mathf.Abs(clamped.y) > Eps)
            {
                sb.AppendLine($"PolicyClamp: expected (3,0) got {clamped}.");
            }
        }

        private static void CheckStatModifiersParseStrengthAndAll(StringBuilder sb)
        {
            var strength = TacticalFormationStatOverlay.Parse("Stat=Strength|Mul=1.15", "Form_Test");
            if (Mathf.Abs(strength.StrengthMul - 1.15f) > 0.0001f || !Mathf.Approximately(strength.MaxHpBodyLifeMul, 1f))
            {
                sb.AppendLine($"StatParse Strength: got {strength}");
            }

            var all = TacticalFormationStatOverlay.Parse("Stat=All|Mul=1.1", "Form_Test");
            if (Mathf.Abs(all.StrengthMul - 1.1f) > 0.0001f
                || Mathf.Abs(all.MoveSpeedMul - 1.1f) > 0.0001f
                || Mathf.Abs(all.MaxHpBodyLifeMul - 1.1f) > 0.0001f)
            {
                sb.AppendLine($"StatParse All: got {all}");
            }

            var stats = new StatBlock { Strength = 100f, MoveSpeed = 4f };
            strength.ApplyToBattleStats(ref stats);
            if (Mathf.Abs(stats.Strength - 115f) > 0.01f)
            {
                sb.AppendLine($"StatApply Strength: expected 115 got {stats.Strength}");
            }
        }

        private static void CheckOverlayActiveAtStart(StringBuilder sb)
        {
            var runtime = StartOverlayRuntime(
                "Form_Test",
                new[] { "w1", "w2", "w3" },
                minCount: 3,
                new CombatStatMulBuff(1f, 1.15f, 1f, 1f));
            if (!runtime.IsOverlayActive("w1") || !runtime.TryGetStatMul("w1", out var mul) || Mathf.Abs(mul.StrengthMul - 1.15f) > 0.0001f)
            {
                sb.AppendLine("OverlayStart: w1 should be active with Strength×1.15.");
            }
        }

        private static void CheckDissolveBelowMinRemovesOverlay(StringBuilder sb)
        {
            var runtime = StartOverlayRuntime(
                "Form_Test",
                new[] { "w1", "w2", "w3" },
                minCount: 3,
                new CombatStatMulBuff(1f, 1.15f, 1f, 1f));
            if (!runtime.TryNotifyMemberLost("w1", TacticalFormationMemberLostReason.CombatDead, out var result)
                || !result.SquadDissolved)
            {
                sb.AppendLine("Dissolve: expected squad dissolve at living 2 < Min 3.");
                return;
            }

            if (result.OverlayRemovedWarriorIds == null || result.OverlayRemovedWarriorIds.Length != 2)
            {
                sb.AppendLine("Dissolve: expected 2 remaining members to lose overlay.");
            }

            if (runtime.IsMember("w2") || runtime.IsOverlayActive("w2") || runtime.IsOverlayActive("w1"))
            {
                sb.AppendLine("Dissolve: remaining members should not stay in squad/overlay.");
            }

            if (TacticalFormationCombatGoalPolicy.TryResolveIdleGoal(
                    runtime,
                    "w2",
                    TacticalFormationIdleFallback.Objective,
                    default,
                    out _,
                    out _))
            {
                sb.AppendLine("Dissolve: Policy should not handle dissolved members.");
            }
        }

        private static void CheckRebelLeavesWithoutDissolvingOthers(StringBuilder sb)
        {
            var runtime = StartOverlayRuntime(
                "Form_Test",
                new[] { "w1", "w2", "w3", "w4" },
                minCount: 3,
                new CombatStatMulBuff(1f, 1.2f, 1f, 1f),
                exclusiveSkills: new[] { "Skill_Form_X" });
            if (!runtime.TryNotifyMemberLost("w1", TacticalFormationMemberLostReason.Rebel, out var result)
                || result.SquadDissolved)
            {
                sb.AppendLine("RebelLeave: living 3 >= Min 3 should not dissolve.");
                return;
            }

            if (runtime.IsOverlayActive("w1") || runtime.IsMember("w1"))
            {
                sb.AppendLine("RebelLeave: rebel should have no overlay/membership.");
            }

            if (!runtime.IsOverlayActive("w2") || runtime.GetExclusiveSkillIds("w2").Count != 1)
            {
                sb.AppendLine("RebelLeave: remaining members should keep overlay + exclusive skills.");
            }
        }

        private static void CheckExclusiveSkillOverlayReadOnly(StringBuilder sb)
        {
            var runtime = StartOverlayRuntime(
                "Form_Test",
                new[] { "w1", "w2", "w3" },
                minCount: 3,
                CombatStatMulBuff.Identity,
                exclusiveSkills: new[] { "Skill_Form_X" },
                exclusiveEffects: new[] { "SE_Form_X" });
            var merged = TacticalFormationSkillOverlay.MergeForCast(null, runtime, "w1");
            if (merged.Count != 1 || merged[0] == null || merged[0].SkillId != "Skill_Form_X")
            {
                sb.AppendLine("SkillOverlay: expected virtual Skill_Form_X@Lv1.");
            }

            if (runtime.GetExclusiveSkillEffectIds("w1").Count != 1)
            {
                sb.AppendLine("SkillOverlay: expected ExclusiveSkillEffectIds on active member.");
            }
        }

        private static void CheckStatMulCombineWithMagicBook(StringBuilder sb)
        {
            var book = new CombatStatMulBuff(1f, 1.1f, 1f, 1f);
            var locks = new[]
            {
                new TacticalFormationCombatLock(
                    "Form_Test",
                    new[] { "w1" },
                    new[] { Vector2.zero },
                    TacticalFormationMoveParams.CreateDefault(),
                    Vector2.zero,
                    0f,
                    1,
                    new CombatStatMulBuff(1f, 1.15f, 1f, 1f),
                    System.Array.Empty<string>(),
                    System.Array.Empty<string>())
            };
            var combined = TacticalFormationStatOverlay.CombineWithMemberLocks(book, locks, "w1");
            if (Mathf.Abs(combined.StrengthMul - 1.1f * 1.15f) > 0.0001f)
            {
                sb.AppendLine($"Combine: expected Strength×{1.1f * 1.15f} got {combined.StrengthMul}");
            }

            var outsider = TacticalFormationStatOverlay.CombineWithMemberLocks(book, locks, "w2");
            if (Mathf.Abs(outsider.StrengthMul - 1.1f) > 0.0001f)
            {
                sb.AppendLine("Combine: non-member should keep magic-book mul only.");
            }
        }

        private static void CheckFormationLevelLookup(StringBuilder sb)
        {
            var configs = new ConfigCsvRepository();
            if (!configs.TryLoadAll(Gravedigger2026.Core.CampaignMode.Mode2))
            {
                sb.AppendLine("FormationLevel: Mode2 load failed: " + configs.LastError);
                return;
            }

            if (!configs.TryGetTacticalFormation("Form_Wedge_01", out var identity)
                || identity == null
                || identity.FormationLevel != 1
                || identity.FormationSkillId != "Skill_Form_Wedge"
                || identity.StatModifiers != "Stat=Strength|Mul=1.15")
            {
                sb.AppendLine(
                    "FormationLevel: GrantFormationSkill identity Form_Wedge_01 should be level 1 Strength×1.15.");
            }

            ExpectComputedFormationLevel(sb, configs, 4, 1, "Stat=Strength|Mul=1.15");
            ExpectComputedFormationLevel(sb, configs, 5, 5, "Stat=Strength|Mul=1.30");
            ExpectComputedFormationLevel(sb, configs, 9, 5, "Stat=Strength|Mul=1.30");
            if (configs.TryGetTacticalFormationForComputedLevel("Form_Wedge_01", 0, out _))
            {
                sb.AppendLine("FormationLevel: computed level 0 should miss.");
            }

            var ids = configs.GetTacticalFormationIds();
            if (ids == null || ids.Count != 2
                || ids[0] != "Form_Wedge_01"
                || ids[1] != "Form_Wedge_02")
            {
                sb.AppendLine("FormationLevel: catalog ids should be Form_Wedge_01 then Form_Wedge_02.");
            }
        }

        private static void ExpectComputedFormationLevel(
            StringBuilder sb,
            ConfigCsvRepository configs,
            int computedLevel,
            int expectedLevel,
            string expectedStats)
        {
            if (!configs.TryGetTacticalFormationForComputedLevel(
                    "Form_Wedge_01",
                    computedLevel,
                    out var row)
                || row == null
                || row.FormationLevel != expectedLevel
                || row.StatModifiers != expectedStats)
            {
                var got = row == null
                    ? "none"
                    : "Lv" + row.FormationLevel + " " + row.StatModifiers;
                sb.AppendLine(
                    $"FormationLevel: computed {computedLevel} expected Lv{expectedLevel} {expectedStats}, got {got}.");
            }
        }

        private static void CheckDuplicateFormationCompositeKeyThrows(StringBuilder sb)
        {
            var index = new TacticalFormationConfigIndex();
            index.Add(new TacticalFormationConfigRow
            {
                FormationId = "Form_Dup",
                FormationLevel = 1
            });

            var threw = false;
            try
            {
                index.Add(new TacticalFormationConfigRow
                {
                    FormationId = "Form_Dup",
                    FormationLevel = 1
                });
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            if (!threw)
            {
                sb.AppendLine("FormationLevel: duplicate (FormationId, FormationLevel) should throw.");
            }

            try
            {
                index.Add(new TacticalFormationConfigRow
                {
                    FormationId = "Form_Dup",
                    FormationLevel = 5
                });
            }
            catch (InvalidOperationException)
            {
                sb.AppendLine("FormationLevel: same FormationId at a different level should be allowed.");
            }
        }

        private static void CheckManualGroupSession(StringBuilder sb)
        {
            var configs = new ConfigCsvRepository();
            if (!configs.TryLoadAll(Gravedigger2026.Core.CampaignMode.Mode2))
            {
                sb.AppendLine("TFG-02: Mode2 load failed: " + configs.LastError);
                return;
            }

            if (!configs.TryGetTacticalFormation("Form_Wedge_01", out var wedge) || wedge == null)
            {
                sb.AppendLine("TFG-02: missing Form_Wedge_01 identity row.");
                return;
            }

            wedge.MaxMemberCount = 6;

            var pool = new WarriorPoolService();
            var formation = new BattleFormationService(pool);
            var layout = new TacticalFormationLayoutService();
            var patterns = new StubSlotPatterns(8);
            var zones = new List<FormationClassZoneSnapshot>
            {
                new FormationClassZoneSnapshot("Class_Warrior", 0f, 0f, 40f, 40f)
            };
            var context = TacticalFormationLayoutContext.DefaultPlusZ(zones);

            for (var i = 0; i < 12; i++)
            {
                var id = "W" + i.ToString("D2");
                var warrior = new WarriorInstance
                {
                    Id = id,
                    ClassId = "Class_Warrior",
                    RemainingHP = 10f
                };
                warrior.SoldierSkills.Add(new SoldierSkillEntry
                {
                    SkillId = "Skill_Form_Wedge",
                    SkillLevel = 1
                });
                if (i == 0)
                {
                    warrior.SoldierSkills.Add(new SoldierSkillEntry
                    {
                        SkillId = "Skill_Form_Wedge_02",
                        SkillLevel = 1
                    });
                }

                pool.Add(warrior);
                if (!formation.TryDeployAt(id, i * 3f, 0f, out var error))
                {
                    sb.AppendLine("TFG-02: deploy " + id + " failed: " + error);
                    return;
                }
            }

            if (!layout.TryCreateGroup("Form_Wedge_01", formation, pool, configs, patterns, context))
            {
                sb.AppendLine("TFG-02: first create should succeed.");
                return;
            }

            if (!layout.TryCreateGroup("Form_Wedge_01", formation, pool, configs, patterns, context))
            {
                sb.AppendLine("TFG-02: second create should succeed.");
                return;
            }

            var squads = new List<TacticalFormationSquadSnapshot>(4);
            layout.CollectActiveSquads(squads);
            if (squads.Count != 2
                || squads[0].MemberIds.Length != 6
                || squads[1].MemberIds.Length != 6)
            {
                sb.AppendLine("TFG-02: expected two groups of 6.");
                return;
            }

            if (SharesMember(squads[0], squads[1]))
            {
                sb.AppendLine("TFG-02: grouped soldiers joined the next group of the same formation.");
            }

            if (squads[0].ComputedLevel != 2
                || squads[0].MatchedLevelRow == null
                || squads[0].MatchedLevelRow.FormationLevel != 1)
            {
                sb.AppendLine(
                    "TFG-02: six Class_Warrior (level 2) should match FormationLevel 1, got level "
                    + squads[0].ComputedLevel + ".");
            }

            var beforeThird = SnapshotPositions(formation);
            if (layout.TryCreateGroup("Form_Wedge_01", formation, pool, configs, patterns, context))
            {
                sb.AppendLine("TFG-02: third create should fail (below Min).");
            }

            if (!PositionsEqual(formation, beforeThird))
            {
                sb.AppendLine("TFG-02: failed create moved coordinates.");
            }

            for (var i = 0; i < 3; i++)
            {
                var id = "D" + i.ToString("D2");
                var warrior = new WarriorInstance
                {
                    Id = id,
                    ClassId = string.Empty,
                    RemainingHP = 10f
                };
                warrior.SoldierSkills.Add(new SoldierSkillEntry
                {
                    SkillId = "Skill_Form_Wedge",
                    SkillLevel = 1
                });
                warrior.SoldierSkills.Add(new SoldierSkillEntry
                {
                    SkillId = "Skill_Form_Wedge_02",
                    SkillLevel = 1
                });
                pool.Add(warrior);
                if (!formation.TryDeployAt(id, 100f + i, 0f, out var error))
                {
                    sb.AppendLine("TFG-02: deploy dual-skill " + id + " failed: " + error);
                    return;
                }
            }

            if (!layout.TryCreateGroup("Form_Wedge_02", formation, pool, configs, patterns, context))
            {
                sb.AppendLine("TFG-02: ungrouped dual-skill soldiers should form the second formation.");
                return;
            }

            layout.CollectActiveSquads(squads);
            TacticalFormationSquadSnapshot parallel = null;
            for (var i = 0; i < squads.Count; i++)
            {
                if (squads[i].FormationId == "Form_Wedge_02")
                {
                    parallel = squads[i];
                }
            }

            if (parallel == null || parallel.MemberIds.Length != 3 || parallel.Contains("W00"))
            {
                sb.AppendLine("TFG-02: second formation should be the three ungrouped dual-skill soldiers.");
            }
            else if (parallel.ComputedLevel != 0 || parallel.MatchedLevelRow != null)
            {
                sb.AppendLine("TFG-02: missing class rows should keep the group with empty stats.");
            }

            var restored = new TacticalFormationLayoutService();
            restored.Restore(formation, pool, configs);
            var restoredSquads = new List<TacticalFormationSquadSnapshot>(4);
            restored.CollectActiveSquads(restoredSquads);
            if (restoredSquads.Count != squads.Count)
            {
                sb.AppendLine(
                    "TFG-02: restore lost groups. had " + squads.Count + " got " + restoredSquads.Count + ".");
            }

            var legacy = JsonUtility.FromJson<BattleFormationSaveData>("{\"Entries\":[]}");
            if (legacy == null || legacy.Groups != null)
            {
                sb.AppendLine("TFG-02: old JSON without Groups should deserialize Groups as null.");
            }
            else
            {
                var loaded = new BattleFormationService(pool);
                loaded.ImportSaveData(legacy);
                if (loaded.Groups.Count != 0)
                {
                    sb.AppendLine("TFG-02: old JSON should load as no groups.");
                }
            }

            var roundDto = CopyFormationSave(formation);
            var roundJson = JsonUtility.ToJson(roundDto);
            var roundBack = JsonUtility.FromJson<BattleFormationSaveData>(roundJson);
            var roundService = new BattleFormationService(pool);
            roundService.ImportSaveData(roundBack);
            var roundLayout = new TacticalFormationLayoutService();
            roundLayout.Restore(roundService, pool, configs);
            var roundSquads = new List<TacticalFormationSquadSnapshot>(4);
            roundLayout.CollectActiveSquads(roundSquads);
            if (roundSquads.Count != squads.Count)
            {
                sb.AppendLine("TFG-02: JSON roundtrip restore lost groups.");
            }

            var keep = squads[1];
            var keepPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            for (var i = 0; i < keep.MemberIds.Length; i++)
            {
                if (formation.TryGetEntry(keep.MemberIds[i], out var entry) && entry != null)
                {
                    keepPositions[keep.MemberIds[i]] = new Vector2(entry.PositionX, entry.PositionZ);
                }
            }

            var disbandIds = squads[0].MemberIds;
            var survivorA = disbandIds[4];
            var survivorB = disbandIds[5];
            float survivorAx = 0f;
            float survivorAz = 0f;
            float survivorBx = 0f;
            float survivorBz = 0f;
            if (formation.TryGetEntry(survivorA, out var sa) && sa != null)
            {
                survivorAx = sa.PositionX;
                survivorAz = sa.PositionZ;
            }

            if (formation.TryGetEntry(survivorB, out var sbEntry) && sbEntry != null)
            {
                survivorBx = sbEntry.PositionX;
                survivorBz = sbEntry.PositionZ;
            }

            for (var i = 0; i < 4; i++)
            {
                formation.TryUndeploy(disbandIds[i], out _);
            }

            layout.PruneGroups(formation, pool, configs, context);
            layout.CollectActiveSquads(squads);
            var stillHasFirst = false;
            TacticalFormationSquadSnapshot kept = null;
            for (var i = 0; i < squads.Count; i++)
            {
                if (squads[i].GroupInstanceId == keep.GroupInstanceId)
                {
                    kept = squads[i];
                }

                if (squads[i].Contains(survivorA) || squads[i].Contains(survivorB))
                {
                    stillHasFirst = true;
                }
            }

            if (stillHasFirst || kept == null || kept.MemberIds.Length != 6)
            {
                sb.AppendLine("TFG-02: pruning below Min should disband only that group.");
            }

            for (var i = 0; i < keep.MemberIds.Length; i++)
            {
                var id = keep.MemberIds[i];
                if (!formation.TryGetEntry(id, out var entry)
                    || entry == null
                    || !keepPositions.TryGetValue(id, out var pos)
                    || (entry.PositionX - pos.x) * (entry.PositionX - pos.x)
                    + (entry.PositionZ - pos.y) * (entry.PositionZ - pos.y) > 0.0001f)
                {
                    sb.AppendLine("TFG-02: the other group moved during disband.");
                    break;
                }
            }

            if (!formation.TryGetEntry(survivorA, out var movedA)
                || movedA == null
                || !formation.TryGetEntry(survivorB, out var movedB)
                || movedB == null
                || (Mathf.Abs(movedA.PositionX - survivorAx) < 0.01f
                    && Mathf.Abs(movedA.PositionZ - survivorAz) < 0.01f
                    && Mathf.Abs(movedB.PositionX - survivorBx) < 0.01f
                    && Mathf.Abs(movedB.PositionZ - survivorBz) < 0.01f))
            {
                sb.AppendLine("TFG-02: disbanded members should return to the class zone.");
            }

            var idlePool = new WarriorPoolService();
            var idleFormation = new BattleFormationService(idlePool);
            for (var i = 0; i < 6; i++)
            {
                var id = "I" + i.ToString("D2");
                var warrior = new WarriorInstance
                {
                    Id = id,
                    ClassId = "Class_Warrior",
                    RemainingHP = 10f
                };
                warrior.SoldierSkills.Add(new SoldierSkillEntry
                {
                    SkillId = "Skill_Form_Wedge",
                    SkillLevel = 1
                });
                idlePool.Add(warrior);
                idleFormation.TryDeployAt(id, i, 1f, out _);
            }

            if (idleFormation.Groups.Count != 0)
            {
                sb.AppendLine("TFG-02: deploy without TryCreateGroup should not create groups.");
            }
        }

        private static void CheckSearchExtractLayoutKeepsGroupOffset(StringBuilder sb)
        {
            var runtime = new TacticalFormationRuntimeService();
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_A",
                        new[] { "a" },
                        new[] { new Vector2(1f, 0f) },
                        TacticalFormationMoveParams.CreateDefault(),
                        new Vector2(0f, 0f),
                        0f,
                        1,
                        CombatStatMulBuff.Identity,
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        "group-a"),
                    new TacticalFormationCombatLock(
                        "Form_B",
                        new[] { "b" },
                        new[] { new Vector2(0f, 2f) },
                        TacticalFormationMoveParams.CreateDefault(),
                        new Vector2(10f, 0f),
                        0f,
                        1,
                        CombatStatMulBuff.Identity,
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        "group-b")
                },
                TacticalFormationCenterMode.Hold,
                3.5f);

            var mapCenter = new Vector2(100f, 50f);
            var anchor = new Vector2(100f, 5f);
            var objective = new Vector2(200f, 80f);
            runtime.PlaceCentersPreservingLayout(objective, anchor, mapCenter);
            runtime.PlaceCentersPreservingLayout(objective, anchor, mapCenter);

            if (!runtime.TryGetSlotWorldXZ("a", out var slotA)
                || (slotA - new Vector2(201f, 125f)).sqrMagnitude > 0.0001f)
            {
                sb.AppendLine("SE layout: group A slot should keep its offset from the army centroid. got " + slotA);
            }

            if (!runtime.TryGetSlotWorldXZ("b", out var slotB)
                || (slotB - new Vector2(210f, 127f)).sqrMagnitude > 0.0001f)
            {
                sb.AppendLine("SE layout: group B should not stack on group A. got " + slotB);
            }
        }

        private static void CheckCatalogFillsFromSoldierBar(StringBuilder sb)
        {
            var configs = new ConfigCsvRepository();
            if (!configs.TryLoadAll(Gravedigger2026.Core.CampaignMode.Mode2))
            {
                sb.AppendLine("TFG-bar: Mode2 load failed: " + configs.LastError);
                return;
            }

            var patterns = new StubSlotPatterns(8);
            var zones = new List<FormationClassZoneSnapshot>
            {
                new FormationClassZoneSnapshot("Class_Warrior", 0f, 0f, 40f, 40f)
            };
            var context = TacticalFormationLayoutContext.DefaultPlusZ(zones);

            var pool = new WarriorPoolService();
            var formation = new BattleFormationService(pool);
            var layout = new TacticalFormationLayoutService();
            AddFormationWarrior(pool, "D00", true);
            AddFormationWarrior(pool, "Skip", false);
            AddFormationWarrior(pool, "B00", true);
            AddFormationWarrior(pool, "B01", true);
            if (!formation.TryDeployAt("D00", 10f, 4f, out var deployError))
            {
                sb.AppendLine("TFG-bar: deploy D00 failed: " + deployError);
                return;
            }

            if (!layout.TryCreateGroup("Form_Wedge_01", formation, pool, configs, patterns, context))
            {
                sb.AppendLine("TFG-bar: one deployed plus two bar soldiers should create a group.");
                return;
            }

            var squads = new List<TacticalFormationSquadSnapshot>(2);
            layout.CollectActiveSquads(squads);
            if (squads.Count != 1 || squads[0].MemberIds.Length != 3)
            {
                sb.AppendLine("TFG-bar: expected one group of 3.");
                return;
            }

            var members = squads[0].MemberIds;
            if (members[0] != "D00" || members[1] != "B00" || members[2] != "B01")
            {
                sb.AppendLine("TFG-bar: slot order should be deployed then pool order.");
            }

            if (formation.IsDeployed("Skip"))
            {
                sb.AppendLine("TFG-bar: a soldier without the formation skill was deployed.");
            }

            if (!formation.TryGetEntry("D00", out var anchor) || anchor == null
                || Mathf.Abs(anchor.PositionX - 10f) > 0.001f
                || Mathf.Abs(anchor.PositionZ - 4f) > 0.001f)
            {
                sb.AppendLine("TFG-bar: center should stay on the deployed member.");
            }

            if (!formation.TryGetEntry("B00", out var filled) || filled == null
                || Mathf.Abs(filled.PositionX - 10.5f) > 0.001f
                || Mathf.Abs(filled.PositionZ - 4f) > 0.001f)
            {
                sb.AppendLine("TFG-bar: first bar soldier should occupy slot 1 around the deployed centroid.");
            }

            var barOnlyPool = new WarriorPoolService();
            var barOnlyFormation = new BattleFormationService(barOnlyPool);
            var barOnlyLayout = new TacticalFormationLayoutService();
            AddFormationWarrior(barOnlyPool, "U00", true);
            AddFormationWarrior(barOnlyPool, "U01", true);
            AddFormationWarrior(barOnlyPool, "U02", true);
            var fallback = context.WithFallbackCenter(7f, 8f);
            if (!barOnlyLayout.TryCreateGroup("Form_Wedge_01", barOnlyFormation, barOnlyPool, configs, patterns, fallback))
            {
                sb.AppendLine("TFG-bar: three undeployed soldiers should create a group at the fallback center.");
                return;
            }

            if (!barOnlyFormation.TryGetEntry("U00", out var u0) || u0 == null
                || Mathf.Abs(u0.PositionX - 7f) > 0.001f
                || Mathf.Abs(u0.PositionZ - 8f) > 0.001f)
            {
                sb.AppendLine("TFG-bar: bar-only group center should be the fallback point.");
            }

            var shortPool = new WarriorPoolService();
            var shortFormation = new BattleFormationService(shortPool);
            var shortLayout = new TacticalFormationLayoutService();
            AddFormationWarrior(shortPool, "S00", true);
            AddFormationWarrior(shortPool, "S01", true);
            if (shortLayout.TryCreateGroup(
                    "Form_Wedge_01",
                    shortFormation,
                    shortPool,
                    configs,
                    patterns,
                    fallback))
            {
                sb.AppendLine("TFG-bar: two undeployed soldiers are below Min and must not create a group.");
            }

            if (shortFormation.Entries.Count != 0)
            {
                sb.AppendLine("TFG-bar: a failed bar-only create deployed soldiers.");
            }
        }

        private static void AddFormationWarrior(WarriorPoolService pool, string id, bool withFormationSkill)
        {
            var warrior = new WarriorInstance
            {
                Id = id,
                ClassId = "Class_Warrior",
                RemainingHP = 10f
            };
            if (withFormationSkill)
            {
                warrior.SoldierSkills.Add(new SoldierSkillEntry
                {
                    SkillId = "Skill_Form_Wedge",
                    SkillLevel = 1
                });
            }

            pool.Add(warrior);
        }

        private static bool SharesMember(TacticalFormationSquadSnapshot a, TacticalFormationSquadSnapshot b)
        {
            if (a?.MemberIds == null || b?.MemberIds == null)
            {
                return false;
            }

            for (var i = 0; i < a.MemberIds.Length; i++)
            {
                if (b.Contains(a.MemberIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<string, Vector2> SnapshotPositions(BattleFormationService formation)
        {
            var map = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            var entries = formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.WarriorId))
                {
                    continue;
                }

                map[e.WarriorId] = new Vector2(e.PositionX, e.PositionZ);
            }

            return map;
        }

        private static bool PositionsEqual(BattleFormationService formation, Dictionary<string, Vector2> before)
        {
            var entries = formation.Entries;
            if (entries.Count != before.Count)
            {
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null
                    || !before.TryGetValue(e.WarriorId, out var pos)
                    || Mathf.Abs(e.PositionX - pos.x) > 0.0001f
                    || Mathf.Abs(e.PositionZ - pos.y) > 0.0001f)
                {
                    return false;
                }
            }

            return true;
        }

        private static BattleFormationSaveData CopyFormationSave(BattleFormationService formation)
        {
            var entries = formation.Entries;
            var groups = formation.Groups;
            var data = new BattleFormationSaveData
            {
                Entries = new BattleFormationSaveEntry[entries.Count],
                Groups = new TacticalFormationGroupSaveEntry[groups.Count]
            };
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                data.Entries[i] = new BattleFormationSaveEntry
                {
                    WarriorId = e.WarriorId,
                    PositionX = e.PositionX,
                    PositionZ = e.PositionZ,
                    RemainingHP = e.RemainingHP
                };
            }

            for (var i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var members = g.MemberIds ?? Array.Empty<string>();
                var copy = new string[members.Length];
                Array.Copy(members, copy, members.Length);
                data.Groups[i] = new TacticalFormationGroupSaveEntry
                {
                    GroupInstanceId = g.GroupInstanceId,
                    FormationId = g.FormationId,
                    MemberIds = copy,
                    FacingYawDegrees = g.FacingYawDegrees
                };
            }

            return data;
        }

        private sealed class StubSlotPatterns : ITacticalFormationPatternLookup
        {
            private readonly Vector3[] _slots;

            public StubSlotPatterns(int count)
            {
                _slots = new Vector3[count];
                for (var i = 0; i < count; i++)
                {
                    _slots[i] = new Vector3(i * 0.5f, 0f, 0f);
                }
            }

            public bool TryGetSlotLocalXZ(string prefabId, out Vector3[] slotLocalXZ)
            {
                slotLocalXZ = _slots;
                return _slots.Length > 0;
            }

            public bool TryGetMoveParams(string prefabId, out TacticalFormationMoveParams moveParams)
            {
                moveParams = TacticalFormationMoveParams.CreateDefault();
                return true;
            }
        }

        private static void CheckTwoGroupsSameFormationIndependentLevel(StringBuilder sb)
        {
            var configs = new ConfigCsvRepository();
            if (!configs.TryLoadAll(Gravedigger2026.Core.CampaignMode.Mode2))
            {
                sb.AppendLine("TFG-05: Mode2 load failed: " + configs.LastError);
                return;
            }

            var high = TacticalFormationStatOverlay.Parse("Stat=Strength|Mul=1.30", "Form_Wedge_01");
            var local = new Vector2(0f, 1f);
            var runtime = new TacticalFormationRuntimeService();
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        "Form_Wedge_01",
                        new[] { "a1", "a2", "a3", "a4" },
                        new[] { local, local, local, local },
                        TacticalFormationMoveParams.CreateDefault(),
                        Vector2.zero,
                        0f,
                        3,
                        high,
                        new[] { "Skill_A" },
                        Array.Empty<string>(),
                        "group-a",
                        new[] { 6, 6, 6, 2 }),
                    new TacticalFormationCombatLock(
                        "Form_Wedge_01",
                        new[] { "b1", "b2", "b3" },
                        new[] { local, local, local },
                        TacticalFormationMoveParams.CreateDefault(),
                        new Vector2(20f, 0f),
                        0f,
                        3,
                        high,
                        new[] { "Skill_B" },
                        Array.Empty<string>(),
                        "group-b",
                        new[] { 6, 6, 6 })
                },
                TacticalFormationCenterMode.Hold,
                0f);

            if (runtime.SquadCount != 2)
            {
                sb.AppendLine("TFG-05: same FormationId should keep two groups, got " + runtime.SquadCount);
                return;
            }

            var gotA = runtime.TryGetSlotWorldXZ("a1", out var slotA);
            var gotB = runtime.TryGetSlotWorldXZ("b1", out var slotB);
            if (!gotA || !gotB || (slotA - slotB).sqrMagnitude < 100f)
            {
                sb.AppendLine($"TFG-05: members should keep their own group slots, a={slotA} b={slotB}.");
            }

            if (!runtime.TryNotifyMemberLost(
                    "a1",
                    TacticalFormationMemberLostReason.CombatDead,
                    out var drop,
                    configs)
                || drop.SquadDissolved)
            {
                sb.AppendLine("TFG-05: living 3 >= Min 3 should swap overlay, not dissolve.");
                return;
            }

            if (!runtime.TryGetStatMul("a2", out var dropped) || Mathf.Abs(dropped.StrengthMul - 1.15f) > 0.0001f)
            {
                sb.AppendLine("TFG-05: group A average level 4 should drop to Strength×1.15.");
            }

            if (runtime.GetExclusiveSkillIds("a2").Count != 0)
            {
                sb.AppendLine("TFG-05: group A should take the level-1 row's empty exclusive skills.");
            }

            if (!ContainsId(drop.OverlayRefreshedWarriorIds, "a2")
                || ContainsId(drop.OverlayRefreshedWarriorIds, "b1"))
            {
                sb.AppendLine("TFG-05: only group A living members should refresh.");
            }

            if (runtime.IsMember("a1") || runtime.IsOverlayActive("a1"))
            {
                sb.AppendLine("TFG-05: dead member should leave group A.");
            }

            if (!runtime.TryGetStatMul("b1", out var stayed)
                || Mathf.Abs(stayed.StrengthMul - 1.30f) > 0.0001f
                || runtime.GetExclusiveSkillIds("b1").Count != 1)
            {
                sb.AppendLine("TFG-05: group B should keep Strength×1.30 and its exclusive skill.");
            }

            if (!runtime.TryNotifyMemberLost(
                    "a2",
                    TacticalFormationMemberLostReason.CombatDead,
                    out var dissolved,
                    configs)
                || !dissolved.SquadDissolved)
            {
                sb.AppendLine("TFG-05: living 2 < Min 3 should dissolve only group A.");
                return;
            }

            if (runtime.IsMember("a3") || runtime.IsOverlayActive("a3") || runtime.SquadCount != 1)
            {
                sb.AppendLine("TFG-05: group A should be gone; other groups stay.");
            }

            if (!runtime.IsMember("b1")
                || !runtime.TryGetStatMul("b1", out var still)
                || Mathf.Abs(still.StrengthMul - 1.30f) > 0.0001f)
            {
                sb.AppendLine("TFG-05: dissolving group A must not change group B.");
            }
        }

        private static bool ContainsId(string[] ids, string warriorId)
        {
            if (ids == null || string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], warriorId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static TacticalFormationRuntimeService StartOverlayRuntime(
            string formationId,
            string[] members,
            int minCount,
            CombatStatMulBuff statMul,
            string[] exclusiveSkills = null,
            string[] exclusiveEffects = null)
        {
            var locals = new Vector2[members.Length];
            var runtime = new TacticalFormationRuntimeService();
            runtime.OnStartBattle(
                new[]
                {
                    new TacticalFormationCombatLock(
                        formationId,
                        members,
                        locals,
                        TacticalFormationMoveParams.CreateDefault(),
                        Vector2.zero,
                        0f,
                        minCount,
                        statMul,
                        exclusiveSkills ?? System.Array.Empty<string>(),
                        exclusiveEffects ?? System.Array.Empty<string>())
                },
                TacticalFormationCenterMode.Hold,
                0f);
            return runtime;
        }
    }
}
