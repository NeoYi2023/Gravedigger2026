using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// This-round COC hit settlement (SPEC_03 §3.21 slice 03b, Approach A).
    /// Registers soldiers with WarriorCombatMath and monsters with MonsterConfig.MaxHP.
    /// Destructible obstacles take 1 HP per hit (D-100 slice 02).
    /// Treat-as-target monsters use the same −1 channel (D-100 slice 03b).
    /// Normal attacks only. No experience, shield, rebel, skill burst, or settlement.
    /// </summary>
    public sealed class CocFieldCombatSession : IWarriorMassCombatSession
    {
        private readonly ConfigCsvRepository _configs;
        private readonly Dictionary<string, DefendCombatWarriorState> _warriors =
            new Dictionary<string, DefendCombatWarriorState>(StringComparer.Ordinal);
        private readonly Dictionary<string, MonsterHp> _monsters =
            new Dictionary<string, MonsterHp>(StringComparer.Ordinal);
        private readonly Dictionary<string, MonsterHp> _obstacles =
            new Dictionary<string, MonsterHp>(StringComparer.Ordinal);
        private readonly CombatNoiseService _combatNoise;
        private Func<string, Vector2?> _monsterWorldXZProvider;
        private readonly List<MonsterWorldXZ> _aliveMonstersXZScratch = new List<MonsterWorldXZ>(32);
        private readonly HashSet<string> _returnHomeInvincible =
            new HashSet<string>(StringComparer.Ordinal);

        public const float ObstacleHitDamage = 1f;

        public CocFieldCombatSession(ConfigCsvRepository configs)
        {
            _configs = configs;
            _combatNoise = configs != null ? new CombatNoiseService(configs) : null;
            if (_combatNoise != null)
            {
                _combatNoise.BerserkTriggered += (runtimeId, mode, provoke) =>
                    MonsterBerserkTriggered?.Invoke(runtimeId, mode, provoke);
            }
        }

        public bool IsCombatGameplayActive { get; set; } = true;

        public event Action<string, string, float> MonsterDied;
        /// <summary>Confirmed soldier hit on a monster (runtimeId, damage). Includes the killing blow.</summary>
        public event Action<string, float> MonsterDamageSettled;
        public event Action<string, float, float> ObstacleDamaged;
        public event Action<string, string> ObstacleDestroyed;
        /// <summary>D-102: runtimeId, new AggroMode, provoke.</summary>
        public event Action<string, AggroMode, bool> MonsterBerserkTriggered;

        public bool TryRegisterWarrior(
            WarriorInstance warrior,
            ClassConfigRow classRow,
            out DefendCombatWarriorState state,
            out string error)
        {
            state = null;
            error = null;
            if (warrior == null || string.IsNullOrEmpty(warrior.Id))
            {
                error = "Warrior missing";
                return false;
            }

            if (_warriors.ContainsKey(warrior.Id))
            {
                error = "Warrior already registered";
                return false;
            }

            var battleStats = WarriorCombatMath.ComputeBattleStats(
                warrior,
                WarriorCombatMath.ResolveClassBaseMoveSpeed(classRow));
            var bodyLife = warrior.BodyLife > 0f
                ? warrior.BodyLife
                : WarriorStatMath.ComputeBodyLife(warrior.BaseStats, warrior.EquipStats);
            var coeffDefaults = _configs != null
                ? _configs.GetCombatConvertCoeffDefaults()
                : CombatConvertCoeffs.SafetyDefaults;
            var coeffs = CombatConvertCoeffs.Parse(
                classRow != null ? classRow.CombatConvertCoeffs : null,
                coeffDefaults);
            var primaryKind = classRow != null ? classRow.PrimaryStat : StatKind.Strength;
            var primary = WarriorCombatMath.ResolvePrimary(battleStats, primaryKind);
            var maxHpMult = _configs != null
                ? _configs.GetMaxHpStrengthMult()
                : CombatConvertCoeffs.SafetyMaxHpStrengthMult;
            var maxHp = (float)WarriorStatMath.ComputeMaxHP(bodyLife, battleStats.Strength, maxHpMult);
            var remaining = Mathf.Min(Mathf.Max(0f, warrior.RemainingHP), maxHp);
            if (remaining <= 0f && maxHp > 0f)
            {
                remaining = maxHp;
            }

            state = new DefendCombatWarriorState
            {
                WarriorId = warrior.Id,
                ClassId = classRow != null
                    ? classRow.ClassId ?? string.Empty
                    : warrior.ClassId ?? string.Empty,
                BaseClass = classRow != null ? classRow.BaseClass : BaseClassKind.Unspecified,
                AttackMode = ParabolaCombatRegistration.ResolveAttackMode(warrior, classRow),
                MaxHp = maxHp,
                RemainingHp = remaining,
                NormalAttackPower = WarriorCombatMath.ComputeNormalAttackPower(primary, coeffs),
                AttackSpeed = WarriorCombatMath.ComputeAttackSpeed(battleStats.Agility, coeffs),
                MoveSpeed = Mathf.Max(0.1f, battleStats.MoveSpeed > 0.01f ? battleStats.MoveSpeed : 3.5f),
                AttackRange = (classRow != null ? Mathf.Max(0.1f, classRow.AttackRange) : 1.5f)
                    * WarriorVisualModelScale.Resolve(warrior),
                MeleeWindupSeconds = classRow != null ? Mathf.Max(0f, classRow.MeleeWindupSeconds) : 0.3f,
                RangedWindupHoldFrame = classRow != null ? Mathf.Max(0, classRow.RangedWindupHoldFrame) : 0,
                NormalAttackAnims = classRow != null ? classRow.NormalAttackAnims ?? string.Empty : string.Empty,
                RangedProjectileSpeed = classRow != null ? Mathf.Max(0.1f, classRow.RangedProjectileSpeed) : 10f,
                RangedTimeoutSeconds = classRow != null ? Mathf.Max(0.1f, classRow.RangedTimeoutSeconds) : 2f,
                HasGems = warrior.GemIds != null && warrior.GemIds.Count > 0,
                IsCombatDead = remaining <= 0f,
                IsPermanentDead = false,
                IsRebel = false,
                RaceId = warrior.RaceId ?? string.Empty,
                GemIds = warrior.GemIds != null ? new List<string>(warrior.GemIds) : new List<string>(),
                SoldierSkills = warrior.SoldierSkills != null
                    ? new List<SoldierSkillEntry>(warrior.SoldierSkills)
                    : new List<SoldierSkillEntry>(),
                CastSkillId = string.Empty,
                CastSkillLevel = 0,
                SkillCooldownSeconds = 0f,
                SkillCdRemaining = 0f,
                SkillInternalCdRemaining = new Dictionary<string, float>(StringComparer.Ordinal),
                SkillInternalCooldownSeconds = new Dictionary<string, float>(StringComparer.Ordinal),
                EffectStackByKind = new Dictionary<string, EffectStackState>(StringComparer.Ordinal)
            };
            ParabolaCombatRegistration.CopyParams(state, classRow);
            _warriors.Add(warrior.Id, state);
            return true;
        }

        public void ForgetWarrior(string warriorId)
        {
            if (!string.IsNullOrEmpty(warriorId))
            {
                _warriors.Remove(warriorId);
            }
        }

        public void TryRegisterMonster(string runtimeId, string monsterId, float maxHp)
        {
            if (string.IsNullOrEmpty(runtimeId) || _monsters.ContainsKey(runtimeId))
            {
                return;
            }

            var hp = Mathf.Max(1f, maxHp);
            _monsters.Add(runtimeId, new MonsterHp(hp, hp));
            _combatNoise?.RegisterMonster(runtimeId, monsterId);
        }

        public void TryRegisterObstacle(string runtimeId, float maxHp)
        {
            if (string.IsNullOrEmpty(runtimeId) || _obstacles.ContainsKey(runtimeId))
            {
                return;
            }

            var hp = Mathf.Max(1f, maxHp);
            _obstacles.Add(runtimeId, new MonsterHp(hp, hp));
        }

        public bool IsObstacleTargetable(string runtimeId)
        {
            return _obstacles.TryGetValue(runtimeId, out var hp) && hp.Remaining > 0f;
        }

        public bool TryApplyMonsterHit(string monsterRuntimeId, string warriorId, float attackPower)
        {
            if (!IsCombatGameplayActive || !IsMonsterAlive(monsterRuntimeId))
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior) || warrior == null)
            {
                return false;
            }

            var dmg = Mathf.Max(0f, attackPower);
            warrior.RemainingHp = Mathf.Max(0f, warrior.RemainingHp - dmg);
            if (warrior.RemainingHp <= 0f)
            {
                warrior.IsCombatDead = true;
            }

            return true;
        }

        /// <summary>Monster treat-as-target hit: 1 HP, not <c>AttackPower</c> (D-100 slice 03b).</summary>
        public bool TryApplyMonsterObstacleHit(string monsterRuntimeId, string obstacleRuntimeId)
        {
            if (!IsCombatGameplayActive || !IsMonsterAlive(monsterRuntimeId))
            {
                return false;
            }

            if (!_obstacles.TryGetValue(obstacleRuntimeId, out var hp) || hp.Remaining <= 0f)
            {
                return false;
            }

            hp.Remaining = Mathf.Max(0f, hp.Remaining - ObstacleHitDamage);
            _obstacles[obstacleRuntimeId] = hp;
            ObstacleDamaged?.Invoke(obstacleRuntimeId, hp.Remaining, hp.Max);
            if (hp.Remaining <= 0f)
            {
                ObstacleDestroyed?.Invoke(obstacleRuntimeId, monsterRuntimeId);
            }

            return true;
        }

        public bool IsWarriorCombatActive(string warriorId)
        {
            return TryGetWarrior(warriorId, out var state) &&
                   state != null &&
                   !state.IsCombatDead &&
                   !state.IsPermanentDead &&
                   state.RemainingHp > 0f;
        }

        public bool TryGetWarrior(string warriorId, out DefendCombatWarriorState state)
        {
            state = null;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            return _warriors.TryGetValue(warriorId, out state) && state != null;
        }

        public bool IsMonsterTargetable(string runtimeId)
        {
            return IsMonsterAlive(runtimeId);
        }

        public bool IsMonsterAlive(string monsterRuntimeId)
        {
            return _monsters.TryGetValue(monsterRuntimeId, out var hp) && hp.Remaining > 0f;
        }

        public bool IsProjectileCombatActive(string warriorId)
        {
            return IsCombatGameplayActive && IsWarriorCombatActive(warriorId);
        }

        public bool TryConfirmMeleeHit(string warriorId, string monsterRuntimeId, bool stillInRange)
        {
            if (!stillInRange || !TryGetWarrior(warriorId, out var warrior) || warrior == null)
            {
                return false;
            }

            if (warrior.AttackMode != AttackMode.Melee)
            {
                return false;
            }

            return SettleHit(warrior, monsterRuntimeId);
        }

        public bool TryConfirmParabolaMeleeHit(string warriorId, string monsterRuntimeId, bool stillInRange)
        {
            if (!stillInRange || !TryGetWarrior(warriorId, out var warrior) || warrior == null)
            {
                return false;
            }

            return SettleHit(warrior, monsterRuntimeId);
        }

        public bool TryConfirmRangedHit(string warriorId, string monsterRuntimeId)
        {
            if (!TryGetWarrior(warriorId, out var warrior) || warrior == null)
            {
                return false;
            }

            return SettleHit(warrior, monsterRuntimeId);
        }

        public bool TryAcquireWarriorTarget(
            string warriorId,
            Vector2 warriorPositionXZ,
            float warriorBodyRadius,
            IReadOnlyList<MonsterWorldXZ> candidates,
            Func<Vector2, float, Vector2?> sampleWalkableXZ,
            out string overrideTargetId,
            out Vector2 teleportLandingXZ)
        {
            overrideTargetId = null;
            teleportLandingXZ = default;
            return false;
        }

        public bool TryCommitSkillBurst(string warriorId, out int burstHitCount)
        {
            burstHitCount = 0;
            return false;
        }

        public bool TryGetSkillCooldownRemaining(string warriorId, out float remaining)
        {
            remaining = 0f;
            return false;
        }

        /// <summary>COC attack-stuck return. Hits are rejected until the stage clears the flag.</summary>
        public void SetReturnHomeInvincible(string monsterRuntimeId, bool on)
        {
            if (string.IsNullOrEmpty(monsterRuntimeId))
            {
                return;
            }

            if (on)
            {
                _returnHomeInvincible.Add(monsterRuntimeId);
            }
            else
            {
                _returnHomeInvincible.Remove(monsterRuntimeId);
            }
        }

        public void Clear()
        {
            _warriors.Clear();
            _monsters.Clear();
            _obstacles.Clear();
            _aliveMonstersXZScratch.Clear();
            _returnHomeInvincible.Clear();
            _combatNoise?.ResetBattle();
            IsCombatGameplayActive = false;
        }

        public void SetMonsterWorldXZProvider(Func<string, Vector2?> provider)
        {
            _monsterWorldXZProvider = provider;
        }

        private bool SettleHit(DefendCombatWarriorState warrior, string targetRuntimeId)
        {
            if (_obstacles.ContainsKey(targetRuntimeId))
            {
                return SettleObstacleDamage(warrior, targetRuntimeId);
            }

            return SettleMonsterDamage(warrior, targetRuntimeId);
        }

        private bool SettleObstacleDamage(DefendCombatWarriorState warrior, string obstacleRuntimeId)
        {
            if (!IsCombatGameplayActive || warrior == null || !IsWarriorCombatActive(warrior.WarriorId))
            {
                return false;
            }

            if (!_obstacles.TryGetValue(obstacleRuntimeId, out var hp) || hp.Remaining <= 0f)
            {
                return false;
            }

            hp.Remaining = Mathf.Max(0f, hp.Remaining - ObstacleHitDamage);
            _obstacles[obstacleRuntimeId] = hp;
            ObstacleDamaged?.Invoke(obstacleRuntimeId, hp.Remaining, hp.Max);
            if (hp.Remaining <= 0f)
            {
                ObstacleDestroyed?.Invoke(obstacleRuntimeId, warrior.WarriorId);
            }

            return true;
        }

        private bool SettleMonsterDamage(DefendCombatWarriorState warrior, string monsterRuntimeId)
        {
            if (!string.IsNullOrEmpty(monsterRuntimeId) &&
                _returnHomeInvincible.Contains(monsterRuntimeId))
            {
                return false;
            }

            if (!IsCombatGameplayActive || warrior == null || !IsWarriorCombatActive(warrior.WarriorId))
            {
                return false;
            }

            if (!_monsters.TryGetValue(monsterRuntimeId, out var hp) || hp.Remaining <= 0f)
            {
                return false;
            }

            var dmg = Mathf.Max(0f, warrior.NormalAttackPower);
            hp.Remaining = Mathf.Max(0f, hp.Remaining - dmg);
            _monsters[monsterRuntimeId] = hp;
            TryApplyClassAttackNoise(warrior, monsterRuntimeId);
            MonsterDamageSettled?.Invoke(monsterRuntimeId, dmg);
            if (hp.Remaining <= 0f)
            {
                MonsterDied?.Invoke(monsterRuntimeId, warrior.WarriorId, dmg);
            }

            return true;
        }

        private void TryApplyClassAttackNoise(DefendCombatWarriorState warrior, string monsterRuntimeId)
        {
            if (_combatNoise == null || warrior == null)
            {
                return;
            }

            if (!TryResolveMonsterWorldXZ(monsterRuntimeId, out var center))
            {
                return;
            }

            FillAliveMonstersXZScratch();
            _combatNoise.TryApplyClassPulse(warrior, center, _aliveMonstersXZScratch);
        }

        private bool TryResolveMonsterWorldXZ(string monsterRuntimeId, out Vector2 positionXZ)
        {
            positionXZ = default;
            if (string.IsNullOrEmpty(monsterRuntimeId) || _monsterWorldXZProvider == null)
            {
                return false;
            }

            var maybe = _monsterWorldXZProvider(monsterRuntimeId);
            if (!maybe.HasValue)
            {
                return false;
            }

            positionXZ = maybe.Value;
            return true;
        }

        private void FillAliveMonstersXZScratch()
        {
            _aliveMonstersXZScratch.Clear();
            if (_monsterWorldXZProvider == null)
            {
                return;
            }

            foreach (var pair in _monsters)
            {
                if (pair.Value.Remaining <= 0f)
                {
                    continue;
                }

                if (!TryResolveMonsterWorldXZ(pair.Key, out var xz))
                {
                    continue;
                }

                _aliveMonstersXZScratch.Add(new MonsterWorldXZ(pair.Key, xz));
            }
        }

        private struct MonsterHp
        {
            public float Max;
            public float Remaining;

            public MonsterHp(float max, float remaining)
            {
                Max = max;
                Remaining = remaining;
            }
        }
    }
}
