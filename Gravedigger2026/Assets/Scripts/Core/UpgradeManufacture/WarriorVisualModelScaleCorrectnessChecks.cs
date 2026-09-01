using System.Text;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Scene-free checks for D-082 hit body-scale (SPEC_03 §3.15 6b). configs=null → Safety 1.15 / 3.
    /// </summary>
    public static class WarriorVisualModelScaleCorrectnessChecks
    {
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckHitStepOnly(sb);
            CheckHitPlusScaleModel(sb);
            CheckClampMax(sb);
            CheckMaterialDoesNotSkipStep(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static WarriorInstance NewWarrior()
        {
            return new WarriorInstance { Id = "W_test", VisualModelScale = 1f };
        }

        private static void CheckHitStepOnly(StringBuilder sb)
        {
            var w = NewWarrior();
            var row = new MagicBookConfigRow
            {
                VisualStyleId = string.Empty,
                VisualIntensityAdd = 1f
            };
            WarriorVisualStyleBake.TryApply(w, row, configs: null);
            const float expect = 1.15f;
            if (Mathf.Abs(WarriorVisualModelScale.Resolve(w) - expect) > 1e-4f)
            {
                sb.AppendLine($"HitStepOnly: scale={w.VisualModelScale:F4} expect {expect:F4}");
            }
        }

        private static void CheckHitPlusScaleModel(StringBuilder sb)
        {
            var w = NewWarrior();
            var row = new MagicBookConfigRow
            {
                VisualStyleId = WarriorVisualModelScale.StyleId,
                VisualIntensityAdd = 1.5f
            };
            WarriorVisualStyleBake.TryApply(w, row, configs: null);
            const float expect = 1.15f * 1.5f;
            if (Mathf.Abs(WarriorVisualModelScale.Resolve(w) - expect) > 1e-4f)
            {
                sb.AppendLine($"HitPlusScaleModel: scale={w.VisualModelScale:F4} expect {expect:F4}");
            }
        }

        private static void CheckClampMax(StringBuilder sb)
        {
            var w = NewWarrior();
            w.VisualModelScale = 2.9f;
            var row = new MagicBookConfigRow
            {
                VisualStyleId = WarriorVisualModelScale.StyleId,
                VisualIntensityAdd = 2f
            };
            WarriorVisualStyleBake.TryApply(w, row, configs: null);
            // 2.9 * 1.15 * 2 = 6.67 → clamp 3
            if (Mathf.Abs(WarriorVisualModelScale.Resolve(w) - 3f) > 1e-4f)
            {
                sb.AppendLine($"ClampMax: scale={w.VisualModelScale:F4} expect 3");
            }
        }

        private static void CheckMaterialDoesNotSkipStep(StringBuilder sb)
        {
            var w = NewWarrior();
            var row = new MagicBookConfigRow
            {
                VisualStyleId = "Style_WarriorGlow",
                VisualPriority = 20,
                VisualIntensityAdd = 1f
            };
            WarriorVisualStyleBake.TryApply(w, row, configs: null);
            if (Mathf.Abs(WarriorVisualModelScale.Resolve(w) - 1.15f) > 1e-4f)
            {
                sb.AppendLine($"MaterialHitStep: scale={w.VisualModelScale:F4} expect 1.15");
            }

            if (w.VisualStyleId != "Style_WarriorGlow" || w.VisualPriority != 20)
            {
                sb.AppendLine(
                    $"MaterialHitStep: style={w.VisualStyleId}/{w.VisualPriority} expect Style_WarriorGlow/20");
            }
        }
    }
}
