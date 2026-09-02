using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Unified combat status tick + query (SPEC_03 §3.12 / D-073).
    /// Warrior bucket: Invincible. Monster bucket: Stun / Slow / Burn (SE-03/SE-04+).
    /// </summary>
    public sealed class CombatStatusService
    {
        private sealed class TimedStatus
        {
            public float RemainingSeconds;
            public string SkillId;
            public bool IsHold;
            public float MoveMul = 1f;
            public float AttackMul = 1f;
        }

        private sealed class BurnStatus
        {
            public float RemainingSeconds;
            public float TickIntervalSeconds;
            public float TickAccumulator;
            public string SkillId;
            public string SourceWarriorId;
            public float TickDamage;
        }

        private readonly Dictionary<string, TimedStatus> _warriorInvincible =
            new Dictionary<string, TimedStatus>(StringComparer.Ordinal);

        private readonly Dictionary<string, TimedStatus> _monsterStun =
            new Dictionary<string, TimedStatus>(StringComparer.Ordinal);

        private readonly Dictionary<string, TimedStatus> _monsterInvincible =
            new Dictionary<string, TimedStatus>(StringComparer.Ordinal);

        private readonly Dictionary<string, TimedStatus> _monsterSlow =
            new Dictionary<string, TimedStatus>(StringComparer.Ordinal);

        private readonly Dictionary<string, BurnStatus> _monsterBurn =
            new Dictionary<string, BurnStatus>(StringComparer.Ordinal);

        /// <summary>warriorId, skillId, on</summary>
        public event Action<string, string, bool> WarriorInvincibleChanged;

        /// <summary>monsterRuntimeId, skillId, on</summary>
        public event Action<string, string, bool> MonsterStunChanged;

        /// <summary>monsterRuntimeId, skillId, on</summary>
        public event Action<string, string, bool> MonsterInvincibleChanged;

        /// <summary>monsterRuntimeId, skillId, on</summary>
        public event Action<string, string, bool> MonsterSlowChanged;

        /// <summary>monsterRuntimeId, sourceWarriorId, tickDamage, skillId</summary>
        public event Action<string, string, float, string> MonsterBurnTick;

        public bool IsWarriorInvincible(string warriorId)
        {
            return !string.IsNullOrEmpty(warriorId)
                   && _warriorInvincible.TryGetValue(warriorId, out var state)
                   && state != null
                   && state.RemainingSeconds > 0f;
        }

        public bool IsMonsterStunned(string monsterRuntimeId)
        {
            return !string.IsNullOrEmpty(monsterRuntimeId)
                   && _monsterStun.TryGetValue(monsterRuntimeId, out var state)
                   && state != null
                   && state.RemainingSeconds > 0f;
        }

        public bool IsMonsterInvincible(string monsterRuntimeId)
        {
            return !string.IsNullOrEmpty(monsterRuntimeId)
                   && _monsterInvincible.TryGetValue(monsterRuntimeId, out var state)
                   && state != null
                   && state.RemainingSeconds > 0f;
        }

        public bool IsMonsterSlowed(string monsterRuntimeId)
        {
            return !string.IsNullOrEmpty(monsterRuntimeId)
                   && _monsterSlow.TryGetValue(monsterRuntimeId, out var state)
                   && state != null
                   && state.RemainingSeconds > 0f;
        }

        public float GetMonsterSlowMoveMul(string monsterRuntimeId)
        {
            if (!IsMonsterSlowed(monsterRuntimeId))
            {
                return 1f;
            }

            return Math.Max(0f, _monsterSlow[monsterRuntimeId].MoveMul);
        }

        public float GetMonsterSlowAttackMul(string monsterRuntimeId)
        {
            if (!IsMonsterSlowed(monsterRuntimeId))
            {
                return 1f;
            }

            return Math.Max(0f, _monsterSlow[monsterRuntimeId].AttackMul);
        }

        public bool IsMonsterBurned(string monsterRuntimeId)
        {
            return !string.IsNullOrEmpty(monsterRuntimeId)
                   && _monsterBurn.TryGetValue(monsterRuntimeId, out var state)
                   && state != null
                   && state.RemainingSeconds > 0f;
        }

        public void ApplyWarriorInvincible(string warriorId, string skillId, float seconds)
        {
            if (string.IsNullOrEmpty(warriorId) || seconds <= 0f)
            {
                return;
            }

            var wasOn = IsWarriorInvincible(warriorId);
            _warriorInvincible[warriorId] = new TimedStatus
            {
                RemainingSeconds = seconds,
                SkillId = skillId ?? string.Empty,
                IsHold = false
            };

            if (!wasOn)
            {
                WarriorInvincibleChanged?.Invoke(warriorId, skillId ?? string.Empty, true);
            }
        }

        /// <summary>
        /// Hold invincible until <see cref="ClearWarrior"/> (SearchExtract point success / UI-032).
        /// </summary>
        public void ApplyWarriorInvincibleHold(string warriorId, string skillId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            var wasOn = IsWarriorInvincible(warriorId);
            _warriorInvincible[warriorId] = new TimedStatus
            {
                RemainingSeconds = float.PositiveInfinity,
                SkillId = skillId ?? string.Empty,
                IsHold = true
            };

            if (!wasOn)
            {
                WarriorInvincibleChanged?.Invoke(warriorId, skillId ?? string.Empty, true);
            }
        }

        public void ApplyMonsterStun(string monsterRuntimeId, string skillId, float seconds)
        {
            if (string.IsNullOrEmpty(monsterRuntimeId) || seconds <= 0f)
            {
                return;
            }

            var wasOn = IsMonsterStunned(monsterRuntimeId);
            _monsterStun[monsterRuntimeId] = new TimedStatus
            {
                RemainingSeconds = seconds,
                SkillId = skillId ?? string.Empty
            };

            if (!wasOn)
            {
                MonsterStunChanged?.Invoke(monsterRuntimeId, skillId ?? string.Empty, true);
            }
        }

        public void ApplyMonsterInvincible(string monsterRuntimeId, string skillId, float seconds)
        {
            if (string.IsNullOrEmpty(monsterRuntimeId) || seconds <= 0f)
            {
                return;
            }

            var wasOn = IsMonsterInvincible(monsterRuntimeId);
            _monsterInvincible[monsterRuntimeId] = new TimedStatus
            {
                RemainingSeconds = seconds,
                SkillId = skillId ?? string.Empty
            };

            if (!wasOn)
            {
                MonsterInvincibleChanged?.Invoke(monsterRuntimeId, skillId ?? string.Empty, true);
            }
        }

        public void ApplyMonsterSlow(
            string monsterRuntimeId,
            string skillId,
            float seconds,
            float moveMul,
            float attackMul)
        {
            if (string.IsNullOrEmpty(monsterRuntimeId) || seconds <= 0f)
            {
                return;
            }

            var wasOn = IsMonsterSlowed(monsterRuntimeId);
            _monsterSlow[monsterRuntimeId] = new TimedStatus
            {
                RemainingSeconds = seconds,
                SkillId = skillId ?? string.Empty,
                MoveMul = Math.Max(0f, moveMul),
                AttackMul = Math.Max(0f, attackMul)
            };

            if (!wasOn)
            {
                MonsterSlowChanged?.Invoke(monsterRuntimeId, skillId ?? string.Empty, true);
            }
        }

        public void ApplyMonsterBurn(
            string monsterRuntimeId,
            string skillId,
            string sourceWarriorId,
            float durationSeconds,
            float tickIntervalSeconds,
            float tickDamage,
            string stackMode)
        {
            if (string.IsNullOrEmpty(monsterRuntimeId)
                || durationSeconds <= 0f
                || tickIntervalSeconds <= 0f
                || tickDamage <= 0f)
            {
                return;
            }

            var refreshDuration = string.IsNullOrWhiteSpace(stackMode)
                                  || string.Equals(stackMode, "RefreshDuration", StringComparison.Ordinal);

            if (refreshDuration
                && _monsterBurn.TryGetValue(monsterRuntimeId, out var existing)
                && existing != null
                && existing.RemainingSeconds > 0f)
            {
                existing.RemainingSeconds = durationSeconds;
                existing.TickIntervalSeconds = tickIntervalSeconds;
                existing.TickDamage = tickDamage;
                existing.SourceWarriorId = sourceWarriorId ?? string.Empty;
                existing.SkillId = skillId ?? string.Empty;
                return;
            }

            _monsterBurn[monsterRuntimeId] = new BurnStatus
            {
                RemainingSeconds = durationSeconds,
                TickIntervalSeconds = tickIntervalSeconds,
                TickAccumulator = 0f,
                SkillId = skillId ?? string.Empty,
                SourceWarriorId = sourceWarriorId ?? string.Empty,
                TickDamage = tickDamage
            };
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            TickTimedBucket(_warriorInvincible, deltaTime, WarriorInvincibleChanged);
            TickTimedBucket(_monsterStun, deltaTime, MonsterStunChanged);
            TickTimedBucket(_monsterInvincible, deltaTime, MonsterInvincibleChanged);
            TickTimedBucket(_monsterSlow, deltaTime, MonsterSlowChanged);
            TickBurnBucket(deltaTime);
        }

        public void ClearWarrior(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            if (_warriorInvincible.Remove(warriorId, out var state) && state != null)
            {
                WarriorInvincibleChanged?.Invoke(warriorId, state.SkillId, false);
            }
        }

        public void ClearMonster(string monsterRuntimeId)
        {
            if (string.IsNullOrEmpty(monsterRuntimeId))
            {
                return;
            }

            if (_monsterStun.Remove(monsterRuntimeId, out var stun) && stun != null)
            {
                MonsterStunChanged?.Invoke(monsterRuntimeId, stun.SkillId, false);
            }

            if (_monsterInvincible.Remove(monsterRuntimeId, out var invincible) && invincible != null)
            {
                MonsterInvincibleChanged?.Invoke(monsterRuntimeId, invincible.SkillId, false);
            }

            if (_monsterSlow.Remove(monsterRuntimeId, out var slow) && slow != null)
            {
                MonsterSlowChanged?.Invoke(monsterRuntimeId, slow.SkillId, false);
            }

            _monsterBurn.Remove(monsterRuntimeId);
        }

        public void ClearAll()
        {
            if (_warriorInvincible.Count > 0)
            {
                var warriorIds = new List<string>(_warriorInvincible.Keys);
                for (var i = 0; i < warriorIds.Count; i++)
                {
                    ClearWarrior(warriorIds[i]);
                }
            }

            if (_monsterStun.Count > 0 || _monsterInvincible.Count > 0 || _monsterSlow.Count > 0 || _monsterBurn.Count > 0)
            {
                var monsterIds = new HashSet<string>(_monsterStun.Keys);
                foreach (var id in _monsterInvincible.Keys)
                {
                    monsterIds.Add(id);
                }

                foreach (var id in _monsterSlow.Keys)
                {
                    monsterIds.Add(id);
                }

                foreach (var id in _monsterBurn.Keys)
                {
                    monsterIds.Add(id);
                }

                foreach (var id in monsterIds)
                {
                    ClearMonster(id);
                }
            }
        }

        private void TickBurnBucket(float deltaTime)
        {
            if (_monsterBurn.Count == 0)
            {
                return;
            }

            var expired = new List<string>();
            foreach (var pair in _monsterBurn)
            {
                var state = pair.Value;
                if (state == null)
                {
                    expired.Add(pair.Key);
                    continue;
                }

                state.TickAccumulator += deltaTime;
                while (state.TickAccumulator >= state.TickIntervalSeconds && state.RemainingSeconds > 0f)
                {
                    state.TickAccumulator -= state.TickIntervalSeconds;
                    MonsterBurnTick?.Invoke(
                        pair.Key,
                        state.SourceWarriorId,
                        state.TickDamage,
                        state.SkillId);
                }

                state.RemainingSeconds -= deltaTime;
                if (state.RemainingSeconds <= 0f)
                {
                    expired.Add(pair.Key);
                }
            }

            for (var i = 0; i < expired.Count; i++)
            {
                _monsterBurn.Remove(expired[i]);
            }
        }

        private static void TickTimedBucket(
            Dictionary<string, TimedStatus> bucket,
            float deltaTime,
            Action<string, string, bool> changed)
        {
            if (bucket.Count == 0)
            {
                return;
            }

            var expired = new List<string>();
            foreach (var pair in bucket)
            {
                if (pair.Value == null || pair.Value.IsHold)
                {
                    continue;
                }

                pair.Value.RemainingSeconds -= deltaTime;
                if (pair.Value.RemainingSeconds <= 0f)
                {
                    expired.Add(pair.Key);
                }
            }

            for (var i = 0; i < expired.Count; i++)
            {
                var id = expired[i];
                if (!bucket.TryGetValue(id, out var state) || state == null)
                {
                    continue;
                }

                var skillId = state.SkillId;
                bucket.Remove(id);
                changed?.Invoke(id, skillId, false);
            }
        }
    }
}
