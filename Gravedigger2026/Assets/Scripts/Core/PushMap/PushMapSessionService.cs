using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.PushMap
{
    /// <summary>
    /// PushMap stage rules (PM-03–PM-07, Approach A).
    /// Prepare→Combat, StartBattle ≥1, Shield, LOC lock, objective capture, spawn/trap,
    /// Boss clear → VictorySettled(StageExpReward), CaptureLoot/DungeonUnlock hooks.
    /// Position resolution and instantiation are View concerns.
    /// </summary>
    public sealed class PushMapSessionService
    {
        private bool _active;
        private bool _outcomeSettled;
        private int _shield;
        private int _shieldCap;
        private float _lockedLossOfControlDegree;
        private int _lockedLossOfControlTierId;
        private float _lockedTierChance;
        private int _pendingBossCount;

        public event Action<PushMapPhase> PhaseChanged;
        public event Action<int, int> ShieldChanged;
        public event Action LevelFailureRequested;
        public event Action<long> VictorySettled;
        public event Action<int> ObjectiveCaptured;
        public event Action<int> CurrentObjectiveChanged;
        public event Action<PushMapSpawnRequest> PushMapSpawnRequested;

        private const float MinCaptureSeconds = 0.01f;

        private readonly List<int> _objectiveOrders = new List<int>();
        private readonly HashSet<int> _capturedObjectives = new HashSet<int>();
        private int _currentObjectiveOrder;
        private float _captureTimerSeconds;

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

        /// <summary>Capture seconds required (Config.CaptureSeconds, clamped ≥0.01; §3.14).</summary>
        public float CaptureSecondsRequired => Mathf.Max(MinCaptureSeconds, Config?.CaptureSeconds ?? 5f);

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
                $"ExpReward={config.StageExpReward} CaptureSeconds={config.CaptureSeconds}");
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
            _lockedLossOfControlDegree = 0f;
            _lockedLossOfControlTierId = 0;
            _lockedTierChance = 0f;
            _objectiveOrders.Clear();
            _capturedObjectives.Clear();
            _currentObjectiveOrder = 0;
            _captureTimerSeconds = 0f;
            _spawnRows.Clear();
            _trapSpawnPointsFired.Clear();
            _spawnPointsStoppedByCapture.Clear();
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
            Debug.Log(
                $"[PushMapSession] StartBattle Shield={_shield} Deployed={deployedSoldierCount} " +
                $"Degree={_lockedLossOfControlDegree:0.###} Tier={_lockedLossOfControlTierId} " +
                $"TierChance={_lockedTierChance:0.###}");

            LoadSpawnRowsAndFireStartBattle(configs);
            return true;
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

                    FireRow(row, PushMapSpawnTrigger.Trap);
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

        private void LoadSpawnRowsAndFireStartBattle(ConfigCsvRepository configs)
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

                FireRow(row, PushMapSpawnTrigger.StartBattle);
            }
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

        private void FireRow(PushMapSpawnConfigRow row, PushMapSpawnTrigger trigger)
        {
            if (row == null || row.SpawnCount < 1)
            {
                return;
            }

            if (row.IsBoss)
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
            _captureTimerSeconds = 0f;

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
                $"CurrentObjective={_currentObjectiveOrder} CaptureSeconds={CaptureSecondsRequired:0.##}");
            if (previous != _currentObjectiveOrder)
            {
                CurrentObjectiveChanged?.Invoke(_currentObjectiveOrder);
            }
        }

        /// <summary>
        /// PM-04: tick capture for CurrentObjective. hasLivingMonsterInCurrentZone=true → timer resets;
        /// otherwise accumulate; reaching CaptureSecondsRequired → capture + ObjectiveCaptured + advance.
        /// </summary>
        public void TickCapture(float deltaTime, bool hasLivingMonsterInCurrentZone)
        {
            if (!_active || Phase != PushMapPhase.Combat || _outcomeSettled || _currentObjectiveOrder <= 0)
            {
                return;
            }

            if (hasLivingMonsterInCurrentZone)
            {
                if (_captureTimerSeconds > 0f)
                {
                    Debug.Log($"[PushMapSession] Capture reset (monster in zone) Objective={_currentObjectiveOrder}");
                }
                _captureTimerSeconds = 0f;
                return;
            }

            _captureTimerSeconds += deltaTime;
            if (_captureTimerSeconds < CaptureSecondsRequired)
            {
                return;
            }

            Capture(_currentObjectiveOrder);
        }

        private void Capture(int order)
        {
            _captureTimerSeconds = 0f;
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

        private void EnterVictory()
        {
            if (!_active || _outcomeSettled)
            {
                return;
            }

            _outcomeSettled = true;
            Phase = PushMapPhase.Ended;
            PhaseChanged?.Invoke(Phase);
            var exp = Config != null ? Math.Max(0, Config.StageExpReward) : 0;
            Debug.Log($"[PushMapSession] VictorySettled StageExpReward=+{exp} (Boss clear) — credit via presentation.");
            VictorySettled?.Invoke(exp);
        }

        /// <summary>Shield ≤ 0 → LevelFailure (no stage Exp; §3.14 / §3.12).</summary>
        public void RequestLevelFailure(string reason = null)
        {
            if (!_active || _outcomeSettled)
            {
                return;
            }

            _outcomeSettled = true;
            Phase = PushMapPhase.Ended;
            PhaseChanged?.Invoke(Phase);
            Debug.LogWarning(
                $"[PushMapSession] LevelFailure{(string.IsNullOrEmpty(reason) ? string.Empty : ": " + reason)} — abort Level, no stage Exp (VictorySettled not fired).");
            LevelFailureRequested?.Invoke();
        }
    }
}
