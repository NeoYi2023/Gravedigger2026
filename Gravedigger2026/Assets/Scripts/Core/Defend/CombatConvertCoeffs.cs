using System;
using System.Globalization;
using UnityEngine;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Parses ClassConfig.CombatConvertCoeffs (`Key_Value|…`) with §3.12 / SPEC_04 §9.9b defaults.
    /// </summary>
    public readonly struct CombatConvertCoeffs
    {
        public const float DefaultNormalAttackPrimaryMult = 1.5f;
        public const float DefaultAttackSpeedBase = 0.5f;
        public const float DefaultAttackSpeedAgiDiv = 60f;
        public const float DefaultSkillCdIntDiv = 30f;
        public const float DefaultSkillCdFloor = 0.1f;

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

        public static CombatConvertCoeffs Defaults => new CombatConvertCoeffs(
            DefaultNormalAttackPrimaryMult,
            DefaultAttackSpeedBase,
            DefaultAttackSpeedAgiDiv,
            DefaultSkillCdIntDiv,
            DefaultSkillCdFloor);

        public static CombatConvertCoeffs Parse(string encoded)
        {
            var result = Defaults;
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return result;
            }

            var mult = result.NormalAttackPrimaryMult;
            var aspdBase = result.AttackSpeedBase;
            var aspdDiv = result.AttackSpeedAgiDiv;
            var cdDiv = result.SkillCdIntDiv;
            var cdFloor = result.SkillCdFloor;

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
    }
}
