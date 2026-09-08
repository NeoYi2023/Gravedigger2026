using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.SearchExtract
{
    /// <summary>
    /// Scene-free HoldFraming box checks (SE-CAM-01 / SE-CAM-03 / SPEC_03 §3.19). No Play Mode.
    /// </summary>
    public static class SearchExtractHoldFramingCorrectnessChecks
    {
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckExpandGrowsSize(sb);
            CheckTightenWaitsForDwell(sb);
            CheckOutOfRadiusExcluded(sb);
            CheckLookAtClampedToRadius(sb);
            CheckTypicalClusterFitsWithinMax(sb);
            CheckTightClusterStaysAtMin(sb);
            CheckRadiusEdgeClampsToMax(sb);
            CheckFarChaseDoesNotPullLookAt(sb);
            CheckSampleIntervalHoldsTarget(sb);
            CheckLockedSampleRange(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        public static string RunTableKeys(ConfigCsvRepository configs)
        {
            if (configs == null)
            {
                return "TableKeys: configs is null.";
            }

            var sb = new StringBuilder();
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldOrthoSizeMin, 3f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldOrthoSizeMax, 8f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldMaxPanRadius, 8f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldViewportPad, 0.08f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldTopHudPad, 0.12f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldInnerPad, 0.18f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldZoomInDelaySeconds, 0.8f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldSmoothTimeOut, 0.2f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldSmoothTimeIn, 0.55f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldSampleInterval, 0.2f);
            ExpectKey(sb, configs, CombatConstantKeys.SearchExtractHoldObjectiveBias, 0.65f);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void ExpectKey(
            StringBuilder sb,
            ConfigCsvRepository configs,
            string key,
            float expected)
        {
            var value = configs.GetCombatConstantOrFallback(key, float.NaN);
            if (float.IsNaN(value) || Mathf.Abs(value - expected) > 1e-4f)
            {
                sb.AppendLine($"TableKeys: {key}={value} expect {expected}");
            }
        }

        private static CameraPresentationConstants Presentation()
        {
            return CameraPresentationConstants.SafetyDefaults;
        }

        private static SearchExtractHoldFramingSolver.CameraBasis Basis(Vector3 lookAt)
        {
            return SearchExtractHoldFramingSolver.CameraBasis.CreateTopDown(lookAt, 18f, 16f / 9f);
        }

        private static SearchExtractHoldFramingSolver.CameraBasis CombatBasis(Vector3 lookAt)
        {
            var presentation = Presentation();
            var rotation = presentation.ResolveCombatCameraRotation();
            return new SearchExtractHoldFramingSolver.CameraBasis(
                rotation * Vector3.right,
                rotation * Vector3.up,
                rotation * Vector3.forward,
                presentation.ResolveCombatCameraPosition(lookAt),
                16f / 9f);
        }

        private static void CheckExpandGrowsSize(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3> { new Vector3(8f, 0f, 0f) };
            var result = solver.Evaluate(
                Basis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                3f,
                presentation,
                0f);

            if (!result.WantsExpand || result.OrthoSize <= 3f + 1e-3f)
            {
                sb.AppendLine(
                    $"Expand: expected Size↑ from 3, got size={result.OrthoSize:F3} expand={result.WantsExpand}.");
            }
        }

        private static void CheckTightenWaitsForDwell(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3> { Vector3.zero };
            const float startSize = 8f;

            var early = solver.Evaluate(
                Basis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                startSize,
                presentation,
                0f);
            if (early.AllowsTighten || SizeDropped(early.OrthoSize, startSize))
            {
                sb.AppendLine(
                    $"Tighten/early: Size should stay {startSize}, got {early.OrthoSize:F3} tighten={early.AllowsTighten}.");
            }

            HoldFramingTick(solver, Basis(lookAt), lookAt, soldiers, startSize, presentation, 0.2f, 2);
            var mid = solver.Evaluate(
                Basis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                startSize,
                presentation,
                0.2f);
            if (mid.AllowsTighten || SizeDropped(mid.OrthoSize, startSize))
            {
                sb.AppendLine(
                    $"Tighten/mid (0.6s): Size should stay {startSize}, got {mid.OrthoSize:F3} tighten={mid.AllowsTighten}.");
            }

            var late = solver.Evaluate(
                Basis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                startSize,
                presentation,
                0.2f);
            if (!late.AllowsTighten || late.OrthoSize >= startSize - 1e-3f)
            {
                sb.AppendLine(
                    $"Tighten/late (0.8s): expected Size↓, got size={late.OrthoSize:F3} tighten={late.AllowsTighten}.");
            }
        }

        private static void HoldFramingTick(
            SearchExtractHoldFramingSolver solver,
            SearchExtractHoldFramingSolver.CameraBasis basis,
            Vector3 lookAt,
            List<Vector3> soldiers,
            float currentSize,
            in CameraPresentationConstants presentation,
            float dt,
            int times)
        {
            for (var i = 0; i < times; i++)
            {
                solver.Evaluate(
                    basis,
                    Vector3.zero,
                    soldiers,
                    lookAt,
                    currentSize,
                    presentation,
                    dt);
            }
        }

        private static bool SizeDropped(float size, float start)
        {
            return size < start - 1e-3f;
        }

        private static void CheckOutOfRadiusExcluded(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(50f, 0f, 0f),
            };
            var result = solver.Evaluate(
                Basis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                3f,
                presentation,
                0f);

            if (result.WantsExpand || result.OrthoSize > 3f + 1e-3f)
            {
                sb.AppendLine(
                    $"OutOfRadius: far soldier must not expand, size={result.OrthoSize:F3} expand={result.WantsExpand}.");
            }
        }

        private static void CheckLookAtClampedToRadius(StringBuilder sb)
        {
            var hold = SearchExtractHoldFramingConstants.SafetyDefaults;
            var clamped = hold.ClampLookAt(new Vector3(20f, 1f, 0f), Vector3.zero);
            var dist = new Vector2(clamped.x, clamped.z).magnitude;
            if (dist > hold.MaxPanRadius + 1e-3f)
            {
                sb.AppendLine($"ClampLookAt: dist={dist:F3} > radius {hold.MaxPanRadius}.");
            }

            if (Mathf.Abs(dist - hold.MaxPanRadius) > 0.05f)
            {
                sb.AppendLine($"ClampLookAt: expected on radius, dist={dist:F3} pos={clamped}.");
            }
        }

        private static void CheckTypicalClusterFitsWithinMax(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3>
            {
                new Vector3(2.5f, 0f, 2.5f),
                new Vector3(2.5f, 0f, -2.5f),
                new Vector3(-2.5f, 0f, 2.5f),
                new Vector3(-2.5f, 0f, -2.5f),
            };
            var tight = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                3f,
                presentation,
                0f);
            if (tight.OrthoSize > presentation.HoldFraming.OrthoSizeMax + 1e-3f)
            {
                sb.AppendLine(
                    $"TypicalCluster: Size {tight.OrthoSize:F3} exceeds Max {presentation.HoldFraming.OrthoSizeMax}.");
            }

            var framed = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                presentation.HoldFraming.OrthoSizeMax,
                presentation,
                0.2f);
            if (framed.WantsExpand)
            {
                sb.AppendLine("TypicalCluster: 4 soldiers at ±2.5 should fit outer frame at Size Max.");
            }
        }

        private static void CheckTightClusterStaysAtMin(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(0.3f, 0f, 0.3f),
            };
            const float startSize = 8f;
            solver.Evaluate(CombatBasis(lookAt), Vector3.zero, soldiers, lookAt, startSize, presentation, 0f);
            HoldFramingTick(solver, CombatBasis(lookAt), lookAt, soldiers, startSize, presentation, 0.2f, 4);
            var late = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                startSize,
                presentation,
                0.2f);
            if (!late.AllowsTighten)
            {
                sb.AppendLine($"TightCluster: expected tighten after dwell, tighten={late.AllowsTighten}.");
            }

            if (late.OrthoSize < presentation.HoldFraming.OrthoSizeMin - 1e-3f)
            {
                sb.AppendLine(
                    $"TightCluster: Size {late.OrthoSize:F3} dropped below Min {presentation.HoldFraming.OrthoSizeMin} (face-hug).");
            }
        }

        private static void CheckRadiusEdgeClampsToMax(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3> { new Vector3(0f, 0f, 7.5f) };
            var result = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                3f,
                presentation,
                0f);
            if (!result.WantsExpand)
            {
                sb.AppendLine("RadiusEdge: in-radius soldier at Z=7.5 should expand from Size 3.");
            }

            if (result.OrthoSize > presentation.HoldFraming.OrthoSizeMax + 1e-3f)
            {
                sb.AppendLine(
                    $"RadiusEdge: Size {result.OrthoSize:F3} exceeded Max (whole-map too small).");
            }

            var pan = new Vector2(result.LookAt.x, result.LookAt.z).magnitude;
            if (pan > presentation.HoldFraming.MaxPanRadius + 1e-3f)
            {
                sb.AppendLine($"RadiusEdge: look-at pan {pan:F3} exceeds MaxPanRadius.");
            }
        }

        private static void CheckFarChaseDoesNotPullLookAt(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var soldiers = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(20f, 0f, 0f),
            };
            var result = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                soldiers,
                lookAt,
                3f,
                presentation,
                0f);
            if (result.WantsExpand || result.OrthoSize > 3f + 1e-3f)
            {
                sb.AppendLine(
                    $"FarChase: out-of-radius chaser must not expand, size={result.OrthoSize:F3} expand={result.WantsExpand}.");
            }

            var pan = new Vector2(result.LookAt.x, result.LookAt.z).magnitude;
            if (pan > 0.5f)
            {
                sb.AppendLine($"FarChase: look-at followed chaser, pan={pan:F3}.");
            }
        }

        private static void CheckSampleIntervalHoldsTarget(StringBuilder sb)
        {
            var solver = new SearchExtractHoldFramingSolver();
            var lookAt = Vector3.zero;
            var presentation = Presentation();
            var close = new List<Vector3> { Vector3.zero };
            var first = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                close,
                lookAt,
                3f,
                presentation,
                0f);
            var spread = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(0f, 0f, 6f),
            };
            var held = solver.Evaluate(
                CombatBasis(lookAt),
                Vector3.zero,
                spread,
                lookAt,
                3f,
                presentation,
                0.05f);
            if (held.WantsExpand
                || Mathf.Abs(held.OrthoSize - first.OrthoSize) > 1e-4f
                || (held.LookAt - first.LookAt).sqrMagnitude > 1e-6f)
            {
                sb.AppendLine(
                    $"SampleHold: dt=0.05 should keep last target, size={held.OrthoSize:F3} expand={held.WantsExpand}.");
            }
        }

        private static void CheckLockedSampleRange(StringBuilder sb)
        {
            var hold = SearchExtractHoldFramingConstants.SafetyDefaults;
            if (hold.SmoothTimeIn <= hold.SmoothTimeOut + 1e-4f)
            {
                sb.AppendLine(
                    $"Smooth: In {hold.SmoothTimeIn:F2} must be > Out {hold.SmoothTimeOut:F2}.");
            }

            if (Mathf.Abs(hold.OrthoSizeMin - 3f) > 1e-4f || Mathf.Abs(hold.OrthoSizeMax - 8f) > 1e-4f)
            {
                sb.AppendLine(
                    $"SampleRange: Size [{hold.OrthoSizeMin},{hold.OrthoSizeMax}] expected [3,8].");
            }

            if (hold.MaxPanRadius < 6f - 1e-4f || hold.MaxPanRadius > 10f + 1e-4f)
            {
                sb.AppendLine(
                    $"SampleRange: MaxPanRadius {hold.MaxPanRadius} expected 6～10.");
            }
        }
    }
}
