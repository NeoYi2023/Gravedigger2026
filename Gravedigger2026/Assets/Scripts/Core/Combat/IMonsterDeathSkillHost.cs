using System;
using Gravedigger2026.Core.Defend;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Shared View contract for D-074 monster death skills (SPEC_03 §3.14 / §3.19).
    /// Implemented by PushMapSessionService and SearchExtractSessionService.
    /// </summary>
    public interface IMonsterDeathSkillHost
    {
        event Action<string, string, float, string> MonsterEnteredCombatDead;

        event Action<string, float> MonsterReviveStarted;

        event Action<string> MonsterRevived;

        event Action<string, string, bool> MonsterInvincibleChanged;

        bool TryGetMonster(string runtimeId, out DefendCombatMonsterState state);

        bool TryNotifyMonsterDeathPresentationComplete(string runtimeId);

        bool TryNotifyMonsterReviveAnimComplete(string runtimeId);

        bool IsMonsterInvincible(string monsterRuntimeId);
    }
}
