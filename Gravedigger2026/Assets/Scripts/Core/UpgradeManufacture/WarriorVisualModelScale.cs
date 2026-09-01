using System;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// VisualModelScale helpers: hit step, Style_ScaleModel channel, Max clamp (SPEC_03 §3.15 6b / D-082).
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

        public static float ResolvePerHit(ConfigCsvRepository configs)
        {
            var perHit = configs != null
                ? configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.WarriorVisualModelScalePerHit,
                    CombatConstantKeys.Safety.WarriorVisualModelScalePerHit)
                : CombatConstantKeys.Safety.WarriorVisualModelScalePerHit;
            return perHit <= 0f ? CombatConstantKeys.Safety.WarriorVisualModelScalePerHit : perHit;
        }

        public static float ResolveMax(ConfigCsvRepository configs)
        {
            var max = configs != null
                ? configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.WarriorVisualModelScaleMax,
                    CombatConstantKeys.Safety.WarriorVisualModelScaleMax)
                : CombatConstantKeys.Safety.WarriorVisualModelScaleMax;
            return max < 1f ? CombatConstantKeys.Safety.WarriorVisualModelScaleMax : max;
        }

        public static void ApplyHitScaleStep(WarriorInstance warrior, ConfigCsvRepository configs)
        {
            if (warrior == null)
            {
                return;
            }

            warrior.VisualModelScale = Resolve(warrior) * ResolvePerHit(configs);
        }

        public static void ClampToMax(WarriorInstance warrior, ConfigCsvRepository configs)
        {
            if (warrior == null)
            {
                return;
            }

            var max = ResolveMax(configs);
            var current = Resolve(warrior);
            if (current > max)
            {
                warrior.VisualModelScale = max;
            }
            else
            {
                warrior.VisualModelScale = current;
            }
        }
    }
}
