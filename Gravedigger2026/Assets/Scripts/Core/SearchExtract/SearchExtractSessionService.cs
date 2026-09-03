using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.SearchExtract
{
    /// <summary>
    /// SearchExtract stage rules (SE-03 Approach A). Prepare→Combat, StartBattle ≥1,
    /// gather activation + countdown (SE-04), formation relocate flag (SE-05),
    /// directional wave spawn (SE-06), point success + UI-032 (SE-07),
    /// multi-point advance + Leave→Ended (SE-08), loyal wipe → LevelFailure (SE-09).
    /// Does not touch PushMapSessionService Capture.
    /// D-074 Approach B: implements IMonsterDeathSkillHost (shared MonsterDeathSkillService).
    /// </summary>
    public sealed class SearchExtractSessionService : IWarriorMassCombatSession, IMonsterDeathSkillHost
    {
        public const string PointSuccessInvincibleSkillId = "SearchExtractPointSuccess";

        private bool _active;
        private bool _outcomeSettled;
        private bool _currentPointActivated;
        private bool _currentPointSpawnStopped;
        private bool _awaitingPointDecision;
        private bool _formationRelocateActive;
        private float _gatherCountdownRemaining;
        private float _waveElapsedSinceActivation;
        private readonly List<WaveRecipeRuntimeState> _waveRecipeStates =
            new List<WaveRecipeRuntimeState>();
        private ConfigCsvRepository _configs;
        private readonly CombatStatusService _combatStatus = new CombatStatusService();
        private readonly MonsterDeathSkillService _monsterDeathSkills = new MonsterDeathSkillService();
        private readonly List<int> _gatherOrders = new List<int>();
        private readonly HashSet<int> _completedGatherOrders = new HashSet<int>();
        private readonly List<SearchExtractWaveSpawnConfigRow> _allWaveRows =
            new List<SearchExtractWaveSpawnConfigRow>();
        private readonly List<SearchExtractWaveSpawnConfigRow> _currentPointWaveRows =
            new List<SearchExtractWaveSpawnConfigRow>();
        private readonly Dictionary<string, DefendCombatWarriorState> _warriors =
            new Dictionary<string, DefendCombatWarriorState>(StringComparer.Ordinal);
        private readonly Dictionary<string, DefendCombatMonsterState> _monsters =
            new Dictionary<string, DefendCombatMonsterState>(StringComparer.Ordinal);
        private readonly List<string> _clearScratch = new List<string>(32);

        public event Action<SearchExtractPhase> PhaseChanged;
        public event Action GatherPointActivated;
        public event Action<int> GatherCountdownSecondsChanged;
        public event Action<SearchExtractSpawnRequest> SpawnRequested;
        public event Action<string, float> MonsterDamageSettled;
        public event Action<string, string, float, string> MonsterKilled;
        public event Action<string, string, float, string> MonsterEnteredCombatDead;
        public event Action<string, float> MonsterReviveStarted;
        public event Action<string> MonsterRevived;
        public event Action<string, string, bool> MonsterInvincibleChanged;
        public event Action<string, float> WarriorDamageSettled;
        public event Action<string> WarriorCombatDead;
        public event Action<SearchExtractPointDecisionInfo> PointSucceeded;
        public event Action PointContinueRequested;
        public event Action PointLeaveRequested;
        /// <summary>Loyal wipe during active gather → whole-Level LevelFailure (no stage Exp).</summary>
        public event Action LevelFailureRequested;

        public SearchExtractGameplayConfigRow Config { get; private set; }
        public SearchExtractPhase Phase { get; private set; } = SearchExtractPhase.Prepare;
        public bool IsActive => _active;
        public int GatherPointCount { get; private set; }
        public int CurrentGatherOrder { get; private set; }
        public bool IsCurrentPointActivated => _currentPointActivated;
        public bool IsCurrentPointSpawnEligible =>
            _currentPointActivated && !_currentPointSpawnStopped && !_awaitingPointDecision;
        public bool IsAwaitingPointDecision => _awaitingPointDecision;
        public bool IsFormationRelocateActive => _formationRelocateActive;
        public bool IsLastGatherPoint =>
            _gatherOrders.Count > 0
            && CurrentGatherOrder == _gatherOrders[_gatherOrders.Count - 1];
        public float GatherCountdownRemaining => _gatherCountdownRemaining;
        public int GatherCountdownRemainingSeconds =>
            _currentPointActivated ? Mathf.CeilToInt(Mathf.Max(0f, _gatherCountdownRemaining)) : 0;
        public float LockedLossOfControlDegree { get; private set; }
        public int LockedLossOfControlTierId { get; private set; }
        public string LevelId { get; private set; }
        public string GameplayOptionId { get; private set; }

        public IReadOnlyList<int> GatherOrders => _gatherOrders;

        public bool IsGatherOrderCompleted(int order) => _completedGatherOrders.Contains(order);

        public bool IsCombatGameplayActive =>
            _active && Phase == SearchExtractPhase.Combat;

        public void BindCombatConfigs(ConfigCsvRepository configs)
        {
            _configs = configs;
        }

        public void BindWaveRows(IReadOnlyList<SearchExtractWaveSpawnConfigRow> rows)
        {
            _allWaveRows.Clear();
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    _allWaveRows.Add(rows[i]);
                }
            }
        }

        public void BeginPrepare(
            SearchExtractGameplayConfigRow config,
            int configuredGatherPointCount,
            string levelId,
            string gameplayOptionId)
        {
            // Stop() clears runtime state and would null _configs; BindCombatConfigs must
            // survive Prepare so RegisterMonster can parse MonsterConfig.Skills (D-074).
            var retainedConfigs = _configs;
            Stop();
            _configs = retainedConfigs;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            LevelId = levelId ?? string.Empty;
            GameplayOptionId = gameplayOptionId ?? string.Empty;
            GatherPointCount = Mathf.Max(0, configuredGatherPointCount);
            _completedGatherOrders.Clear();
            _active = true;
            Phase = SearchExtractPhase.Prepare;
            PhaseChanged?.Invoke(Phase);
            Debug.Log(
                $"[SearchExtractSession] Prepare Level={LevelId} Option={GameplayOptionId} " +
                $"Config={config.GameplayConfigId} Map={config.MapId} N={GatherPointCount} " +
                $"Countdown={config.GatherCountdownSeconds} Waves={_allWaveRows.Count} " +
                $"ConfigsBound={_configs != null}");
        }

        /// <summary>
        /// Bind map ObjectiveOrder list (ascending). Runtime N = min(configured, authored).
        /// Map shortfall logs Warning (SPEC_03 §3.19 / SPEC_04 §9.31).
        /// </summary>
        public void BindGatherOrders(IReadOnlyList<int> authoredOrdersAscending)
        {
            _gatherOrders.Clear();
            CurrentGatherOrder = 0;
            if (authoredOrdersAscending == null || authoredOrdersAscending.Count == 0)
            {
                GatherPointCount = 0;
                Debug.LogWarning("[SearchExtractSession] Map has no ObjectivePoint — N=0.");
                return;
            }

            var configured = GatherPointCount < 1 ? authoredOrdersAscending.Count : GatherPointCount;
            if (authoredOrdersAscending.Count < configured)
            {
                Debug.LogWarning(
                    $"[SearchExtractSession] Map objectives={authoredOrdersAscending.Count} < N={configured}; using actual count.");
                configured = authoredOrdersAscending.Count;
            }

            GatherPointCount = configured;
            for (var i = 0; i < configured; i++)
            {
                _gatherOrders.Add(authoredOrdersAscending[i]);
            }

            CurrentGatherOrder = _gatherOrders[0];
            Debug.Log(
                $"[SearchExtractSession] Gather N={GatherPointCount} CurrentOrder={CurrentGatherOrder} " +
                $"Orders={string.Join(",", _gatherOrders)}");
        }

        public bool TryStartBattle(int deployedSoldierCount, float lossOfControlDegree, out string error)
        {
            error = null;
            if (!_active || Config == null)
            {
                error = "SearchExtract session not started";
                return false;
            }

            if (Phase != SearchExtractPhase.Prepare)
            {
                error = "Only Prepare can StartBattle";
                return false;
            }

            if (deployedSoldierCount < 1)
            {
                error = "需要至少上阵 1 名士兵";
                return false;
            }

            LockedLossOfControlDegree = lossOfControlDegree;
            LockedLossOfControlTierId = LossOfControlMath.MapTierId(lossOfControlDegree);
            ResetGatherCountdownState();
            ResetWaveSpawnState();
            BindDeathSkillHost();
            Phase = SearchExtractPhase.Combat;
            PhaseChanged?.Invoke(Phase);
            Debug.Log(
                $"[SearchExtractSession] StartBattle Level={LevelId} Option={GameplayOptionId} " +
                $"Deployed={deployedSoldierCount} N={GatherPointCount} CurrentOrder={CurrentGatherOrder} " +
                $"Degree={LockedLossOfControlDegree:0.###} Tier={LockedLossOfControlTierId}");
            return true;
        }

        /// <summary>
        /// First loyal entry into current CaptureZone activates gather countdown (SPEC_03 §3.19).
        /// Idempotent while the point stays active and incomplete.
        /// </summary>
        public bool TryActivateGatherPoint()
        {
            if (!_active || Phase != SearchExtractPhase.Combat || Config == null || CurrentGatherOrder <= 0)
            {
                return false;
            }

            if (_currentPointActivated || _awaitingPointDecision)
            {
                return false;
            }

            _currentPointActivated = true;
            _formationRelocateActive = true;
            _gatherCountdownRemaining = Mathf.Max(0.01f, Config.GatherCountdownSeconds);
            LoadCurrentPointWaveRows();
            ResetWaveSpawnState();
            var seconds = GatherCountdownRemainingSeconds;
            GatherCountdownSecondsChanged?.Invoke(seconds);
            Debug.Log(
                $"[SearchExtractSession] GatherPointActivated Order={CurrentGatherOrder} " +
                $"Countdown={_gatherCountdownRemaining:0.###}s Recipes={_waveRecipeStates.Count}");
            GatherPointActivated?.Invoke();
            return true;
        }

        /// <summary>Decrements gather countdown after activation; zero → TryCompleteGatherPoint.</summary>
        public void TickGatherCountdown(float deltaTime)
        {
            if (!_active
                || Phase != SearchExtractPhase.Combat
                || !_currentPointActivated
                || _awaitingPointDecision)
            {
                return;
            }

            if (_gatherCountdownRemaining <= 0f)
            {
                return;
            }

            var prevSeconds = GatherCountdownRemainingSeconds;
            _gatherCountdownRemaining = Mathf.Max(0f, _gatherCountdownRemaining - deltaTime);
            var newSeconds = GatherCountdownRemainingSeconds;
            if (newSeconds != prevSeconds)
            {
                GatherCountdownSecondsChanged?.Invoke(newSeconds);
                Debug.Log(
                    $"[SearchExtractSession] GatherCountdown Order={CurrentGatherOrder} Remaining={newSeconds}s");
            }

            if (_gatherCountdownRemaining <= 0f && prevSeconds > 0)
            {
                StopCurrentPointSpawns();
                if (!TryCompleteGatherPoint())
                {
                    TryEvaluateLoyalWipe();
                }
            }
        }

        public void TickCombatStatus(float deltaTime)
        {
            if (!_active || Phase != SearchExtractPhase.Combat)
            {
                return;
            }

            _combatStatus.Tick(deltaTime);
            TickMonsterRevive(deltaTime);
        }

        /// <summary>
        /// Point success: invincible hold + stop spawn + rules-clear living monsters + UI-032.
        /// Point loot is credited by the presentation layer (SE-08 Approach A).
        /// </summary>
        public bool TryCompleteGatherPoint()
        {
            if (!_active
                || Phase != SearchExtractPhase.Combat
                || !_currentPointActivated
                || _awaitingPointDecision)
            {
                return false;
            }

            if (CountLivingLoyalWarriors() < 1)
            {
                return false;
            }

            StopCurrentPointSpawns();
            ApplyPointSuccessInvincible();
            ClearLivingMonstersRules();
            _awaitingPointDecision = true;
            _formationRelocateActive = false;
            _completedGatherOrders.Add(CurrentGatherOrder);

            var info = new SearchExtractPointDecisionInfo
            {
                GatherPointOrder = CurrentGatherOrder,
                GatherPointCount = GatherPointCount,
                IsLastPoint = IsLastGatherPoint,
                ShowContinue = !IsLastGatherPoint
            };

            Debug.Log(
                $"[SearchExtractSession] PointSucceeded Order={info.GatherPointOrder}/{info.GatherPointCount} " +
                $"Last={info.IsLastPoint} ShowContinue={info.ShowContinue}");
            PointSucceeded?.Invoke(info);
            return true;
        }

        /// <summary>
        /// Continue: drop invincible, advance CurrentGatherOrder, reset activation
        /// (next point still requires zone enter). Does not revive CombatDead.
        /// </summary>
        public bool TryContinueAfterPointSuccess()
        {
            if (!_active || Phase != SearchExtractPhase.Combat || !_awaitingPointDecision)
            {
                return false;
            }

            if (IsLastGatherPoint)
            {
                Debug.LogWarning(
                    $"[SearchExtractSession] Continue ignored — already last Order={CurrentGatherOrder}");
                return false;
            }

            var completedOrder = CurrentGatherOrder;
            _completedGatherOrders.Add(completedOrder);
            ClearPointSuccessInvincible();

            var idx = _gatherOrders.IndexOf(completedOrder);
            if (idx < 0 || idx + 1 >= _gatherOrders.Count)
            {
                Debug.LogWarning(
                    $"[SearchExtractSession] Continue failed — no next Order after {completedOrder}");
                _awaitingPointDecision = false;
                return false;
            }

            CurrentGatherOrder = _gatherOrders[idx + 1];
            ResetGatherCountdownState();
            ResetWaveSpawnState();
            _currentPointWaveRows.Clear();
            _formationRelocateActive = true;

            Debug.Log(
                $"[SearchExtractSession] Continue after point Order={completedOrder} → " +
                $"CurrentOrder={CurrentGatherOrder} (invincible cleared; re-enter required)");
            PointContinueRequested?.Invoke();
            return true;
        }

        /// <summary>Leave: drop invincible → Ended; presentation credits StageExp + TryAdvanceStage.</summary>
        public bool TryLeaveAfterPointSuccess()
        {
            if (!_active || _outcomeSettled || !_awaitingPointDecision)
            {
                return false;
            }

            var leaveOrder = CurrentGatherOrder;
            _completedGatherOrders.Add(leaveOrder);
            ClearPointSuccessInvincible();
            _awaitingPointDecision = false;
            _formationRelocateActive = false;
            StopCurrentPointSpawns();
            _outcomeSettled = true;

            Phase = SearchExtractPhase.Ended;
            PhaseChanged?.Invoke(Phase);
            Debug.Log(
                $"[SearchExtractSession] Leave after point Order={leaveOrder} → Ended " +
                $"(completed={_completedGatherOrders.Count}/{GatherPointCount})");
            PointLeaveRequested?.Invoke();
            return true;
        }

        /// <summary>
        /// LevelFailure when gather is active (activated; countdown or UI-032 pending)
        /// and no living loyal remains. SPEC_03 §3.19 / SE-09 Approach A.
        /// </summary>
        public void TryEvaluateLoyalWipe()
        {
            if (!_active
                || _outcomeSettled
                || Phase != SearchExtractPhase.Combat
                || !_currentPointActivated
                || _warriors.Count < 1)
            {
                return;
            }

            if (CountLivingLoyalWarriors() > 0)
            {
                return;
            }

            RequestLevelFailure("我方士兵全灭");
        }

        /// <summary>Sync View rebel into rules; may trigger loyal wipe LevelFailure.</summary>
        public void SetWarriorRebel(string warriorId, bool isRebel)
        {
            if (!_active || Phase != SearchExtractPhase.Combat || string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            if (!_warriors.TryGetValue(warriorId, out var state) || state == null)
            {
                return;
            }

            state.IsRebel = isRebel;
            if (isRebel)
            {
                TryEvaluateLoyalWipe();
            }
        }

        /// <summary>Active-gather loyal wipe → LevelFailure (no stage Exp; §3.19).</summary>
        public void RequestLevelFailure(string reason = null)
        {
            if (!_active || _outcomeSettled)
            {
                return;
            }

            _outcomeSettled = true;
            _awaitingPointDecision = false;
            _formationRelocateActive = false;
            StopCurrentPointSpawns();
            Phase = SearchExtractPhase.Ended;
            PhaseChanged?.Invoke(Phase);
            Debug.LogWarning(
                $"[SearchExtractSession] LevelFailure" +
                $"{(string.IsNullOrEmpty(reason) ? string.Empty : ": " + reason)} " +
                $"Order={CurrentGatherOrder} — AbortLevel, no stage Exp, no warehouse clawback.");
            LevelFailureRequested?.Invoke();
        }

        public bool IsWarriorInvincible(string warriorId)
        {
            return _combatStatus.IsWarriorInvincible(warriorId);
        }

        /// <summary>
        /// Per-row spawn recipes: FirstWaveDelay → optional Interval × RepeatSpawnCount (SPEC_04 §9.33).
        /// Only runs while the current point is activated and spawn-eligible.
        /// </summary>
        public void TickWaveSpawn(float deltaTime)
        {
            if (!_active
                || Phase != SearchExtractPhase.Combat
                || !_currentPointActivated
                || _currentPointSpawnStopped
                || deltaTime <= 0f)
            {
                return;
            }

            if (_waveRecipeStates.Count == 0)
            {
                return;
            }

            _waveElapsedSinceActivation += deltaTime;
            var firedAny = false;
            do
            {
                firedAny = false;
                for (var i = 0; i < _waveRecipeStates.Count; i++)
                {
                    var state = _waveRecipeStates[i];
                    if (state == null || state.IsComplete)
                    {
                        continue;
                    }

                    if (_waveElapsedSinceActivation + 1e-4f < state.NextDueElapsed)
                    {
                        continue;
                    }

                    FireWaveSpawn(state.Row);
                    firedAny = true;

                    if (!state.FirstFired)
                    {
                        state.FirstFired = true;
                        if (state.RepeatsLeft <= 0)
                        {
                            state.IsComplete = true;
                            continue;
                        }

                        state.NextDueElapsed =
                            state.NextDueElapsed + Mathf.Max(0f, state.Row.WaveIntervalSeconds);
                        continue;
                    }

                    state.RepeatsLeft--;
                    if (state.RepeatsLeft <= 0)
                    {
                        state.IsComplete = true;
                        continue;
                    }

                    state.NextDueElapsed =
                        state.NextDueElapsed + Mathf.Max(0f, state.Row.WaveIntervalSeconds);
                }
            }
            while (firedAny && !_currentPointSpawnStopped);
        }

        /// <summary>Stop new spawns for the current gather point (point success or countdown elapsed).</summary>
        public void StopCurrentPointSpawns()
        {
            if (_currentPointSpawnStopped)
            {
                return;
            }

            _currentPointSpawnStopped = true;
            Debug.Log($"[SearchExtractSession] Point spawn stopped Order={CurrentGatherOrder}");
        }

        public void Stop()
        {
            _active = false;
            _outcomeSettled = false;
            Config = null;
            Phase = SearchExtractPhase.Prepare;
            ResetGatherCountdownState();
            _allWaveRows.Clear();
            _currentPointWaveRows.Clear();
            ResetWaveSpawnState();
            _completedGatherOrders.Clear();
            _gatherOrders.Clear();
            _warriors.Clear();
            _monsters.Clear();
            UnbindDeathSkillHost();
            _combatStatus.ClearAll();
            GatherPointCount = 0;
            CurrentGatherOrder = 0;
            LockedLossOfControlDegree = 0f;
            LockedLossOfControlTierId = 0;
            LevelId = null;
            GameplayOptionId = null;
            _configs = null;
        }

        public bool TryRegisterWarrior(
            WarriorInstance warrior,
            ClassConfigRow classRow,
            out DefendCombatWarriorState state,
            out string error)
        {
            state = null;
            error = null;
            if (!IsCombatGameplayActive)
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
            var maxHp = WarriorStatMath.ComputeMaxHP(bodyLife, battleStats.Strength, maxHpMult);
            var remaining = Math.Min(Math.Max(0f, warrior.RemainingHP), maxHp);
            if (remaining <= 0f && maxHp > 0f)
            {
                remaining = maxHp;
            }

            state = new DefendCombatWarriorState
            {
                WarriorId = warrior.Id,
                BaseClass = classRow != null ? classRow.BaseClass : BaseClassKind.Unspecified,
                AttackMode = warrior.AttackMode,
                MaxHp = maxHp,
                RemainingHp = remaining,
                NormalAttackPower = WarriorCombatMath.ComputeNormalAttackPower(primary, coeffs),
                AttackSpeed = WarriorCombatMath.ComputeAttackSpeed(battleStats.Agility, coeffs),
                MoveSpeed = Math.Max(0.1f, battleStats.MoveSpeed > 0.01f ? battleStats.MoveSpeed : 3.5f),
                AttackRange = (classRow != null ? Math.Max(0.1f, classRow.AttackRange) : 1.5f)
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
                    : new List<SoldierSkillEntry>(),
                CastSkillId = string.Empty,
                CastSkillLevel = 0,
                SkillCooldownSeconds = 0f,
                SkillCdRemaining = 0f,
                SkillInternalCdRemaining = new Dictionary<string, float>(StringComparer.Ordinal),
                SkillInternalCooldownSeconds = new Dictionary<string, float>(StringComparer.Ordinal),
                EffectStackByKind = new Dictionary<string, EffectStackState>(StringComparer.Ordinal)
            };

            _warriors[warrior.Id] = state;
            warrior.RemainingHP = state.RemainingHp;
            Debug.Log(
                $"[SearchExtractSession] RegisterWarrior {state.WarriorId} HP={state.RemainingHp:0}/{state.MaxHp} " +
                $"Atk={state.NormalAttackPower:0.##} ASPD={state.AttackSpeed:0.##}");
            return true;
        }

        public bool RegisterMonster(string runtimeId, string monsterId, float maxHp)
        {
            if (!IsCombatGameplayActive || string.IsNullOrEmpty(runtimeId))
            {
                return false;
            }

            if (_monsters.ContainsKey(runtimeId))
            {
                Debug.LogWarning($"[SearchExtractSession] RegisterMonster duplicate '{runtimeId}' — ignored.");
                return false;
            }

            _monsters[runtimeId] = new DefendCombatMonsterState
            {
                RuntimeId = runtimeId,
                MonsterId = monsterId ?? string.Empty,
                MaxHp = Math.Max(1f, maxHp),
                RemainingHp = Math.Max(1f, maxHp),
                IsAlive = true,
                IsCombatDead = false
            };
            if (_configs == null)
            {
                Debug.LogWarning(
                    $"[SearchExtractSession] RegisterMonster '{runtimeId}' — configs unbound; " +
                    "MonsterConfig.Skills will not initialize (call BindCombatConfigs before Prepare).");
            }

            _monsterDeathSkills.InitializeMonsterState(_monsters[runtimeId], _configs);
            return true;
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

        public bool IsMonsterTargetable(string runtimeId)
        {
            if (!TryGetMonster(runtimeId, out var state) || state == null)
            {
                return false;
            }

            return !state.IsCombatDead
                   && state.IsAlive
                   && state.RemainingHp > 0f
                   && !_combatStatus.IsMonsterInvincible(runtimeId);
        }

        public bool IsProjectileCombatActive(string warriorId)
        {
            return IsCombatGameplayActive && IsWarriorCombatActive(warriorId);
        }

        public bool TryConfirmMeleeHit(string warriorId, string monsterRuntimeId, bool stillInRange)
        {
            if (!IsCombatGameplayActive || !stillInRange)
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

            if (!IsMonsterTargetable(monsterRuntimeId) || !TryGetMonster(monsterRuntimeId, out var monster))
            {
                return false;
            }

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - warrior.NormalAttackPower);
            Debug.Log(
                $"[SearchExtractSession] MeleeHit {warriorId} -> {monsterRuntimeId} " +
                $"dmg={warrior.NormalAttackPower:0.##} HP={monster.RemainingHp:0}/{monster.MaxHp}");
            MonsterDamageSettled?.Invoke(monsterRuntimeId, warrior.NormalAttackPower);

            if (monster.RemainingHp <= 0f)
            {
                TryFinalizeMonsterDeath(monster, monsterRuntimeId, warriorId, warrior.NormalAttackPower, string.Empty);
            }

            return true;
        }

        public bool TryConfirmRangedHit(string warriorId, string monsterRuntimeId)
        {
            if (!IsCombatGameplayActive)
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

            if (!IsMonsterTargetable(monsterRuntimeId) || !TryGetMonster(monsterRuntimeId, out var monster))
            {
                return false;
            }

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - warrior.NormalAttackPower);
            Debug.Log(
                $"[SearchExtractSession] RangedHit {warriorId} -> {monsterRuntimeId} " +
                $"dmg={warrior.NormalAttackPower:0.##} HP={monster.RemainingHp:0}/{monster.MaxHp}");
            MonsterDamageSettled?.Invoke(monsterRuntimeId, warrior.NormalAttackPower);

            if (monster.RemainingHp <= 0f)
            {
                TryFinalizeMonsterDeath(monster, monsterRuntimeId, warriorId, warrior.NormalAttackPower, string.Empty);
            }

            return true;
        }

        public bool TryApplyMonsterDamageToWarrior(string monsterRuntimeId, string warriorId, float attackPower)
        {
            if (!IsCombatGameplayActive || !IsMonsterAlive(monsterRuntimeId))
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior))
            {
                return false;
            }

            var dmg = Math.Max(0f, attackPower);
            var invincibleNote = string.Empty;
            if (dmg > 0f && _combatStatus.IsWarriorInvincible(warriorId))
            {
                dmg = 0f;
                invincibleNote = " Invincible=1";
            }

            if (dmg > 0f)
            {
                warrior.RemainingHp = Math.Max(0f, warrior.RemainingHp - dmg);
            }

            Debug.Log(
                $"[SearchExtractSession] MonsterHit {monsterRuntimeId} -> {warriorId} dmg={dmg:0.##} " +
                $"HP={warrior.RemainingHp:0}/{warrior.MaxHp}{invincibleNote}");
            WarriorDamageSettled?.Invoke(warriorId, dmg);

            if (warrior.RemainingHp <= 0f)
            {
                EnterWarriorCombatDead(warrior);
            }

            return true;
        }

        public bool TryNotifyMonsterDeathPresentationComplete(string runtimeId)
        {
            if (!TryGetMonster(runtimeId, out var monster))
            {
                return false;
            }

            return _monsterDeathSkills.TryNotifyDeathPresentationComplete(monster);
        }

        public bool TryNotifyMonsterReviveAnimComplete(string runtimeId)
        {
            if (!TryGetMonster(runtimeId, out var monster))
            {
                return false;
            }

            if (!_monsterDeathSkills.TryCompleteReviveAnim(monster, _combatStatus))
            {
                return false;
            }

            Debug.Log(
                $"[SearchExtractSession] MonsterRevived {runtimeId} ({monster.MonsterId}) " +
                $"HP={monster.RemainingHp:0}/{monster.MaxHp} revivesLeft={monster.RevivesRemaining}");
            MonsterRevived?.Invoke(runtimeId);
            return true;
        }

        public bool IsMonsterInvincible(string monsterRuntimeId)
        {
            return _combatStatus.IsMonsterInvincible(monsterRuntimeId);
        }

        public bool IsMonsterStunned(string monsterRuntimeId)
        {
            return _combatStatus.IsMonsterStunned(monsterRuntimeId);
        }

        public float GetMonsterSlowMoveMul(string monsterRuntimeId)
        {
            return _combatStatus.GetMonsterSlowMoveMul(monsterRuntimeId);
        }

        public float GetMonsterSlowAttackMul(string monsterRuntimeId)
        {
            return _combatStatus.GetMonsterSlowAttackMul(monsterRuntimeId);
        }

        public bool TryApplyCorpseSmashDamage(
            string corpseRuntimeId,
            string killerWarriorId,
            float killerOutgoingDamage,
            string targetRuntimeId)
        {
            if (!IsCombatGameplayActive || killerOutgoingDamage <= 0f)
            {
                return false;
            }

            if (string.IsNullOrEmpty(targetRuntimeId)
                || string.Equals(targetRuntimeId, corpseRuntimeId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsMonsterTargetable(targetRuntimeId) || !TryGetMonster(targetRuntimeId, out var monster))
            {
                return false;
            }

            var dmg = CorpseSmashCombatMath.ComputeSmashDamage(killerOutgoingDamage);
            if (dmg <= 0f)
            {
                return false;
            }

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - dmg);
            Debug.Log(
                $"[SearchExtractSession] CorpseSmash {killerWarriorId} corpse={corpseRuntimeId} -> {targetRuntimeId} " +
                $"dmg={dmg:0.##} HP={monster.RemainingHp:0}/{monster.MaxHp}");

            MonsterDamageSettled?.Invoke(targetRuntimeId, dmg);
            TryFinalizeMonsterDeath(monster, targetRuntimeId, killerWarriorId ?? string.Empty, dmg, "CorpseSmash");
            return true;
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

        private void LoadCurrentPointWaveRows()
        {
            _currentPointWaveRows.Clear();
            if (Config == null || CurrentGatherOrder <= 0)
            {
                return;
            }

            for (var i = 0; i < _allWaveRows.Count; i++)
            {
                var row = _allWaveRows[i];
                if (row != null && row.GatherPointOrder == CurrentGatherOrder)
                {
                    _currentPointWaveRows.Add(row);
                }
            }

            _currentPointWaveRows.Sort((a, b) => a.WaveIndex.CompareTo(b.WaveIndex));
        }

        private void FireWaveSpawn(SearchExtractWaveSpawnConfigRow row)
        {
            if (row == null)
            {
                return;
            }

            var request = new SearchExtractSpawnRequest
            {
                GatherPointOrder = row.GatherPointOrder,
                WaveIndex = row.WaveIndex,
                SpawnPointId = row.SpawnPointId ?? string.Empty,
                MonsterId = row.MonsterId ?? string.Empty,
                SpawnCount = Mathf.Max(1, row.SpawnCount)
            };

            Debug.Log(
                $"[SearchExtractSession] WaveSpawn Order={request.GatherPointOrder} Wave={request.WaveIndex} " +
                $"Elapsed={_waveElapsedSinceActivation:0.###}s Point={request.SpawnPointId} " +
                $"Monster={request.MonsterId} x{request.SpawnCount}");
            SpawnRequested?.Invoke(request);
        }

        private void TryFinalizeMonsterDeath(
            DefendCombatMonsterState monster,
            string runtimeId,
            string killerWarriorId,
            float outgoingDamage,
            string deathTag)
        {
            if (monster == null || monster.RemainingHp > 0f)
            {
                return;
            }

            var allowRevive = !string.Equals(deathTag, "PointClear", StringComparison.Ordinal);
            if (allowRevive && _monsterDeathSkills.TryInterceptDeath(monster))
            {
                Debug.Log(
                    $"[SearchExtractSession] MonsterCombatDead {runtimeId} ({monster.MonsterId}) " +
                    $"revivesLeft={monster.RevivesRemaining} ({deathTag})");
                MonsterEnteredCombatDead?.Invoke(
                    runtimeId,
                    killerWarriorId ?? string.Empty,
                    outgoingDamage,
                    deathTag ?? string.Empty);
                return;
            }

            _monsterDeathSkills.ForceTrueDeath(monster);
            _combatStatus.ClearMonster(runtimeId);
            Debug.Log($"[SearchExtractSession] MonsterDead {runtimeId} ({monster.MonsterId}) ({deathTag})");
            MonsterKilled?.Invoke(runtimeId, killerWarriorId ?? string.Empty, outgoingDamage, deathTag ?? string.Empty);
        }

        private void TickMonsterRevive(float deltaTime)
        {
            if (!IsCombatGameplayActive || deltaTime <= 0f)
            {
                return;
            }

            foreach (var pair in _monsters)
            {
                var monster = pair.Value;
                if (monster == null || !monster.IsCombatDead)
                {
                    continue;
                }

                _monsterDeathSkills.Tick(monster, deltaTime);
            }
        }

        private void BindDeathSkillHost()
        {
            _monsterDeathSkills.MonsterReviveStarted -= HandleMonsterReviveStartedInternal;
            _monsterDeathSkills.MonsterReviveStarted += HandleMonsterReviveStartedInternal;
            _combatStatus.MonsterInvincibleChanged -= HandleMonsterInvincibleChangedInternal;
            _combatStatus.MonsterInvincibleChanged += HandleMonsterInvincibleChangedInternal;
        }

        private void UnbindDeathSkillHost()
        {
            _monsterDeathSkills.MonsterReviveStarted -= HandleMonsterReviveStartedInternal;
            _combatStatus.MonsterInvincibleChanged -= HandleMonsterInvincibleChangedInternal;
        }

        private void HandleMonsterReviveStartedInternal(string runtimeId, float animSeconds)
        {
            MonsterReviveStarted?.Invoke(runtimeId, animSeconds);
        }

        private void HandleMonsterInvincibleChangedInternal(string runtimeId, string skillId, bool on)
        {
            MonsterInvincibleChanged?.Invoke(runtimeId, skillId, on);
        }

        private void EnterWarriorCombatDead(DefendCombatWarriorState warrior)
        {
            if (warrior == null || warrior.IsCombatDead)
            {
                return;
            }

            warrior.IsCombatDead = true;
            warrior.RemainingHp = 0f;
            _combatStatus.ClearWarrior(warrior.WarriorId);
            Debug.Log($"[SearchExtractSession] WarriorCombatDead {warrior.WarriorId}");
            WarriorCombatDead?.Invoke(warrior.WarriorId);
            TryEvaluateLoyalWipe();
        }

        private int CountLivingLoyalWarriors()
        {
            var count = 0;
            foreach (var pair in _warriors)
            {
                var warrior = pair.Value;
                if (warrior == null || warrior.IsRebel || warrior.IsCombatDead || warrior.IsPermanentDead)
                {
                    continue;
                }

                if (warrior.RemainingHp > 0f)
                {
                    count++;
                }
            }

            return count;
        }

        private void ApplyPointSuccessInvincible()
        {
            foreach (var pair in _warriors)
            {
                var warrior = pair.Value;
                if (warrior == null
                    || warrior.IsRebel
                    || warrior.IsCombatDead
                    || warrior.IsPermanentDead
                    || warrior.RemainingHp <= 0f)
                {
                    continue;
                }

                _combatStatus.ApplyWarriorInvincibleHold(warrior.WarriorId, PointSuccessInvincibleSkillId);
            }
        }

        private void ClearPointSuccessInvincible()
        {
            foreach (var pair in _warriors)
            {
                var warrior = pair.Value;
                if (warrior == null)
                {
                    continue;
                }

                _combatStatus.ClearWarrior(warrior.WarriorId);
            }
        }

        /// <summary>Rules-layer wipe: true death; skip SelfRevive; cancel in-flight fake death.</summary>
        private void ClearLivingMonstersRules()
        {
            _clearScratch.Clear();
            foreach (var pair in _monsters)
            {
                var monster = pair.Value;
                if (monster == null)
                {
                    continue;
                }

                var living = monster.IsAlive && !monster.IsCombatDead && monster.RemainingHp > 0f;
                if (!living && !monster.IsCombatDead)
                {
                    continue;
                }

                _clearScratch.Add(pair.Key);
            }

            for (var i = 0; i < _clearScratch.Count; i++)
            {
                var runtimeId = _clearScratch[i];
                if (!_monsters.TryGetValue(runtimeId, out var monster) || monster == null)
                {
                    continue;
                }

                var alreadyPresentedFakeDeath = monster.IsCombatDead;
                _monsterDeathSkills.ForceTrueDeath(monster);
                _combatStatus.ClearMonster(runtimeId);
                if (alreadyPresentedFakeDeath)
                {
                    continue;
                }

                Debug.Log($"[SearchExtractSession] MonsterDead {runtimeId} ({monster.MonsterId}) (PointClear)");
                MonsterKilled?.Invoke(runtimeId, string.Empty, 0f, "PointClear");
            }

            Debug.Log(
                $"[SearchExtractSession] Rules clear monsters count={_clearScratch.Count} Order={CurrentGatherOrder}");
        }

        private void ResetGatherCountdownState()
        {
            _currentPointActivated = false;
            _formationRelocateActive = false;
            _gatherCountdownRemaining = 0f;
            _currentPointSpawnStopped = false;
            _awaitingPointDecision = false;
        }

        private void ResetWaveSpawnState()
        {
            _waveElapsedSinceActivation = 0f;
            _waveRecipeStates.Clear();
            for (var i = 0; i < _currentPointWaveRows.Count; i++)
            {
                var row = _currentPointWaveRows[i];
                if (row == null)
                {
                    continue;
                }

                _waveRecipeStates.Add(new WaveRecipeRuntimeState
                {
                    Row = row,
                    FirstFired = false,
                    RepeatsLeft = Mathf.Max(0, row.RepeatSpawnCount),
                    NextDueElapsed = Mathf.Max(0f, row.FirstWaveDelaySeconds),
                    IsComplete = false
                });
            }
        }

        private sealed class WaveRecipeRuntimeState
        {
            public SearchExtractWaveSpawnConfigRow Row;
            public bool FirstFired;
            public int RepeatsLeft;
            public float NextDueElapsed;
            public bool IsComplete;
        }
    }
}

