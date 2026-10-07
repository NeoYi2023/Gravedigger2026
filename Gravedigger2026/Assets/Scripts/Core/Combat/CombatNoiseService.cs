using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Per-battle attack-noise accumulation and one-shot berserk (SPEC_03 §3.12 / D-102).
    /// </summary>
    public sealed class CombatNoiseService
    {
        private readonly ConfigCsvRepository _configs;
        private readonly Dictionary<string, MonsterNoiseState> _states =
            new Dictionary<string, MonsterNoiseState>(StringComparer.Ordinal);

        public CombatNoiseService(ConfigCsvRepository configs)
        {
            _configs = configs;
        }

        /// <summary>runtimeId, new AggroMode, provoke (PassiveChase / StationaryPassive).</summary>
        public event Action<string, AggroMode, bool> BerserkTriggered;

        public void ResetBattle()
        {
            _states.Clear();
        }

        public void RegisterMonster(string runtimeId, MonsterConfigRow config)
        {
            if (string.IsNullOrEmpty(runtimeId) || config == null)
            {
                return;
            }

            _states[runtimeId] = new MonsterNoiseState
            {
                Acc = 0f,
                Threshold = config.BerserkNoiseThreshold,
                HasBerserkMode = config.HasBerserkAggroMode,
                BerserkMode = config.BerserkAggroMode,
                Berserked = false
            };
        }

        public void RegisterMonster(string runtimeId, string monsterId)
        {
            if (string.IsNullOrEmpty(runtimeId) || _configs == null)
            {
                return;
            }

            if (!_configs.TryGetMonster(monsterId, out var row) || row == null)
            {
                return;
            }

            RegisterMonster(runtimeId, row);
        }

        public void UnregisterMonster(string runtimeId)
        {
            if (!string.IsNullOrEmpty(runtimeId))
            {
                _states.Remove(runtimeId);
            }
        }

        public void TryApplyClassPulse(
            DefendCombatWarriorState warrior,
            Vector2 centerXZ,
            IReadOnlyList<MonsterWorldXZ> alive)
        {
            if (warrior == null || _configs == null)
            {
                return;
            }

            if (!_configs.TryGetClass(warrior.ClassId, out var classRow) || classRow == null)
            {
                return;
            }

            ApplyPulse(centerXZ, classRow.AttackNoiseValue, classRow.NoiseRadius, alive);
        }

        public void ApplyPulse(
            Vector2 centerXZ,
            float noiseValue,
            float radius,
            IReadOnlyList<MonsterWorldXZ> alive)
        {
            if (noiseValue <= 0f || alive == null)
            {
                return;
            }

            var radiusSq = radius * radius;
            for (var i = 0; i < alive.Count; i++)
            {
                var snapshot = alive[i];
                if (string.IsNullOrEmpty(snapshot.RuntimeId)
                    || !_states.TryGetValue(snapshot.RuntimeId, out var state)
                    || state == null
                    || state.Berserked)
                {
                    continue;
                }

                var delta = snapshot.PositionXZ - centerXZ;
                if (delta.sqrMagnitude > radiusSq)
                {
                    continue;
                }

                state.Acc += noiseValue;
                if (state.Threshold <= 0f || state.Acc < state.Threshold)
                {
                    continue;
                }

                state.Berserked = true;
                if (!state.HasBerserkMode)
                {
                    continue;
                }

                var provoke = state.BerserkMode == AggroMode.PassiveChase
                              || state.BerserkMode == AggroMode.StationaryPassive;
                BerserkTriggered?.Invoke(snapshot.RuntimeId, state.BerserkMode, provoke);
            }
        }

        private sealed class MonsterNoiseState
        {
            public float Acc;
            public float Threshold;
            public bool HasBerserkMode;
            public AggroMode BerserkMode;
            public bool Berserked;
        }
    }
}
