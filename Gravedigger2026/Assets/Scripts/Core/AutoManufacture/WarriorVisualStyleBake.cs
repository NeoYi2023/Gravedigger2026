using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Competes MagicBook VisualStyle onto a warrior only after a token hit (SPEC_03 §3.15 6b).
    /// Scale-channel Style_ScaleModel does not compete with AllIn1 material.
    /// </summary>
    public static class WarriorVisualStyleBake
    {
        public static void TryApply(WarriorInstance warrior, MagicBookConfigRow row)
        {
            if (warrior == null || row == null)
            {
                return;
            }

            var styleId = row.VisualStyleId;
            if (string.IsNullOrWhiteSpace(styleId))
            {
                return;
            }

            styleId = styleId.Trim();
            var add = WarriorVisualModelScale.ClampFactor(row.VisualIntensityAdd);

            if (WarriorVisualModelScale.IsScaleStyle(styleId))
            {
                warrior.VisualModelScale = WarriorVisualModelScale.Resolve(warrior) * add;
                return;
            }

            var priority = row.VisualPriority;
            if (string.IsNullOrEmpty(warrior.VisualStyleId) || priority > warrior.VisualPriority)
            {
                warrior.VisualStyleId = styleId;
                warrior.VisualPriority = priority;
                warrior.VisualIntensity = add;
                return;
            }

            if (string.Equals(warrior.VisualStyleId, styleId, StringComparison.Ordinal))
            {
                warrior.VisualIntensity += add;
            }
        }
    }
}
