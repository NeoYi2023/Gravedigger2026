using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    public sealed class MonsterSelfReviveOnDeathParams
    {
        public float DelaySeconds = 10f;
        public float ReviveHpRatio = 0.75f;
        public float InvincibleSeconds = 1f;
        public float ReviveAnimSeconds = 1.5f;
        public int MaxReviveCount = 2;
        /// <summary>When true, first revive replaces instance AlertRadius; later revives ignore.</summary>
        public bool HasAlertRadius;
        public float AlertRadius;

        public static bool TryParse(MonsterSkillEffectConfigRow row, out MonsterSelfReviveOnDeathParams result)
        {
            result = null;
            if (row == null
                || !string.Equals(
                    row.EffectKind,
                    MonsterSkillEffectKind.MonsterSelfReviveOnDeath,
                    System.StringComparison.Ordinal))
            {
                return false;
            }

            var parsed = new MonsterSelfReviveOnDeathParams();
            var map = SkillEffectParams.Parse(
                row.EffectParams,
                new[]
                {
                    "DelaySeconds",
                    "ReviveHpRatio",
                    "InvincibleSeconds",
                    "ReviveAnimSeconds",
                    "MaxReviveCount",
                    "AlertRadius"
                });

            if (SkillEffectParams.TryGetFloat(map, "DelaySeconds", out var delay))
            {
                parsed.DelaySeconds = Mathf.Max(0f, delay);
            }

            if (SkillEffectParams.TryGetFloat(map, "ReviveHpRatio", out var ratio))
            {
                parsed.ReviveHpRatio = Mathf.Clamp(ratio, 0.01f, 1f);
            }

            if (SkillEffectParams.TryGetFloat(map, "InvincibleSeconds", out var invincible))
            {
                parsed.InvincibleSeconds = Mathf.Max(0f, invincible);
            }

            if (SkillEffectParams.TryGetFloat(map, "ReviveAnimSeconds", out var animSeconds))
            {
                parsed.ReviveAnimSeconds = Mathf.Max(0.01f, animSeconds);
            }

            if (SkillEffectParams.TryGetFloat(map, "MaxReviveCount", out var maxCount))
            {
                parsed.MaxReviveCount = Mathf.Max(0, Mathf.RoundToInt(maxCount));
            }

            if (SkillEffectParams.TryGetFloat(map, "AlertRadius", out var alertRadius))
            {
                parsed.HasAlertRadius = true;
                parsed.AlertRadius = Mathf.Max(0f, alertRadius);
            }

            result = parsed;
            return true;
        }
    }
}
