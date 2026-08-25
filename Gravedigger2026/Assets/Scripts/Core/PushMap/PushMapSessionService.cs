using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Combat.SkillEffects;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.PushMap
{
    /// <summary>
    /// PushMap stage rules (PM-03–PM-07, Approach A; PM-12/13 Approach B).
    /// Prepare→Combat, StartBattle ≥1, Shield, LOC lock, objective capture, spawn/trap,
    /// Boss clear → VictorySettled(StageExpReward), CaptureLoot/DungeonUnlock hooks.
    /// PM-12/13: independently mirrors Defend StartBattle registry + HitConfirm
    /// (WarriorCombatMath + ClassConfig; no DefendSessionService lifetime binding).
    /// D-069: Skill_03 burst commit / SkillCooldown tick / post-cast LOC re-roll (Approach C);
    /// Skill_01 block on monster AA (SC-02 Approach B);
    /// Skill_02 Comfort outgoing mul on SettleMonsterDamage (SC-03 Approach A).
    /// D-073: SkillEffectPipeline on OutgoingDamageSettle (SE-01 Skill_04) + OnWarriorWouldDie (SE-02 Skill_05)
    /// + OnWarriorAaHitConfirm (SE-03 Skill_06 AOE Stun / SE-04 Skill_07 AOE Slow / SE-05 Skill_08 Elite / SE-08 Skill_11 Burn)
    /// + OnSkillInternalCooldown (SE-06 Skill_09 StackingOutgoingMulTimed)
    /// + OnProjectileHit (SE-07 Skill_10 Pierce)
    /// + OnWarriorTargetAcquired (SE-09 Skill_12 Blink).
    /// D-071: SkillIconPopup / SkillPersistChanged for CombatSkillIcon (rules only).
    /// Soldier→monster RemainingHp≤0 → MonsterKilled(runtimeId, killerWarriorId); monster→soldier → CombatDead.
    /// Position resolution and instantiation are View concerns; AOE uses injected world-XZ provider.
    /// </summary>
    public sealed class PushMapSessionService : IProjectileCombatSession, IProjectilePierceChannel
    {
        private bool _active;
        private bool _outcomeSettled;
        private int _shield;
        private int _shieldCap;
        private float _lockedLossOfControlDegree;
        private int _lockedLossOfControlTierId;
        private float _lockedTierChance;
        private ConfigCsvRepository _configs;
        private int _pendingBossCount;
        private float _combatStartRealtime;
        private float _combatEndRealtime;
        private bool _combatIntroActive;
        private bool _combatClockRunning;
        private int _monstersKilled;
        private bool _isVictory;
        private long _stageExpCredited;
        private readonly Dictionary<string, int> _captureLootLedger =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<PushMapPhase> PhaseChanged;
        public event Action<int, int> ShieldChanged;
        public event Action LevelFailureRequested;
        public event Action<long> VictorySettled;
        public event Action<int> ObjectiveCaptured;
        public event Action<int> CurrentObjectiveChanged;
        public event Action<PushMapSpawnRequest> PushMapSpawnRequested;

        /// <summary>PM-12: soldier HitConfirm settled damage on a monster (runtimeId, damage).</summary>
        public event Action<string, float> MonsterDamageSettled;

        /// <summary>PM-12: monster RemainingHp≤0 (runtimeId, killerWarriorId). View NotifyKilled; Boss → TryNotifyBossKilled.</summary>
        public event Action<string, string> MonsterKilled;

        /// <summary>PM-13: monster AttackPower settled on a warrior (warriorId, damage).</summary>
        public event Action<string, float> WarriorDamageSettled;

        /// <summary>PM-13: warrior RemainingHp≤0 → CombatDead (or gem PermanentDeath mark); View PlayDie + stop.</summary>
        public event Action<string> WarriorCombatDead;

        /// <summary>D-071: overhead CombatSkillIcon popup (warriorId, skillId).</summary>
        public event Action<string, string> SkillIconPopup;

        /// <summary>D-071: persist CombatSkillIcon at feet (warriorId, skillId, on).</summary>
        public event Action<string, string, bool> SkillPersistChanged;

        private readonly Dictionary<string, DefendCombatWarriorState> _warriors =
            new Dictionary<string, DefendCombatWarriorState>(StringComparer.Ordinal);
        private readonly Dictionary<string, DefendCombatMonsterState> _monsters =
            new Dictionary<string, DefendCombatMonsterState>(StringComparer.Ordinal);
        private readonly HashSet<string> _skill02PersistOn = new HashSet<string>(StringComparer.Ordinal);
        private readonly CombatStatusService _combatStatus = new CombatStatusService();
        private SkillEffectPipeline _skillEffectPipeline;
        private Func<string, Vector2?> _monsterWorldXZProvider;
        private readonly List<MonsterWorldXZ> _aliveMonstersXZScratch = new List<MonsterWorldXZ>(32);

        private readonly List<int> _objectiveOrders = new List<int>();
        private readonly HashSet<int> _capturedObjectives = new HashSet<int>();
        private int _currentObjectiveOrder;

        private readonly List<PushMapSpawnConfigRow> _spawnRows = new List<PushMapSpawnConfigRow>();
        private readonly HashSet<string> _trapSpawnPointsFired = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _spawnPointsStoppedByCapture = new HashSet<string>(StringComparer.Ordinal);

        public PushMapGameplayConfigRow Config { get; private set; }
        public PushMapPhase Phase { get; private set; } = PushMapPhase.Prepare;
        public bool IsActive => _active;
        public int Shield => _shield;
        public int ShieldCap => _shieldCap;
        public float LockedLossOfControlDegree => _lockedLossOfControlDegree;
        public int LockedLossOfControlTierId => _lockedLossOfControlTierId;
        public float LockedTierChance => _lockedTierChance;
        public int PendingBossCount => _pendingBossCount;
        public bool OutcomeSettled => _outcomeSettled;
        public bool IsVictory => _isVictory;
        public int MonstersKilled => _monstersKilled;
        public long StageExpCredited => _stageExpCredited;

        /// <summary>Always false after v0.83.13 — Prepare owns path preview; StartBattle has no intro latch.</summary>
        public bool IsCombatIntroActive => false;

        /// <summary>
        /// True when Combat gameplay may tick (not Prepare/Ended).
        /// </summary>
        public bool IsCombatGameplayActive =>
            _active && Phase == PushMapPhase.Combat && !_outcomeSettled;

        /// <summary>Seconds from StartBattle (clock start) to Ended.</summary>
        public float CombatElapsedSeconds
        {
            get
            {
                if (!_active || Phase == PushMapPhase.Prepare || !_combatClockRunning)
                {
                    return 0f;
                }

                var end = _outcomeSettled ? _combatEndRealtime : Time.realtimeSinceStartup;
                return Mathf.Max(0f, end - _combatStartRealtime);
            }
        }

        public IReadOnlyDictionary<string, int> CaptureLootLedger => _captureLootLedger;

        /// <summary>Current uncaptured objective order; 0 = not started / none / all captured.</summary>
        public int CurrentObjectiveOrder => _currentObjectiveOrder;

        public bool IsObjectiveCaptured(int order) => _capturedObjectives.Contains(order);

        public void BeginPrepare(PushMapGameplayConfigRow config)
        {
            Stop();
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _active = true;
            Phase = PushMapPhase.Prepare;
            PhaseChanged?.Invoke(Phase);
            ShieldChanged?.Invoke(_shield, _shieldCap);
            Debug.Log(
                $"[PushMapSession] Prepare Config={config.GameplayConfigId} Map={config.MapId} " +
                $"ExpReward={config.StageExpReward} CaptureSeconds={config.CaptureSeconds} (ignored; arrive=Capture)");
        }

        public void Stop()
        {
            _active = false;
            Config = null;
            Phase = PushMapPhase.Prepare;
            _shield = 0;
            _shieldCap = 0;
            _outcomeSettled = false;
            _pendingBossCount = 0;
            _combatStartRealtime = 0f;
            _combatEndRealtime = 0f;
            _combatIntroActive = false;
            _combatClockRunning = false;
            _monstersKilled = 0;
            _isVictory = false;
            _stageExpCredited = 0;
            _captureLootLedger.Clear();
            _lockedLossOfControlDegree = 0f;
            _lockedLossOfControlTierId = 0;
            _lockedTierChance = 0f;
            _configs = null;
            _skillEffectPipeline = null;
            _monsterWorldXZProvider = null;
            _aliveMonstersXZScratch.Clear();
            _objectiveOrders.Clear();
            _capturedObjectives.Clear();
            _currentObjectiveOrder = 0;
            _spawnRows.Clear();
            _trapSpawnPointsFired.Clear();
            _spawnPointsStoppedByCapture.Clear();
            _warriors.Clear();
            _monsters.Clear();
            _skill02PersistOn.Clear();
            _combatStatus.WarriorInvincibleChanged -= HandleWarriorInvincibleChanged;
            _combatStatus.MonsterBurnTick -= HandleMonsterBurnTick;
            _combatStatus.ClearAll();
        }

        /// <summary>
        /// Stage injects View world XZ lookup for AOE skill effects (Skill_06+).
        /// Returns null when the runtime id has no live View position.
        /// </summary>
        public void SetMonsterWorldXZProvider(Func<string, Vector2?> provider)
        {
            _monsterWorldXZProvider = provider;
        }

        /// <summary>D-073 SE-03: CombatStatus Stun gate for monster AI (no SkillId in View).</summary>
        public bool IsMonsterStunned(string monsterRuntimeId)
        {
            return _combatStatus.IsMonsterStunned(monsterRuntimeId);
        }

        /// <summary>D-073 SE-04: Slow move mul (1 = none). View polls; no SkillId.</summary>
        public float GetMonsterSlowMoveMul(string monsterRuntimeId)
        {
            return _combatStatus.GetMonsterSlowMoveMul(monsterRuntimeId);
        }

        /// <summary>D-073 SE-04: Slow attack-speed mul (1 = none).</summary>
        public float GetMonsterSlowAttackMul(string monsterRuntimeId)
        {
            return _combatStatus.GetMonsterSlowAttackMul(monsterRuntimeId);
        }

        public bool CanStartBattle(int deployedSoldierCount)
        {
            return _active && Phase == PushMapPhase.Prepare && deployedSoldierCount >= 1;
        }

        /// <summary>
        /// Prepare → Combat: init Shield and lock LossOfControl Degree/Tier.
        /// Rebel rolls for each deployed soldier are owned by the stage controller (PM-03).
        /// </summary>
        public bool TryStartBattle(
            int deployedSoldierCount,
            int protagonistMaxHp,
            float lossOfControlDegree,
            ConfigCsvRepository configs,
            out string error)
        {
            error = null;
            if (!_active || Config == null)
            {
                error = "PushMap session not started";
                return false;
            }

            if (Phase != PushMapPhase.Prepare)
            {
                error = "Only Prepare can StartBattle";
                return false;
            }

            if (deployedSoldierCount < 1)
            {
                error = "需要至少上阵 1 名士兵";
                return false;
            }

            _shieldCap = Mathf.Max(1, protagonistMaxHp);
            _shield = _shieldCap;
            _lockedLossOfControlDegree = lossOfControlDegree;
            _lockedLossOfControlTierId = LossOfControlMath.MapTierId(lossOfControlDegree);
            _lockedTierChance = 0f;
            _configs = configs;
            _skillEffectPipeline = configs != null ? new SkillEffectPipeline(configs) : null;
            _combatStatus.WarriorInvincibleChanged -= HandleWarriorInvincibleChanged;
            _combatStatus.WarriorInvincibleChanged += HandleWarriorInvincibleChanged;
            _combatStatus.MonsterBurnTick -= HandleMonsterBurnTick;
            _combatStatus.MonsterBurnTick += HandleMonsterBurnTick;
            if (_lockedLossOfControlTierId > 0
                && configs != null
                && configs.TryGetLossOfControlTier(_lockedLossOfControlTierId, out var tierRow)
                && tierRow != null)
            {
                _lockedTierChance = LossOfControlMath.ClampChance(tierRow.LossOfControlChance);
            }

            Phase = PushMapPhase.Combat;
            PhaseChanged?.Invoke(Phase);
            ShieldChanged?.Invoke(_shield, _shieldCap);
            _combatIntroActive = false;
            _combatStartRealtime = Time.realtimeSinceStartup;
            _combatClockRunning = true;
            _combatEndRealtime = 0f;
            _monstersKilled = 0;
            _isVictory = false;
            _stageExpCredited = 0;
            _captureLootLedger.Clear();
            Debug.Log(
                $"[PushMapSession] StartBattle Shield={_shield} Deployed={deployedSoldierCount} " +
                $"Degree={_lockedLossOfControlDegree:0.###} Tier={_lockedLossOfControlTierId} " +
                $"TierChance={_lockedTierChance:0.###} IntroActive=false");

            LoadSpawnRows(configs);
            return true;
        }

        /// <summary>
        /// Legacy no-op-safe hook: StartBattle now starts the combat clock immediately (no intro latch).
        /// Kept for call-site compatibility; idempotent if already running.
        /// </summary>
        public void EndCombatIntro()
        {
            if (!_active || Phase != PushMapPhase.Combat)
            {
                return;
            }

            if (!_combatIntroActive && _combatClockRunning)
            {
                return;
            }

            _combatIntroActive = false;
            if (!_combatClockRunning)
            {
                _combatStartRealtime = Time.realtimeSinceStartup;
                _combatClockRunning = true;
            }

            Debug.Log("[PushMapSession] EndCombatIntro — combat clock started.");
        }

        /// <summary>
        /// PM-05 / v0.66: fire non-trap StartBattle spawns after View has baked NavMesh and deployed soldiers.
        /// </summary>
        public void FireStartBattleSpawns()
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled || _spawnRows.Count == 0)
            {
                return;
            }

            FireNonTrapEligibleRows(PushMapSpawnTrigger.StartBattle, countTowardBossVictory: true);
        }

        /// <summary>
        /// Prepare Idle preview of the same non-trap StartBattle-eligible rows (SPEC_03 §3.14).
        /// Does not increment PendingBoss / register HP — View must not treat as combat units.
        /// </summary>
        public void FirePreparePreviewSpawns(ConfigCsvRepository configs)
        {
            if (!_active || Phase != PushMapPhase.Prepare)
            {
                return;
            }

            if (_spawnRows.Count == 0)
            {
                LoadSpawnRows(configs);
            }

            FireNonTrapEligibleRows(PushMapSpawnTrigger.PreparePreview, countTowardBossVictory: false);
        }

        private void FireNonTrapEligibleRows(PushMapSpawnTrigger trigger, bool countTowardBossVictory)
        {
            if (_spawnRows.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _spawnRows.Count; i++)
            {
                var row = _spawnRows[i];
                if (row == null || !string.IsNullOrEmpty(row.TrapZoneId))
                {
                    continue;
                }

                if (!row.IsBoss && IsLinkedObjectiveCaptured(row.LinkedObjectiveOrder))
                {
                    continue;
                }

                FireRow(row, trigger, countTowardBossVictory);
            }
        }

        /// <summary>
        /// PM-05: view reports a loyal soldier's first entry into TrapZone. If that trap maps to a
        /// pending spawn point whose linked objective is uncaptured, fire all its rows (once per
        /// point per battle). Position resolution is the View's concern.
        /// </summary>
        public void TryNotifyTrapEnter(string trapZoneId)
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled
                || string.IsNullOrEmpty(trapZoneId) || _spawnRows.Count == 0)
            {
                return;
            }

            var pendingPoints = new List<string>();
            for (var i = 0; i < _spawnRows.Count; i++)
            {
                var row = _spawnRows[i];
                if (row == null || string.IsNullOrEmpty(row.TrapZoneId)
                    || !string.Equals(row.TrapZoneId.Trim(), trapZoneId.Trim(), StringComparison.Ordinal))
                {
                    continue;
                }

                var pointId = row.SpawnPointId ?? string.Empty;
                if (pointId.Length == 0
                    || _trapSpawnPointsFired.Contains(pointId)
                    || _spawnPointsStoppedByCapture.Contains(pointId))
                {
                    continue;
                }

                if (!pendingPoints.Contains(pointId))
                {
                    pendingPoints.Add(pointId);
                }
            }

            if (pendingPoints.Count == 0)
            {
                return;
            }

            for (var p = 0; p < pendingPoints.Count; p++)
            {
                var pointId = pendingPoints[p];
                var anyFired = false;
                var rowsForPoint = CollectRowsForPoint(pointId, requireTrap: true);
                for (var i = 0; i < rowsForPoint.Count; i++)
                {
                    var row = rowsForPoint[i];
                    if (!row.IsBoss && IsLinkedObjectiveCaptured(row.LinkedObjectiveOrder))
                    {
                        continue;
                    }

                    FireRow(row, PushMapSpawnTrigger.Trap, countTowardBossVictory: true);
                    anyFired = true;
                }

                _trapSpawnPointsFired.Add(pointId);
                if (anyFired)
                {
                    Debug.Log($"[PushMapSession] Trap '{trapZoneId}' fired spawn point '{pointId}'.");
                }
                else
                {
                    Debug.Log($"[PushMapSession] Trap '{trapZoneId}' point '{pointId}' marked fired (linked objective already captured).");
                }
            }
        }

        /// <summary>
        /// PM-07: one Boss instance killed. When pending reaches 0 → Ended + VictorySettled(StageExpReward).
        /// </summary>
        public void TryNotifyBossKilled()
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled)
            {
                return;
            }

            if (_pendingBossCount <= 0)
            {
                Debug.LogWarning("[PushMapSession] TryNotifyBossKilled with PendingBossCount=0 — ignored.");
                return;
            }

            _pendingBossCount--;
            Debug.Log($"[PushMapSession] Boss killed — pending remaining={_pendingBossCount}");
            if (_pendingBossCount > 0)
            {
                return;
            }

            EnterVictory();
        }

        /// <summary>
        /// View reports whether the map has a BossPoint after markers are collected.
        /// Warns when IsBoss rows were fired but BossPoint is missing (consistency convention).
        /// </summary>
        public void NotifyBossPointPresence(bool hasBossPoint)
        {
            if (_pendingBossCount > 0 && !hasBossPoint)
            {
                Debug.LogWarning(
                    $"[PushMapSession] IsBoss pending={_pendingBossCount} but map has no BossPoint — " +
                    "position will fall back; author should align Prefab marker with IsBoss rows.");
            }
        }

        private void LoadSpawnRows(ConfigCsvRepository configs)
        {
            _spawnRows.Clear();
            _trapSpawnPointsFired.Clear();
            _spawnPointsStoppedByCapture.Clear();
            _pendingBossCount = 0;

            if (configs == null || Config == null || string.IsNullOrEmpty(Config.GameplayConfigId))
            {
                return;
            }

            var rows = configs.GetPushMapSpawnRows(Config.GameplayConfigId);
            if (rows != null)
            {
                _spawnRows.AddRange(rows);
            }

            _spawnRows.Sort(CompareSpawnRows);
            Debug.Log($"[PushMapSession] Loaded {_spawnRows.Count} PushMapSpawn rows for '{Config.GameplayConfigId}'.");
        }

        private List<PushMapSpawnConfigRow> CollectRowsForPoint(string spawnPointId, bool requireTrap)
        {
            var result = new List<PushMapSpawnConfigRow>();
            for (var i = 0; i < _spawnRows.Count; i++)
            {
                var row = _spawnRows[i];
                if (row == null || !string.Equals(row.SpawnPointId, spawnPointId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (requireTrap && string.IsNullOrEmpty(row.TrapZoneId))
                {
                    continue;
                }

                result.Add(row);
            }
            result.Sort(CompareSpawnRows);
            return result;
        }

        private static int CompareSpawnRows(PushMapSpawnConfigRow a, PushMapSpawnConfigRow b)
        {
            var point = string.CompareOrdinal(a?.SpawnPointId, b?.SpawnPointId);
            return point != 0 ? point : (a?.SpawnOrder ?? 0).CompareTo(b?.SpawnOrder ?? 0);
        }

        private bool IsLinkedObjectiveCaptured(int linkedObjectiveOrder)
        {
            return linkedObjectiveOrder > 0 && _capturedObjectives.Contains(linkedObjectiveOrder);
        }

        private void FireRow(PushMapSpawnConfigRow row, PushMapSpawnTrigger trigger, bool countTowardBossVictory)
        {
            if (row == null || row.SpawnCount < 1)
            {
                return;
            }

            if (countTowardBossVictory && row.IsBoss)
            {
                _pendingBossCount += row.SpawnCount;
            }

            var request = new PushMapSpawnRequest
            {
                SpawnPointId = row.SpawnPointId ?? string.Empty,
                MonsterId = row.MonsterId ?? string.Empty,
                SpawnCount = row.SpawnCount,
                LinkedObjectiveOrder = row.LinkedObjectiveOrder,
                IsBoss = row.IsBoss,
                SpawnOrder = row.SpawnOrder,
                Trigger = trigger
            };

            Debug.Log(
                $"[PushMapSession] Spawn ({trigger}) Point={request.SpawnPointId} Monster={request.MonsterId} " +
                $"x{request.SpawnCount} Order={request.SpawnOrder} LinkedObj={request.LinkedObjectiveOrder} Boss={request.IsBoss} " +
                $"(PendingBoss={_pendingBossCount})");
            PushMapSpawnRequested?.Invoke(request);
        }

        /// <summary>
        /// PM-04: begin objective chain after Combat. Orders are sorted/deduped; current = min uncaptured.
        /// </summary>
        public void TryBeginObjectiveChain(IEnumerable<int> objectiveOrders)
        {
            _objectiveOrders.Clear();
            _capturedObjectives.Clear();

            if (objectiveOrders != null)
            {
                foreach (var order in objectiveOrders)
                {
                    if (order >= 1 && !_objectiveOrders.Contains(order))
                    {
                        _objectiveOrders.Add(order);
                    }
                }
                _objectiveOrders.Sort();
            }

            var previous = _currentObjectiveOrder;
            _currentObjectiveOrder = _objectiveOrders.Count > 0 ? _objectiveOrders[0] : 0;
            Debug.Log(
                $"[PushMapSession] ObjectiveChain begin orders=[{string.Join(",", _objectiveOrders)}] " +
                $"CurrentObjective={_currentObjectiveOrder} (arrive=Capture)");
            if (previous != _currentObjectiveOrder)
            {
                CurrentObjectiveChanged?.Invoke(_currentObjectiveOrder);
            }
        }

        /// <summary>
        /// PM-04 / v0.74.8: any loyal soldier already in current CaptureZone → immediate Capture.
        /// No timer; no clear-monsters condition (§3.14).
        /// </summary>
        public void TickCapture(bool anyLoyalSoldierInCurrentZone)
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled || _currentObjectiveOrder <= 0)
            {
                return;
            }

            if (!anyLoyalSoldierInCurrentZone)
            {
                return;
            }

            Capture(_currentObjectiveOrder);
        }

        private void Capture(int order)
        {
            if (_capturedObjectives.Contains(order))
            {
                return;
            }

            _capturedObjectives.Add(order);
            MarkSpawnPointsStoppedByCapture(order);
            Debug.Log(
                $"[PushMapSession] ObjectiveCaptured {order} — linked spawns stopped (living kept); " +
                $"CaptureLoot='{Config?.CaptureLoot}' Unlock='{Config?.DungeonUnlockIds}' (credit via presentation).");
            ObjectiveCaptured?.Invoke(order);

            var next = 0;
            for (var i = 0; i < _objectiveOrders.Count; i++)
            {
                var candidate = _objectiveOrders[i];
                if (!_capturedObjectives.Contains(candidate))
                {
                    next = candidate;
                    break;
                }
            }

            var previous = _currentObjectiveOrder;
            _currentObjectiveOrder = next;
            Debug.Log($"[PushMapSession] CurrentObjective {previous} → {_currentObjectiveOrder}");
            CurrentObjectiveChanged?.Invoke(_currentObjectiveOrder);
        }

        /// <summary>
        /// PM-05: mark points whose non-boss rows link to the captured objective as stopped.
        /// Already-spawned monsters are unaffected; IsBoss rows are never capture-stopped.
        /// </summary>
        private void MarkSpawnPointsStoppedByCapture(int capturedOrder)
        {
            for (var i = 0; i < _spawnRows.Count; i++)
            {
                var row = _spawnRows[i];
                if (row == null || row.IsBoss || row.LinkedObjectiveOrder != capturedOrder)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(row.SpawnPointId))
                {
                    _spawnPointsStoppedByCapture.Add(row.SpawnPointId);
                }
            }
        }

        /// <summary>Enemy/rebel normal attack on BattleProtagonist: Shield -= 1 (PM-03 boundary).</summary>
        public void ApplyShieldHit(string sourceTag = null)
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled)
            {
                return;
            }

            _shield = Mathf.Max(0, _shield - 1);
            ShieldChanged?.Invoke(_shield, _shieldCap);
            Debug.Log($"[PushMapSession] Shield hit by {sourceTag ?? "?"} -> {_shield}/{_shieldCap}");
            if (_shield <= 0)
            {
                RequestLevelFailure("护盾归零");
            }
        }

        /// <summary>
        /// PM-12 (SPEC_04 §9.22): deploy-time warrior registry, mirrored from Defend —
        /// WarriorCombatMath + ClassConfig → HP / NormalAttackPower / AttackSpeed / windup /
        /// projectile params. Rebel flag stays View-owned this slice (stage SetRebel).
        /// </summary>
        public bool TryRegisterWarrior(
            WarriorInstance warrior,
            ClassConfigRow classRow,
            out DefendCombatWarriorState state,
            out string error)
        {
            state = null;
            error = null;
            if (!_active || Phase != PushMapPhase.Combat)
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

            SeedInternalSkillCooldowns(state, battleStats, coeffs);

            if (SoldierSkillCast.TryResolveSkill03(state.SoldierSkills, _configs, out var skillRow)
                && skillRow != null)
            {
                state.CastSkillId = SoldierSkillCast.Skill03Id;
                state.CastSkillLevel = skillRow.SkillLevel;
                state.SkillCooldownSeconds = WarriorCombatMath.ComputeSkillCooldown(
                    battleStats.Intelligence,
                    skillRow.BaseCooldownSeconds,
                    coeffs);
                state.SkillCdRemaining = 0f;
            }

            _warriors[warrior.Id] = state;
            warrior.RemainingHP = state.RemainingHp;
            var skillLog = string.IsNullOrEmpty(state.CastSkillId)
                ? "Skill=none"
                : $"Skill={state.CastSkillId} Lv{state.CastSkillLevel} CD={state.SkillCooldownSeconds:0.##}s";
            Debug.Log(
                $"[PushMapSession] RegisterWarrior {state.WarriorId} Mode={state.AttackMode} " +
                $"HP={state.RemainingHp:0}/{state.MaxHp} Atk={state.NormalAttackPower:0.##} " +
                $"ASPD={state.AttackSpeed:0.##} Range={state.AttackRange:0.##} " +
                $"ProjSpeed={state.RangedProjectileSpeed:0.##} ProjTimeout={state.RangedTimeoutSeconds:0.##} " +
                skillLog);
            return true;
        }

        /// <summary>PM-12: spawn-time monster registry. runtimeId = View RuntimeTargetId (GameObject name).</summary>
        public bool RegisterMonster(string runtimeId, string monsterId, float maxHp)
        {
            if (!_active || Phase != PushMapPhase.Combat || string.IsNullOrEmpty(runtimeId))
            {
                return false;
            }

            if (_monsters.ContainsKey(runtimeId))
            {
                Debug.LogWarning($"[PushMapSession] RegisterMonster duplicate runtimeId '{runtimeId}' — ignored.");
                return false;
            }

            _monsters[runtimeId] = new DefendCombatMonsterState
            {
                RuntimeId = runtimeId,
                MonsterId = monsterId ?? string.Empty,
                MaxHp = Math.Max(1f, maxHp),
                RemainingHp = Math.Max(1f, maxHp),
                IsAlive = true
            };
            return true;
        }

        public bool TryGetWarrior(string warriorId, out DefendCombatWarriorState state)
        {
            state = null;
            return !string.IsNullOrEmpty(warriorId)
                   && _warriors.TryGetValue(warriorId, out state)
                   && state != null;
        }

        /// <summary>D-073: tick CombatStatusService while Combat gameplay is active.</summary>
        public void TickCombatStatus(float deltaTime)
        {
            if (!IsCombatGameplayActive || deltaTime <= 0f)
            {
                return;
            }

            _combatStatus.Tick(deltaTime);
        }

        /// <summary>D-069/D-073: tick Skill_03 CD and per-skill internal CDs while Combat is active.</summary>
        public void TickSkillCooldowns(float deltaTime)
        {
            if (!IsCombatGameplayActive || deltaTime <= 0f)
            {
                return;
            }

            foreach (var pair in _warriors)
            {
                var warrior = pair.Value;
                if (warrior == null)
                {
                    continue;
                }

                if (warrior.SkillCdRemaining > 0f)
                {
                    warrior.SkillCdRemaining = Math.Max(0f, warrior.SkillCdRemaining - deltaTime);
                }

                TickWarriorInternalSkillCooldowns(warrior, deltaTime);
            }
        }

        private void TickWarriorInternalSkillCooldowns(DefendCombatWarriorState warrior, float deltaTime)
        {
            if (warrior?.SkillInternalCdRemaining == null || warrior.SkillInternalCdRemaining.Count == 0)
            {
                return;
            }

            if (warrior.IsCombatDead || warrior.IsRebel)
            {
                return;
            }

            var keys = new List<string>(warrior.SkillInternalCdRemaining.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var skillId = keys[i];
                var remaining = warrior.SkillInternalCdRemaining[skillId];
                if (remaining > 0f)
                {
                    warrior.SkillInternalCdRemaining[skillId] = Math.Max(0f, remaining - deltaTime);
                    if (warrior.SkillInternalCdRemaining[skillId] > 0f)
                    {
                        continue;
                    }
                }

                TryDispatchSkillInternalCooldown(warrior, skillId);
            }
        }

        private void TryDispatchSkillInternalCooldown(DefendCombatWarriorState warrior, string skillId)
        {
            if (_skillEffectPipeline == null || warrior == null || string.IsNullOrWhiteSpace(skillId))
            {
                return;
            }

            if (!TryResolveSkillEffectForInternalCd(warrior, skillId, out _))
            {
                return;
            }

            var ctx = new SkillEffectContext
            {
                Warrior = warrior,
                CombatStatus = _combatStatus
            };
            _skillEffectPipeline.Dispatch(SkillEffectTriggerHook.OnSkillInternalCooldown, ctx);
            if (!string.IsNullOrEmpty(ctx.TriggeredSkillId))
            {
                SkillIconPopup?.Invoke(warrior.WarriorId, ctx.TriggeredSkillId);
            }

            if (ctx.SkillPersistOn && !string.IsNullOrEmpty(ctx.SkillPersistSkillId))
            {
                SkillPersistChanged?.Invoke(warrior.WarriorId, ctx.SkillPersistSkillId, true);
            }
        }

        private bool TryResolveSkillEffectForInternalCd(
            DefendCombatWarriorState warrior,
            string skillId,
            out SkillEffectConfigRow effectRow)
        {
            effectRow = null;
            if (warrior?.SoldierSkills == null || _configs == null || string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            for (var i = 0; i < warrior.SoldierSkills.Count; i++)
            {
                var entry = warrior.SoldierSkills[i];
                if (entry == null || !string.Equals(entry.SkillId, skillId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out var skillRow) || skillRow == null)
                {
                    return false;
                }

                if (!_configs.TryGetSkillEffect(skillRow.SkillEffectId, out effectRow) || effectRow == null)
                {
                    return false;
                }

                return string.Equals(
                    effectRow.TriggerHook,
                    SkillEffectTriggerHook.OnSkillInternalCooldown,
                    StringComparison.Ordinal);
            }

            return false;
        }

        private void SeedInternalSkillCooldowns(
            DefendCombatWarriorState state,
            StatBlock battleStats,
            CombatConvertCoeffs coeffs)
        {
            if (state?.SoldierSkills == null || _configs == null)
            {
                return;
            }

            for (var i = 0; i < state.SoldierSkills.Count; i++)
            {
                var entry = state.SoldierSkills[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.SkillId))
                {
                    continue;
                }

                if (string.Equals(entry.SkillId, SoldierSkillCast.Skill03Id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out var skillRow) || skillRow == null)
                {
                    continue;
                }

                if (skillRow.BaseCooldownSeconds <= 0f)
                {
                    continue;
                }

                state.SkillInternalCooldownSeconds[entry.SkillId] = WarriorCombatMath.ComputeSkillCooldown(
                    battleStats.Intelligence,
                    skillRow.BaseCooldownSeconds,
                    coeffs);

                var initialCd = 0f;
                if (!string.IsNullOrWhiteSpace(skillRow.SkillEffectId)
                    && _configs.TryGetSkillEffect(skillRow.SkillEffectId, out var effectRow)
                    && effectRow != null
                    && string.Equals(
                        effectRow.EffectKind,
                        SkillEffectKind.StackingOutgoingMulTimed,
                        StringComparison.Ordinal)
                    && StackingOutgoingMulTimedHandler.TryParseTickSeconds(
                        effectRow,
                        skillRow,
                        out var tickSeconds)
                    && tickSeconds > 0f)
                {
                    initialCd = tickSeconds;
                }

                state.SkillInternalCdRemaining[entry.SkillId] = initialCd;
            }
        }

        /// <summary>
        /// D-073 SE-09: dispatch OnWarriorTargetAcquired. Handler may override to farthest
        /// enemy and a sampled landing. View Warps; this method does not touch Transform.
        /// </summary>
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
            if (!IsCombatGameplayActive || _skillEffectPipeline == null)
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior) || warrior == null)
            {
                return false;
            }

            if (warrior.IsRebel)
            {
                return false;
            }

            var ctx = new SkillEffectContext
            {
                Warrior = warrior,
                CombatStatus = _combatStatus,
                WarriorPositionXZ = warriorPositionXZ,
                HasWarriorPositionXZ = true,
                WarriorBodyRadius = warriorBodyRadius,
                ArriveEpsilon = CombatRuntimeTuning.MassMoveArriveEpsilon,
                AliveMonstersXZ = candidates,
                SampleWalkableXZ = sampleWalkableXZ
            };
            _skillEffectPipeline.Dispatch(SkillEffectTriggerHook.OnWarriorTargetAcquired, ctx);
            if (!ctx.HasTeleportOverride || string.IsNullOrEmpty(ctx.OverrideTargetRuntimeId))
            {
                return false;
            }

            if (!IsMonsterAlive(ctx.OverrideTargetRuntimeId))
            {
                return false;
            }

            overrideTargetId = ctx.OverrideTargetRuntimeId;
            teleportLandingXZ = ctx.TeleportLandingXZ;

            if (!string.IsNullOrEmpty(ctx.TriggeredSkillId))
            {
                SkillIconPopup?.Invoke(warrior.WarriorId, ctx.TriggeredSkillId);
            }

            if (ctx.CommittedInternalCooldown)
            {
                TryRollLossOfControlAfterSkillCast(warrior);
            }

            return true;
        }

        private void HandleMonsterBurnTick(string monsterRuntimeId, string sourceWarriorId, float tickDamage, string skillId)
        {
            if (!IsCombatGameplayActive || tickDamage <= 0f)
            {
                return;
            }

            if (!IsMonsterAlive(monsterRuntimeId) || !TryGetMonster(monsterRuntimeId, out var monster))
            {
                return;
            }

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - tickDamage);
            Debug.Log(
                $"[PushMapSession] BurnTick {sourceWarriorId} -> {monsterRuntimeId} skill={skillId} " +
                $"dmg={tickDamage:0.##} HP={monster.RemainingHp:0}/{monster.MaxHp}");

            MonsterDamageSettled?.Invoke(monsterRuntimeId, tickDamage);
            if (monster.RemainingHp <= 0f)
            {
                monster.IsAlive = false;
                _combatStatus.ClearMonster(monsterRuntimeId);
                _monstersKilled++;
                Debug.Log(
                    $"[PushMapSession] MonsterDead {monsterRuntimeId} ({monster.MonsterId}) " +
                    $"kills={_monstersKilled} (Burn)");
                MonsterKilled?.Invoke(monsterRuntimeId, sourceWarriorId ?? string.Empty);
            }
        }

        private void HandleWarriorInvincibleChanged(string warriorId, string skillId, bool on)
        {
            if (string.IsNullOrEmpty(warriorId) || string.IsNullOrEmpty(skillId))
            {
                return;
            }

            SkillPersistChanged?.Invoke(warriorId, skillId, on);
        }

        public bool TryGetSkillCooldownRemaining(string warriorId, out float remaining)
        {
            remaining = 0f;
            if (!TryGetWarrior(warriorId, out var warrior) || warrior == null)
            {
                return false;
            }

            remaining = Math.Max(0f, warrior.SkillCdRemaining);
            return !string.IsNullOrEmpty(warrior.CastSkillId);
        }

        /// <summary>
        /// D-069: commit Skill_03 burst (Mode2 CD starts now). Returns hit count (3) on success.
        /// Does not settle damage — View occupies the scheme-D channel.
        /// </summary>
        public bool TryCommitSkillBurst(string warriorId, out int burstHitCount)
        {
            burstHitCount = 0;
            if (!IsCombatGameplayActive)
            {
                return false;
            }

            if (!IsWarriorCombatActive(warriorId) || !TryGetWarrior(warriorId, out var warrior))
            {
                return false;
            }

            if (warrior.IsRebel)
            {
                return false;
            }

            if (string.IsNullOrEmpty(warrior.CastSkillId) || warrior.SkillCdRemaining > 0f)
            {
                return false;
            }

            if (!SoldierSkillCast.TryResolveSkill03(warrior.SoldierSkills, _configs, out var skillRow)
                || skillRow == null)
            {
                return false;
            }

            warrior.SkillCdRemaining = Math.Max(warrior.SkillCooldownSeconds, 0f);
            burstHitCount = SoldierSkillCast.BurstHitCount;
            Debug.Log(
                $"[PushMapSession] SkillCast {warrior.CastSkillId} Lv{warrior.CastSkillLevel} " +
                $"{warrior.WarriorId} hits={burstHitCount} cd={warrior.SkillCdRemaining:0.##}s");
            SkillIconPopup?.Invoke(warrior.WarriorId, SoldierSkillCast.Skill03Id);
            TryRollLossOfControlAfterSkillCast(warrior);
            return true;
        }

        private void TryRollLossOfControlAfterSkillCast(DefendCombatWarriorState warrior)
        {
            if (warrior == null || warrior.IsRebel || _lockedLossOfControlDegree <= 0f)
            {
                return;
            }

            var skillBonus = SoldierSkillGrant.SumLossOfControlChanceBonus(warrior.SoldierSkills, _configs);
            if (Math.Abs(skillBonus) < 0.0001f)
            {
                return;
            }

            var raceBonus = 0f;
            var gemBonus = 0f;
            if (_configs != null)
            {
                if (!string.IsNullOrEmpty(warrior.RaceId)
                    && _configs.TryGetRace(warrior.RaceId, out var raceRow)
                    && raceRow != null)
                {
                    raceBonus = raceRow.LossOfControlChanceBonus;
                }

                if (warrior.GemIds != null)
                {
                    for (var g = 0; g < warrior.GemIds.Count; g++)
                    {
                        if (_configs.TryGetGem(warrior.GemIds[g], out var gemRow) && gemRow != null)
                        {
                            gemBonus += gemRow.LossOfControlChanceBonus;
                        }
                    }
                }
            }

            var chance = LossOfControlMath.ComputeFinalLossChance(
                _lockedTierChance, raceBonus, gemBonus, skillBonus);
            var roll = UnityEngine.Random.value;
            var rebel = roll < chance;
            if (rebel)
            {
                SetWarriorRebel(warrior.WarriorId, true);
            }

            Debug.Log(
                $"[PushMapSession] SkillCastRebelRoll {warrior.WarriorId} chance={chance:0.###} " +
                $"roll={roll:0.###} → {(rebel ? "REBEL" : "loyal")} " +
                $"(SkillBonus={skillBonus:0.###})");
        }

        public bool TryGetMonster(string runtimeId, out DefendCombatMonsterState state)
        {
            state = null;
            return !string.IsNullOrEmpty(runtimeId)
                   && _monsters.TryGetValue(runtimeId, out state)
                   && state != null;
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

        /// <summary>IProjectileCombatSession: gates ProjectileView flight/settlement.</summary>
        public bool IsProjectileCombatActive(string warriorId)
        {
            return IsCombatGameplayActive && IsWarriorCombatActive(warriorId);
        }

        /// <summary>
        /// PM-12 melee HitConfirm: View finished windup and re-checked range; rules re-check
        /// alive + View-supplied inRange, then settle NormalAttackPower.
        /// </summary>
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

            return SettleMonsterDamage(warrior, monsterRuntimeId, "MeleeHit");
        }

        /// <summary>
        /// PM-12 ranged HitConfirm: View reports soft-collision hit; rules settle
        /// NormalAttackPower if still alive. Timeout miss must not call this.
        /// </summary>
        public bool TryConfirmRangedHit(string warriorId, string monsterRuntimeId)
        {
            return TryConfirmRangedHit(warriorId, monsterRuntimeId, flight: null);
        }

        /// <summary>
        /// Generic pierce settle (SE-07): Dispatch OnProjectileHit; Handler writes ExtraHitsRemaining.
        /// </summary>
        public bool TryConfirmRangedHit(
            string warriorId,
            string monsterRuntimeId,
            ProjectileHitFlightContext flight)
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

            return SettleMonsterDamage(warrior, monsterRuntimeId, "RangedHit", flight);
        }

        /// <summary>
        /// PM-13: monster normal attack on a loyal soldier — AttackPower, no armor.
        /// Skill_01 (SC-02): independent on-hit hook may zero this damage (still a hit).
        /// D-073 SE-02: invincible zeroes damage; OnWarriorWouldDie may intercept lethal hit.
        /// RemainingHp≤0 → CombatDead (gems → PermanentDeath mark Demo-min; no material polish).
        /// </summary>
        public bool TryApplyMonsterDamageToWarrior(string monsterRuntimeId, string warriorId, float attackPower)
        {
            if (!IsCombatGameplayActive)
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
            var blockNote = string.Empty;
            var blocked = SoldierSkillCast.TryRollSkill01Block(
                warrior.SoldierSkills, _configs, out var skill01Row, out var chance);
            if (skill01Row != null)
            {
                if (blocked)
                {
                    dmg = 0f;
                    SkillIconPopup?.Invoke(warriorId, SoldierSkillCast.Skill01Id);
                }

                blockNote =
                    $" Skill_01 Lv{skill01Row.SkillLevel} chance={chance:0.00} blocked={blocked}";
            }

            var invincibleNote = string.Empty;
            if (dmg > 0f && _combatStatus.IsWarriorInvincible(warriorId))
            {
                dmg = 0f;
                invincibleNote = " Invincible=1";
            }

            var interceptNote = string.Empty;
            var wouldDie = dmg > 0f && warrior.RemainingHp - dmg <= 0f;
            if (wouldDie && _skillEffectPipeline != null)
            {
                var ctx = new SkillEffectContext
                {
                    Warrior = warrior,
                    IncomingDamage = dmg,
                    CombatStatus = _combatStatus
                };
                _skillEffectPipeline.Dispatch(SkillEffectTriggerHook.OnWarriorWouldDie, ctx);
                if (ctx.WouldDieIntercepted)
                {
                    dmg = 0f;
                    interceptNote = $" Skill_05 intercept HP=1";
                    if (!string.IsNullOrEmpty(ctx.TriggeredSkillId))
                    {
                        SkillIconPopup?.Invoke(warriorId, ctx.TriggeredSkillId);
                    }
                }
            }

            if (dmg > 0f)
            {
                warrior.RemainingHp = Math.Max(0f, warrior.RemainingHp - dmg);
            }

            Debug.Log(
                $"[PushMapSession] MonsterHit {monsterRuntimeId} -> {warriorId} dmg={dmg:0.##} " +
                $"HP={warrior.RemainingHp:0}/{warrior.MaxHp}{blockNote}{invincibleNote}{interceptNote}");

            WarriorDamageSettled?.Invoke(warriorId, dmg);
            if (warrior.RemainingHp <= 0f)
            {
                EnterWarriorDowned(warrior);
            }
            else
            {
                SyncSkill02Persist(warrior);
            }

            return true;
        }

        private bool SettleMonsterDamage(
            DefendCombatWarriorState warrior,
            string monsterRuntimeId,
            string tag,
            ProjectileHitFlightContext flight = null)
        {
            if (!IsMonsterAlive(monsterRuntimeId) || !TryGetMonster(monsterRuntimeId, out var monster))
            {
                return false;
            }

            var dmg = warrior.NormalAttackPower;
            var comfortNote = string.Empty;
            var comfort = SoldierSkillCast.TryGetSkill02OutgoingBonus(
                warrior.SoldierSkills,
                _configs,
                warrior.RemainingHp,
                warrior.MaxHp,
                out var skill02Row,
                out var bonus);
            if (skill02Row != null)
            {
                if (comfort)
                {
                    dmg *= 1f + bonus;
                }

                comfortNote =
                    $" Skill_02 Lv{skill02Row.SkillLevel} bonus=+{bonus:0.00} applied={comfort}";
            }

            var isPierceExtra = flight?.AlreadyHitRuntimeIds != null && flight.AlreadyHitRuntimeIds.Count > 1;
            var isNewTargetFirstHit = !isPierceExtra && !string.Equals(
                warrior.LastNormalAttackTargetRuntimeId,
                monsterRuntimeId,
                StringComparison.Ordinal);
            var pipelineNote = string.Empty;
            if (_skillEffectPipeline != null)
            {
                var ctx = new SkillEffectContext
                {
                    Warrior = warrior,
                    TargetMonster = monster,
                    TargetMonsterRuntimeId = monsterRuntimeId,
                    OutgoingDamage = dmg,
                    IsNewTargetFirstHit = isNewTargetFirstHit,
                    CombatStatus = _combatStatus,
                    AlreadyHitRuntimeIds = flight != null ? flight.AlreadyHitRuntimeIds : null
                };
                var beforePipeline = dmg;
                if (flight != null)
                {
                    _skillEffectPipeline.Dispatch(SkillEffectTriggerHook.OnProjectileHit, ctx);
                    dmg = ctx.OutgoingDamage;
                    flight.ExtraHitsRemaining = Math.Max(0, ctx.ExtraHitsRemaining);
                    if (!string.IsNullOrEmpty(ctx.TriggeredSkillId))
                    {
                        SkillIconPopup?.Invoke(warrior.WarriorId, ctx.TriggeredSkillId);
                    }
                }

                _skillEffectPipeline.Dispatch(SkillEffectTriggerHook.OnOutgoingDamageSettle, ctx);
                dmg = ctx.OutgoingDamage;
                if (Math.Abs(dmg - beforePipeline) > 0.001f)
                {
                    pipelineNote = $" Pipeline mul={dmg / Math.Max(0.001f, beforePipeline):0.##} newTarget={isNewTargetFirstHit}";
                }
            }

            warrior.LastNormalAttackTargetRuntimeId = monsterRuntimeId;

            var hitCenter = default(Vector2);
            var hasHitCenter = TryResolveMonsterWorldXZ(monsterRuntimeId, out hitCenter);

            monster.RemainingHp = Math.Max(0f, monster.RemainingHp - dmg);
            Debug.Log(
                $"[PushMapSession] {tag} {warrior.WarriorId} -> {monsterRuntimeId} " +
                $"dmg={dmg:0.##} HP={monster.RemainingHp:0}/{monster.MaxHp}{comfortNote}{pipelineNote}");

            MonsterDamageSettled?.Invoke(monsterRuntimeId, dmg);
            if (monster.RemainingHp <= 0f)
            {
                monster.IsAlive = false;
                _combatStatus.ClearMonster(monsterRuntimeId);
                _monstersKilled++;
                Debug.Log($"[PushMapSession] MonsterDead {monsterRuntimeId} ({monster.MonsterId}) kills={_monstersKilled}");
                MonsterKilled?.Invoke(monsterRuntimeId, warrior.WarriorId);
            }

            if (_skillEffectPipeline != null && hasHitCenter)
            {
                FillAliveMonstersXZScratch();
                var aaCtx = new SkillEffectContext
                {
                    Warrior = warrior,
                    TargetMonster = monster,
                    TargetMonsterRuntimeId = monsterRuntimeId,
                    HitCenterXZ = hitCenter,
                    HasHitCenterXZ = true,
                    AliveMonstersXZ = _aliveMonstersXZScratch,
                    CombatStatus = _combatStatus
                };
                _skillEffectPipeline.Dispatch(SkillEffectTriggerHook.OnWarriorAaHitConfirm, aaCtx);
                if (!string.IsNullOrEmpty(aaCtx.TriggeredSkillId))
                {
                    SkillIconPopup?.Invoke(warrior.WarriorId, aaCtx.TriggeredSkillId);
                }

                if (aaCtx.CommittedInternalCooldown)
                {
                    TryRollLossOfControlAfterSkillCast(warrior);
                }
            }

            return true;
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
                var state = pair.Value;
                if (state == null || !state.IsAlive || state.RemainingHp <= 0f)
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

        private void EnterWarriorDowned(DefendCombatWarriorState warrior)
        {
            if (warrior == null || warrior.IsCombatDead)
            {
                return;
            }

            if (warrior.HasGems)
            {
                warrior.IsPermanentDead = true;
                warrior.IsCombatDead = true;
                Debug.LogWarning(
                    $"[PushMapSession] PermanentDeath (gems) {warrior.WarriorId} — material fate deferred; stop acting.");
            }
            else
            {
                warrior.IsCombatDead = true;
                Debug.Log($"[PushMapSession] CombatDead {warrior.WarriorId} (stop acting)");
            }

            WarriorCombatDead?.Invoke(warrior.WarriorId);
            _combatStatus.ClearWarrior(warrior.WarriorId);
            SyncSkill02Persist(warrior);
            TryEvaluateLoyalWipe();
        }

        /// <summary>
        /// D-071: after soldier Views exist, emit Skill_02 persist + one overhead popup
        /// for each registered warrior that starts Combat at full HP with Comfort.
        /// Do not call from TryRegisterWarrior — HUD is not bound yet.
        /// </summary>
        public void EmitStartBattleSkillIcons()
        {
            if (!_active || Phase != PushMapPhase.Combat)
            {
                return;
            }

            foreach (var pair in _warriors)
            {
                SyncSkill02Persist(pair.Value);
            }
        }

        /// <summary>
        /// Skill_02 persist on when held + full HP; off when damaged or CombatDead.
        /// Activate (off→on) also fires one overhead popup. Full-HP 0-damage block does not re-fire.
        /// </summary>
        private void SyncSkill02Persist(DefendCombatWarriorState warrior)
        {
            if (warrior == null || string.IsNullOrEmpty(warrior.WarriorId))
            {
                return;
            }

            var hasSkill02 = SoldierSkillCast.TryResolveSkill02(warrior.SoldierSkills, _configs, out _);
            var fullHp = !warrior.IsCombatDead &&
                         !warrior.IsPermanentDead &&
                         warrior.MaxHp > 0f &&
                         warrior.RemainingHp >= warrior.MaxHp;
            var wantOn = hasSkill02 && fullHp;
            var wasOn = _skill02PersistOn.Contains(warrior.WarriorId);
            if (wantOn == wasOn)
            {
                return;
            }

            if (wantOn)
            {
                _skill02PersistOn.Add(warrior.WarriorId);
                SkillPersistChanged?.Invoke(warrior.WarriorId, SoldierSkillCast.Skill02Id, true);
                SkillIconPopup?.Invoke(warrior.WarriorId, SoldierSkillCast.Skill02Id);
                return;
            }

            _skill02PersistOn.Remove(warrior.WarriorId);
            SkillPersistChanged?.Invoke(warrior.WarriorId, SoldierSkillCast.Skill02Id, false);
        }

        /// <summary>Sync View rebel roll into rules state; may trigger loyal wipe LevelFailure.</summary>
        public void SetWarriorRebel(string warriorId, bool isRebel)
        {
            if (!_active || Phase != PushMapPhase.Combat || string.IsNullOrEmpty(warriorId))
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

        /// <summary>
        /// LevelFailure when ≥1 warrior registered and no living loyal remains
        /// (!IsRebel && !IsCombatDead && RemainingHp&gt;0).
        /// </summary>
        public void TryEvaluateLoyalWipe()
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled || _warriors.Count < 1)
            {
                return;
            }

            foreach (var pair in _warriors)
            {
                var w = pair.Value;
                if (w != null && !w.IsRebel && !w.IsCombatDead && !w.IsPermanentDead && w.RemainingHp > 0f)
                {
                    return;
                }
            }

            RequestLevelFailure("我方士兵全灭");
        }

        /// <summary>Accumulate CaptureLoot entries already credited to Warehouse (display only).</summary>
        public void RecordCaptureLoot(IReadOnlyList<LootDropEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (string.IsNullOrEmpty(e.Id) || e.Count < 1)
                {
                    continue;
                }

                if (_captureLootLedger.TryGetValue(e.Id, out var existing))
                {
                    _captureLootLedger[e.Id] = existing + e.Count;
                }
                else
                {
                    _captureLootLedger[e.Id] = e.Count;
                }
            }
        }

        private void EnterVictory()
        {
            if (!_active || _outcomeSettled)
            {
                return;
            }

            _outcomeSettled = true;
            _isVictory = true;
            _combatEndRealtime = Time.realtimeSinceStartup;
            Phase = PushMapPhase.Ended;
            PhaseChanged?.Invoke(Phase);
            var exp = Config != null ? Math.Max(0, Config.StageExpReward) : 0;
            _stageExpCredited = exp;
            Debug.Log(
                $"[PushMapSession] VictorySettled StageExpReward=+{exp} kills={_monstersKilled} " +
                $"elapsed={CombatElapsedSeconds:0.##}s (Boss clear) — credit via presentation.");
            VictorySettled?.Invoke(exp);
        }

        /// <summary>Shield ≤ 0 or loyal wipe → LevelFailure (no stage Exp; §3.14 / §3.12).</summary>
        public void RequestLevelFailure(string reason = null)
        {
            if (!_active || _outcomeSettled)
            {
                return;
            }

            _outcomeSettled = true;
            _isVictory = false;
            _stageExpCredited = 0;
            _combatEndRealtime = Time.realtimeSinceStartup;
            Phase = PushMapPhase.Ended;
            PhaseChanged?.Invoke(Phase);
            Debug.LogWarning(
                $"[PushMapSession] LevelFailure{(string.IsNullOrEmpty(reason) ? string.Empty : ": " + reason)} " +
                $"kills={_monstersKilled} elapsed={CombatElapsedSeconds:0.##}s — abort Level, no stage Exp.");
            LevelFailureRequested?.Invoke();
        }
    }
}
