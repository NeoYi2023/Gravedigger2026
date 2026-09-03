using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Shared monster death-triggered skills (SPEC_03 §3.14 / §3.19 / §9.21c).
    /// Hosted via IMonsterDeathSkillHost (PushMap + SearchExtract). First slice: MonsterSelfReviveOnDeath.
    /// </summary>
    public sealed class MonsterDeathSkillService
    {
        public event Action<string, float> MonsterReviveStarted;

        public void InitializeMonsterState(
            DefendCombatMonsterState monster,
            ConfigCsvRepository configs)
        {
            if (monster == null || configs == null || string.IsNullOrEmpty(monster.MonsterId))
            {
                return;
            }

            monster.RevivePhase = MonsterRevivePhase.None;
            monster.IsCombatDead = false;
            monster.RevivesRemaining = 0;
            monster.PhaseTimer = 0f;
            monster.ReviveSkillId = string.Empty;
            monster.SelfReviveParams = null;
            monster.PostReviveAlertRadiusApplied = false;
            monster.RuntimeAlertRadius = 0f;

            if (!configs.TryGetMonster(monster.MonsterId, out var monsterRow) || monsterRow == null)
            {
                return;
            }

            var entries = MonsterSkillParser.Parse(monsterRow.Skills);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                {
                    continue;
                }

                if (!configs.TryGetMonsterSkillEffect(entry.SkillId, out var effectRow) || effectRow == null)
                {
                    continue;
                }

                if (MonsterSelfReviveOnDeathParams.TryParse(effectRow, out var reviveParams))
                {
                    monster.ReviveSkillId = entry.SkillId;
                    monster.SelfReviveParams = reviveParams;
                    monster.RevivesRemaining = reviveParams.MaxReviveCount;
                    return;
                }

                if (MonsterSkillEffectKind.IsRegistered(effectRow.EffectKind))
                {
                    Debug.LogWarning(
                        $"[MonsterDeathSkill] Registered kind '{effectRow.EffectKind}' on " +
                        $"{entry.SkillId} has no runtime handler yet.");
                }
            }
        }

        public bool TryInterceptDeath(DefendCombatMonsterState monster)
        {
            if (monster == null
                || monster.IsCombatDead
                || monster.SelfReviveParams == null
                || monster.RevivesRemaining <= 0)
            {
                return false;
            }

            monster.RevivesRemaining--;
            monster.IsCombatDead = true;
            monster.IsAlive = false;
            monster.RemainingHp = 0f;
            monster.RevivePhase = MonsterRevivePhase.AwaitingPresentation;
            monster.PhaseTimer = 0f;
            return true;
        }

        public bool TryNotifyDeathPresentationComplete(DefendCombatMonsterState monster)
        {
            if (monster == null || !monster.IsCombatDead || monster.SelfReviveParams == null)
            {
                return false;
            }

            if (monster.RevivePhase != MonsterRevivePhase.AwaitingPresentation)
            {
                return false;
            }

            monster.RevivePhase = MonsterRevivePhase.WaitingDelay;
            monster.PhaseTimer = Mathf.Max(0f, monster.SelfReviveParams.DelaySeconds);
            return true;
        }

        public void Tick(DefendCombatMonsterState monster, float deltaTime)
        {
            if (monster == null
                || !monster.IsCombatDead
                || monster.SelfReviveParams == null
                || deltaTime <= 0f)
            {
                return;
            }

            if (monster.RevivePhase != MonsterRevivePhase.WaitingDelay)
            {
                return;
            }

            monster.PhaseTimer -= deltaTime;
            if (monster.RevivePhase == MonsterRevivePhase.WaitingDelay && monster.PhaseTimer > 0f)
            {
                return;
            }

            monster.RevivePhase = MonsterRevivePhase.Reviving;
            MonsterReviveStarted?.Invoke(
                monster.RuntimeId,
                Mathf.Max(0.01f, monster.SelfReviveParams.ReviveAnimSeconds));
        }

        public bool TryCompleteReviveAnim(
            DefendCombatMonsterState monster,
            CombatStatusService combatStatus)
        {
            if (monster == null
                || !monster.IsCombatDead
                || monster.SelfReviveParams == null
                || monster.RevivePhase != MonsterRevivePhase.Reviving)
            {
                return false;
            }

            var ratio = Mathf.Clamp(monster.SelfReviveParams.ReviveHpRatio, 0.01f, 1f);
            monster.RemainingHp = Mathf.Max(1f, monster.MaxHp * ratio);
            monster.IsAlive = true;
            monster.IsCombatDead = false;
            monster.RevivePhase = MonsterRevivePhase.None;
            monster.PhaseTimer = 0f;

            if (!monster.PostReviveAlertRadiusApplied
                && monster.SelfReviveParams.HasAlertRadius)
            {
                monster.RuntimeAlertRadius = Mathf.Max(0f, monster.SelfReviveParams.AlertRadius);
                monster.PostReviveAlertRadiusApplied = true;
            }

            if (combatStatus != null && monster.SelfReviveParams.InvincibleSeconds > 0f)
            {
                combatStatus.ApplyMonsterInvincible(
                    monster.RuntimeId,
                    monster.ReviveSkillId ?? string.Empty,
                    monster.SelfReviveParams.InvincibleSeconds);
            }

            return true;
        }

        /// <summary>
        /// Rules wipe / true death: cancel fake-death revive without intercept.
        /// SearchExtract PointClear uses this so SelfRevive does not fire.
        /// </summary>
        public void ForceTrueDeath(DefendCombatMonsterState monster)
        {
            if (monster == null)
            {
                return;
            }

            monster.IsAlive = false;
            monster.IsCombatDead = false;
            monster.RemainingHp = 0f;
            monster.RevivePhase = MonsterRevivePhase.None;
            monster.PhaseTimer = 0f;
        }
    }
}
