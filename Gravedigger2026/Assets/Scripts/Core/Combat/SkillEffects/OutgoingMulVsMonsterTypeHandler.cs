using System;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_08 精英克制 — outgoing damage multiplier vs matching MonsterType.</summary>
    public sealed class OutgoingMulVsMonsterTypeHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys = { "MonsterType", "Mul" };

        private readonly ConfigCsvRepository _configs;

        public OutgoingMulVsMonsterTypeHandler(ConfigCsvRepository configs)
        {
            _configs = configs;
        }

        public string EffectKind => SkillEffectKind.OutgoingMulVsMonsterType;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.TargetMonster == null || _configs == null)
            {
                return;
            }

            var monsterId = context.TargetMonster.MonsterId;
            if (string.IsNullOrWhiteSpace(monsterId))
            {
                return;
            }

            if (!_configs.TryGetMonster(monsterId, out var monsterRow) || monsterRow == null)
            {
                return;
            }

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (!SkillEffectParams.TryGet(map, "MonsterType", out var monsterTypeText))
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing MonsterType in EffectParams.");
                return;
            }

            if (!Enum.TryParse(monsterTypeText, true, out MonsterType requiredType))
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: invalid MonsterType '{monsterTypeText}'.");
                return;
            }

            if (monsterRow.MonsterType != requiredType)
            {
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "Mul", out var mul) || mul <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid Mul in EffectParams.");
                return;
            }

            context.OutgoingDamage *= mul;
        }
    }
}
