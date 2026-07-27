using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Pure Dig rules (SPEC_03 §3.10). Views subscribe; no Transform/Animator here.
    /// </summary>
    public sealed class DigSessionService
    {
        public const float DigTriggerDwellSeconds = 0.2f;
        public const int PlacementMaxRetries = 32;

        private readonly ConfigCsvRepository _configs;
        private readonly WarehouseService _warehouse;
        private readonly DigStageRewardLedger _ledger;
        private readonly DigProtagonistCapabilities _caps;
        private readonly System.Random _rng = new System.Random();

        private DigGameplayConfigRow _config;
        private readonly List<WeightedFieldParser.WeightedId> _spawnWeights =
            new List<WeightedFieldParser.WeightedId>();
        private readonly List<DigGraveRuntime> _graves = new List<DigGraveRuntime>();
        private readonly Dictionary<int, DigGraveRuntime> _gravesById = new Dictionary<int, DigGraveRuntime>();

        private float _remainingSeconds;
        private float _spawnInterval;
        private int _spawnCountPerInterval;
        private float _spawnAccumulator;
        private float _diggerObstacleRadius = 0.8f;
        private Vector3 _diggerPosition;
        private Vector2 _placeableHalfExtents = new Vector2(5f, 2.5f);
        private int _nextGraveId = 1;
        private bool _active;
        private bool _timeUp;
        private bool _inputLocked;

        private Vector3 _cursorWorld;
        private bool _cursorValid;
        private int _dwellGraveId = -1;
        private float _dwellSeconds;
        private int _activeDigGraveId = -1;
        private float _activeDigRemaining;

        public event Action<float, float> RemainingTimeChanged;
        public event Action<DigGraveRuntime> GraveSpawned;
        public event Action<DigGraveRuntime> GraveUpdated;
        public event Action<DigGraveRuntime> DigActionStarted;
        public event Action<DigGraveRuntime> DigActionEnded;
        public event Action<DigGraveRuntime, string> GraveClearedForReward;
        public event Action<bool> DiggingPresenceChanged;
        public event Action StageTimeUp;
        public event Action WarehouseChanged;

        public DigSessionService(
            ConfigCsvRepository configs,
            WarehouseService warehouse,
            DigStageRewardLedger ledger,
            DigProtagonistCapabilities caps)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _caps = caps ?? throw new ArgumentNullException(nameof(caps));
        }

        public bool IsActive => _active;
        public bool IsTimeUp => _timeUp;
        public float RemainingSeconds => _remainingSeconds;
        public float EffectiveDuration { get; private set; }
        public DigProtagonistCapabilities Capabilities => _caps;
        public DigStageRewardLedger Ledger => _ledger;
        public WarehouseService Warehouse => _warehouse;
        public Vector3 DiggerPosition => _diggerPosition;
        public IReadOnlyList<DigGraveRuntime> Graves => _graves;
        public bool HasBusyGrave => _activeDigGraveId >= 0;

        public void Begin(
            DigGameplayConfigRow config,
            Vector3 diggerWorldPosition,
            float diggerObstacleRadius,
            Vector2 placeableHalfExtents)
        {
            Stop();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _diggerPosition = diggerWorldPosition;
            _diggerObstacleRadius = Mathf.Max(0.05f, diggerObstacleRadius);
            _placeableHalfExtents = new Vector2(
                Mathf.Max(0.5f, placeableHalfExtents.x),
                Mathf.Max(0.5f, placeableHalfExtents.y));

            _spawnWeights.Clear();
            _spawnWeights.AddRange(WeightedFieldParser.ParseGraveSpawnWeights(config.GraveSpawnWeights));

            if (!WeightedFieldParser.TryParseSpawnRate(config.SpawnRate, out _spawnInterval, out _spawnCountPerInterval))
            {
                _spawnInterval = 5f;
                _spawnCountPerInterval = 0;
                Debug.LogWarning($"[DigSession] Bad SpawnRate '{config.SpawnRate}', process spawn disabled.");
            }

            EffectiveDuration = config.LevelDurationSeconds + _caps.DigStageDurationBonus;
            _remainingSeconds = EffectiveDuration;
            _spawnAccumulator = 0f;
            _ledger.Clear();
            _active = true;
            _timeUp = false;
            _inputLocked = false;
            ClearCursorState();
            RemainingTimeChanged?.Invoke(_remainingSeconds, EffectiveDuration);

            for (var i = 0; i < config.InitialGraveCount; i++)
            {
                TrySpawnOneGrave();
            }

            DiggingPresenceChanged?.Invoke(false);
            WarehouseChanged?.Invoke();
            Debug.Log(
                $"[DigSession] Begin Config={config.GameplayConfigId} Duration={EffectiveDuration:0.##}s Initial={config.InitialGraveCount} Map={config.DigMapId}");
        }

        public void Stop()
        {
            CancelActiveDigAction(settleDamage: false);
            _graves.Clear();
            _gravesById.Clear();
            _active = false;
            _timeUp = false;
            _inputLocked = true;
            ClearCursorState();
        }

        public void Tick(float deltaTime)
        {
            if (!_active || _timeUp || deltaTime <= 0f)
            {
                return;
            }

            _remainingSeconds -= deltaTime;
            if (_remainingSeconds < 0f)
            {
                _remainingSeconds = 0f;
            }

            RemainingTimeChanged?.Invoke(_remainingSeconds, EffectiveDuration);

            if (_remainingSeconds <= 0f)
            {
                HandleTimeUp();
                return;
            }

            // Prefer sampling first so dwell accumulates same frame as hover.
            TickCursorDwell(deltaTime);
            TickProcessSpawn(deltaTime);
            TickDigAction(deltaTime);
        }

        public void SetCursorWorld(Vector3 worldPosition, bool valid)
        {
            _cursorWorld = worldPosition;
            _cursorValid = valid && !_inputLocked && _active && !_timeUp;
            if (!_cursorValid)
            {
                ClearCursorState();
            }
        }

        /// <summary>Called by View when DigReward flyer arrives at digger.</summary>
        public void CreditPendingLoot(string lootDropEncoded)
        {
            if (string.IsNullOrEmpty(lootDropEncoded))
            {
                return;
            }

            var entries = LootDropParser.Parse(
                lootDropEncoded,
                msg => Debug.LogWarning($"[DigSession] {msg}"));

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                _warehouse.CreditLootEntry(
                    entry,
                    _configs,
                    (matId, count) =>
                    {
                        _ledger.Add(matId, count);
                    },
                    spirit =>
                    {
                        _ledger.Add(LootDropParser.SpiritId, spirit);
                    });
            }

            WarehouseChanged?.Invoke();
        }

        private void HandleTimeUp()
        {
            if (_timeUp)
            {
                return;
            }

            _timeUp = true;
            _inputLocked = true;
            CancelActiveDigAction(settleDamage: false);
            ClearCursorState();
            DiggingPresenceChanged?.Invoke(false);
            StageTimeUp?.Invoke();
            Debug.Log("[DigSession] Effective duration reached 0 — stage settlement.");
        }

        private void TickProcessSpawn(float deltaTime)
        {
            if (_spawnCountPerInterval <= 0 || _spawnInterval <= 0f)
            {
                return;
            }

            _spawnAccumulator += deltaTime;
            while (_spawnAccumulator >= _spawnInterval)
            {
                _spawnAccumulator -= _spawnInterval;
                for (var i = 0; i < _spawnCountPerInterval; i++)
                {
                    TrySpawnOneGrave();
                }
            }
        }

        private void TickDigAction(float deltaTime)
        {
            if (_activeDigGraveId < 0)
            {
                return;
            }

            if (!_gravesById.TryGetValue(_activeDigGraveId, out var grave) || grave.IsCleared)
            {
                CancelActiveDigAction(settleDamage: false);
                return;
            }

            _activeDigRemaining -= deltaTime;
            if (_activeDigRemaining > 0f)
            {
                return;
            }

            var finished = grave;
            finished.IsBusy = false;
            _activeDigGraveId = -1;
            _activeDigRemaining = 0f;
            DigActionEnded?.Invoke(finished);
            DiggingPresenceChanged?.Invoke(false);

            ApplyDamage(finished);
        }

        private void TickCursorDwell(float deltaTime)
        {
            if (_inputLocked || !_cursorValid || _activeDigGraveId >= 0)
            {
                return;
            }

            var target = FindGraveUnderCursor();
            if (target == null)
            {
                ClearCursorState();
                return;
            }

            if (!_caps.DiggableQualityIds.Contains(target.QualityId))
            {
                ClearCursorState();
                return;
            }

            if (target.IsBusy || target.IsCleared)
            {
                ClearCursorState();
                return;
            }

            if (_dwellGraveId != target.InstanceId)
            {
                _dwellGraveId = target.InstanceId;
                _dwellSeconds = 0f;
            }

            _dwellSeconds += deltaTime;
            if (_dwellSeconds < DigTriggerDwellSeconds)
            {
                return;
            }

            StartDigAction(target);
            _dwellSeconds = 0f;
        }

        private DigGraveRuntime FindGraveUnderCursor()
        {
            DigGraveRuntime best = null;
            var bestDist = float.MaxValue;
            var radius = _caps.DigCursorRadius;

            for (var i = 0; i < _graves.Count; i++)
            {
                var g = _graves[i];
                if (g.IsCleared)
                {
                    continue;
                }

                var flat = g.WorldPosition;
                flat.y = _cursorWorld.y;
                var dist = Vector3.Distance(_cursorWorld, flat);
                if (dist <= radius && dist < bestDist)
                {
                    bestDist = dist;
                    best = g;
                }
            }

            return best;
        }

        private void StartDigAction(DigGraveRuntime grave)
        {
            grave.IsBusy = true;
            _activeDigGraveId = grave.InstanceId;
            _activeDigRemaining = _caps.DigActionDuration;
            DigActionStarted?.Invoke(grave);
            DiggingPresenceChanged?.Invoke(true);
        }

        private void CancelActiveDigAction(bool settleDamage)
        {
            if (_activeDigGraveId < 0)
            {
                return;
            }

            if (_gravesById.TryGetValue(_activeDigGraveId, out var grave))
            {
                grave.IsBusy = false;
                DigActionEnded?.Invoke(grave);
                if (settleDamage)
                {
                    ApplyDamage(grave);
                }
            }

            _activeDigGraveId = -1;
            _activeDigRemaining = 0f;
            DiggingPresenceChanged?.Invoke(HasBusyGrave);
        }

        private void ApplyDamage(DigGraveRuntime grave)
        {
            if (grave == null || grave.IsCleared)
            {
                return;
            }

            grave.CurrentHP = Mathf.Max(0f, grave.CurrentHP - _caps.DigDamage);
            GraveUpdated?.Invoke(grave);

            if (grave.CurrentHP > 0f)
            {
                return;
            }

            grave.IsCleared = true;
            grave.IsBusy = false;
            GraveClearedForReward?.Invoke(grave, grave.LootDropEncoded);
            _graves.Remove(grave);
            _gravesById.Remove(grave.InstanceId);
        }

        private void TrySpawnOneGrave()
        {
            var qualityId = WeightedFieldParser.PickWeighted(_spawnWeights, _rng);
            if (string.IsNullOrEmpty(qualityId))
            {
                return;
            }

            if (!_configs.TryGetGraveQuality(qualityId, out var quality))
            {
                Debug.LogWarning($"[DigSession] QualityId '{qualityId}' missing from GraveQualityConfig — skip spawn.");
                return;
            }

            if (!TrySamplePlaceablePosition(out var pos, out var radius))
            {
                return;
            }

            var grave = new DigGraveRuntime
            {
                InstanceId = _nextGraveId++,
                QualityId = qualityId,
                MaxHP = quality.MaxHP,
                CurrentHP = quality.MaxHP,
                WorldPosition = pos,
                ObstacleRadius = radius,
                LootDropEncoded = quality.LootDrop
            };

            _graves.Add(grave);
            _gravesById[grave.InstanceId] = grave;
            GraveSpawned?.Invoke(grave);
        }

        private bool TrySamplePlaceablePosition(out Vector3 position, out float graveRadius)
        {
            graveRadius = 0.55f;
            position = _diggerPosition;

            for (var attempt = 0; attempt < PlacementMaxRetries; attempt++)
            {
                var x = (float)((_rng.NextDouble() * 2.0 - 1.0) * _placeableHalfExtents.x);
                var z = (float)((_rng.NextDouble() * 2.0 - 1.0) * _placeableHalfExtents.y);
                var candidate = new Vector3(_diggerPosition.x + x, _diggerPosition.y, _diggerPosition.z + z);

                if (!MapFootprintMath.ContainsXZ(_diggerPosition, _placeableHalfExtents, candidate))
                {
                    continue;
                }

                if (CirclesOverlap(candidate, graveRadius, _diggerPosition, _diggerObstacleRadius))
                {
                    continue;
                }

                var blocked = false;
                for (var i = 0; i < _graves.Count; i++)
                {
                    var g = _graves[i];
                    if (g.IsCleared)
                    {
                        continue;
                    }

                    if (CirclesOverlap(candidate, graveRadius, g.WorldPosition, g.ObstacleRadius))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            return false;
        }

        private static bool CirclesOverlap(Vector3 a, float ra, Vector3 b, float rb)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            var min = ra + rb;
            return dx * dx + dz * dz < min * min;
        }

        private void ClearCursorState()
        {
            _dwellGraveId = -1;
            _dwellSeconds = 0f;
            _cursorValid = false;
        }
    }
}
