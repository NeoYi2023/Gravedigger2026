using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// SPEC_04 §9.24 EffectParams: Key=Value or Key=Value|Key=Value|…
    /// Unknown keys warn+skip; duplicate keys last-wins.
    /// </summary>
    public static class MagicBookEffectParams
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
                    Debug.LogWarning($"[MagicBook] EffectParams ignored (need Key=Value): '{seg}'");
                    continue;
                }

                var key = seg.Substring(0, eq).Trim();
                var value = seg.Substring(eq + 1).Trim();
                if (key.Length == 0 || value.Length == 0)
                {
                    Debug.LogWarning($"[MagicBook] EffectParams ignored (empty Key/Value): '{seg}'");
                    continue;
                }

                if (allowedKeys != null && !allowedKeys.Contains(key))
                {
                    Debug.LogWarning($"[MagicBook] EffectParams unknown Key '{key}' ignored.");
                    continue;
                }

                if (map.ContainsKey(key))
                {
                    Debug.LogWarning($"[MagicBook] EffectParams duplicate Key '{key}' — last wins.");
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
    }
}
