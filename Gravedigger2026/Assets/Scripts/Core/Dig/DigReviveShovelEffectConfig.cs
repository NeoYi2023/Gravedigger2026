using System;
using System.Globalization;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Parsed Dig grave-clear revive-shovel payload (SPEC_03 §3.16 / D-091).
    /// Not merged into DigProtagonistCapabilities.
    /// </summary>
    public sealed class DigReviveShovelEffectConfig
    {
        public const string EquipId = "Equip_ReviveShovel";
        public const string TriggerKey = "DigOnGraveClear";
        public const string PreviewSecKey = "DigLightningPreviewSec";

        public float TriggerChance { get; private set; }
        public float PreviewSeconds { get; private set; }

        public bool IsEnabled => TriggerChance > 0f;

        public static bool TryParse(ProtagonistEquipmentConfigRow row, out DigReviveShovelEffectConfig config)
        {
            config = null;
            if (row == null || string.IsNullOrEmpty(row.EquipEffect))
            {
                return false;
            }

            return TryParse(row.EquipEffect, out config);
        }

        public static bool TryParse(string encoded, out DigReviveShovelEffectConfig config)
        {
            config = null;
            if (string.IsNullOrEmpty(encoded))
            {
                return false;
            }

            var parsed = new DigReviveShovelEffectConfig
            {
                PreviewSeconds = 2f
            };
            var segments = encoded.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();
                if (segment.Length == 0)
                {
                    continue;
                }

                var underscore = segment.LastIndexOf('_');
                if (underscore <= 0 || underscore >= segment.Length - 1)
                {
                    continue;
                }

                var key = segment.Substring(0, underscore);
                var valueText = segment.Substring(underscore + 1);
                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                if (string.Equals(key, TriggerKey, StringComparison.Ordinal))
                {
                    parsed.TriggerChance = value;
                }
                else if (string.Equals(key, PreviewSecKey, StringComparison.Ordinal))
                {
                    parsed.PreviewSeconds = value;
                }
            }

            if (!parsed.IsEnabled)
            {
                return false;
            }

            parsed.PreviewSeconds = Mathf.Max(0.01f, parsed.PreviewSeconds);
            config = parsed;
            return true;
        }
    }
}
