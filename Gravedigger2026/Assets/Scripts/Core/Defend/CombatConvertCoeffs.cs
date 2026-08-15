using System;
using System.Globalization;
using UnityEngine;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Parses ClassConfig.CombatConvertCoeffs (`Key_Value|…`).
    /// Missing keys fall back to values from CombatConstantConfig (passed as <paramref name="fallback"/>).
    /// </summary>
    public readonly struct CombatConvertCoeffs
    {
        /// <summary>Safety only when constants table key missing — not business authority.</summary>
        public const float SafetyNormalAttackPrimaryMult = 15f;
        public const float SafetyAttackSpeedBase = 0.5f;
        public const float SafetyAttackSpeedAgiDiv = 60f;
        public const float SafetySkillCdIntDiv = 30f;
        public const float SafetySkillCdFloor = 0.1f;
        public const float SafetyMaxHpStrengthMult = 3f;

        public float NormalAttackPrimaryMult { get; }
        public float AttackSpeedBase { get; }
        public float AttackSpeedAgiDiv { get; }
        public float SkillCdIntDiv { get; }
        public float SkillCdFloor { get; }

        public CombatConvertCoeffs(
            float normalAttackPrimaryMult,
            float attackSpeedBase,
            float attackSpeedAgiDiv,
            float skillCdIntDiv,
            float skillCdFloor)
        {
            NormalAttackPrimaryMult = normalAttackPrimaryMult;
            AttackSpeedBase = attackSpeedBase;
            AttackSpeedAgiDiv = attackSpeedAgiDiv;
            SkillCdIntDiv = skillCdIntDiv;
            SkillCdFloor = skillCdFloor;
        }

        /// <summary>Sample safety pack (same numbers as CSV samples).</summary>
        public static CombatConvertCoeffs SafetyDefaults => new CombatConvertCoeffs(
            SafetyNormalAttackPrimaryMult,
            SafetyAttackSpeedBase,
            SafetyAttackSpeedAgiDiv,
            SafetySkillCdIntDiv,
            SafetySkillCdFloor);

        /// <summary>
        /// Parse class override string. Empty / missing keys use <paramref name="fallback"/>
        /// (normally from ConfigCsvRepository.GetCombatConvertCoeffDefaults).
        /// </summary>
        public static CombatConvertCoeffs Parse(string encoded, in CombatConvertCoeffs fallback)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return fallback;
            }

            var mult = fallback.NormalAttackPrimaryMult;
            var aspdBase = fallback.AttackSpeedBase;
            var aspdDiv = fallback.AttackSpeedAgiDiv;
            var cdDiv = fallback.SkillCdIntDiv;
            var cdFloor = fallback.SkillCdFloor;

            var segments = encoded.Split('|');
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i]?.Trim();
                if (string.IsNullOrEmpty(seg))
                {
                    continue;
                }

                var underscore = seg.LastIndexOf('_');
                if (underscore <= 0 || underscore >= seg.Length - 1)
                {
                    Debug.LogWarning($"[CombatConvertCoeffs] Skip illegal segment '{seg}'.");
                    continue;
                }

                var key = seg.Substring(0, underscore);
                var valueText = seg.Substring(underscore + 1);
                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    Debug.LogWarning($"[CombatConvertCoeffs] Skip non-float segment '{seg}'.");
                    continue;
                }

                switch (key)
                {
                    case "NormalAttackPrimaryMult":
                        mult = value;
                        break;
                    case "AttackSpeedBase":
                        aspdBase = value;
                        break;
                    case "AttackSpeedAgiDiv":
                        aspdDiv = value;
                        break;
                    case "SkillCdIntDiv":
                        cdDiv = value;
                        break;
                    case "SkillCdFloor":
                        cdFloor = value;
                        break;
                    default:
                        Debug.LogWarning($"[CombatConvertCoeffs] Unknown key '{key}' in '{seg}'.");
                        break;
                }
            }

            return new CombatConvertCoeffs(mult, aspdBase, aspdDiv, cdDiv, cdFloor);
        }

        /// <summary>Legacy: empty overrides → safety defaults (prefer Parse with table fallback).</summary>
        public static CombatConvertCoeffs Parse(string encoded)
        {
            return Parse(encoded, SafetyDefaults);
        }
    }
}
