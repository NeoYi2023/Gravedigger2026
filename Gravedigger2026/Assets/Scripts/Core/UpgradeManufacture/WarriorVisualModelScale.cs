using System;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Scale-channel helper for Style_ScaleModel (SPEC_03 §3.15 6b).
    /// </summary>
    public static class WarriorVisualModelScale
    {
        public const string StyleId = "Style_ScaleModel";
        public const string StyleIdAlias = "放大模型";

        public static bool IsScaleStyle(string styleId)
        {
            if (string.IsNullOrWhiteSpace(styleId))
            {
                return false;
            }

            styleId = styleId.Trim();
            return string.Equals(styleId, StyleId, StringComparison.Ordinal)
                || string.Equals(styleId, StyleIdAlias, StringComparison.Ordinal);
        }

        public static float Resolve(WarriorInstance warrior)
        {
            if (warrior == null || warrior.VisualModelScale <= 0f)
            {
                return 1f;
            }

            return warrior.VisualModelScale;
        }

        public static float ClampFactor(float add)
        {
            return add <= 0f ? 1f : add;
        }
    }
}
