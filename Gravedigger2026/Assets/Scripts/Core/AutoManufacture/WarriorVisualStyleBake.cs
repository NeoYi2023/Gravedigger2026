using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// On MagicBook token hit: body-scale step, then material or Style_ScaleModel, then Max clamp
    /// (SPEC_03 §3.15 6b / D-082). Call only after a real EffectPayload hit.
    /// </summary>
    public static class WarriorVisualStyleBake
    {
        public static void TryApply(
            WarriorInstance warrior,
            MagicBookConfigRow row,
            ConfigCsvRepository configs)
        {
            if (warrior == null || row == null)
            {
                return;
            }

            WarriorVisualModelScale.ApplyHitScaleStep(warrior, configs);

            var styleId = row.VisualStyleId;
            if (!string.IsNullOrWhiteSpace(styleId))
            {
                styleId = styleId.Trim();
                var add = WarriorVisualModelScale.ClampFactor(row.VisualIntensityAdd);

                if (WarriorVisualModelScale.IsScaleStyle(styleId))
                {
                    warrior.VisualModelScale = WarriorVisualModelScale.Resolve(warrior) * add;
                }
                else
                {
                    var priority = row.VisualPriority;
                    if (string.IsNullOrEmpty(warrior.VisualStyleId) || priority > warrior.VisualPriority)
                    {
                        warrior.VisualStyleId = styleId;
                        warrior.VisualPriority = priority;
                        warrior.VisualIntensity = add;
                    }
                    else if (string.Equals(warrior.VisualStyleId, styleId, StringComparison.Ordinal))
                    {
                        warrior.VisualIntensity += add;
                    }
                }
            }

            WarriorVisualModelScale.ClampToMax(warrior, configs);
        }
    }
}
