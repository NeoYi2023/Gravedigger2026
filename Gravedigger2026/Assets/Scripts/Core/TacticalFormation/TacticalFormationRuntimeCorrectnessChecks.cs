using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
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
