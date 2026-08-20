using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Parsed Dig timed lightning payload from ProtagonistEquipment EquipEffect (SPEC_03 §3.16 / D-078).
    /// Not merged into DigProtagonistCapabilities.
    /// </summary>
    public sealed class DigLightningEffectConfig
    {
        public const string EquipId = "Equip_Elctr";
        public const string IntervalKey = "DigLightningIntervalSec";
        public const string FrameSecKey = "DigLightningFrameSec";
        public const string PreviewSecKey = "DigLightningPreviewSec";

        public float IntervalSeconds { get; private set; }
        public float FrameSeconds { get; private set; }
        public float PreviewSeconds { get; private set; }

        public bool IsEnabled => IntervalSeconds > 0f;

        public static bool TryParse(ProtagonistEquipmentConfigRow row, out DigLightningEffectConfig config)
        {
            config = null;
            if (row == null || string.IsNullOrEmpty(row.EquipEffect))
            {
                return false;
            }

            return TryParse(row.EquipEffect, out config);
        }

        public static bool TryParse(string encoded, out DigLightningEffectConfig config)
        {
            config = null;
            if (string.IsNullOrEmpty(encoded))
            {
                return false;
            }

            var parsed = new DigLightningEffectConfig
            {
                FrameSeconds = 0.05f,
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

                if (string.Equals(key, IntervalKey, StringComparison.Ordinal))
                {
                    parsed.IntervalSeconds = value;
                }
                else if (string.Equals(key, FrameSecKey, StringComparison.Ordinal))
                {
                    parsed.FrameSeconds = value;
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

            parsed.FrameSeconds = Mathf.Max(0.01f, parsed.FrameSeconds);
            parsed.PreviewSeconds = Mathf.Max(0.01f, parsed.PreviewSeconds);
            config = parsed;
            return true;
        }

        public static bool TryPickPrimaryHand(
            string lootDropEncoded,
            ConfigCsvRepository configs,
            System.Random rng,
            out BodyPartConfigRow part)
        {
            part = null;
            if (configs == null)
            {
                return false;
            }

            var entries = LootDropParser.ParseWeighted(
                lootDropEncoded,
                msg => Debug.LogWarning($"[DigLightning] {msg}"));
            var hands = new List<BodyPartConfigRow>(4);
            for (var i = 0; i < entries.Count; i++)
            {
                if (!configs.TryGetBodyPart(entries[i].Id, out var body) || body == null)
                {
                    continue;
                }

                if (body.IsPrimaryHand == 1)
                {
                    hands.Add(body);
                }
            }

            if (hands.Count == 0)
            {
                return false;
            }

            var pick = rng != null ? rng.Next(hands.Count) : 0;
            part = hands[pick];
            return part != null;
        }

        public static bool TryPickClassId(string classRestrict, System.Random rng, out string classId)
        {
            classId = null;
            if (string.IsNullOrEmpty(classRestrict))
            {
                return false;
            }

            var parts = classRestrict.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var ids = new List<string>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i].Trim();
                if (id.Length > 0)
                {
                    ids.Add(id);
                }
            }

            if (ids.Count == 0)
            {
                return false;
            }

            var pick = rng != null ? rng.Next(ids.Count) : 0;
            classId = ids[pick];
            return true;
        }
    }
}
