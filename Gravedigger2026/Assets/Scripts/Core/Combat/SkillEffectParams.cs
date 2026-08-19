using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// SPEC_04 §9.21b EffectParams parser (aligned with MagicBookEffectParams).
    /// </summary>
    public static class SkillEffectParams
    {
        public static Dictionary<string, string> Parse(string encoded, ICollection<string> allowedKeys)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return map;
            }

            var parts = encoded.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var seg = parts[i]?.Trim();
                if (string.IsNullOrEmpty(seg))
                {
                    continue;
                }

                var eq = seg.IndexOf('=');
                if (eq <= 0 || eq >= seg.Length - 1)
                {
                    Debug.LogWarning($"[SkillEffect] EffectParams ignored (need Key=Value): '{seg}'");
                    continue;
                }

                var key = seg.Substring(0, eq).Trim();
                var value = seg.Substring(eq + 1).Trim();
                if (key.Length == 0 || value.Length == 0)
                {
                    Debug.LogWarning($"[SkillEffect] EffectParams ignored (empty Key/Value): '{seg}'");
                    continue;
                }

                if (allowedKeys != null && !allowedKeys.Contains(key))
                {
                    Debug.LogWarning($"[SkillEffect] EffectParams unknown Key '{key}' ignored.");
                    continue;
                }

                if (map.ContainsKey(key))
                {
                    Debug.LogWarning($"[SkillEffect] EffectParams duplicate Key '{key}' — last wins.");
                }

                map[key] = value;
            }

            return map;
        }

        public static bool TryGet(Dictionary<string, string> map, string key, out string value)
        {
            value = null;
            if (map == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            return map.TryGetValue(key, out value) && !string.IsNullOrEmpty(value);
        }

        public static bool TryGetFloat(Dictionary<string, string> map, string key, out float value)
        {
            value = 0f;
            if (!TryGet(map, key, out var text))
            {
                return false;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
