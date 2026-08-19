using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Pure Defend rules: Prepare / StartBattle / Shield / countdown / wave spawn /
    /// melee + ranged HitConfirm / LossOfControl Rebel / clear victory / LevelFailure (SPEC_03 §3.12 / D-040–D-043).
    /// </summary>
    public sealed class DefendSessionService : IProjectileCombatSession
    {
        /// <summary>Demo fixed stage Exp credited on clear victory (SPEC_03 §3.12 / D-043).</summary>
        /// <summary>Defend victory Demo stage Exp ← CombatConstantConfig DefendVictoryStageExp.</summary>
        public static long DemoStageExperienceReward => CombatRuntimeTuning.DefendVictoryStageExp;

        private DefendGameplayConfigRow _config;
        private IReadOnlyList<WaveSpawnConfigRow> _waveRows = Array.Empty<WaveSpawnConfigRow>();
        private readonly HashSet<int> _firedWaveRowIndices = new HashSet<int>();
        private readonly Dictionary<string, DefendCombatWarriorState> _warriors =
            new Dictionary<string, DefendCombatWarriorState>(StringComparer.Ordinal);
        private readonly Dictionary<string, DefendCombatMonsterState> _monsters =
            new Dictionary<string, DefendCombatMonsterState>(StringComparer.Ordinal);
        private int _monsterSeq;
        private bool _active;
        private DefendPhase _phase = DefendPhase.Prepare;
        private int _shield;
        private int _shieldCap;
        private int _remainingCombatSeconds;
        private float _secondAccumulator;
        private bool _clearVictorySignaled;
        private bool _outcomeSettled;
        private float _lockedLossOfControlDegree;
        private int _lockedLossOfControlTierId;
        private float _lockedTierChance;
        private ConfigCsvRepository _configs;

        public event Action<DefendPhase> PhaseChanged;
        public event Action<int, int> ShieldChanged;
        public event Action<int> RemainingCombatSecondsChanged;
        public event Action<DefendWaveSpawnRequest> WaveSpawnRequested;
        public event Action LevelFailureRequested;
        public event Action ClearVictoryConditionDetected;
        public event Action<long> VictorySettled;
        public event Action<string> WarriorCombatStateChanged;
        public event Action<string> MonsterCombatStateChanged;
        /// <summary>Monster RemainingHp≤0 (runtimeId, killerWarriorId). View NotifyKilled + death knockback.</summary>
        public event Action<string, string> MonsterKilled;

        public bool IsActive => _active;
        public DefendPhase Phase => _phase;
        public int Shield => _shield;
        public int ShieldCap => _shieldCap;
        public int RemainingCombatSeconds => _remainingCombatSeconds;
        public DefendGameplayConfigRow Config => _config;
        public bool IsClearVictoryConditionMet => EvaluateClearVictoryCondition();
        public float LockedLossOfControlDegree => _lockedLossOfControlDegree;
        public int LockedLossOfControlTierId => _lockedLossOfControlTierId;
        public bool OutcomeSettled => _outcomeSettled;
        public int AliveMonsterCount
        {
            get
            {
                var n = 0;
                foreach (var kv in _monsters)
                {
                    if (kv.Value != null && kv.Value.IsAlive)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public int RegisteredMonsterCount => _monsters.Count;
        public bool AreAllWaveRowsFired =>
            _waveRows != null && _waveRows.Count > 0 && _firedWaveRowIndices.Count >= _waveRows.Count;

        public void BeginPrepare(DefendGameplayConfigRow config)
        {
            Stop();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _active = true;
            _phase = DefendPhase.Prepare;
            _shield = 0;
            _shieldCap = 0;
            _remainingCombatSeconds = 0;
            _secondAccumulator = 0f;
            _waveRows = Array.Empty<WaveSpawnConfigRow>();
            _firedWaveRowIndices.Clear();
            _clearVictorySignaled = false;
            _outcomeSettled = false;
            _lockedLossOfControlDegree = 0f;
            _lockedLossOfControlTierId = 0;
            _lockedTierChance = 0f;
            PhaseChanged?.Invoke(_phase);
            ShieldChanged?.Invoke(_shield, _shieldCap);
            RemainingCombatSecondsChanged?.Invoke(_remainingCombatSeconds);
            Debug.Log(
                $"[DefendSession] Prepare Config={config.GameplayConfigId} Map={config.BattleMapId} Wave={config.WaveConfigId} Duration={config.CombatDurationSeconds}s");
        }

        public void Stop()
        {
            _active = false;
            _config = null;
            _phase = DefendPhase.Prepare;
            _shield = 0;
            _shieldCap = 0;
            _remainingCombatSeconds = 0;
            _secondAccumulator = 0f;
            _waveRows = Array.Empty<WaveSpawnConfigRow>();
            _firedWaveRowIndices.Clear();
            _warriors.Clear();
            _monsters.Clear();
            _monsterSeq = 0;
            _clearVictorySignaled = false;
            _outcomeSettled = false;
            _lockedLossOfControlDegree = 0f;
            _lockedLossOfControlTierId = 0;
            _lockedTierChance = 0f;
            _configs = null;
        }

        public bool CanStartBattle(int deployedSoldierCount)
        {
            return _active && _phase == DefendPhase.Prepare && deployedSoldierCount >= 1;
        }

        /// <summary>
        /// Prepare to Combat: init Shield / RemainingCombatSeconds / wave rows and lock LossOfControl Degree/Tier.
        /// Caller deploys units then calls <see cref="ResolveStartBattleRebelRolls"/>.
        /// </summary>
        public bool TryStartBattle(
            int deployedSoldierCount,
            int protagonistMaxHp,
            IReadOnlyList<WaveSpawnConfigRow> waveRows,
            float lossOfControlDegree,
            ConfigCsvRepository configs,
            out string error)
        {
            error = null;
            if (!_active || _config == null)
            {
                error = "Defend session not started";
                return false;
            }

            if (_phase != DefendPhase.Prepare)
            {
                error = "Only Prepare can StartBattle";
                return false;
            }

            if (deployedSoldierCount < 1)
            {
                error = "Need at least 1 deployed warrior";
                return false;
            }

            _shieldCap = Math.Max(0, protagonistMaxHp);
            _shield = _shieldCap;
            _remainingCombatSeconds = Math.Max(0, _config.CombatDurationSeconds);
            _secondAccumulator = 0f;
            _waveRows = waveRows ?? Array.Empty<WaveSpawnConfigRow>();
            _firedWaveRowIndices.Clear();
            _warriors.Clear();
            _monsters.Clear();
            _monsterSeq = 0;
            _clearVictorySignaled = false;
            _outcomeSettled = false;

            _lockedLossOfControlDegree = lossOfControlDegree;
            _lockedLossOfControlTierId = LossOfControlMath.MapTierId(lossOfControlDegree);
            _lockedTierChance = 0f;
            _configs = configs;
            if (_lockedLossOfControlTierId > 0
                && configs != null
                && configs.TryGetLossOfControlTier(_lockedLossOfControlTierId, out var tierRow)
                && tierRow != null)
            {
                _lockedTierChance = LossOfControlMath.ClampChance(tierRow.LossOfControlChance);
            }

            _phase = DefendPhase.Combat;
            PhaseChanged?.Invoke(_phase);
            ShieldChanged?.Invoke(_shield, _shieldCap);
            RemainingCombatSecondsChanged?.Invoke(_remainingCombatSeconds);
            Debug.Log(
                $"[DefendSession] StartBattle Shield={_shield} Remaining={_remainingCombatSeconds}s Deployed={deployedSoldierCount} " +
                $"WaveRows={_waveRows.Count} Degree={_lockedLossOfControlDegree:0.###} Tier={_lockedLossOfControlTierId} " +
                $"TierChance={_lockedTierChance:0.###}");
            ProcessWaveSpawnsForCurrentSecond();
            return true;
        }

        /// <summary>
        /// After warriors are registered: if locked Degree &gt; 0, each soldier rolls FinalLossChance once.
        /// </summary>
        public void ResolveStartBattleRebelRolls(ConfigCsvRepository configs)
        {
            if (!_active || _phase != DefendPhase.Combat)
            {
                return;
            }

            if (_lockedLossOfControlDegree <= 0f || _lockedLossOfControlTierId <= 0)
            {
                Debug.Log("[DefendSession] LossOfControl Degree≤0 — no rebel rolls.");
                return;
            }

            foreach (var kv in _warriors)
            {
                var state = kv.Value;
                if (state == null || state.IsCombatDead || state.IsPermanentDead)
                {
                    continue;
                }

                float raceBonus = 0f;
                float gemBonus = 0f;
                var skillBonus = SoldierSkillGrant.SumLossOfControlChanceBonus(state.SoldierSkills, configs);
                if (configs != null)
                {
                    if (!string.IsNullOrEmpty(state.RaceId)
                        && configs.TryGetRace(state.RaceId, out var raceRow)
                        && raceRow != null)
                    {
                        raceBonus = raceRow.LossOfControlChanceBonus;
                    }

                    if (state.GemIds != null)
                    {
                        for (var g = 0; g < state.GemIds.Count; g++)
                        {
                            if (configs.TryGetGem(state.GemIds[g], out var gemRow) && gemRow != null)
                            {
                                gemBonus += gemRow.LossOfControlChanceBonus;
                            }
                        }
                    }
                }

                var chance = LossOfControlMath.ComputeFinalLossChance(
                    _lockedTierChance,
                    raceBonus,
                    gemBonus,
                    skillBonus);
                var roll = UnityEngine.Random.value;
                var rebel = roll < chance;
                state.IsRebel = rebel;
                Debug.Log(
                    $"[DefendSession] RebelRoll {state.WarriorId} chance={chance:0.###} roll={roll:0.###} " +
                    $"→ {(rebel ? "REBEL" : "loyal")} (Tier={_lockedTierChance:0.###} Race={raceBonus:0.###} " +
                    $"Gem={gemBonus:0.###} Skill={skillBonus:0.###})");
                WarriorCombatStateChanged?.Invoke(state.WarriorId);
            }
        }

        public bool TryRegisterWarrior(
            WarriorInstance warrior,
            ClassConfigRow classRow,
            out DefendCombatWarriorState state,
            out string error)
        {
            state = null;
            error = null;
            if (!_active || _phase != DefendPhase.Combat)
            {
                error = "Not in Combat";
                return false;
            }

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
            var maxHp = WarriorCombatMath.ComputeBattleMaxHp(warrior, battleStats, maxHpMult);
            var remaining = Math.Min(Math.Max(0f, warrior.RemainingHP), maxHp);
            if (remaining <= 0f && maxHp > 0)
            {
                remaining = maxHp;
            }

            state = new DefendCombatWarriorState
            {
                WarriorId = warrior.Id,
                AttackMode = warrior.AttackMode,
                MaxHp = maxHp,
                RemainingHp = remaining,
                NormalAttackPower = WarriorCombatMath.ComputeNormalAttackPower(primary, coeffs),
                AttackSpeed = WarriorCombatMath.ComputeAttackSpeed(battleStats.Agility, coeffs),
                MoveSpeed = Math.Max(0.1f, battleStats.MoveSpeed > 0.01f ? battleStats.MoveSpeed : 3.5f),
                AttackRange = (classRow != null ? Math.Max(0.2f, classRow.AttackRange) : 1.5f)
                    * WarriorVisualModelScale.Resolve(warrior),
                MeleeWindupSeconds = classRow != null ? Math.Max(0f, classRow.MeleeWindupSeconds) : 0.3f,
                RangedProjectileSpeed = classRow != null ? Math.Max(0.1f, classRow.RangedProjectileSpeed) : 10f,
                RangedTimeoutSeconds = classRow != null ? Math.Max(0.1f, classRow.RangedTimeoutSeconds) : 2f,
                HasGems = warrior.GemIds != null && warrior.GemIds.Count > 0,
                IsCombatDead = remaining <= 0f,
                IsPermanentDead = false,
                IsRebel = false,
                RaceId = warrior.RaceId ?? string.Empty,
                GemIds = warrior.GemIds != null ? new List<string>(warrior.GemIds) : new List<string>(),
                SoldierSkills = warrior.SoldierSkills != null
                    ? new List<SoldierSkillEntry>(warrior.SoldierSkills)
                    : new List<SoldierSkillEntry>()
            };

            if (state.IsCombatDead && state.HasGems)
            {
                state.IsPermanentDead = true;
            }

            _warriors[warrior.Id] = state;
            warrior.RemainingHP = state.RemainingHp;
            Debug.Log(
                $"[DefendSession] RegisterWarrior {state.WarriorId} Mode={state.AttackMode} HP={state.RemainingHp:0}/{state.MaxHp} " +
                $"Atk={state.NormalAttackPower:0.##} ASPD={state.AttackSpeed:0.##} Range={state.AttackRange:0.##} " +
                $"ProjSpeed={state.RangedProjectileSpeed:0.##} ProjTimeout={state.RangedTimeoutSeconds:0.##}");
            WarriorCombatStateChanged?.Invoke(state.WarriorId);
            return true;
        }

        public string RegisterMonster(string monsterId, float maxHp)
        {
            if (!_active || _phase != DefendPhase.Combat)
            {
                return null;
            }

            _monsterSeq++;
            var runtimeId = $"M{_monsterSeq}_{monsterId ?? "Unknown"}";
            var state = new DefendCombatMonsterState
            {
                RuntimeId = runtimeId,
                MonsterId = monsterId ?? string.Empty,
                MaxHp = Math.Max(1f, maxHp),
                RemainingHp = Math.Max(1f, maxHp),
                IsAlive = true
            };
            _monsters[runtimeId] = state;
            MonsterCombatStateChanged?.Invoke(runtimeId);
            return runtimeId;
        }

        public bool TryGetWarrior(string warriorId, out DefendCombatWarriorState state)
        {
            return _warriors.TryGetValue(warriorId, out state) && state != null;
        }

        public bool TryGetMonster(string runtimeId, out DefendCombatMonsterState state)
        {
            return _monsters.TryGetValue(runtimeId, out state) && state != null;
        }

        public bool IsWarriorCombatActive(string warriorId)
        {
            return TryGetWarrior(warriorId, out var state)
                   && !state.IsCombatDead
                   && !state.IsPermanentDead
                   && state.RemainingHp > 0f;
        }

        public bool IsMonsterAlive(string runtimeId)
        {
            return TryGetMonster(runtimeId, out var state) && state.IsAlive && state.RemainingHp > 0f;
        }

        /// <summary>IProjectileCombatSession: gates ProjectileView flight/settlement (PM-12 shared contract).</summary>
        public bool IsProjectileCombatActive(string warriorId)
        {
            return _active && _phase == DefendPhase.Combat && IsWarriorCombatActive(warriorId);
        }

        /// <summary>
        /// Melee HitConfirm: caller (View) already finished windup and verified range visually;
        /// rules re-check alive + optional distance from View-supplied inRange flag.
        /// </summary>
        public bool TryConfirmMeleeHit(string warriorId, string monsterRuntimeId, bool stillInRange)
        {
            if (!_active || _phase != DefendPhase.Combat || !stillInRange)
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior))
            {
                return false;
            }

            if (warrior.AttackMode != AttackMode.Melee)
            {
                return false;
            }

            if (!IsMonsterAlive(monsterRuntimeId) || !TryGetMonster(monsterRuntimeId, out var monster))
            {
                return false;
            }

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - warrior.NormalAttackPower);
            Debug.Log(
                $"[DefendSession] MeleeHit {warriorId} -> {monsterRuntimeId} dmg={warrior.NormalAttackPower:0.##} " +
                $"HP={monster.RemainingHp:0}/{monster.MaxHp}");

            if (monster.RemainingHp <= 0f)
            {
                monster.IsAlive = false;
                Debug.Log($"[DefendSession] MonsterDead {monsterRuntimeId} ({monster.MonsterId})");
                MonsterKilled?.Invoke(monsterRuntimeId, warriorId);
            }

            MonsterCombatStateChanged?.Invoke(monsterRuntimeId);
            TrySignalClearVictory();
            return true;
        }

        /// <summary>
        /// Ranged HitConfirm: View reports soft-collision hit; rules settle NormalAttackPower if still alive.
        /// Timeout miss must not call this (no HP change).
        /// </summary>
        public bool TryConfirmRangedHit(string warriorId, string monsterRuntimeId)
        {
            if (!_active || _phase != DefendPhase.Combat)
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior))
            {
                return false;
            }

            if (warrior.AttackMode != AttackMode.Ranged)
            {
                return false;
            }

            if (!IsMonsterAlive(monsterRuntimeId) || !TryGetMonster(monsterRuntimeId, out var monster))
            {
                return false;
            }

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - warrior.NormalAttackPower);
            Debug.Log(
                $"[DefendSession] RangedHit {warriorId} -> {monsterRuntimeId} dmg={warrior.NormalAttackPower:0.##} " +
                $"HP={monster.RemainingHp:0}/{monster.MaxHp}");

            if (monster.RemainingHp <= 0f)
            {
                monster.IsAlive = false;
                Debug.Log($"[DefendSession] MonsterDead {monsterRuntimeId} ({monster.MonsterId})");
                MonsterKilled?.Invoke(monsterRuntimeId, warriorId);
            }

            MonsterCombatStateChanged?.Invoke(monsterRuntimeId);
            TrySignalClearVictory();
            return true;
        }

        /// <summary>
        /// Rebel hits another soldier with NormalAttackPower (scheme D channel).
        /// </summary>
        public bool TryConfirmRebelHitOnWarrior(string attackerWarriorId, string targetWarriorId, bool stillInRange)
        {
            if (!_active || _phase != DefendPhase.Combat || !stillInRange)
            {
                return false;
            }

            if (!IsWarriorCombatActive(attackerWarriorId) || !TryGetWarrior(attackerWarriorId, out var attacker))
            {
                return false;
            }

            if (!attacker.IsRebel)
            {
                return false;
            }

            if (string.Equals(attackerWarriorId, targetWarriorId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsWarriorCombatActive(targetWarriorId) || !TryGetWarrior(targetWarriorId, out var target))
            {
                return false;
            }

            target.RemainingHp = Math.Max(0f, target.RemainingHp - attacker.NormalAttackPower);
            Debug.Log(
                $"[DefendSession] RebelHit {attackerWarriorId} -> warrior {targetWarriorId} dmg={attacker.NormalAttackPower:0.##} " +
                $"HP={target.RemainingHp:0}/{target.MaxHp}");

            if (target.RemainingHp <= 0f)
            {
                EnterWarriorDowned(target);
            }

            WarriorCombatStateChanged?.Invoke(targetWarriorId);
            return true;
        }

        public bool TryApplyMonsterDamageToWarrior(string monsterRuntimeId, string warriorId, float attackPower)
        {
            if (!_active || _phase != DefendPhase.Combat)
            {
                return false;
            }

            if (!IsMonsterAlive(monsterRuntimeId))
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior))
            {
                return false;
            }

            var dmg = Math.Max(0f, attackPower);
            warrior.RemainingHp = Math.Max(0f, warrior.RemainingHp - dmg);
            Debug.Log(
                $"[DefendSession] MonsterHit {monsterRuntimeId} -> {warriorId} dmg={dmg:0.##} " +
                $"HP={warrior.RemainingHp:0}/{warrior.MaxHp}");

            if (warrior.RemainingHp <= 0f)
            {
                EnterWarriorDowned(warrior);
            }

            WarriorCombatStateChanged?.Invoke(warriorId);
            return true;
        }

        public void SyncWarriorRemainingHpToInstance(WarriorInstance instance)
        {
            if (instance == null || !TryGetWarrior(instance.Id, out var state))
            {
                return;
            }

            instance.RemainingHP = state.RemainingHp;
        }

        public void Tick(float deltaTime)
        {
            if (!_active || _phase != DefendPhase.Combat || deltaTime <= 0f)
            {
                return;
            }

            if (_remainingCombatSeconds <= 0)
            {
                TrySignalClearVictory();
                return;
            }

            _secondAccumulator += deltaTime;
            while (_secondAccumulator >= 1f && _remainingCombatSeconds > 0 && _phase == DefendPhase.Combat)
            {
                _secondAccumulator -= 1f;
                _remainingCombatSeconds--;
                RemainingCombatSecondsChanged?.Invoke(_remainingCombatSeconds);
                ProcessWaveSpawnsForCurrentSecond();
                TrySignalClearVictory();
            }
        }

        /// <summary>Enemy/rebel normal attack on BattleProtagonist: Shield -= 1 (ignore AttackPower).</summary>
        public void ApplyProtagonistNormalHit(string sourceTag = null)
        {
            if (!_active || _phase != DefendPhase.Combat)
            {
                return;
            }

            _shield = Math.Max(0, _shield - 1);
            ShieldChanged?.Invoke(_shield, _shieldCap);
            Debug.Log($"[DefendSession] Shield hit by {sourceTag ?? "?"} -> {_shield}/{_shieldCap}");

            if (_shield <= 0)
            {
                EnterLevelFailure();
            }
        }

        private void EnterWarriorDowned(DefendCombatWarriorState warrior)
        {
            if (warrior.HasGems)
            {
                warrior.IsPermanentDead = true;
                warrior.IsCombatDead = true;
                Debug.LogWarning(
                    $"[DefendSession] PermanentDeath (gems) {warrior.WarriorId} — settle on Ended.");
            }
            else
            {
                warrior.IsCombatDead = true;
                Debug.Log($"[DefendSession] CombatDead {warrior.WarriorId} (stop acting)");
            }
        }

        private bool EvaluateClearVictoryCondition()
        {
            if (!_active || _phase != DefendPhase.Combat)
            {
                return false;
            }

            if (_waveRows == null || _waveRows.Count == 0)
            {
                return false;
            }

            if (_firedWaveRowIndices.Count < _waveRows.Count)
            {
                return false;
            }

            foreach (var kv in _monsters)
            {
                if (kv.Value != null && kv.Value.IsAlive)
                {
                    return false;
                }
            }

            // Must have spawned at least one monster for a meaningful clear (Demo).
            return _monsters.Count > 0;
        }

        private void TrySignalClearVictory()
        {
            if (_clearVictorySignaled || !EvaluateClearVictoryCondition())
            {
                return;
            }

            _clearVictorySignaled = true;
            Debug.Log(
                "[DefendSession] ClearVictoryConditionDetected — entering victory Ended (D-043).");
            ClearVictoryConditionDetected?.Invoke();
            EnterVictory();
        }

        private void EnterVictory()
        {
            if (_outcomeSettled || _phase == DefendPhase.Ended)
            {
                return;
            }

            _phase = DefendPhase.Ended;
            PhaseChanged?.Invoke(_phase);
            SettleCombatDeadToPermanentDeath();
            _outcomeSettled = true;
            Debug.Log(
                $"[DefendSession] VictorySettled DemoStageExp=+{DemoStageExperienceReward}");
            VictorySettled?.Invoke(DemoStageExperienceReward);
        }

        private void EnterLevelFailure()
        {
            if (_outcomeSettled || _phase == DefendPhase.Ended)
            {
                return;
            }

            _phase = DefendPhase.Ended;
            PhaseChanged?.Invoke(_phase);
            SettleCombatDeadToPermanentDeath();
            _outcomeSettled = true;
            Debug.LogWarning("[DefendSession] LevelFailure: Shield <= 0 — abort Level, no stage Exp (D-043).");
            LevelFailureRequested?.Invoke();
        }

        private void SettleCombatDeadToPermanentDeath()
        {
            foreach (var kv in _warriors)
            {
                var w = kv.Value;
                if (w == null)
                {
                    continue;
                }

                if (w.IsPermanentDead)
                {
                    continue;
                }

                if (w.IsCombatDead || w.RemainingHp <= 0f)
                {
                    w.IsCombatDead = true;
                    w.IsPermanentDead = true;
                    Debug.Log($"[DefendSession] PermanentDeath settle {w.WarriorId}");
                    WarriorCombatStateChanged?.Invoke(w.WarriorId);
                }
            }
        }

        /// <summary>Warrior ids marked PermanentDead (for pool/formation cleanup).</summary>
        public List<string> CollectPermanentDeadWarriorIds()
        {
            var list = new List<string>();
            foreach (var kv in _warriors)
            {
                if (kv.Value != null && kv.Value.IsPermanentDead)
                {
                    list.Add(kv.Key);
                }
            }

            return list;
        }

        private void ProcessWaveSpawnsForCurrentSecond()
        {
            if (_phase != DefendPhase.Combat || _waveRows == null || _waveRows.Count == 0)
            {
                return;
            }

            var matches = new List<int>();
            for (var i = 0; i < _waveRows.Count; i++)
            {
                if (_firedWaveRowIndices.Contains(i))
                {
                    continue;
                }

                var row = _waveRows[i];
                if (row != null && row.SpawnRemainingSeconds == _remainingCombatSeconds)
                {
                    matches.Add(i);
                }
            }

            matches.Sort((a, b) =>
            {
                var oa = _waveRows[a].SpawnOrder;
                var ob = _waveRows[b].SpawnOrder;
                var cmp = oa.CompareTo(ob);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            for (var m = 0; m < matches.Count; m++)
            {
                var index = matches[m];
                _firedWaveRowIndices.Add(index);
                var row = _waveRows[index];
                var request = new DefendWaveSpawnRequest
                {
                    WaveConfigId = row.WaveConfigId,
                    SpawnOrder = row.SpawnOrder,
                    SpawnRemainingSeconds = row.SpawnRemainingSeconds,
                    MonsterId = row.MonsterId,
                    SpawnCount = Math.Max(1, row.SpawnCount),
                    AppearLocation = row.AppearLocation,
                    SpawnMode = row.SpawnMode,
                    SpawnClockHour = row.SpawnClockHour
                };
                Debug.Log(
                    $"[DefendSession] WaveSpawn Remaining={_remainingCombatSeconds} Order={row.SpawnOrder} Monster={row.MonsterId} x{request.SpawnCount}");
                WaveSpawnRequested?.Invoke(request);
            }

            TrySignalClearVictory();
        }
    }

    public sealed class DefendCombatWarriorState
    {
        public string WarriorId;
        public AttackMode AttackMode;
        public float MaxHp;
        public float RemainingHp;
        public float NormalAttackPower;
        public float AttackSpeed;
        public float MoveSpeed;
        public float AttackRange;
        public float MeleeWindupSeconds;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
        public bool HasGems;
        public bool IsCombatDead;
        public bool IsPermanentDead;
        public bool IsRebel;
        public string RaceId;
        public List<string> GemIds;
        public List<SoldierSkillEntry> SoldierSkills;
        /// <summary>Empty = this Demo has no active Skill_03 on the soldier.</summary>
        public string CastSkillId;
        public int CastSkillLevel;
        /// <summary>Computed SkillCooldown duration (seconds); 0 if no active skill.</summary>
        public float SkillCooldownSeconds;
        public float SkillCdRemaining;
        /// <summary>Last monster runtimeId hit by normal attack (D-073 Skill_04 new-target first hit).</summary>
        public string LastNormalAttackTargetRuntimeId;
        /// <summary>Per-skill internal CD remaining (Skill_05 / Skill_07 / Skill_12); keyed by SkillId.</summary>
        public Dictionary<string, float> SkillInternalCdRemaining;
        /// <summary>Precomputed internal CD duration per SkillId at register (Mode2 formula).</summary>
        public Dictionary<string, float> SkillInternalCooldownSeconds;
        /// <summary>Per-EffectKind stack state (D-073 Skill_09 / StackingOutgoingMulTimed).</summary>
        public Dictionary<string, EffectStackState> EffectStackByKind;
    }

    public sealed class DefendCombatMonsterState
    {
        public string RuntimeId;
        public string MonsterId;
        public float MaxHp;
        public float RemainingHp;
        public bool IsAlive;
    }
}
