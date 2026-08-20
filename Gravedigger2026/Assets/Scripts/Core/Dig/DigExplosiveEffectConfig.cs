using System;
using System.Globalization;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Parsed Dig event payload from ProtagonistEquipment EquipEffect (SPEC_03 §3.16 / D-077).
    /// Not merged into DigProtagonistCapabilities.
    /// </summary>
    public sealed class DigExplosiveEffectConfig
    {
        public const string EquipId = "Equip_Explosives";
        public const string TriggerKey = "DigOnGraveClear";
        public const string ThrowRadiusKey = "ExplosiveThrowRadius";
        public const string BlastRadiusKey = "ExplosiveBlastRadius";
        public const string BlastDamageKey = "ExplosiveBlastDamage";
        public const string FlightSecKey = "ExplosiveFlightSec";
        public const string FuseSecKey = "ExplosiveFuseSec";
        public const string RingSecKey = "ExplosiveRingSec";

        public float TriggerChance { get; private set; }
        public float ThrowRadius { get; private set; }
        public float BlastRadius { get; private set; }
        public float BlastDamage { get; private set; }
        public float FlightSeconds { get; private set; }
        public float FuseSeconds { get; private set; }
        public float RingSeconds { get; private set; }

        public bool IsEnabled =>
            TriggerChance > 0f && ThrowRadius > 0f && BlastRadius > 0f && BlastDamage > 0f;

        public static bool TryParse(ProtagonistEquipmentConfigRow row, out DigExplosiveEffectConfig config)
        {
            config = null;
            if (row == null || string.IsNullOrEmpty(row.EquipEffect))
            {
                return false;
            }

            return TryParse(row.EquipEffect, out config);
        }

        public static bool TryParse(string encoded, out DigExplosiveEffectConfig config)
        {
            config = null;
            if (string.IsNullOrEmpty(encoded))
            {
                return false;
            }

            var parsed = new DigExplosiveEffectConfig();
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
                else if (string.Equals(key, ThrowRadiusKey, StringComparison.Ordinal))
                {
                    parsed.ThrowRadius = value;
                }
                else if (string.Equals(key, BlastRadiusKey, StringComparison.Ordinal))
                {
                    parsed.BlastRadius = value;
                }
                else if (string.Equals(key, BlastDamageKey, StringComparison.Ordinal))
                {
                    parsed.BlastDamage = value;
                }
                else if (string.Equals(key, FlightSecKey, StringComparison.Ordinal))
                {
                    parsed.FlightSeconds = value;
                }
                else if (string.Equals(key, FuseSecKey, StringComparison.Ordinal))
                {
                    parsed.FuseSeconds = value;
                }
                else if (string.Equals(key, RingSecKey, StringComparison.Ordinal))
                {
                    parsed.RingSeconds = value;
                }
            }

            if (!parsed.IsEnabled)
            {
                return false;
            }

            parsed.FlightSeconds = Mathf.Max(0.01f, parsed.FlightSeconds);
            parsed.FuseSeconds = Mathf.Max(0f, parsed.FuseSeconds);
            parsed.RingSeconds = Mathf.Max(0.01f, parsed.RingSeconds);
            config = parsed;
            return true;
        }
    }
}
