using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free correctness checks for SC-03 wiring (SPEC_03 §3.12 / SPEC_04 §9.7 B+):
    /// MassMoveScheduler composes SoftCollisionService — registration mirrors, per-body
    /// repulsion scale follows GoalKind, corrections surface through TryGetCorrection even
    /// on zero-steer hold frames, and CombatMoveModePolicy derives Surround for melee only.
    /// Call <see cref="RunAll"/> from Editor/console or a future EditMode test.
    /// </summary>
    public static class SoftCollisionWireCorrectnessChecks
    {
        private const float Dt = 1f / 60f;
        private const float Radius = 0.1f;

        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckRegisterUnregisterSync(sb);
            CheckOverlapYieldsCorrection(sb);
            CheckEngageGoalLowersRepulsion(sb);
            CheckResolveOffZeroesCorrection(sb);
            CheckZeroSteerHoldStillSeparates(sb);
            CheckCombatMoveModePolicy(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckRegisterUnregisterSync(StringBuilder sb)
        {
            var scheduler = new MassMoveScheduler();
            scheduler.Register(1, Radius, MassMoveScheduler.DetourGroupLoyal);
            if (scheduler.SoftCollision.Count != 1)
            {
                sb.AppendLine($"RegisterSync: SoftCollision.Count {scheduler.SoftCollision.Count} != 1 after Register.");
            }

            scheduler.Register(2, Radius, MassMoveScheduler.DetourGroupMonster);
            scheduler.Unregister(1);
            if (scheduler.SoftCollision.Count != 1)
            {
                sb.AppendLine($"RegisterSync: SoftCollision.Count {scheduler.SoftCollision.Count} != 1 after Unregister.");
            }

            if (scheduler.SoftCollision.TryGetCorrection(1, out _))
            {
                sb.AppendLine("RegisterSync: unregistered id still resolves a correction.");
            }

            scheduler.Clear();
            if (scheduler.SoftCollision.Count != 0)
            {
                sb.AppendLine($"RegisterSync: SoftCollision.Count {scheduler.SoftCollision.Count} != 0 after Clear.");
            }
        }

        private static void CheckOverlapYieldsCorrection(StringBuilder sb)
        {
            var scheduler = new MassMoveScheduler();
            scheduler.Register(1, Radius);
            scheduler.Register(2, Radius);
            scheduler.SetGoal(1, GoalKind.FormationHome, new Vector2(5f, 0f));
            scheduler.SetGoal(2, GoalKind.FormationHome, new Vector2(-5f, 0f));

            TickWithPositions(scheduler, new Vector2(0f, 0f), new Vector2(0.1f, 0f));

            if (!scheduler.TryGetCorrection(1, out var cA) || !scheduler.TryGetCorrection(2, out var cB))
            {
                sb.AppendLine("Overlap: correction missing after Tick.");
                return;
            }

            if (cA.sqrMagnitude < 1e-8f || cB.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine($"Overlap: zero correction on overlapping pair ({cA.magnitude:F4}/{cB.magnitude:F4}).");
            }

            if (cA.x >= 0f || cB.x <= 0f)
            {
                sb.AppendLine($"Overlap: pushes not anti-parallel along x ({cA.x:F4}/{cB.x:F4}).");
            }
        }

        private static void CheckEngageGoalLowersRepulsion(StringBuilder sb)
        {
            var scheduler = new MassMoveScheduler();
            // Two identical overlapping pairs, far apart: one engage (AttackSlot), one not.
            scheduler.Register(1, Radius);
            scheduler.Register(2, Radius);
            scheduler.Register(3, Radius);
            scheduler.Register(4, Radius);
            scheduler.SetGoal(1, GoalKind.AttackSlot, new Vector2(0.05f, 0f));
            scheduler.SetGoal(2, GoalKind.AttackSlot, new Vector2(0.05f, 0f));
            scheduler.SetGoal(3, GoalKind.FormationHome, new Vector2(10f, 0f));
            scheduler.SetGoal(4, GoalKind.FormationHome, new Vector2(10f, 0f));

            if (!scheduler.SoftCollision.TryGetRepulsionScale(1, out var scaleEngage) ||
                Mathf.Abs(scaleEngage - MassMoveScheduler.AttackSlotRepulsionScale) > 1e-4f)
            {
                sb.AppendLine($"EngageScale: AttackSlot body scale {scaleEngage:F3} != {MassMoveScheduler.AttackSlotRepulsionScale:F2}.");
            }

            if (!scheduler.SoftCollision.TryGetRepulsionScale(3, out var scaleCalm) ||
                Mathf.Abs(scaleCalm - 1f) > 1e-4f)
            {
                sb.AppendLine($"EngageScale: FormationHome body scale {scaleCalm:F3} != 1.00.");
            }

            // Large dt keeps both pushes below the impulse cap so the ratio reflects scale.
            TickWithPositions(
                scheduler,
                new Vector2(0f, 0f), new Vector2(0.1f, 0f),
                new Vector2(10f, 0f), new Vector2(10.1f, 0f),
                dt: 1f);

            scheduler.TryGetCorrection(1, out var engage);
            scheduler.TryGetCorrection(3, out var calm);
            if (calm.sqrMagnitude < 1e-8f || engage.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("EngageScale: correction missing on one of the pairs.");
                return;
            }

            var ratio = engage.magnitude / calm.magnitude;
            var expected = MassMoveScheduler.AttackSlotRepulsionScale;
            if (Mathf.Abs(ratio - expected) > 0.05f)
            {
                sb.AppendLine($"EngageScale: correction ratio {ratio:F3} != expected {expected:F2} (±0.05).");
            }
        }

        private static void CheckResolveOffZeroesCorrection(StringBuilder sb)
        {
            var scheduler = new MassMoveScheduler();
            scheduler.Register(1, Radius);
            scheduler.Register(2, Radius);
            scheduler.SoftCollision.ResolveCollisions = false;

            TickWithPositions(scheduler, new Vector2(0f, 0f), new Vector2(0.1f, 0f));

            scheduler.TryGetCorrection(1, out var cA);
            scheduler.TryGetCorrection(2, out var cB);
            if (cA.sqrMagnitude > 1e-8f || cB.sqrMagnitude > 1e-8f)
            {
                sb.AppendLine("ResolveOff: corrections not zeroed via scheduler-owned service.");
            }
        }

        private static void CheckZeroSteerHoldStillSeparates(StringBuilder sb)
        {
            // No FlowField bound: Objective steer resolves to zero, but the soft-collision
            // correction must still surface (View early-out change relies on this).
            var scheduler = new MassMoveScheduler();
            scheduler.Register(1, Radius);
            scheduler.Register(2, Radius);
            scheduler.SetGoal(1, GoalKind.Objective);
            scheduler.SetGoal(2, GoalKind.Objective);

            TickWithPositions(scheduler, new Vector2(0f, 0f), new Vector2(0.1f, 0f));

            scheduler.TryGetSteer(1, out var steer);
            if (steer.sqrMagnitude > 1e-8f)
            {
                sb.AppendLine($"ZeroSteerHold: expected zero steer without a FlowField, got {steer}.");
            }

            scheduler.TryGetCorrection(1, out var correction);
            if (correction.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("ZeroSteerHold: correction lost on zero-steer hold frame.");
            }
        }

        private static void CheckCombatMoveModePolicy(StringBuilder sb)
        {
            ExpectMode(sb, GoalKind.AttackSlot, AttackMode.Melee, CombatMoveMode.Surround, true);
            ExpectMode(sb, GoalKind.ChaseAnchor, AttackMode.Melee, CombatMoveMode.Surround, true);
            ExpectMode(sb, GoalKind.AttackSlot, AttackMode.Ranged, CombatMoveMode.Chase, false);
            ExpectMode(sb, GoalKind.ChaseAnchor, AttackMode.Ranged, CombatMoveMode.Chase, false);
            ExpectMode(sb, GoalKind.Objective, AttackMode.Melee, CombatMoveMode.Chase, false);
            ExpectMode(sb, GoalKind.FormationHome, AttackMode.Melee, CombatMoveMode.Chase, false);

            var surround = CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, AttackMode.Melee);
            if (!surround.HasValue ||
                surround.Value.GapDir != SurroundParams.Default.GapDir ||
                Mathf.Abs(surround.Value.GapDegrees - SurroundParams.DefaultGapDegrees) > 1e-4f)
            {
                sb.AppendLine("Policy: SurroundFor(melee) did not yield SurroundParams.Default.");
            }
        }

        private static void ExpectMode(
            StringBuilder sb,
            GoalKind kind,
            AttackMode attackMode,
            CombatMoveMode expected,
            bool expectSurround)
        {
            var mode = CombatMoveModePolicy.Derive(kind, attackMode);
            if (mode != expected)
            {
                sb.AppendLine($"Policy: Derive({kind}, {attackMode}) = {mode}, expected {expected}.");
            }

            var hasSurround = CombatMoveModePolicy.SurroundOrNull(mode).HasValue;
            if (hasSurround != expectSurround)
            {
                sb.AppendLine($"Policy: SurroundOrNull({mode}).HasValue = {hasSurround}, expected {expectSurround}.");
            }
        }

        private static void TickWithPositions(
            MassMoveScheduler scheduler,
            Vector2 a,
            Vector2 b,
            Vector2? c = null,
            Vector2? d = null,
            float dt = Dt)
        {
            var samples = new List<MassMoveSample>(4)
            {
                new MassMoveSample(1, a, Radius, true),
                new MassMoveSample(2, b, Radius, true),
            };
            if (c.HasValue)
            {
                samples.Add(new MassMoveSample(3, c.Value, Radius, true));
            }

            if (d.HasValue)
            {
                samples.Add(new MassMoveSample(4, d.Value, Radius, true));
            }

            scheduler.Tick(samples, dt);
        }
    }
}
