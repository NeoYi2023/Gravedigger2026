using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat.SkillEffects;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.TacticalFormation;
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
        private readonly HashSet<string> _dispatchedEffectIds =
            new HashSet<string>(StringComparer.Ordinal);
        private ITacticalFormationOverlayLookup _formationOverlay;

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

        public void SetFormationOverlay(ITacticalFormationOverlayLookup overlay)
        {
            _formationOverlay = overlay;
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
            _dispatchedEffectIds.Clear();

            var skills = context.Warrior.SoldierSkills;
            if (skills != null)
            {
                for (var i = 0; i < skills.Count; i++)
                {
                    DispatchSkillEntry(context, skills[i]);
                }
            }

            DispatchFormationOverlay(context);
        }

        private void DispatchFormationOverlay(SkillEffectContext context)
        {
            var overlay = _formationOverlay;
            var warriorId = context.Warrior.WarriorId;
            if (overlay == null || !overlay.IsOverlayActive(warriorId))
            {
                return;
            }

            var exclusiveSkills = overlay.GetExclusiveSkillIds(warriorId);
            if (exclusiveSkills != null)
            {
                for (var i = 0; i < exclusiveSkills.Count; i++)
                {
                    var skillId = exclusiveSkills[i];
                    if (string.IsNullOrEmpty(skillId) || ContainsSkillId(context.Warrior.SoldierSkills, skillId))
                    {
                        continue;
                    }

                    if (!TryResolveExclusiveSkill(skillId, out var skillRow) || skillRow == null)
                    {
                        continue;
                    }

                    context.CurrentSkillRow = skillRow;
                    DispatchEffectId(context, skillRow.SkillEffectId);
                }
            }

            var exclusiveEffects = overlay.GetExclusiveSkillEffectIds(warriorId);
            if (exclusiveEffects == null)
            {
                return;
            }

            for (var i = 0; i < exclusiveEffects.Count; i++)
            {
                context.CurrentSkillRow = null;
                DispatchEffectId(context, exclusiveEffects[i]);
            }
        }

        private void DispatchSkillEntry(SkillEffectContext context, SoldierSkillEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (!_configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out var skillRow) || skillRow == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(skillRow.SkillEffectId))
            {
                return;
            }

            context.CurrentSkillRow = skillRow;
            DispatchEffectId(context, skillRow.SkillEffectId);
        }

        private bool TryResolveExclusiveSkill(string skillId, out SkillConfigRow skillRow)
        {
            skillRow = null;
            if (!_configs.TryGetSkillLevelRange(skillId, out var minLevel, out _) || minLevel < 1)
            {
                return false;
            }

            return _configs.TryGetSkill(skillId, minLevel, out skillRow) && skillRow != null;
        }

        private static bool ContainsSkillId(IReadOnlyList<SoldierSkillEntry> skills, string skillId)
        {
            if (skills == null)
            {
                return false;
            }

            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry != null && string.Equals(entry.SkillId, skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void DispatchEffectId(SkillEffectContext context, string skillEffectId)
        {
            if (string.IsNullOrWhiteSpace(skillEffectId) || !_dispatchedEffectIds.Add(skillEffectId))
            {
                return;
            }

            if (!_configs.TryGetSkillEffect(skillEffectId, out var effectRow) || effectRow == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(effectRow.EffectKind))
            {
                return;
            }

            if (!MatchesTriggerHook(effectRow, context.DispatchTriggerHook))
            {
                return;
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

                return;
            }

            handler.Apply(context, effectRow);
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
