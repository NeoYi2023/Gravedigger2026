using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Pure Dig rules (SPEC_03 §3.10). Views subscribe; no Transform/Animator here.
    /// </summary>
    public sealed class DigSessionService
    {
        /// <summary>
        /// Cursor dwell before DigAction (CombatConstantConfig DigTriggerDwellSeconds).
        /// </summary>
        public float DigTriggerDwellSeconds { get; private set; } =
            CombatConstantKeys.Safety.DigTriggerDwellSeconds;

        public const int PlacementMaxRetries = 32;

        private readonly ConfigCsvRepository _configs;
        private readonly WarehouseService _warehouse;
        private readonly DigStageRewardLedger _ledger;
        private readonly DigProtagonistCapabilities _caps;
        private readonly ProtagonistEquipmentService _equipment;
        private readonly GmSoldierGrantService _soldierGrant;
        private readonly System.Random _rng = new System.Random();
        private readonly DigExplosiveScheduler _explosives = new DigExplosiveScheduler();
        private readonly DigLightningScheduler _lightning = new DigLightningScheduler();
        private readonly List<DigGraveRuntime> _blastScratch = new List<DigGraveRuntime>(16);
        private readonly List<DigGraveRuntime> _unclearedScratch = new List<DigGraveRuntime>(16);
        private bool _lightningWasEnabled;

        private DigGameplayConfigRow _config;
        private readonly List<WeightedFieldParser.WeightedId> _spawnWeights =
            new List<WeightedFieldParser.WeightedId>();
        private readonly List<DigGraveRuntime> _graves = new List<DigGraveRuntime>();
        private readonly Dictionary<int, DigGraveRuntime> _gravesById = new Dictionary<int, DigGraveRuntime>();

        private float _remainingSeconds;
        private float _spawnInterval;
        private int _baseSpawnCountPerInterval;
        private float _spawnAccumulator;
        private Vector3 _mapCenter;
        private Vector2 _placeableHalfExtents = new Vector2(5f, 2.5f);
        private int _nextGraveId = 1;
        private bool _active;
        private bool _timeUp;
        private bool _inputLocked;

        private Vector3 _cursorWorld;
        private bool _cursorValid;
        private float _dwellSeconds;
        private readonly Dictionary<int, float> _activeDigRemainingById = new Dictionary<int, float>();
        private readonly List<DigGraveRuntime> _eligibleScratch = new List<DigGraveRuntime>(16);
        private readonly List<int> _activeIdScratch = new List<int>(16);

        public event Action<float, float> RemainingTimeChanged;
        public event Action<DigGraveRuntime> GraveSpawned;
        public event Action<DigGraveRuntime> GraveUpdated;
        public event Action<DigGraveRuntime> DigActionStarted;
        public event Action<DigGraveRuntime> DigActionEnded;
        public event Action<DigGraveRuntime, string> GraveClearedForReward;
        public event Action<int, Vector3, Vector3, float> ExplosiveBarrelQueued;
        public event Action<int, Vector3, float, float> ExplosiveBlastStarted;
        public event Action<DigGraveRuntime> GraveRemovedWithoutLoot;
        public event Action<Vector3, float> LightningStrikeQueued;
        public event Action<string, Vector3, float> LightningSoldierPreview;
        public event Action<bool> DiggingPresenceChanged;
        public event Action StageTimeUp;
        public event Action WarehouseChanged;

        public DigSessionService(
            ConfigCsvRepository configs,
            WarehouseService warehouse,
            DigStageRewardLedger ledger,
            DigProtagonistCapabilities caps,
            ProtagonistEquipmentService equipment = null,
            GmSoldierGrantService soldierGrant = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _caps = caps ?? throw new ArgumentNullException(nameof(caps));
            _equipment = equipment;
            _soldierGrant = soldierGrant;
            _explosives.BarrelQueued += HandleBarrelQueued;
            _explosives.BlastStarted += HandleBlastStarted;
        }

        public bool IsActive => _active;
        public bool IsTimeUp => _timeUp;
        public float RemainingSeconds => _remainingSeconds;
        public float EffectiveDuration { get; private set; }
        public DigProtagonistCapabilities Capabilities => _caps;
        public DigStageRewardLedger Ledger => _ledger;
        public WarehouseService Warehouse => _warehouse;
        public Vector3 MapCenter => _mapCenter;
        public IReadOnlyList<DigGraveRuntime> Graves => _graves;
        public bool HasBusyGrave => _activeDigRemainingById.Count > 0;

        public void Begin(
            DigGameplayConfigRow config,
            Vector3 mapCenterWorldPosition,
            Vector2 placeableHalfExtents)
        {
            Stop();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            DigTriggerDwellSeconds = _configs != null
                ? _configs.GetDigTriggerDwellSeconds()
                : CombatConstantKeys.Safety.DigTriggerDwellSeconds;
            _mapCenter = mapCenterWorldPosition;
            _placeableHalfExtents = new Vector2(
                Mathf.Max(0.5f, placeableHalfExtents.x),
                Mathf.Max(0.5f, placeableHalfExtents.y));

            _spawnWeights.Clear();
            _spawnWeights.AddRange(WeightedFieldParser.ParseGraveSpawnWeights(config.GraveSpawnWeights));

            if (!WeightedFieldParser.TryParseSpawnRate(config.SpawnRate, out _spawnInterval, out _baseSpawnCountPerInterval))
            {
                _spawnInterval = 5f;
                _baseSpawnCountPerInterval = 0;
                Debug.LogWarning($"[DigSession] Bad SpawnRate '{config.SpawnRate}', process spawn disabled.");
            }

            EffectiveDuration = config.LevelDurationSeconds + _caps.DigStageDurationBonus;
            _remainingSeconds = EffectiveDuration;
            _spawnAccumulator = 0f;
            _ledger.Clear();
            _active = true;
            _timeUp = false;
            _inputLocked = false;
            _lightningWasEnabled = false;
            _lightning.Clear();
            ClearCursorState();
            RemainingTimeChanged?.Invoke(_remainingSeconds, EffectiveDuration);

            for (var i = 0; i < config.InitialGraveCount; i++)
            {
                TrySpawnOneGrave();
            }

            DiggingPresenceChanged?.Invoke(false);
            WarehouseChanged?.Invoke();
            var effectiveM = GetEffectiveSpawnCountPerInterval();
            Debug.Log(
                $"[DigSession] Begin Config={config.GameplayConfigId} Duration={EffectiveDuration:0.##}s Initial={config.InitialGraveCount} Map={config.DigMapId} SpawnRate N={_spawnInterval:0.##}s baseM={_baseSpawnCountPerInterval} bonus={(int)(_caps?.DigProcessSpawnCountBonus ?? 0f)} effectiveM={effectiveM}");
        }

        public void Stop()
        {
            CancelActiveDigAction(settleDamage: false);
            _explosives.Clear();
            _lightning.Clear();
            _lightningWasEnabled = false;
            _graves.Clear();
            _gravesById.Clear();
            _active = false;
            _timeUp = false;
            _inputLocked = true;
            ClearCursorState();
        }

        public void Tick(float deltaTime)
        {
            if (!_active || deltaTime <= 0f)
            {
                return;
            }

            if (_timeUp)
            {
                TickExplosives(deltaTime);
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
                TickExplosives(deltaTime);
                return;
            }

            // Prefer sampling first so dwell accumulates same frame as hover.
            TickCursorDwell(deltaTime);
            TickProcessSpawn(deltaTime);
            TickDigAction(deltaTime);
            TickExplosives(deltaTime);
            TickLightning(deltaTime);
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

        /// <summary>Demo GM: attempt up to <paramref name="count"/> weighted grave spawns (same rules as process spawn).</summary>
        public int DebugSpawnGraves(int count)
        {
            if (!_active || _timeUp || count < 1)
            {
                return 0;
            }

            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                if (TrySpawnOneGrave())
                {
                    spawned++;
                }
            }

            return spawned;
        }

        /// <summary>Demo GM: grant <paramref name="countEach"/> of every BodyPartConfig row (no AutoConvert).</summary>
        public void DebugGrantAllBodyParts(int countEach)
        {
            if (!_active || _timeUp || countEach < 1)
            {
                return;
            }

            foreach (var part in _configs.BodyParts)
            {
                if (part == null || string.IsNullOrEmpty(part.BodyPartId))
                {
                    continue;
                }

                _warehouse.AddItem(part.BodyPartId, countEach);
            }

            WarehouseChanged?.Invoke();
        }

        /// <summary>Called by View when DigReward flyer arrives at HUD portrait.</summary>
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

        private int GetEffectiveSpawnCountPerInterval()
        {
            var bonus = _caps != null ? (int)_caps.DigProcessSpawnCountBonus : 0;
            return Mathf.Max(0, _baseSpawnCountPerInterval + bonus);
        }

        private void TickProcessSpawn(float deltaTime)
        {
            var spawnCount = GetEffectiveSpawnCountPerInterval();
            if (spawnCount <= 0 || _spawnInterval <= 0f)
            {
                return;
            }

            _spawnAccumulator += deltaTime;
            while (_spawnAccumulator >= _spawnInterval)
            {
                _spawnAccumulator -= _spawnInterval;
                for (var i = 0; i < spawnCount; i++)
                {
                    TrySpawnOneGrave();
                }
            }
        }

        private void TickDigAction(float deltaTime)
        {
            if (_activeDigRemainingById.Count == 0)
            {
                return;
            }

            _activeIdScratch.Clear();
            foreach (var id in _activeDigRemainingById.Keys)
            {
                _activeIdScratch.Add(id);
            }

            for (var i = 0; i < _activeIdScratch.Count; i++)
            {
                var id = _activeIdScratch[i];
                if (!_activeDigRemainingById.TryGetValue(id, out var remaining))
                {
                    continue;
                }

                if (!_gravesById.TryGetValue(id, out var grave) || grave.IsCleared)
                {
                    _activeDigRemainingById.Remove(id);
                    continue;
                }

                remaining -= deltaTime;
                if (remaining > 0f)
                {
                    _activeDigRemainingById[id] = remaining;
                    continue;
                }

                _activeDigRemainingById.Remove(id);
                grave.IsBusy = false;
                DigActionEnded?.Invoke(grave);
                ApplyDamage(grave);
            }

            if (_activeDigRemainingById.Count == 0)
            {
                DiggingPresenceChanged?.Invoke(false);
            }
        }

        private void TickCursorDwell(float deltaTime)
        {
            if (_inputLocked || !_cursorValid)
            {
                return;
            }

            CollectEligibleGravesUnderCursor(_eligibleScratch);
            if (_eligibleScratch.Count == 0)
            {
                _dwellSeconds = 0f;
                return;
            }

            _dwellSeconds += deltaTime;
            if (_dwellSeconds < DigTriggerDwellSeconds)
            {
                return;
            }

            var hadBusy = HasBusyGrave;
            for (var i = 0; i < _eligibleScratch.Count; i++)
            {
                StartDigAction(_eligibleScratch[i], invokePresence: false);
            }

            if (!hadBusy && HasBusyGrave)
            {
                DiggingPresenceChanged?.Invoke(true);
            }

            _dwellSeconds = 0f;
        }

        private void CollectEligibleGravesUnderCursor(List<DigGraveRuntime> into)
        {
            into.Clear();
            var radius = _caps.DigCursorRadius;
            var cursorXZ = new Vector2(_cursorWorld.x, _cursorWorld.z);

            for (var i = 0; i < _graves.Count; i++)
            {
                var g = _graves[i];
                if (g.IsCleared || g.IsBusy)
                {
                    continue;
                }

                if (!_caps.DiggableQualityIds.Contains(g.QualityId))
                {
                    continue;
                }

                var graveXZ = new Vector2(g.WorldPosition.x, g.WorldPosition.z);
                var broadR = g.HasHitPolygon
                    ? g.HitBoundingRadius
                    : Mathf.Max(0.05f, g.ObstacleRadius);
                var dx = cursorXZ.x - graveXZ.x;
                var dz = cursorXZ.y - graveXZ.y;
                var broad = radius + broadR;
                if (dx * dx + dz * dz > broad * broad)
                {
                    continue;
                }

                if (g.HasHitPolygon)
                {
                    if (!DigHitShapeMath.CircleIntersectsConvexPolygonLocal(
                            cursorXZ, radius, g.HitLocalXZ, graveXZ))
                    {
                        continue;
                    }
                }
                else if (!DigHitShapeMath.CircleIntersectsCircle(cursorXZ, radius, graveXZ, broadR))
                {
                    continue;
                }

                into.Add(g);
            }
        }

        private void StartDigAction(DigGraveRuntime grave, bool invokePresence = true)
        {
            if (grave == null || grave.IsBusy || grave.IsCleared)
            {
                return;
            }

            var wasBusy = HasBusyGrave;
            grave.IsBusy = true;
            _activeDigRemainingById[grave.InstanceId] = _caps.DigActionDuration;
            DigActionStarted?.Invoke(grave);
            if (invokePresence && !wasBusy)
            {
                DiggingPresenceChanged?.Invoke(true);
            }
        }

        private void CancelActiveDigAction(bool settleDamage)
        {
            if (_activeDigRemainingById.Count == 0)
            {
                return;
            }

            _activeIdScratch.Clear();
            foreach (var id in _activeDigRemainingById.Keys)
            {
                _activeIdScratch.Add(id);
            }

            _activeDigRemainingById.Clear();

            for (var i = 0; i < _activeIdScratch.Count; i++)
            {
                var id = _activeIdScratch[i];
                if (!_gravesById.TryGetValue(id, out var grave))
                {
                    continue;
                }

                grave.IsBusy = false;
                DigActionEnded?.Invoke(grave);
                if (settleDamage)
                {
                    ApplyDamage(grave);
                }
            }

            DiggingPresenceChanged?.Invoke(false);
        }

        private void ApplyDamage(DigGraveRuntime grave)
        {
            ApplyDamageAmount(grave, _caps.DigDamage);
        }

        private void ApplyDamageAmount(DigGraveRuntime grave, float amount, bool triggerExplosiveOnClear = true)
        {
            if (grave == null || grave.IsCleared)
            {
                return;
            }

            grave.CurrentHP = Mathf.Max(0f, grave.CurrentHP - amount);
            GraveUpdated?.Invoke(grave);

            if (grave.CurrentHP > 0f)
            {
                return;
            }

            grave.IsCleared = true;
            var wasBusy = grave.IsBusy;
            grave.IsBusy = false;
            _activeDigRemainingById.Remove(grave.InstanceId);
            if (wasBusy)
            {
                DigActionEnded?.Invoke(grave);
                if (_activeDigRemainingById.Count == 0)
                {
                    DiggingPresenceChanged?.Invoke(false);
                }
            }
            var origin = grave.WorldPosition;
            var settled = LootDropParser.Resolve(
                grave.LootDropEncoded,
                grave.DropMode,
                _rng,
                msg => Debug.LogWarning($"[DigSession] {msg}"));
            var settledEncoded = LootDropParser.Encode(settled);
            grave.LootDropEncoded = settledEncoded;
            if (triggerExplosiveOnClear)
            {
                TryEnqueueExplosiveBarrel(origin);
            }
            GraveClearedForReward?.Invoke(grave, settledEncoded);
            _graves.Remove(grave);
            _gravesById.Remove(grave.InstanceId);
        }

        private void TickExplosives(float deltaTime)
        {
            _explosives.Tick(deltaTime, ApplyExplosiveAreaDamage);
        }

        private void TickLightning(float deltaTime)
        {
            if (!TryGetLightningEffect(out var effect))
            {
                _lightningWasEnabled = false;
                _lightning.Clear();
                return;
            }

            if (!_lightningWasEnabled)
            {
                _lightning.Reset(effect.IntervalSeconds);
                _lightningWasEnabled = true;
            }

            _lightning.Tick(deltaTime, effect.IntervalSeconds, () => FireLightning(effect));
        }

        private bool TryGetLightningEffect(out DigLightningEffectConfig effect)
        {
            effect = null;
            if (_equipment == null ||
                !_equipment.TryGetOwned(DigLightningEffectConfig.EquipId, out var owned) ||
                owned == null)
            {
                return false;
            }

            if (!_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level, out var row) ||
                row == null ||
                !EffectDomainIncludesDig(row.EffectDomain) ||
                !DigLightningEffectConfig.TryParse(row, out effect))
            {
                return false;
            }

            return true;
        }

        private void FireLightning(DigLightningEffectConfig effect)
        {
            if (effect == null)
            {
                return;
            }

            CollectUnclearedGraves(_unclearedScratch);
            if (_unclearedScratch.Count > 0)
            {
                var grave = _unclearedScratch[_rng.Next(_unclearedScratch.Count)];
                var pos = grave.WorldPosition;
                LightningStrikeQueued?.Invoke(pos, effect.FrameSeconds);

                string appearanceId = null;
                if (DigLightningEffectConfig.TryPickPrimaryHand(
                        grave.LootDropEncoded, _configs, _rng, out var hand) &&
                    DigLightningEffectConfig.TryPickClassId(hand.ClassRestrict, _rng, out var classId) &&
                    _soldierGrant != null &&
                    _soldierGrant.TryGrantOne(classId, hand.RaceId, out var warrior, out _))
                {
                    appearanceId = warrior != null ? warrior.AppearanceId : null;
                }

                RemoveGraveWithoutLoot(grave);
                if (!string.IsNullOrEmpty(appearanceId))
                {
                    LightningSoldierPreview?.Invoke(appearanceId, pos, effect.PreviewSeconds);
                }

                return;
            }

            if (!TrySamplePlaceablePosition(out var fallback, out _))
            {
                fallback = _mapCenter;
            }

            LightningStrikeQueued?.Invoke(fallback, effect.FrameSeconds);
        }

        private void CollectUnclearedGraves(List<DigGraveRuntime> into)
        {
            into.Clear();
            for (var i = 0; i < _graves.Count; i++)
            {
                var g = _graves[i];
                if (g != null && !g.IsCleared)
                {
                    into.Add(g);
                }
            }
        }

        private void RemoveGraveWithoutLoot(DigGraveRuntime grave)
        {
            if (grave == null || grave.IsCleared)
            {
                return;
            }

            grave.IsCleared = true;
            var wasBusy = grave.IsBusy;
            grave.IsBusy = false;
            _activeDigRemainingById.Remove(grave.InstanceId);
            if (wasBusy)
            {
                DigActionEnded?.Invoke(grave);
                if (_activeDigRemainingById.Count == 0)
                {
                    DiggingPresenceChanged?.Invoke(false);
                }
            }

            GraveRemovedWithoutLoot?.Invoke(grave);
            _graves.Remove(grave);
            _gravesById.Remove(grave.InstanceId);
        }

        private void ApplyExplosiveAreaDamage(Vector3 center, float radius, float damage)
        {
            _blastScratch.Clear();
            var rSqr = radius * radius;
            for (var i = 0; i < _graves.Count; i++)
            {
                var g = _graves[i];
                if (g == null || g.IsCleared)
                {
                    continue;
                }

                var dx = g.WorldPosition.x - center.x;
                var dz = g.WorldPosition.z - center.z;
                if (dx * dx + dz * dz <= rSqr)
                {
                    _blastScratch.Add(g);
                }
            }

            for (var i = 0; i < _blastScratch.Count; i++)
            {
                ApplyDamageAmount(_blastScratch[i], damage, triggerExplosiveOnClear: false);
            }
        }

        private void TryEnqueueExplosiveBarrel(Vector3 origin)
        {
            if (_equipment == null ||
                !_equipment.TryGetOwned(DigExplosiveEffectConfig.EquipId, out var owned) ||
                owned == null)
            {
                return;
            }

            if (!_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level, out var row) ||
                row == null ||
                !EffectDomainIncludesDig(row.EffectDomain) ||
                !DigExplosiveEffectConfig.TryParse(row, out var effect))
            {
                return;
            }

            if (effect.TriggerChance < 1f && _rng.NextDouble() > effect.TriggerChance)
            {
                return;
            }

            if (!TrySampleRingPosition(origin, effect.ThrowRadius, out var target))
            {
                Debug.LogWarning("[DigSession] Explosive barrel landing sample failed — skip throw.");
                return;
            }

            _explosives.Enqueue(
                origin,
                target,
                effect.FlightSeconds,
                effect.FuseSeconds,
                effect.BlastRadius,
                effect.BlastDamage,
                effect.RingSeconds);
        }

        private bool TrySampleRingPosition(Vector3 origin, float throwRadius, out Vector3 position)
        {
            position = origin;
            var radius = Mathf.Max(0.01f, throwRadius);
            for (var attempt = 0; attempt < PlacementMaxRetries; attempt++)
            {
                var angle = _rng.NextDouble() * Math.PI * 2.0;
                var candidate = new Vector3(
                    origin.x + (float)(Math.Cos(angle) * radius),
                    origin.y,
                    origin.z + (float)(Math.Sin(angle) * radius));
                if (MapFootprintMath.ContainsXZ(_mapCenter, _placeableHalfExtents, candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            return false;
        }

        private void HandleBarrelQueued(int barrelId, Vector3 origin, Vector3 target, float flightSeconds)
        {
            ExplosiveBarrelQueued?.Invoke(barrelId, origin, target, flightSeconds);
        }

        private void HandleBlastStarted(int barrelId, Vector3 center, float blastRadius, float ringSeconds)
        {
            ExplosiveBlastStarted?.Invoke(barrelId, center, blastRadius, ringSeconds);
        }

        private bool TrySpawnOneGrave()
        {
            var effective = WeightedFieldParser.OverlaySpawnWeightBonuses(
                _spawnWeights,
                _caps != null ? _caps.GraveSpawnWeightBonus : null);
            var qualityId = WeightedFieldParser.PickWeighted(effective, _rng);
            if (string.IsNullOrEmpty(qualityId))
            {
                return false;
            }

            if (!_configs.TryGetGraveQuality(qualityId, out var quality))
            {
                Debug.LogWarning($"[DigSession] QualityId '{qualityId}' missing from GraveQualityConfig — skip spawn.");
                return false;
            }

            if (!TrySamplePlaceablePosition(out var pos, out var radius))
            {
                return false;
            }

            var grave = new DigGraveRuntime
            {
                InstanceId = _nextGraveId++,
                QualityId = qualityId,
                MaxHP = quality.MaxHP,
                CurrentHP = quality.MaxHP,
                WorldPosition = pos,
                ObstacleRadius = radius,
                DropMode = quality.DropMode,
                LootDropEncoded = quality.LootDrop
            };

            _graves.Add(grave);
            _gravesById[grave.InstanceId] = grave;
            GraveSpawned?.Invoke(grave);
            return true;
        }

        private bool TrySamplePlaceablePosition(out Vector3 position, out float graveRadius)
        {
            graveRadius = 0.55f;
            position = _mapCenter;

            for (var attempt = 0; attempt < PlacementMaxRetries; attempt++)
            {
                var x = (float)((_rng.NextDouble() * 2.0 - 1.0) * _placeableHalfExtents.x);
                var z = (float)((_rng.NextDouble() * 2.0 - 1.0) * _placeableHalfExtents.y);
                var candidate = new Vector3(_mapCenter.x + x, _mapCenter.y, _mapCenter.z + z);

                if (!MapFootprintMath.ContainsXZ(_mapCenter, _placeableHalfExtents, candidate))
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

        private static bool EffectDomainIncludesDig(string effectDomain)
        {
            if (string.IsNullOrEmpty(effectDomain))
            {
                return false;
            }

            var segments = effectDomain.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length; i++)
            {
                if (string.Equals(segments[i].Trim(), "Dig", StringComparison.Ordinal))
                {
                    return true;
                }
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
            _dwellSeconds = 0f;
            _cursorValid = false;
        }
    }
}
