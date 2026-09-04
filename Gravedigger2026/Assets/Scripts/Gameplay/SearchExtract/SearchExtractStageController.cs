using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.Rewards;
using Gravedigger2026.Core.SearchExtract;
using Gravedigger2026.Core.TacticalFormation;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Combat;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.PushMap;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.SearchExtract
{
    /// <summary>
    /// SearchExtract stage presentation (SE-03 Approach A). Prepare reuses FormationEditorRoot;
    /// StartBattle ≥1 deploys soldiers, bakes NavMesh+AirWall. SE-04: zone activation + gather countdown HUD.
    /// Pre-activation: FormationHome approach to current Objective (v0.83.92).
    /// D-074: monster death skills via IMonsterDeathSkillHost (shared with PushMap).
    /// SE-05: MassMove FormationHome relocate around Objective + tactical virtual center snap.
    /// SE-06: directional wave spawn after gather activation; PushMapMonsterAgentView + BodyRadius spread.
    /// SE-07: point success → invincible + clear monsters + UI-032 decision panel.
    /// SE-08: point loot via RewardGrantService; Continue advances order + relocate; Leave → StageExp + UI-017 → TryAdvanceStage.
    /// SE-09: active-gather loyal wipe → UI-017 defeat → TitleMenu / Restart.
    /// v0.83.93: Combat camera follows CameraFollowPath + soldiers (PushMapCameraFollowController).
    /// v0.83.94: Combat MassMove Tick includes monsters + AttackSlot chase refresh (same as PushMap).
    /// No BattleProtagonist.
    /// </summary>
    public sealed class SearchExtractStageController : MonoBehaviour
    {
        private const string DecisionPanelResourcePath = "UI/SearchExtract/SearchExtractDecisionPanel";

        private DefendPrefabCatalog _catalog;
        private FormationPrefabCatalog _formationCatalog;
        private ConfigCsvRepository _configs;
        private ProtagonistProgressService _progress;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private WarehouseService _warehouse;
        private SpecialEquipSlotsService _specialEquipSlots;
        private ProtagonistEquipmentService _protagonistEquipment;
        private RewardGrantService _rewardGrant;
        private Action _onVictoryAdvance;
        private Action _onFailureReturnTitle;
        private Action _onFailureRestart;
        private PushMapBattleSettlementView _settlementView;

        private SearchExtractSessionService _session;
        private readonly SearchExtractFormationRelocateService _formationRelocate =
            new SearchExtractFormationRelocateService();
        private GameObject _mapInstance;
        private Transform _worldRoot;
        private Camera _combatCamera;
        private PushMapCameraFollowController _cameraFollow;
        private GameObject _resumeFollowButtonRoot;
        private FormationEditorController _formationEditor;
        private Vector2 _mapHalfExtents = new Vector2(5f, 2.5f);
        private Vector3 _mapCenter;
        private bool _running;
        private bool _driverOutcomeDispatched;
        private NavMeshDataInstance _navMeshInstance;
        private string _gatherPointRewardsEncoded = string.Empty;
        private readonly HashSet<int> _creditedGatherOrders = new HashSet<int>();

        private MassMoveScheduler _moveScheduler;
        private AttackSlotService _attackSlots;
        private TacticalFormationRuntimeService _tacticalRuntime;
        private readonly List<ObjectivePoint> _objectives = new List<ObjectivePoint>();
        private readonly Dictionary<string, SpawnPoint> _spawnPointsById =
            new Dictionary<string, SpawnPoint>(StringComparer.Ordinal);
        private readonly List<PushMapAdvanceView> _advanceViews = new List<PushMapAdvanceView>();
        private readonly List<PushMapMonsterAgentView> _monsters = new List<PushMapMonsterAgentView>();
        private readonly List<MassMoveSample> _moveSamples = new List<MassMoveSample>(32);
        private readonly List<SearchExtractFormationRelocateService.DeployPosition> _deployScratch =
            new List<SearchExtractFormationRelocateService.DeployPosition>(16);
        private int _nextMoveId;
        private int _slotGoalCursor;
        private Text _countdownHudText;
        private GameObject _countdownHudRoot;
        private SearchExtractDecisionPanelView _decisionPanel;

        public void ConfigureCatalog(DefendPrefabCatalog catalog, FormationPrefabCatalog formationCatalog = null)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }

            if (formationCatalog != null)
            {
                _formationCatalog = formationCatalog;
            }
        }

        public void Begin(
            LevelStageContext context,
            ConfigCsvRepository configs,
            ProtagonistProgressService progress,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            WarehouseService warehouse = null,
            SpecialEquipSlotsService specialEquipSlots = null,
            ProtagonistEquipmentService protagonistEquipment = null,
            Action onVictoryAdvance = null,
            Action onFailureReturnTitle = null,
            Action onFailureRestart = null)
        {
            EndInternal(destroyWorld: true);

            if (context?.SearchExtractConfig == null)
            {
                Debug.LogError("[SearchExtractStage] Missing SearchExtractConfig.");
                return;
            }

            _configs = configs ?? throw new System.ArgumentNullException(nameof(configs));
            _progress = progress ?? throw new System.ArgumentNullException(nameof(progress));
            _warriorPool = warriorPool ?? throw new System.ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new System.ArgumentNullException(nameof(formation));
            _warehouse = warehouse;
            _specialEquipSlots = specialEquipSlots;
            _protagonistEquipment = protagonistEquipment;
            _onVictoryAdvance = onVictoryAdvance;
            _onFailureReturnTitle = onFailureReturnTitle;
            _onFailureRestart = onFailureRestart;
            _driverOutcomeDispatched = false;
            _gatherPointRewardsEncoded = context.GatherPointRewards ?? string.Empty;
            _creditedGatherOrders.Clear();
            _rewardGrant = new RewardGrantService(
                _configs, _warehouse, _specialEquipSlots, _protagonistEquipment);

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            var mapId = context.SearchExtractConfig.MapId;
            if (_catalog == null || !_catalog.TryGetMap(mapId, out var mapPrefab) || mapPrefab == null)
            {
                Debug.LogError(
                    $"[SearchExtractStage] Map prefab missing for '{mapId}' (bind in DefendPrefabCatalog.Maps).");
                return;
            }

            EnsureWorldRoot();
            _mapInstance = Instantiate(mapPrefab, _worldRoot);
            _mapInstance.name = mapId;

            var bounds = _mapInstance.GetComponent<DigMapBounds>();
            _mapHalfExtents = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            _mapCenter = bounds != null ? bounds.Center : _mapInstance.transform.position;

            CollectObjectives();
            CollectSpawnPoints();
            EnsureCombatCamera();
            if (_combatCamera != null)
            {
                _combatCamera.gameObject.SetActive(false);
            }

            _session = new SearchExtractSessionService();
            _session.PhaseChanged += HandlePhaseChanged;
            _session.GatherCountdownSecondsChanged += HandleGatherCountdownSecondsChanged;
            _session.GatherPointActivated += HandleGatherPointActivated;
            _session.SpawnRequested += HandleSpawnRequested;
            _session.MonsterDamageSettled += HandleMonsterDamageSettled;
            _session.MonsterKilled += HandleMonsterKilled;
            _session.MonsterEnteredCombatDead += HandleMonsterEnteredCombatDead;
            _session.MonsterReviveStarted += HandleMonsterReviveStarted;
            _session.MonsterRevived += HandleMonsterRevived;
            _session.MonsterInvincibleChanged += HandleMonsterInvincibleChanged;
            _session.WarriorDamageSettled += HandleWarriorDamageSettled;
            _session.WarriorCombatDead += HandleWarriorCombatDead;
            _session.PointSucceeded += HandlePointSucceeded;
            _session.PointContinueRequested += HandlePointContinueRequested;
            _session.PointLeaveRequested += HandlePointLeaveRequested;
            _session.LevelFailureRequested += HandleLevelFailureRequested;
            // Bind after BeginPrepare: BeginPrepare→Stop must not leave configs null (D-074 Skills).
            _session.BeginPrepare(
                context.SearchExtractConfig,
                context.GatherPointCount,
                context.LevelId,
                context.GameplayOptionId);
            _session.BindCombatConfigs(_configs);
            _session.BindGatherOrders(CollectObjectiveOrdersAscending());

            EnsureDecisionPanel();
            OpenFormationEditor();
            _running = true;
            Debug.Log(
                $"[SearchExtractStage] Prepare Level={context.LevelId} Option={context.GameplayOptionId} " +
                $"Map={mapId} N={_session.GatherPointCount} CurrentOrder={_session.CurrentGatherOrder}");
        }

        public void End()
        {
            EndInternal(destroyWorld: true);
        }

        private void EndInternal(bool destroyWorld)
        {
            _running = false;
            CloseFormationEditor();

            if (_session != null)
            {
                _session.PhaseChanged -= HandlePhaseChanged;
                _session.GatherCountdownSecondsChanged -= HandleGatherCountdownSecondsChanged;
                _session.GatherPointActivated -= HandleGatherPointActivated;
                _session.SpawnRequested -= HandleSpawnRequested;
                _session.MonsterDamageSettled -= HandleMonsterDamageSettled;
                _session.MonsterKilled -= HandleMonsterKilled;
                _session.MonsterEnteredCombatDead -= HandleMonsterEnteredCombatDead;
                _session.MonsterReviveStarted -= HandleMonsterReviveStarted;
                _session.MonsterRevived -= HandleMonsterRevived;
                _session.MonsterInvincibleChanged -= HandleMonsterInvincibleChanged;
                _session.WarriorDamageSettled -= HandleWarriorDamageSettled;
                _session.WarriorCombatDead -= HandleWarriorCombatDead;
                _session.PointSucceeded -= HandlePointSucceeded;
                _session.PointContinueRequested -= HandlePointContinueRequested;
                _session.PointLeaveRequested -= HandlePointLeaveRequested;
                _session.LevelFailureRequested -= HandleLevelFailureRequested;
                _session.Stop();
                _session = null;
            }

            ClearDeployedViews();
            ClearSpawnedMonsters();
            ClearMassCombatPathing();
            _formationRelocate.Clear();
            DestroyCountdownHud();
            DestroyDecisionPanel();
            _objectives.Clear();
            _spawnPointsById.Clear();
            _creditedGatherOrders.Clear();
            _gatherPointRewardsEncoded = string.Empty;
            _rewardGrant = null;
            _onVictoryAdvance = null;
            _onFailureReturnTitle = null;
            _onFailureRestart = null;
            _settlementView = null;
            _driverOutcomeDispatched = false;
            ReleaseNavMesh();
            DisableCameraFollow();

            if (destroyWorld && _resumeFollowButtonRoot != null)
            {
                var canvas = _resumeFollowButtonRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Destroy(canvas.gameObject);
                }
                else
                {
                    Destroy(_resumeFollowButtonRoot);
                }

                _resumeFollowButtonRoot = null;
            }

            if (destroyWorld && _mapInstance != null)
            {
                Destroy(_mapInstance);
                _mapInstance = null;
            }

            if (_combatCamera != null)
            {
                _combatCamera.gameObject.SetActive(false);
            }
        }

        private void HandleStartBattleRequested()
        {
            if (_session == null || _formation == null || _progress == null)
            {
                return;
            }

            var tacticalLocks = SnapshotTacticalFormationLocks();
            var deployed = _formation.Entries.Count;
            var degree = _formation.ComputeLossOfControlDegree(_progress.ControlPowerCap);
            if (!_session.TryStartBattle(deployed, degree, out var error))
            {
                Debug.Log($"[SearchExtractStage] StartBattle rejected: {error}");
                return;
            }

            BindWaveRowsForSession();

            CloseFormationEditor();
            EnsureCombatCamera();
            if (_combatCamera != null)
            {
                _combatCamera.gameObject.SetActive(true);
                ApplyCombatCameraPose();
            }

            ReleaseNavMesh();
            var airWallBoxes = CollectAirWallObstacles();
            _navMeshInstance = DefendNavMeshBaker.Bake(_mapCenter, _mapHalfExtents, airWallBoxes);
            EnsurePathingServices();
            ClearMassCombatPathing();

            DeployCombatUnits();
            SnapshotFormationRelocate();
            BeginApproachToCurrentObjective();
            CommitTacticalFormationRuntime(tacticalLocks);
            EnableCameraFollowForCombat();
            EnsureCountdownHud();
            RefreshCountdownHud();
            Debug.Log(
                $"[SearchExtractStage] Combat Level={_session.LevelId} Option={_session.GameplayOptionId} " +
                $"Deployed={deployed} AirWalls={airWallBoxes.Count} NavMesh={_navMeshInstance.valid} " +
                $"AdvanceViews={_advanceViews.Count}");
        }

        private void Update()
        {
            if (!_running || _session == null || _session.Phase != SearchExtractPhase.Combat)
            {
                return;
            }

            if (HasLoyalSoldierInCurrentZone() && !_session.IsAwaitingPointDecision)
            {
                _session.TryActivateGatherPoint();
            }

            _session.TickGatherCountdown(Time.deltaTime);
            _session.TickWaveSpawn(Time.deltaTime);
            _session.TickCombatStatus(Time.deltaTime);
            TickMassCombatPathing();
        }

        private void HandlePointSucceeded(SearchExtractPointDecisionInfo info)
        {
            if (info == null)
            {
                return;
            }

            CreditGatherPointRewards(info.GatherPointOrder);
            EnsureDecisionPanel();
            _decisionPanel?.Show(info.ShowContinue, info.GatherPointOrder, info.GatherPointCount);
            RefreshCountdownHud();
            Debug.Log(
                $"[SearchExtractStage] UI-032 show Order={info.GatherPointOrder}/{info.GatherPointCount} " +
                $"Continue={info.ShowContinue}");
        }

        private void HandlePointContinueRequested()
        {
            _decisionPanel?.Hide();
            RefreshCountdownHud();
            RelocateTowardCurrentObjective("Continue");
            Debug.Log(
                $"[SearchExtractStage] Continue → CurrentOrder={_session?.CurrentGatherOrder} " +
                "(re-enter zone required; CombatDead stay dead)");
        }

        private void HandlePointLeaveRequested()
        {
            _decisionPanel?.Hide();
            CreditStageExpOnLeave();
            ShowVictorySettlementThenAdvance();
        }

        private void HandleDecisionContinueClicked()
        {
            _session?.TryContinueAfterPointSuccess();
        }

        private void HandleDecisionLeaveClicked()
        {
            _session?.TryLeaveAfterPointSuccess();
        }

        private void CreditGatherPointRewards(int gatherOrder)
        {
            if (gatherOrder < 1 || !_creditedGatherOrders.Add(gatherOrder))
            {
                return;
            }

            if (_rewardGrant == null || string.IsNullOrWhiteSpace(_gatherPointRewardsEncoded))
            {
                Debug.Log(
                    $"[SearchExtractStage] Point Order={gatherOrder} loot skipped " +
                    $"(grant={_rewardGrant != null} encoded empty={string.IsNullOrWhiteSpace(_gatherPointRewardsEncoded)})");
                return;
            }

            var byOrder = GatherPointRewardsParser.Parse(
                _gatherPointRewardsEncoded,
                msg => Debug.LogWarning($"[SearchExtractStage] {msg}"));
            if (!byOrder.TryGetValue(gatherOrder, out var entries) || entries == null || entries.Count == 0)
            {
                Debug.Log($"[SearchExtractStage] Point Order={gatherOrder} has no GatherPointRewards.");
                return;
            }

            var granted = _rewardGrant.GrantEntries(
                entries,
                msg => Debug.Log($"[SearchExtractStage] Point loot: {msg}"),
                msg => Debug.LogWarning($"[SearchExtractStage] Point loot: {msg}"));
            Debug.Log(
                $"[SearchExtractStage] Point Order={gatherOrder} credited {granted.Count} loot entries " +
                $"(encoded segment for Order).");
        }

        private void CreditStageExpOnLeave()
        {
            var stageExp = _session?.Config != null ? Mathf.Max(0, _session.Config.StageExpReward) : 0;
            if (stageExp <= 0 || _progress == null)
            {
                Debug.Log(
                    $"[SearchExtractStage] Leave StageExpReward={stageExp} — no Exp credited.");
                return;
            }

            var before = _progress.LifetimeExperience;
            var levels = _progress.AddExperience(stageExp);
            var after = _progress.LifetimeExperience;
            Debug.Log(
                $"[SearchExtractStage] Leave Exp +{stageExp} Lifetime={before}→{after} Level={_progress.Level} (+{levels})");
        }

        private void ShowVictorySettlementThenAdvance()
        {
            EnsureSettlementView();
            var casualties = _session != null ? _session.BuildCasualtyStats() : default;
            if (_settlementView != null)
            {
                _settlementView.ShowVictory(
                    elapsedSeconds: 0f,
                    monstersKilled: 0,
                    showKills: false,
                    showElapsed: false,
                    casualties,
                    DispatchVictoryToDriver);
            }
            else
            {
                DispatchVictoryToDriver();
            }
        }

        private void DispatchVictoryToDriver()
        {
            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _running = false;
            Debug.Log("[SearchExtractStage] Leave → UI-017 Continue → TryAdvanceStage (SubLevel Reward via driver; no point loot re-grant).");
            _onVictoryAdvance?.Invoke();
        }

        private void HandleLevelFailureRequested()
        {
            _decisionPanel?.Hide();
            DestroyCountdownHud();
            CameraFogService.Resolve()?.SetPushMapCombatActive(false);
            ShowDefeatSettlement();
        }

        private void ShowDefeatSettlement()
        {
            EnsureSettlementView();
            var casualties = _session != null ? _session.BuildCasualtyStats() : default;
            if (_settlementView != null)
            {
                _settlementView.ShowDefeat(
                    casualties,
                    DispatchFailureReturnTitle,
                    DispatchFailureRestart);
            }
            else
            {
                DispatchFailureReturnTitle();
            }
        }

        private void EnsureSettlementView()
        {
            if (_settlementView == null)
            {
                _settlementView = PushMapBattleResultUiFactory.EnsureSettlement(transform);
            }
        }

        private void DispatchFailureReturnTitle()
        {
            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _running = false;
            Debug.LogWarning("[SearchExtractStage] LevelFailure → TitleMenu (no stage Exp).");
            _onFailureReturnTitle?.Invoke();
        }

        private void DispatchFailureRestart()
        {
            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _running = false;
            Debug.LogWarning("[SearchExtractStage] LevelFailure → Restart same option (no stage Exp).");
            _onFailureRestart?.Invoke();
        }

        private void RelocateTowardCurrentObjective(string reason)
        {
            if (!TryGetCurrentObjectiveCenter(out var center))
            {
                Debug.LogWarning($"[SearchExtractStage] Relocate ({reason}) skipped — no current Objective.");
                return;
            }

            _formationRelocate.ActivateRelocate(center);
            _tacticalRuntime?.SnapAllCentersTo(center);
            RefreshRelocateGoals(center);
            Debug.Log($"[SearchExtractStage] Relocate ({reason}) center={center}");
        }

        private void HandleGatherPointActivated()
        {
            RelocateTowardCurrentObjective("Activate");
        }

        private void HandlePhaseChanged(SearchExtractPhase phase)
        {
            if (phase == SearchExtractPhase.Combat)
            {
                var fog = CameraFogService.Resolve();
                fog?.SetPushMapCombatActive(true);
            }
            else
            {
                CameraFogService.Resolve()?.SetPushMapCombatActive(false);
                DestroyCountdownHud();
                _decisionPanel?.Hide();
            }
        }

        private void HandleGatherCountdownSecondsChanged(int remainingSeconds)
        {
            RefreshCountdownHud();
        }

        private ObjectivePoint ResolveCurrentObjective()
        {
            if (_session == null || _session.CurrentGatherOrder <= 0)
            {
                return null;
            }

            for (var i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i].ObjectiveOrder == _session.CurrentGatherOrder)
                {
                    return _objectives[i];
                }
            }

            return null;
        }

        private CaptureZone ResolveCurrentCaptureZone()
        {
            var objective = ResolveCurrentObjective();
            return objective != null ? objective.CaptureZone : null;
        }

        private bool HasLoyalSoldierInCurrentZone()
        {
            var zone = ResolveCurrentCaptureZone();
            if (zone == null)
            {
                return false;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier == null || soldier.IsRebel || !soldier.IsCombatActive)
                {
                    continue;
                }

                if (zone.ContainsXZ(soldier.transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        private void CollectObjectives()
        {
            _objectives.Clear();
            if (_mapInstance == null)
            {
                return;
            }

            var found = _mapInstance.GetComponentsInChildren<ObjectivePoint>(true);
            if (found == null || found.Length == 0)
            {
                return;
            }

            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    _objectives.Add(found[i]);
                }
            }

            _objectives.Sort((a, b) => a.ObjectiveOrder.CompareTo(b.ObjectiveOrder));
        }

        private void CollectSpawnPoints()
        {
            _spawnPointsById.Clear();
            if (_mapInstance == null)
            {
                return;
            }

            var spawnPoints = _mapInstance.GetComponentsInChildren<SpawnPoint>(true);
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[SearchExtractStage] Map has no SpawnPoint markers.");
                return;
            }

            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                if (sp == null)
                {
                    continue;
                }

                var id = sp.SpawnPointId;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (_spawnPointsById.ContainsKey(id))
                {
                    Debug.LogWarning($"[SearchExtractStage] Duplicate SpawnPointId '{id}' — keeping first.");
                    continue;
                }

                _spawnPointsById[id] = sp;
            }

            Debug.Log($"[SearchExtractStage] SpawnPoints collected={_spawnPointsById.Count}.");
        }

        private void BindWaveRowsForSession()
        {
            if (_session == null || _session.Config == null || _configs == null)
            {
                return;
            }

            var configId = _session.Config.GameplayConfigId;
            var allRows = new List<SearchExtractWaveSpawnConfigRow>();
            for (var i = 0; i < _session.GatherOrders.Count; i++)
            {
                var order = _session.GatherOrders[i];
                var rows = _configs.GetSearchExtractWaves(configId, order);
                if (rows == null || rows.Count == 0)
                {
                    continue;
                }

                for (var r = 0; r < rows.Count; r++)
                {
                    if (rows[r] != null)
                    {
                        allRows.Add(rows[r]);
                    }
                }
            }

            _session.BindWaveRows(allRows);
            Debug.Log(
                $"[SearchExtractStage] Bound {allRows.Count} wave rows for Config={configId}.");
        }

        private void HandleSpawnRequested(SearchExtractSpawnRequest request)
        {
            if (request == null || _configs == null || _catalog == null || _worldRoot == null || _session == null)
            {
                return;
            }

            if (!_session.IsCurrentPointSpawnEligible)
            {
                Debug.Log(
                    $"[SearchExtractStage] Spawn skipped (point inactive/stopped) Wave={request.WaveIndex}.");
                return;
            }

            if (!_configs.TryGetMonster(request.MonsterId, out var monsterRow) || monsterRow == null)
            {
                Debug.LogWarning($"[SearchExtractStage] MonsterConfig missing: {request.MonsterId} — skip spawn.");
                return;
            }

            if (string.IsNullOrEmpty(request.SpawnPointId)
                || !_spawnPointsById.TryGetValue(request.SpawnPointId, out var spawnPoint)
                || spawnPoint == null)
            {
                Debug.LogWarning(
                    $"[SearchExtractStage] SpawnPoint '{request.SpawnPointId}' missing — skip wave {request.WaveIndex}.");
                return;
            }

            var basePos = spawnPoint.transform.position;
            var bodyRadius = Mathf.Max(0.05f, monsterRow.BodyRadius);
            var occupied = new List<PushMapSpawnSpread.Footprint>(_monsters.Count);
            for (var m = 0; m < _monsters.Count; m++)
            {
                var existing = _monsters[m];
                if (existing == null || !existing.IsAlive)
                {
                    continue;
                }

                occupied.Add(new PushMapSpawnSpread.Footprint(
                    existing.transform.position,
                    existing.BodyRadius));
            }

            var spreadPositions = new List<Vector3>(request.SpawnCount);
            PushMapSpawnSpread.ComputePositions(
                basePos,
                request.SpawnCount,
                bodyRadius,
                occupied,
                spreadPositions);

            for (var i = 0; i < request.SpawnCount; i++)
            {
                var pos = i < spreadPositions.Count ? spreadPositions[i] : basePos;
                var pickedModelId = monsterRow.PickSpawnModelId();
                if (string.IsNullOrEmpty(pickedModelId))
                {
                    Debug.LogWarning(
                        $"[SearchExtractStage] Monster ModelId pool empty for {monsterRow.MonsterId} — skip spawn.");
                    continue;
                }

                _catalog.TryGetMonsterModel(pickedModelId, out var modelPrefab);
                GameObject go;
                if (modelPrefab != null)
                {
                    go = Instantiate(modelPrefab, _worldRoot);
                }
                else
                {
                    Debug.LogWarning(
                        $"[SearchExtractStage] Monster prefab missing: Assets/Prefabs/Defend/Monsters/{pickedModelId}.prefab — temp cube.");
                    go = CreateTempMonsterVisual(pickedModelId);
                    go.transform.SetParent(_worldRoot, false);
                }

                go.name = $"Monster_{monsterRow.MonsterId}_{_monsters.Count}";
                go.transform.position = pos;

                var view = go.GetComponent<PushMapMonsterAgentView>();
                if (view == null)
                {
                    view = go.AddComponent<PushMapMonsterAgentView>();
                }

                var runtimeId = go.name;
                view.Bind(
                    monsterRow,
                    protagonist: null,
                    () => _advanceViews,
                    onHitProtagonist: null,
                    1f,
                    _attackSlots,
                    _moveScheduler,
                    ++_nextMoveId,
                    (monsterRuntimeId, warriorId, attackPower) =>
                        _session.TryApplyMonsterDamageToWarrior(monsterRuntimeId, warriorId, attackPower),
                    () => _session != null && _session.IsMonsterStunned(runtimeId),
                    () => _session != null ? _session.GetMonsterSlowMoveMul(runtimeId) : 1f,
                    () => _session != null ? _session.GetMonsterSlowAttackMul(runtimeId) : 1f);
                view.ApplySpawnInitialFacing(PushMapSpawnFacing.ResolveDirIndex(0));

                view.SetCorpseSmashBridge(
                    (corpseRuntimeId, killerId, killerOutgoing, targetRuntimeId) =>
                        _session != null &&
                        _session.TryApplyCorpseSmashDamage(
                            corpseRuntimeId,
                            killerId,
                            killerOutgoing,
                            targetRuntimeId),
                    EnumerateLivingMonstersForCorpseSmash);

                view.SetReviveCallbacks(
                    () => _session?.TryNotifyMonsterDeathPresentationComplete(runtimeId),
                    () => _session?.TryNotifyMonsterReviveAnimComplete(runtimeId));

                _session.RegisterMonster(runtimeId, monsterRow.MonsterId, monsterRow.MaxHP);
                if (go.GetComponent<HitFlashView>() == null)
                {
                    go.AddComponent<HitFlashView>();
                }

                _monsters.Add(view);
            }

            Debug.Log(
                $"[SearchExtractStage] Spawned {request.SpawnCount}x {request.MonsterId} at '{request.SpawnPointId}' " +
                $"Order={request.GatherPointOrder} Wave={request.WaveIndex}.");
        }

        private static GameObject CreateTempMonsterVisual(string modelId)
        {
            var root = new GameObject(string.IsNullOrEmpty(modelId) ? "MonsterTemp" : modelId);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.75f, 0.25f, 0.25f);
            }

            return root;
        }

        private void ClearSpawnedMonsters()
        {
            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null)
                {
                    Destroy(monster.gameObject);
                }
            }

            _monsters.Clear();
        }

        private PushMapMonsterAgentView FindMonsterView(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null && string.Equals(monster.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    return monster;
                }
            }

            return null;
        }

        private PushMapAdvanceView FindAdvanceView(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return null;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier != null && string.Equals(soldier.AttackerId, warriorId, StringComparison.Ordinal))
                {
                    return soldier;
                }
            }

            return null;
        }

        private void HandleMonsterDamageSettled(string runtimeId, float damage)
        {
            var monster = FindMonsterView(runtimeId);
            if (monster == null)
            {
                return;
            }

            monster.NotifyProvoked();
            var flash = monster.GetComponent<HitFlashView>();
            if (flash != null)
            {
                flash.Play(HitFlashView.MonsterFlashColor);
            }

            SpawnDamagePopup(monster.transform.position, damage, DamagePopupStyle.Monster);
        }

        private void HandleMonsterKilled(string runtimeId, string killerWarriorId, float outgoingDamage, string deathTag)
        {
            ApplyMonsterDeathPresentation(runtimeId, killerWarriorId, outgoingDamage, deathTag, fakeDeathCorpse: false);
        }

        private void HandleMonsterEnteredCombatDead(
            string runtimeId,
            string killerWarriorId,
            float outgoingDamage,
            string deathTag)
        {
            ApplyMonsterDeathPresentation(runtimeId, killerWarriorId, outgoingDamage, deathTag, fakeDeathCorpse: true);
        }

        private void HandleMonsterReviveStarted(string runtimeId, float reviveAnimSeconds)
        {
            FindMonsterView(runtimeId)?.NotifyReviveStarted(reviveAnimSeconds);
        }

        private void HandleMonsterRevived(string runtimeId)
        {
            var monster = FindMonsterView(runtimeId);
            if (monster == null)
            {
                return;
            }

            float? postReviveAlertRadius = null;
            if (_session != null
                && _session.TryGetMonster(runtimeId, out var state)
                && state != null
                && state.PostReviveAlertRadiusApplied)
            {
                postReviveAlertRadius = state.RuntimeAlertRadius;
            }

            monster.NotifyRevived(postReviveAlertRadius);
            if (_session == null || !_session.IsMonsterInvincible(runtimeId))
            {
                monster.NotifyPostReviveInvincibleEnded();
            }
        }

        private void HandleMonsterInvincibleChanged(string runtimeId, string skillId, bool on)
        {
            if (on)
            {
                return;
            }

            FindMonsterView(runtimeId)?.NotifyPostReviveInvincibleEnded();
        }

        private void ApplyMonsterDeathPresentation(
            string runtimeId,
            string killerWarriorId,
            float outgoingDamage,
            string deathTag,
            bool fakeDeathCorpse)
        {
            var monster = FindMonsterView(runtimeId);
            if (monster == null)
            {
                return;
            }

            _attackSlots?.ReleaseAllForTarget(runtimeId);
            Vector3? killerPos = null;
            if (!string.IsNullOrEmpty(killerWarriorId))
            {
                var killer = FindAdvanceView(killerWarriorId);
                if (killer != null)
                {
                    killerPos = killer.transform.position;
                }
            }

            var maxHp = 0f;
            if (_session != null && _session.TryGetMonster(runtimeId, out var state) && state != null)
            {
                maxHp = state.MaxHp;
            }

            var isCorpseSmashKill = string.Equals(deathTag, "CorpseSmash", StringComparison.Ordinal);
            var isPointClear = string.Equals(deathTag, "PointClear", StringComparison.Ordinal);
            var distance = isCorpseSmashKill || isPointClear
                ? 0f
                : MonsterDeathPresentation.ComputeKnockbackDistance(maxHp, outgoingDamage);
            monster.NotifyKilled(killerPos, distance, killerWarriorId, outgoingDamage, fakeDeathCorpse);
        }

        private void EnumerateLivingMonstersForCorpseSmash(Action<string, Vector2, float> visit)
        {
            if (visit == null || _session == null)
            {
                return;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null)
                {
                    continue;
                }

                var runtimeId = monster.RuntimeTargetId;
                if (string.IsNullOrEmpty(runtimeId) || !_session.IsMonsterTargetable(runtimeId))
                {
                    continue;
                }

                var p = monster.transform.position;
                visit(runtimeId, new Vector2(p.x, p.z), monster.BodyRadius);
            }
        }

        private void HandleWarriorDamageSettled(string warriorId, float damage)
        {
            var soldier = FindAdvanceView(warriorId);
            if (soldier == null)
            {
                return;
            }

            var flash = soldier.GetComponent<HitFlashView>();
            if (flash != null)
            {
                flash.Play(HitFlashView.SoldierFlashColor);
            }

            SpawnDamagePopup(soldier.transform.position, damage, DamagePopupStyle.Soldier);
        }

        private void HandleWarriorCombatDead(string warriorId)
        {
            HandleTacticalFormationMemberLost(warriorId, TacticalFormationMemberLostReason.CombatDead);
        }

        private void HandleTacticalFormationMemberLost(string warriorId, TacticalFormationMemberLostReason reason)
        {
            if (_tacticalRuntime == null)
            {
                return;
            }

            _tacticalRuntime.TryNotifyMemberLost(warriorId, reason, out _);
        }

        private void SpawnDamagePopup(Vector3 worldPos, float damage, DamagePopupStyle style)
        {
            var prefab = _catalog != null ? _catalog.DamagePopupPrefab : null;
            if (prefab == null)
            {
                return;
            }

            DamagePopupView.Spawn(prefab, _worldRoot, worldPos, damage, style);
        }

        private List<int> CollectObjectiveOrdersAscending()
        {
            var orders = new List<int>(_objectives.Count);
            for (var i = 0; i < _objectives.Count; i++)
            {
                var point = _objectives[i];
                if (point != null)
                {
                    orders.Add(point.ObjectiveOrder);
                }
            }

            return orders;
        }

        private List<DefendNavMeshBaker.NavMeshBoxObstacle> CollectAirWallObstacles()
        {
            var boxes = new List<DefendNavMeshBaker.NavMeshBoxObstacle>();
            if (_mapInstance == null)
            {
                return boxes;
            }

            var walls = _mapInstance.GetComponentsInChildren<AirWall>(true);
            if (walls == null || walls.Length == 0)
            {
                return boxes;
            }

            for (var i = 0; i < walls.Length; i++)
            {
                var wall = walls[i];
                if (wall == null)
                {
                    continue;
                }

                boxes.Add(new DefendNavMeshBaker.NavMeshBoxObstacle(
                    wall.transform.position,
                    wall.FullSize,
                    wall.transform.rotation));
            }

            Debug.Log($"[SearchExtractStage] AirWall bake obstacles={boxes.Count}.");
            return boxes;
        }

        private void DeployCombatUnits()
        {
            ClearDeployedViews();
            if (_formation == null || _warriorPool == null || _catalog == null || _moveScheduler == null)
            {
                return;
            }

            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!_warriorPool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    Debug.LogWarning($"[SearchExtractStage] Warrior '{entry.WarriorId}' missing from pool — skip deploy.");
                    continue;
                }

                if (!_catalog.TryGetWarriorAppearance(warrior.AppearanceId, out var appearancePrefab)
                    || appearancePrefab == null)
                {
                    Debug.LogWarning(
                        $"[SearchExtractStage] Appearance Prefab missing: Assets/Prefabs/Defend/Warriors/{warrior.AppearanceId}.prefab");
                    continue;
                }

                var worldPos = new Vector3(
                    _mapCenter.x + entry.PositionX,
                    _mapCenter.y,
                    _mapCenter.z + entry.PositionZ);
                if (NavMesh.SamplePosition(worldPos, out var hit, 2f, NavMesh.AllAreas))
                {
                    worldPos = hit.position;
                }

                var go = Instantiate(appearancePrefab, _worldRoot);
                go.name = $"Warrior_{warrior.Id}";
                go.transform.position = worldPos;
                WarriorAllIn1StyleView.ApplyTo(go, _catalog.VisualStyleCatalog, warrior);

                ClassConfigRow classRow = null;
                if (_configs != null && !string.IsNullOrEmpty(warrior.ClassId))
                {
                    _configs.TryGetClass(warrior.ClassId, out classRow);
                }

                var attackRange = 1f;
                if (classRow != null && classRow.AttackRange > 0.05f)
                {
                    attackRange = classRow.AttackRange;
                }

                attackRange *= WarriorVisualModelScale.Resolve(warrior);

                var bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
                var pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient;
                var repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale;
                var facingYawFlip = false;
                if (_configs != null
                    && _configs.TryGetAppearance(warrior.AppearanceId, out var appearanceRow)
                    && appearanceRow != null)
                {
                    bodyRadius = appearanceRow.BodyRadius;
                    pushCoefficient = appearanceRow.PushCoefficient;
                    repulsionScale = appearanceRow.RepulsionScale;
                    facingYawFlip = appearanceRow.FacingYawFlip == 1;
                }

                bodyRadius *= WarriorVisualModelScale.Resolve(warrior);

                var moveSpeed = 1.5f;
                if (_session != null
                    && _session.TryGetWarrior(warrior.Id, out var combatState)
                    && combatState != null)
                {
                    moveSpeed = Mathf.Max(0.1f, combatState.MoveSpeed);
                }

                var chaseMult = classRow != null
                    ? classRow.ChaseMoveSpeedMult
                    : ClassConfigRow.DefaultChaseMoveSpeedMult;

                _nextMoveId++;
                var advance = go.GetComponent<PushMapAdvanceView>();
                if (advance == null)
                {
                    advance = go.AddComponent<PushMapAdvanceView>();
                }

                advance.Bind(
                    _moveScheduler,
                    _nextMoveId,
                    moveSpeed,
                    ProvideMonsters,
                    attackRange,
                    warrior.AttackMode,
                    warrior.Id,
                    _attackSlots,
                    session: _session,
                    bodyRadius: bodyRadius,
                    facingYawFlip: facingYawFlip,
                    pushCoefficient: pushCoefficient,
                    repulsionScale: repulsionScale,
                    chaseMoveSpeedMult: chaseMult);

                if (_session != null)
                {
                    _session.TryRegisterWarrior(warrior, classRow, out _, out var regError);
                    if (!string.IsNullOrEmpty(regError))
                    {
                        Debug.LogWarning(
                            $"[SearchExtractStage] RegisterWarrior '{warrior.Id}' failed: {regError}");
                    }
                }

                var hold = new Vector2(worldPos.x, worldPos.z);
                _moveScheduler.SetGoal(advance.MoveId, GoalKind.FormationHome, hold);
                _moveScheduler.SetPaused(advance.MoveId, false);
                _advanceViews.Add(advance);
            }

            Debug.Log(
                $"[SearchExtractStage] Deployed {_advanceViews.Count} soldiers " +
                "(approach Objective until gather activation).");
        }

        private IReadOnlyList<PushMapMonsterAgentView> ProvideMonsters()
        {
            return _monsters;
        }

        private void ClearDeployedViews()
        {
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _advanceViews.Clear();
        }

        private void SnapshotFormationRelocate()
        {
            _deployScratch.Clear();
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view == null || string.IsNullOrEmpty(view.AttackerId))
                {
                    continue;
                }

                var pos = view.transform.position;
                _deployScratch.Add(new SearchExtractFormationRelocateService.DeployPosition(
                    view.AttackerId,
                    new Vector2(pos.x, pos.z)));
            }

            _formationRelocate.SnapshotFromDeployPositions(_deployScratch);
        }

        /// <summary>
        /// Pre-activation approach: all loyal soldiers seek current Objective center
        /// (SPEC_03 §3.19 Approach A / v0.83.92). Countdown/spawn/offset relocate still wait for zone enter.
        /// </summary>
        private void BeginApproachToCurrentObjective()
        {
            if (_moveScheduler == null)
            {
                return;
            }

            if (!TryGetCurrentObjectiveCenter(out var center))
            {
                Debug.LogWarning("[SearchExtractStage] Approach skipped — no current Objective.");
                return;
            }

            var moved = 0;
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier == null || soldier.IsRebel || soldier.MoveId == 0)
                {
                    continue;
                }

                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationHome, center);
                moved++;
            }

            Debug.Log(
                $"[SearchExtractStage] Approach Objective center={center} soldiers={moved}");
        }

        private void EnsurePathingServices()
        {
            _moveScheduler ??= new MassMoveScheduler();
            _attackSlots ??= new AttackSlotService();
        }

        private void ClearMassCombatPathing()
        {
            _moveScheduler?.Clear();
            _attackSlots?.Clear();
            _tacticalRuntime?.Clear();
            _moveSamples.Clear();
            _nextMoveId = 0;
            _slotGoalCursor = 0;
        }

        private bool TryGetCurrentObjectiveCenter(out Vector2 centerXZ)
        {
            centerXZ = default;
            var objective = ResolveCurrentObjective();
            if (objective == null)
            {
                return false;
            }

            var pos = objective.transform.position;
            centerXZ = new Vector2(pos.x, pos.z);
            return true;
        }

        private void TickMassCombatPathing()
        {
            if (_moveScheduler == null)
            {
                return;
            }

            TickTacticalFormationCenter();
            TickAttackSlotGoals();
            RefreshFormationSlotDestinations();

            _moveSamples.Clear();
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view != null && view.MoveId != 0)
                {
                    _moveSamples.Add(view.BuildSample());
                }
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null && monster.IsAlive && !monster.IsStationary && monster.MoveId != 0)
                {
                    _moveSamples.Add(monster.BuildSample());
                }
            }

            _moveScheduler.Tick(_moveSamples, Time.deltaTime);
        }

        private void TickTacticalFormationCenter()
        {
            if (_tacticalRuntime == null || _tacticalRuntime.SquadCount == 0)
            {
                return;
            }

            if (_session != null
                && _session.IsFormationRelocateActive
                && TryGetCurrentObjectiveCenter(out var center))
            {
                _tacticalRuntime.SnapAllCentersTo(center);
            }
        }

        private void TickAttackSlotGoals()
        {
            if (_attackSlots == null || _moveScheduler == null)
            {
                return;
            }

            var rosterCount = _advanceViews.Count + _monsters.Count;
            if (rosterCount <= 0)
            {
                return;
            }

            var budget = Mathf.Min(MassMoveScheduler.MaxRecalcPerFrame, rosterCount);
            for (var n = 0; n < budget; n++)
            {
                if (_slotGoalCursor >= rosterCount)
                {
                    _slotGoalCursor = 0;
                }

                if (_slotGoalCursor < _advanceViews.Count)
                {
                    RefreshSoldierSlotGoal(_advanceViews[_slotGoalCursor]);
                }
                else
                {
                    var mi = _slotGoalCursor - _advanceViews.Count;
                    if (mi >= 0 && mi < _monsters.Count)
                    {
                        _monsters[mi]?.TryRefreshChaseGoal(_attackSlots, _moveScheduler);
                    }
                }

                _slotGoalCursor++;
            }
        }

        private void RefreshSoldierSlotGoal(PushMapAdvanceView soldier)
        {
            if (soldier == null
                || soldier.IsRebel
                || !soldier.IsCombatActive
                || _moveScheduler == null
                || _attackSlots == null)
            {
                return;
            }

            if (!TryGetRelocateFallbackGoal(soldier.AttackerId, out var relocateGoal))
            {
                _attackSlots.Release(soldier.AttackerId);
                if (TryGetCurrentObjectiveCenter(out var approachGoal))
                {
                    _moveScheduler.SetPaused(soldier.MoveId, false);
                    _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationHome, approachGoal);
                }
                else
                {
                    var hold = new Vector2(soldier.transform.position.x, soldier.transform.position.z);
                    _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationHome, hold);
                    _moveScheduler.SetPaused(soldier.MoveId, true);
                }

                return;
            }

            if (!soldier.TryGetEngageMonster(out var monster) || monster == null)
            {
                if (TryApplyFormationMemberGoal(
                        soldier.AttackerId,
                        soldier.MoveId,
                        soldier.AttackerId,
                        TacticalFormationIdleFallback.FormationHome,
                        relocateGoal,
                        idle: true))
                {
                    return;
                }

                _attackSlots.Release(soldier.AttackerId);
                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationHome, relocateGoal);
                return;
            }

            var distXZ = CombatReach.DistanceXZ(soldier.transform.position, monster.transform.position);
            var inMelee = CombatReach.IsInAttackRange(
                distXZ,
                soldier.AttackRange,
                soldier.AgentRadius,
                monster.BodyRadius);
            var enemyXZ = new Vector2(monster.transform.position.x, monster.transform.position.z);

            if (_tacticalRuntime != null
                && _tacticalRuntime.IsMember(soldier.AttackerId)
                && !TacticalFormationCombatGoalPolicy.IsEnemyInsideLeash(
                    _tacticalRuntime,
                    soldier.AttackerId,
                    enemyXZ))
            {
                if (inMelee)
                {
                    _attackSlots.Release(soldier.AttackerId);
                    var holdHere = new Vector2(soldier.transform.position.x, soldier.transform.position.z);
                    _moveScheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, holdHere);
                    _moveScheduler.SetPaused(soldier.MoveId, true);
                    return;
                }

                if (TryApplyFormationMemberGoal(
                        soldier.AttackerId,
                        soldier.MoveId,
                        soldier.AttackerId,
                        TacticalFormationIdleFallback.FormationHome,
                        relocateGoal,
                        beyondLeash: true))
                {
                    return;
                }
            }

            var claimed = _attackSlots.TryClaim(
                soldier.AttackerId,
                monster.RuntimeTargetId,
                soldier.AttackRange,
                monster.transform.position,
                out var slotPos,
                soldier.AttackMode,
                soldier.transform.position,
                monster.BodyRadius,
                soldier.AgentRadius,
                CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, soldier.AttackMode));

            if (inMelee)
            {
                var hold = claimed
                    ? CombatReach.ChaseDestinationXZ(
                        soldier.transform.position,
                        monster.transform.position,
                        slotPos,
                        soldier.AttackRange,
                        soldier.AgentRadius,
                        monster.BodyRadius,
                        MassMoveScheduler.ArriveEpsilon)
                    : new Vector2(soldier.transform.position.x, soldier.transform.position.z);
                hold = TacticalFormationCombatGoalPolicy.ClampAttackSlot(
                    _tacticalRuntime,
                    soldier.AttackerId,
                    hold);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, hold);
                _moveScheduler.SetPaused(soldier.MoveId, true);
                return;
            }

            if (!claimed)
            {
                if (TryApplyFormationMemberGoal(
                        soldier.AttackerId,
                        soldier.MoveId,
                        soldier.AttackerId,
                        TacticalFormationIdleFallback.FormationHome,
                        relocateGoal,
                        overflow: true))
                {
                    return;
                }

                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationHome, relocateGoal);
                return;
            }

            var dest = CombatReach.ChaseDestinationXZ(
                soldier.transform.position,
                monster.transform.position,
                slotPos,
                soldier.AttackRange,
                soldier.AgentRadius,
                monster.BodyRadius,
                MassMoveScheduler.ArriveEpsilon);
            dest = TacticalFormationCombatGoalPolicy.ClampAttackSlot(
                _tacticalRuntime,
                soldier.AttackerId,
                dest);
            _moveScheduler.SetPaused(soldier.MoveId, false);
            _moveScheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, dest);
        }

        private void RefreshFormationSlotDestinations()
        {
            if (_tacticalRuntime == null
                || _tacticalRuntime.MemberCount == 0
                || _moveScheduler == null)
            {
                return;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier == null
                    || soldier.MoveId == 0
                    || soldier.IsRebel
                    || !soldier.IsCombatActive)
                {
                    continue;
                }

                if (!_moveScheduler.TryGetGoal(soldier.MoveId, out var kind, out _)
                    || kind != GoalKind.FormationSlot)
                {
                    continue;
                }

                if (!_tacticalRuntime.TryGetSlotWorldXZ(soldier.AttackerId, out var slot))
                {
                    continue;
                }

                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationSlot, slot);
            }
        }

        private bool TryApplyFormationMemberGoal(
            string warriorId,
            int moveId,
            string attackerId,
            TacticalFormationIdleFallback fallback,
            Vector2 fallbackHomeXZ,
            bool idle = false,
            bool beyondLeash = false,
            bool overflow = false)
        {
            if (_tacticalRuntime == null || _moveScheduler == null || string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            GoalKind kind = default;
            Vector2 dest = default;
            var handled = false;
            if (idle)
            {
                handled = TacticalFormationCombatGoalPolicy.TryResolveIdleGoal(
                    _tacticalRuntime,
                    warriorId,
                    fallback,
                    fallbackHomeXZ,
                    out kind,
                    out dest);
            }
            else if (beyondLeash)
            {
                handled = TacticalFormationCombatGoalPolicy.TryResolveBeyondLeashHold(
                    _tacticalRuntime,
                    warriorId,
                    out kind,
                    out dest);
            }
            else if (overflow)
            {
                handled = TacticalFormationCombatGoalPolicy.TryResolveOverflow(
                    _tacticalRuntime,
                    warriorId,
                    fallback,
                    fallbackHomeXZ,
                    out kind,
                    out dest);
            }

            if (!handled)
            {
                return false;
            }

            _attackSlots?.Release(attackerId);
            _moveScheduler.SetPaused(moveId, false);
            _moveScheduler.SetGoal(moveId, kind, dest);
            return true;
        }

        private bool TryGetRelocateFallbackGoal(string warriorId, out Vector2 goalXZ)
        {
            goalXZ = default;
            if (_session == null || !_session.IsFormationRelocateActive)
            {
                return false;
            }

            if (!TryGetCurrentObjectiveCenter(out var center))
            {
                return false;
            }

            return _formationRelocate.TryGetRelocateGoal(warriorId, center, out goalXZ);
        }

        private void RefreshRelocateGoals(Vector2 objectiveCenterXZ)
        {
            if (_moveScheduler == null)
            {
                return;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier == null
                    || soldier.IsRebel
                    || string.IsNullOrEmpty(soldier.AttackerId))
                {
                    continue;
                }

                if (!_formationRelocate.TryGetRelocateGoal(soldier.AttackerId, objectiveCenterXZ, out var goal))
                {
                    continue;
                }

                if (_tacticalRuntime != null
                    && _tacticalRuntime.IsMember(soldier.AttackerId)
                    && _tacticalRuntime.TryGetMoveParams(soldier.AttackerId, out var moveParams)
                    && moveParams.KeepFormationWhileEngage
                    && _tacticalRuntime.TryGetSlotWorldXZ(soldier.AttackerId, out var slot))
                {
                    _moveScheduler.SetPaused(soldier.MoveId, false);
                    _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationSlot, slot);
                    continue;
                }

                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.FormationHome, goal);
            }
        }

        private List<TacticalFormationCombatLock> SnapshotTacticalFormationLocks()
        {
            if (_formationEditor == null)
            {
                return new List<TacticalFormationCombatLock>(0);
            }

            return TacticalFormationRuntimeService.BuildLocks(
                _formationEditor.Layout,
                _configs,
                _formationCatalog);
        }

        private void CommitTacticalFormationRuntime(List<TacticalFormationCombatLock> locks)
        {
            _tacticalRuntime ??= new TacticalFormationRuntimeService();
            var speed = ResolveRepresentativeMoveSpeed(locks);
            _tacticalRuntime.OnStartBattle(locks, TacticalFormationCenterMode.Hold, speed);
        }

        private float ResolveRepresentativeMoveSpeed(List<TacticalFormationCombatLock> locks)
        {
            if (locks == null || _session == null)
            {
                return 3.5f;
            }

            for (var i = 0; i < locks.Count; i++)
            {
                var members = locks[i].MemberIds;
                if (members == null)
                {
                    continue;
                }

                for (var m = 0; m < members.Length; m++)
                {
                    var id = members[m];
                    if (string.IsNullOrEmpty(id)
                        || !_session.TryGetWarrior(id, out var state)
                        || state == null
                        || state.MoveSpeed <= 0.01f)
                    {
                        continue;
                    }

                    return state.MoveSpeed;
                }
            }

            return 3.5f;
        }

        private void EnsureCountdownHud()
        {
            if (_countdownHudRoot != null)
            {
                return;
            }

            var canvasGo = new GameObject(
                "SearchExtractCountdownCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _countdownHudRoot = new GameObject("CountdownBar", typeof(RectTransform), typeof(Image));
            _countdownHudRoot.transform.SetParent(canvasGo.transform, false);
            var barRt = _countdownHudRoot.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1f);
            barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, -16f);
            barRt.sizeDelta = new Vector2(320f, 44f);
            _countdownHudRoot.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_countdownHudRoot.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _countdownHudText = textGo.GetComponent<Text>();
            _countdownHudText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _countdownHudText.fontSize = 22;
            _countdownHudText.alignment = TextAnchor.MiddleCenter;
            _countdownHudText.color = Color.white;
        }

        private void DestroyCountdownHud()
        {
            if (_countdownHudRoot == null)
            {
                return;
            }

            var canvas = _countdownHudRoot.transform.parent;
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
            else
            {
                Destroy(_countdownHudRoot);
            }

            _countdownHudRoot = null;
            _countdownHudText = null;
        }

        private void RefreshCountdownHud()
        {
            if (_countdownHudText == null || _session == null)
            {
                return;
            }

            if (!_session.IsCurrentPointActivated)
            {
                _countdownHudText.text =
                    $"搜集点 {_session.CurrentGatherOrder} · 等待进圈激活";
                return;
            }

            if (_session.IsAwaitingPointDecision)
            {
                _countdownHudText.text =
                    $"搜集点 {_session.CurrentGatherOrder} · 已完成 · 请选择";
                return;
            }

            _countdownHudText.text =
                $"搜集点 {_session.CurrentGatherOrder} · 剩余 {_session.GatherCountdownRemainingSeconds}s";
        }

        private void EnsureDecisionPanel()
        {
            if (_decisionPanel != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(DecisionPanelResourcePath);
            if (prefab != null)
            {
                var instance = Instantiate(prefab, transform);
                instance.name = "SearchExtractDecisionPanel";
                _decisionPanel = instance.GetComponent<SearchExtractDecisionPanelView>();
                if (_decisionPanel == null)
                {
                    _decisionPanel = instance.AddComponent<SearchExtractDecisionPanelView>();
                }
            }
            else
            {
                _decisionPanel = BuildDecisionPanelRuntime();
            }

            _decisionPanel.ContinueClicked += HandleDecisionContinueClicked;
            _decisionPanel.LeaveClicked += HandleDecisionLeaveClicked;
            _decisionPanel.Hide();
        }

        private SearchExtractDecisionPanelView BuildDecisionPanelRuntime()
        {
            var canvasGo = new GameObject(
                "SearchExtractDecisionPanel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = new GameObject("Root", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvasGo.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 48f);
            rootRt.sizeDelta = new Vector2(560f, 160f);
            root.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(root.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.55f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, 0f);
            titleRt.offsetMax = new Vector2(-16f, -8f);
            var title = titleGo.GetComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 24;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.text = "搜集点完成";

            var continueGo = CreateDecisionButton(root.transform, "ContinueButton", "继续搜集",
                new Vector2(0.28f, 0.22f), new Color(0.22f, 0.45f, 0.28f, 1f));
            var leaveGo = CreateDecisionButton(root.transform, "LeaveButton", "离开",
                new Vector2(0.72f, 0.22f), new Color(0.4f, 0.28f, 0.18f, 1f));

            var view = canvasGo.AddComponent<SearchExtractDecisionPanelView>();
            view.Bind(root, continueGo.GetComponent<Button>(), leaveGo.GetComponent<Button>(), title);
            return view;
        }

        private static GameObject CreateDecisionButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(200f, 56f);
            go.GetComponent<Image>().color = color;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return go;
        }

        private void DestroyDecisionPanel()
        {
            if (_decisionPanel == null)
            {
                return;
            }

            _decisionPanel.ContinueClicked -= HandleDecisionContinueClicked;
            _decisionPanel.LeaveClicked -= HandleDecisionLeaveClicked;
            Destroy(_decisionPanel.gameObject);
            _decisionPanel = null;
        }

        private void OpenFormationEditor()
        {
            CloseFormationEditor();
            var mode = _formation != null ? _formation.BoundCampaignMode : CampaignMode.Mode1;
            var rootPrefab = _formationCatalog != null ? _formationCatalog.ResolveEditorRoot(mode) : null;
            if (rootPrefab == null)
            {
                Debug.LogError("[SearchExtractStage] FormationEditorRoot missing.");
                return;
            }

            var instance = Instantiate(rootPrefab, transform);
            instance.name = mode == CampaignMode.Mode2
                ? "FormationEditorRoot_Mode2(Clone)"
                : "FormationEditorRoot(Clone)";
            _formationEditor = instance.GetComponent<FormationEditorController>();
            if (_formationEditor == null)
            {
                _formationEditor = instance.AddComponent<FormationEditorController>();
            }

            _formationEditor.StartBattleRequested += HandleStartBattleRequested;
            _formationEditor.Begin(
                FormationEditorMode.SearchExtractPrepare,
                _catalog,
                _warriorPool,
                _formation,
                _progress,
                _configs,
                null,
                _mapInstance,
                _formationCatalog);
        }

        private void CloseFormationEditor()
        {
            if (_formationEditor == null)
            {
                return;
            }

            _formationEditor.StartBattleRequested -= HandleStartBattleRequested;
            _formationEditor.End();
            Destroy(_formationEditor.gameObject);
            _formationEditor = null;
        }

        private void EnsureWorldRoot()
        {
            if (_worldRoot != null)
            {
                return;
            }

            var root = new GameObject("SearchExtractWorldRoot");
            root.transform.SetParent(transform, false);
            _worldRoot = root.transform;
        }

        private void EnsureCombatCamera()
        {
            if (_combatCamera != null)
            {
                EnsureCameraFollowComponent();
                return;
            }

            var camGo = new GameObject("SearchExtractCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            camGo.SetActive(false);
            _combatCamera = camGo.GetComponent<Camera>();
            _combatCamera.orthographic = true;
            _combatCamera.clearFlags = CameraClearFlags.SolidColor;
            _combatCamera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            _combatCamera.depth = 5;
            var cam = _configs != null
                ? _configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            _combatCamera.nearClipPlane = cam.NearClip;
            _combatCamera.farClipPlane = cam.FarClip;
            EnsureCameraFollowComponent();
        }

        private void EnsureCameraFollowComponent()
        {
            if (_combatCamera == null)
            {
                return;
            }

            _cameraFollow = _combatCamera.GetComponent<PushMapCameraFollowController>();
            if (_cameraFollow == null)
            {
                _cameraFollow = _combatCamera.gameObject.AddComponent<PushMapCameraFollowController>();
            }
        }

        private void EnableCameraFollowForCombat()
        {
            EnsureCombatCamera();
            EnsureResumeFollowButton();
            if (_cameraFollow == null || _combatCamera == null)
            {
                return;
            }

            var cameraPath = _mapInstance != null
                ? _mapInstance.GetComponentInChildren<PushMapCameraPath>(true)
                : null;
            if (cameraPath != null && !cameraPath.HasBakedPath)
            {
                if (!cameraPath.TryBake(out var bakeError))
                {
                    Debug.LogWarning($"[SearchExtractStage] CameraFollowPath bake failed: {bakeError}");
                }
            }

            _cameraFollow.Bind(_combatCamera, _advanceViews, ResolveCurrentObjective, cameraPath);
            _cameraFollow.ApplyPresentationConstants(
                _configs != null
                    ? _configs.GetCameraPresentationConstants()
                    : CameraPresentationConstants.SafetyDefaults);
            _cameraFollow.EnableForCombat();
            Debug.Log(
                $"[SearchExtractStage] CameraFollow enabled path={(cameraPath != null && cameraPath.HasBakedPath)} " +
                $"soldiers={_advanceViews.Count}");
        }

        private void DisableCameraFollow()
        {
            if (_cameraFollow != null)
            {
                _cameraFollow.Disable();
            }

            if (_resumeFollowButtonRoot != null)
            {
                _resumeFollowButtonRoot.SetActive(false);
            }
        }

        private void EnsureResumeFollowButton()
        {
            if (_resumeFollowButtonRoot != null)
            {
                if (_cameraFollow != null)
                {
                    var existing = _resumeFollowButtonRoot.GetComponentInChildren<Button>(true);
                    _cameraFollow.BindResumeButton(_resumeFollowButtonRoot, existing);
                }

                return;
            }

            var canvasGo = new GameObject(
                "SearchExtractResumeFollowCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var buttonGo = new GameObject("ResumeFollowButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.1f);
            rt.anchorMax = new Vector2(0.5f, 0.1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(220f, 56f);
            buttonGo.GetComponent<Image>().color = new Color(0.28f, 0.42f, 0.55f, 0.95f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "恢复跟随";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 22;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _resumeFollowButtonRoot = buttonGo;
            buttonGo.SetActive(false);

            var button = buttonGo.GetComponent<Button>();
            if (_cameraFollow != null)
            {
                _cameraFollow.BindResumeButton(_resumeFollowButtonRoot, button);
            }
        }

        private void ApplyCombatCameraPose()
        {
            if (_combatCamera == null)
            {
                return;
            }

            var cam = _configs != null
                ? _configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            cam.ApplyCombatCameraPose(_combatCamera, _mapCenter, cam.PushMapOrthoSize);
            _combatCamera.transparencySortMode = TransparencySortMode.CustomAxis;
            _combatCamera.transparencySortAxis = Vector3.forward;
        }

        private void ReleaseNavMesh()
        {
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
                _navMeshInstance = default;
            }
        }

        private void OnDestroy()
        {
            if (_running)
            {
                EndInternal(destroyWorld: true);
            }
        }
    }
}
