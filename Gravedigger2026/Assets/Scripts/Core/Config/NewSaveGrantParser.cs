using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Parses CombatConstantConfig new-save text keys (SPEC_03 §3.4 / SPEC_04 §9.20b).
    /// </summary>
    public static class NewSaveGrantParser
    {
        public readonly struct EquipGrant
        {
            public EquipGrant(string equipId, int level)
            {
                EquipId = equipId;
                Level = level;
            }

            public string EquipId { get; }
            public int Level { get; }
        }

        /// <summary>
        /// Parse <c>EquipId;Level|EquipId;Level</c>. Illegal segments skipped (caller may log).
        /// Duplicate EquipId → first wins.
        /// </summary>
        public static List<EquipGrant> ParseEquipments(string raw, Action<string> onSkipWarning = null)
        {
            var result = new List<EquipGrant>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                if (segment.Length == 0)
                {
                    continue;
                }

                var semi = segment.IndexOf(';');
                if (semi <= 0 || semi >= segment.Length - 1)
                {
                    onSkipWarning?.Invoke($"Illegal equipment segment '{segment}' (want EquipId;Level).");
                    continue;
                }

                var equipId = segment.Substring(0, semi).Trim();
                var levelText = segment.Substring(semi + 1).Trim();
                if (equipId.Length == 0
                    || !int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                    || level < 1)
                {
                    onSkipWarning?.Invoke($"Illegal equipment segment '{segment}' (Level must be ≥ 1).");
                    continue;
                }

                if (!seen.Add(equipId))
                {
                    onSkipWarning?.Invoke($"Duplicate EquipId '{equipId}' in NewSaveInitialEquipments — first wins.");
                    continue;
                }

                result.Add(new EquipGrant(equipId, level));
            }

            return result;
        }

        /// <summary>
        /// Parse <c>MagicBookId|MagicBookId</c>. Empty tokens skipped.
        /// </summary>
        public static List<string> ParseMagicBooks(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return result;
            }

            var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i].Trim();
                if (id.Length > 0)
                {
                    result.Add(id);
                }
            }

            return result;
        }
    }
}
