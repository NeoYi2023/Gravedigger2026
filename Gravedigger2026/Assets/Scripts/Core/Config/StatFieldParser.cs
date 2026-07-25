using System;
using System.Globalization;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// SPEC_04 §9.12 / §9.14 flat-stat encoding: Attr_Value|Attr_Value|... (additive; empty = none).
    /// </summary>
    public static class StatFieldParser
    {
        public static StatBlock Parse(string encoded, Action<string> onIgnored = null)
        {
            var block = new StatBlock();
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return block;
            }

            var segments = encoded.Split('|');
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i].Trim();
                if (seg.Length == 0)
                {
                    continue;
                }

                var underscore = seg.LastIndexOf('_');
                if (underscore <= 0 || underscore >= seg.Length - 1)
                {
                    onIgnored?.Invoke($"Stat segment ignored (missing Attr_Value): '{seg}'");
                    continue;
                }

                var keyText = seg.Substring(0, underscore).Trim();
                var valueText = seg.Substring(underscore + 1).Trim();
                if (!Enum.TryParse(keyText, false, out StatKind kind))
                {
                    onIgnored?.Invoke($"Stat segment ignored (unknown key): '{seg}'");
                    continue;
                }

                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    onIgnored?.Invoke($"Stat segment ignored (bad value): '{seg}'");
                    continue;
                }

                block.Add(kind, value);
            }

            return block;
        }
    }
}
