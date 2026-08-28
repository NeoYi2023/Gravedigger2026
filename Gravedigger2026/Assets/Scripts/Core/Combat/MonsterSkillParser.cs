using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>Parse MonsterConfig.Skills encoding: SkillId;CdSeconds|… (SPEC_04 §9.19).</summary>
    public static class MonsterSkillParser
    {
        public sealed class Entry
        {
            public string SkillId;
            public float CooldownSeconds;
        }

        public static IReadOnlyList<Entry> Parse(string skillsEncoded)
        {
            var result = new List<Entry>();
            if (string.IsNullOrWhiteSpace(skillsEncoded))
            {
                return result;
            }

            var segments = skillsEncoded.Split('|');
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i]?.Trim();
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                var parts = segment.Split(';');
                if (parts.Length != 2)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[MonsterSkillParser] Ignoring malformed segment '{segment}' (expect SkillId;CdSeconds).");
                    continue;
                }

                var skillId = parts[0]?.Trim();
                var cdText = parts[1]?.Trim();
                if (string.IsNullOrEmpty(skillId))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[MonsterSkillParser] Ignoring segment '{segment}' — empty SkillId.");
                    continue;
                }

                if (!float.TryParse(cdText, NumberStyles.Float, CultureInfo.InvariantCulture, out var cd)
                    || cd < 0f)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[MonsterSkillParser] Ignoring segment '{segment}' — illegal CdSeconds '{cdText}'.");
                    continue;
                }

                result.Add(new Entry { SkillId = skillId, CooldownSeconds = cd });
            }

            return result;
        }
    }
}
