using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat.SkillEffects;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Table-driven skill effect dispatch (SPEC_03 §3.12 / D-073).
    /// Session calls Dispatch at settle points — no SkillId branching here.
    /// </summary>
    public sealed class SkillEffectPipeline
    {
        private readonly ConfigCsvRepository _configs;
        private readonly Dictionary<string, ISkillEffectHandler> _handlersByKind =
            new Dictionary<string, ISkillEffectHandler>(StringComparer.Ordinal);

        public SkillEffectPipeline(ConfigCsvRepository configs)
        {
            _configs = configs;
            Register(new OutgoingMulOnNewTargetFirstHitHandler());
            Register(new CheatDeathInvincibleHandler());
            Register(new OnAaHitChanceAoeStunHandler());
            Register(new OnAaHitAoeSlowHandler());
            Register(new OutgoingMulVsMonsterTypeHandler(_configs));
            Register(new StackingOutgoingMulTimedHandler());
            Register(new RangedPierceExtraHitsHandler());
            Register(new OnAaHitApplyBurnHandler());
            Register(new RetargetFarthestTeleportBehindHandler());
        }

        public void Register(ISkillEffectHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.EffectKind))
            {
                return;
            }

            _handlersByKind[handler.EffectKind] = handler;
        }

        public void Dispatch(string triggerHook, SkillEffectContext context)
        {
            if (string.IsNullOrWhiteSpace(triggerHook) || context?.Warrior == null || _configs == null)
            {
                return;
            }

            context.DispatchTriggerHook = triggerHook;

            var skills = context.Warrior.SoldierSkills;
            if (skills == null || skills.Count == 0)
            {
                return;
            }

            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry == null)
                {
                    continue;
                }

                if (!_configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out var skillRow) || skillRow == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(skillRow.SkillEffectId))
                {
                    continue;
                }

                if (!_configs.TryGetSkillEffect(skillRow.SkillEffectId, out var effectRow) || effectRow == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(effectRow.EffectKind))
                {
                    continue;
                }

                if (!MatchesTriggerHook(effectRow, triggerHook))
                {
                    continue;
                }

                if (!_handlersByKind.TryGetValue(effectRow.EffectKind, out var handler))
                {
                    if (SkillEffectKind.IsRegistered(effectRow.EffectKind))
                    {
                        Debug.LogWarning(
                            $"[SkillEffect] Registered kind '{effectRow.EffectKind}' has no handler.");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[SkillEffect] Unregistered EffectKind '{effectRow.EffectKind}' on " +
                            $"{effectRow.SkillEffectId}.");
                    }

                    continue;
                }

                context.CurrentSkillRow = skillRow;
                handler.Apply(context, effectRow);
            }
        }

        private static bool MatchesTriggerHook(SkillEffectConfigRow effectRow, string triggerHook)
        {
            if (effectRow == null || string.IsNullOrWhiteSpace(triggerHook))
            {
                return false;
            }

            if (string.Equals(effectRow.TriggerHook, triggerHook, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(effectRow.EffectKind, SkillEffectKind.StackingOutgoingMulTimed, StringComparison.Ordinal)
                   && string.Equals(triggerHook, SkillEffectTriggerHook.OnOutgoingDamageSettle, StringComparison.Ordinal);
        }
    }
}
