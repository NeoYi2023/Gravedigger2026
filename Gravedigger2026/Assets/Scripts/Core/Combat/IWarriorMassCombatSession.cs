using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Mass-combat warrior session contract shared by PushMapAdvanceView (PM-12/13) and SearchExtract (SE-06).
    /// Extends ranged projectile settlement; skill hooks may no-op on lighter implementations.
    /// </summary>
    public interface IWarriorMassCombatSession : IProjectileCombatSession
    {
        bool IsCombatGameplayActive { get; }

        bool IsWarriorCombatActive(string warriorId);

        bool TryGetWarrior(string warriorId, out DefendCombatWarriorState state);

        bool IsMonsterTargetable(string runtimeId);

        bool TryConfirmMeleeHit(string warriorId, string monsterRuntimeId, bool stillInRange);

        bool TryAcquireWarriorTarget(
            string warriorId,
            Vector2 warriorPositionXZ,
            float warriorBodyRadius,
            IReadOnlyList<MonsterWorldXZ> candidates,
            Func<Vector2, float, Vector2?> sampleWalkableXZ,
            out string overrideTargetId,
            out Vector2 teleportLandingXZ);

        bool TryCommitSkillBurst(string warriorId, out int burstHitCount);

        bool TryGetSkillCooldownRemaining(string warriorId, out float remaining);
    }
}
