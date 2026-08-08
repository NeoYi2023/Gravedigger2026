using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Defend stage presentation bridge (Approach A / D-040–D-043 + MassCombatPathing MP-06).
    /// Rules live in DefendSessionService. Move: AttackSlot / FormationHome via MassMoveScheduler.
    /// </summary>
    public sealed class DefendStageController : MonoBehaviour
    {
        [SerializeField] private DefendPrefabCatalog _catalog;
        [SerializeField] private FormationPrefabCatalog _formationCatalog;
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Camera _defendCamera;
        [SerializeField] private DefendHudView _hudView;
        [SerializeField] private FormationPanelView _formationPanel;

        private readonly List<string> _poolLabels = new List<string>();
        private readonly List<string> _poolIds = new List<string>();
        private readonly List<string> _formationLabels = new List<string>();
        private readonly List<string> _formationIds = new List<string>();
        private readonly List<GameObject> _deployedViews = new List<GameObject>();
        private readonly List<WarriorAgentView> _warriorAgents = new List<WarriorAgentView>();
        private readonly List<MonsterAgentView> _monsters = new List<MonsterAgentView>();
        private readonly List<MassMoveSample> _moveSamples = new List<MassMoveSample>(64);

        private ConfigCsvRepository _configs;
        private DefendSessionService _session;
        private ProtagonistProgressService _progress;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private WarehouseService _warehouse;
        private Action _onVictoryAdvance;
        private Action<string> _onLevelFailure;
        private GameObject _mapInstance;
        private GameObject _battleProtagonistInstance;
        private DefendSpawnPointSet _spawnPoints;
        private EngageZone _engageZone;
        private NavMeshDataInstance _navMeshInstance;
        private Vector2 _mapHalfExtents = new Vector2(5f, 2.5f);
        private string _selectedWarriorId;
        private Vector3 _mapCenter;
        private bool _running;
        private bool _clearVictoryHintShown;
        private bool _driverOutcomeDispatched;
        private FormationEditorController _formationEditor;

        private MassMoveScheduler _moveScheduler;
        private AttackSlotService _attackSlots;
        private int _nextMoveId;
        private int _slotGoalCursor;

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
            Action onVictoryAdvance = null,
            Action<string> onLevelFailure = null)
        {
            EndInternal(destroyWorld: true);

            if (context?.DefendConfig == null)
            {
                Debug.LogError("[DefendStageController] Missing DefendConfig.");
                return;
            }

            if (_catalog == null)
            {
                Debug.LogError("[DefendStageController] DefendPrefabCatalog missing.");
                return;
            }

            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _warehouse = warehouse;
            _onVictoryAdvance = onVictoryAdvance;
            _onLevelFailure = onLevelFailure;
            _selectedWarriorId = null;
            _driverOutcomeDispatched = false;

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (!_catalog.TryGetMap(context.DefendConfig.BattleMapId, out var mapPrefab))
            {
                Debug.LogError($"[DefendStageController] Map prefab missing for '{context.DefendConfig.BattleMapId}'.");
                return;
            }

            EnsureWorldRoot();
            _mapInstance = Instantiate(mapPrefab, _worldRoot);
            _mapInstance.name = context.DefendConfig.BattleMapId;

            var bounds = _mapInstance.GetComponent<DigMapBounds>();
            _mapHalfExtents = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            _mapCenter = bounds != null ? bounds.Center : _mapInstance.transform.position;

            _engageZone = _mapInstance.GetComponentInChildren<EngageZone>(true);
            if (_engageZone == null)
            {
                var zoneGo = new GameObject("EngageZone");
                zoneGo.transform.SetParent(_mapInstance.transform, false);
                zoneGo.transform.position = _mapCenter;
                _engageZone = zoneGo.AddComponent<EngageZone>();
                _engageZone.SetHalfExtents(_mapHalfExtents * 0.85f);
            }

            _spawnPoints = _mapInstance.GetComponentInChildren<DefendSpawnPointSet>(true);
            if (_spawnPoints == null)
            {
                _spawnPoints = _mapInstance.AddComponent<DefendSpawnPointSet>();
            }

            ReleaseNavMesh();
            _navMeshInstance = DefendNavMeshBaker.Bake(_mapCenter, _mapHalfExtents);

            if (_defendCamera != null)
            {
                _defendCamera.gameObject.SetActive(true);
                _defendCamera.transform.position = _mapCenter + new Vector3(0f, 18f, 0f);
                _defendCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _defendCamera.orthographic = true;
                _defendCamera.orthographicSize = Mathf.Max(_mapHalfExtents.x, _mapHalfExtents.y) - 1.5f;
                _defendCamera.nearClipPlane = 0.1f;
                _defendCamera.farClipPlane = 100f;
                // SPEC_04 §15.2: same-order character sprites draw far-to-near along
                // world +Z — lower on screen (smaller Z) occludes higher.
                _defendCamera.transparencySortMode = TransparencySortMode.CustomAxis;
                _defendCamera.transparencySortAxis = Vector3.forward;
            }

            EnsureDefendLight();

            _session = new DefendSessionService();
            _session.PhaseChanged += HandlePhaseChanged;
            _session.ShieldChanged += HandleShieldChanged;
            _session.RemainingCombatSecondsChanged += HandleRemainingChanged;
            _session.WaveSpawnRequested += HandleWaveSpawnRequested;
            _session.LevelFailureRequested += HandleLevelFailureRequested;
            _session.ClearVictoryConditionDetected += HandleClearVictoryConditionDetected;
            _session.VictorySettled += HandleVictorySettled;
            _session.MonsterCombatStateChanged += HandleMonsterCombatStateChanged;
            _session.WarriorCombatStateChanged += HandleWarriorCombatStateChanged;
            _clearVictoryHintShown = false;
            _session.BeginPrepare(context.DefendConfig);

            if (_progress != null)
            {
                _progress.Changed += HandleProgressOrFormationChanged;
            }

            if (_warriorPool != null)
            {
                _warriorPool.Changed += HandleProgressOrFormationChanged;
            }

            _formation.Changed += HandleProgressOrFormationChanged;

            if (_formationPanel != null)
            {
                _formationPanel.gameObject.SetActive(false);
            }

            if (_hudView != null)
            {
                _hudView.StartBattleRequested += HandleStartBattleRequested;
                _hudView.Show();
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(false);
            }

            if (_defendCamera != null)
            {
                _defendCamera.gameObject.SetActive(false);
            }

            OpenFormationEditor();
            RefreshHud();
            _running = true;

            context.MapResolveNote =
                $"BattleMapId={context.DefendConfig.BattleMapId} → Instantiated {MapPrefabPaths.PrefabFolder}/{context.DefendConfig.BattleMapId}.prefab";
            Debug.Log(
                $"[DefendStage] Map instantiated at {_mapCenter} (EngageZone + SpawnPoints + NavMesh ready).");
        }

        public void End()
        {
            EndInternal(destroyWorld: true);
        }

        private void Update()
        {
            if (!_running || _session == null || !_session.IsActive)
            {
                return;
            }

            _session.Tick(Time.deltaTime);
            if (_session.Phase == DefendPhase.Combat)
            {
                TickMassCombatPathing();
            }
        }

        private void OnDestroy()
        {
            EndInternal(destroyWorld: false);
        }

        private void EndInternal(bool destroyWorld)
        {
            _running = false;

            if (_session != null)
            {
                _session.PhaseChanged -= HandlePhaseChanged;
                _session.ShieldChanged -= HandleShieldChanged;
                _session.RemainingCombatSecondsChanged -= HandleRemainingChanged;
                _session.WaveSpawnRequested -= HandleWaveSpawnRequested;
                _session.LevelFailureRequested -= HandleLevelFailureRequested;
                _session.ClearVictoryConditionDetected -= HandleClearVictoryConditionDetected;
                _session.VictorySettled -= HandleVictorySettled;
                _session.MonsterCombatStateChanged -= HandleMonsterCombatStateChanged;
                _session.WarriorCombatStateChanged -= HandleWarriorCombatStateChanged;
                _session.Stop();
                _session = null;
            }

            _onVictoryAdvance = null;
            _onLevelFailure = null;
            _warehouse = null;
            _driverOutcomeDispatched = false;

            if (_progress != null)
            {
                _progress.Changed -= HandleProgressOrFormationChanged;
                _progress = null;
            }

            if (_warriorPool != null)
            {
                _warriorPool.Changed -= HandleProgressOrFormationChanged;
                _warriorPool = null;
            }

            if (_formation != null)
            {
                _formation.Changed -= HandleProgressOrFormationChanged;
            }

            if (_formationPanel != null)
            {
                _formationPanel.DeployRequested -= HandleDeployRequested;
                _formationPanel.SelectRequested -= HandleSelectRequested;
                _formationPanel.UndeployRequested -= HandleUndeployRequested;
                _formationPanel.NudgeNegXRequested -= HandleNudgeNegX;
                _formationPanel.NudgePosXRequested -= HandleNudgePosX;
                _formationPanel.NudgeNegZRequested -= HandleNudgeNegZ;
                _formationPanel.NudgePosZRequested -= HandleNudgePosZ;
            }

            CloseFormationEditor();

            if (_hudView != null)
            {
                _hudView.StartBattleRequested -= HandleStartBattleRequested;
                _hudView.Hide();
            }

            ClearMonsters();
            ClearDeployedViews();
            ClearMassCombatPathing();
            ReleaseNavMesh();
            _formation = null;
            _configs = null;
            _selectedWarriorId = null;
            _spawnPoints = null;
            _engageZone = null;
            _clearVictoryHintShown = false;

            if (!destroyWorld)
            {
                return;
            }

            if (_battleProtagonistInstance != null)
            {
                Destroy(_battleProtagonistInstance);
                _battleProtagonistInstance = null;
            }

            if (_mapInstance != null)
            {
                Destroy(_mapInstance);
                _mapInstance = null;
            }

            if (_defendCamera != null)
            {
                _defendCamera.gameObject.SetActive(false);
            }
        }

        private void HandleStartBattleRequested()
        {
            if (_session == null || _formation == null || _progress == null || _configs == null)
            {
                return;
            }

            var deployed = _formation.Entries.Count;
            var waveRows = _configs.GetWaveSpawnRows(_session.Config?.WaveConfigId);
            var degree = _formation.ComputeLossOfControlDegree(_progress.ControlPowerCap);
            if (!_session.TryStartBattle(
                    deployed,
                    _progress.ProtagonistMaxHP,
                    waveRows,
                    degree,
                    _configs,
                    out var error))
            {
                _hudView?.SetHint(error ?? "不可开战");
                Debug.Log($"[DefendStage] StartBattle rejected: {error}");
                RefreshHud();
                return;
            }

            CloseFormationEditor();
            if (_defendCamera != null)
            {
                _defendCamera.gameObject.SetActive(true);
                _defendCamera.transform.position = _mapCenter + new Vector3(0f, 18f, 0f);
                _defendCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _defendCamera.orthographic = true;
                _defendCamera.orthographicSize = Mathf.Max(_mapHalfExtents.x, _mapHalfExtents.y) - 1.5f;
                // SPEC_04 §15.2: same-order character sprites draw far-to-near along
                // world +Z — lower on screen (smaller Z) occludes higher.
                _defendCamera.transparencySortMode = TransparencySortMode.CustomAxis;
                _defendCamera.transparencySortAxis = Vector3.forward;
            }

            EnsurePathingServices();
            DeployCombatUnits();
            _session.ResolveStartBattleRebelRolls(_configs);
            if (_hudView != null)
            {
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(true);
                _hudView.SetHint(
                    $"战斗中 Degree={_session.LockedLossOfControlDegree:0.##} Tier={_session.LockedLossOfControlTierId}；清场胜利入账 Exp；护盾归零失败");
            }

            RefreshHud();
        }

        private void OpenFormationEditor()
        {
            CloseFormationEditor();
            if (_formationCatalog == null || _formationCatalog.FormationEditorRootPrefab == null)
            {
                Debug.LogError("[DefendStage] FormationEditorRoot missing — falling back to HUD StartBattle only.");
                if (_hudView != null)
                {
                    _hudView.SetPrepareVisible(true);
                }

                if (_defendCamera != null)
                {
                    _defendCamera.gameObject.SetActive(true);
                }

                return;
            }

            var instance = Instantiate(_formationCatalog.FormationEditorRootPrefab, transform);
            instance.name = "FormationEditorRoot(Clone)";
            _formationEditor = instance.GetComponent<FormationEditorController>();
            if (_formationEditor == null)
            {
                _formationEditor = instance.AddComponent<FormationEditorController>();
            }

            _formationEditor.StartBattleRequested += HandleStartBattleRequested;
            _formationEditor.Begin(
                FormationEditorMode.DefendPrepare,
                _catalog,
                _warriorPool,
                _formation,
                _progress,
                null,
                _mapInstance);
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

        private void HandleWaveSpawnRequested(DefendWaveSpawnRequest request)
        {
            if (request == null || _session == null || _configs == null || _catalog == null)
            {
                return;
            }

            if (!_configs.TryGetMonster(request.MonsterId, out var monsterRow) || monsterRow == null)
            {
                Debug.LogWarning($"[DefendStage] MonsterConfig missing: {request.MonsterId}");
                return;
            }

            _catalog.TryGetMonsterModel(monsterRow.ModelId, out var modelPrefab);
            if (modelPrefab == null)
            {
                Debug.LogWarning(
                    $"[DefendStage] Monster Prefab missing: Assets/Prefabs/Defend/Monsters/{monsterRow.ModelId}.prefab — runtime temp cube.");
            }

            var retarget = _session.Config != null
                ? Mathf.Max(0.1f, _session.Config.TargetRetargetIntervalSeconds)
                : 1f;
            var protagonistTf = _battleProtagonistInstance != null
                ? _battleProtagonistInstance.transform
                : null;

            for (var i = 0; i < request.SpawnCount; i++)
            {
                var pos = _spawnPoints != null
                    ? _spawnPoints.ResolveSpawnPosition(
                        request.AppearLocation,
                        request.SpawnMode,
                        request.SpawnClockHour,
                        _mapCenter,
                        _mapHalfExtents)
                    : _mapCenter + new Vector3(_mapHalfExtents.x * 0.9f, 0f, 0f);

                if (NavMesh.SamplePosition(pos, out var hit, 3f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }

                GameObject go;
                if (modelPrefab != null)
                {
                    go = Instantiate(modelPrefab, _worldRoot);
                }
                else
                {
                    go = CreateTempMonsterVisual(monsterRow.ModelId);
                    go.transform.SetParent(_worldRoot, false);
                }

                go.name = $"Monster_{monsterRow.MonsterId}_{_monsters.Count}";
                go.transform.position = pos;

                var runtimeId = _session.RegisterMonster(monsterRow.MonsterId, monsterRow.MaxHP);
                if (string.IsNullOrEmpty(runtimeId))
                {
                    Destroy(go);
                    continue;
                }

                var agentView = go.GetComponent<MonsterAgentView>();
                if (agentView == null)
                {
                    agentView = go.AddComponent<MonsterAgentView>();
                }

                EnsurePathingServices();
                var moveId = ++_nextMoveId;
                agentView.Bind(
                    _session,
                    runtimeId,
                    monsterRow,
                    protagonistTf,
                    () => _warriorAgents,
                    retarget,
                    _moveScheduler,
                    _attackSlots,
                    moveId);
                _monsters.Add(agentView);
            }

            RefreshHud();
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

        private void HandleLevelFailureRequested()
        {
            ClearMonsters();
            ApplyPermanentDeathFate();
            if (_hudView != null)
            {
                _hudView.SetHint("LevelFailure：护盾归零 — 不入账本阶段经验，关卡中止");
            }

            RefreshHud();
            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _onLevelFailure?.Invoke("护盾归零");
        }

        private void HandleClearVictoryConditionDetected()
        {
            _clearVictoryHintShown = true;
            if (_hudView != null)
            {
                _hudView.SetHint("清场条件已满足 — 进入胜利结算…");
            }

            RefreshHud();
        }

        private void HandleVictorySettled(long stageExp)
        {
            ClearMonsters();
            ApplyPermanentDeathFate();
            var before = _progress != null ? _progress.LifetimeExperience : 0L;
            var levels = _progress != null ? _progress.AddExperience(stageExp) : 0;
            var after = _progress != null ? _progress.LifetimeExperience : 0L;
            if (_hudView != null)
            {
                _hudView.SetHint(
                    $"胜利结算：+{stageExp} Exp（{before}→{after}）升{levels}级 → 推进阶段");
            }

            RefreshHud();
            Debug.Log(
                $"[DefendStage] Victory Exp +{stageExp} Lifetime={after} Level={_progress?.Level} (+{levels})");

            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _onVictoryAdvance?.Invoke();
        }

        private void ApplyPermanentDeathFate()
        {
            if (_session == null)
            {
                return;
            }

            var ids = _session.CollectPermanentDeadWarriorIds();
            for (var i = 0; i < ids.Count; i++)
            {
                var warriorId = ids[i];
                if (_warriorPool != null && _warriorPool.TryGet(warriorId, out var warrior) && warrior != null)
                {
                    if (_warehouse != null && warrior.GemIds != null)
                    {
                        for (var g = 0; g < warrior.GemIds.Count; g++)
                        {
                            var gemId = warrior.GemIds[g];
                            if (!string.IsNullOrEmpty(gemId))
                            {
                                _warehouse.AddItem(gemId, 1);
                            }
                        }
                    }

                    _warriorPool.TryRemove(warriorId);
                }

                if (_formation != null && _formation.IsDeployed(warriorId))
                {
                    _formation.TryUndeploy(warriorId, out _);
                }

                Debug.Log($"[DefendStage] PermanentDeath applied {warriorId} (gems→warehouse, remove pool/formation)");
            }
        }

        private void HandleMonsterCombatStateChanged(string runtimeId)
        {
            if (_session == null || string.IsNullOrEmpty(runtimeId))
            {
                return;
            }

            if (!_session.IsMonsterAlive(runtimeId))
            {
                _attackSlots?.ReleaseAllForTarget(runtimeId);
                for (var i = 0; i < _monsters.Count; i++)
                {
                    var m = _monsters[i];
                    if (m != null && string.Equals(m.RuntimeId, runtimeId, StringComparison.Ordinal))
                    {
                        m.NotifyKilled();
                        break;
                    }
                }
            }

            RefreshHud();
        }

        private void HandleWarriorCombatStateChanged(string warriorId)
        {
            if (_warriorPool == null || _session == null || string.IsNullOrEmpty(warriorId))
            {
                RefreshHud();
                return;
            }

            if (TryFindWarrior(warriorId, out var warrior))
            {
                _session.SyncWarriorRemainingHpToInstance(warrior);
                _warriorPool.NotifyMutated();
                if (_formation != null && _formation.IsDeployed(warriorId))
                {
                    _formation.TrySetRemainingHp(warriorId, warrior.RemainingHP, out _);
                }
            }

            RefreshHud();
        }

        private void DeployCombatUnits()
        {
            ClearDeployedViews();
            _warriorAgents.Clear();
            EnsurePathingServices();
            _moveScheduler.Clear();
            _attackSlots.Clear();
            _nextMoveId = 0;
            _slotGoalCursor = 0;

            var protagonistPrefab = _catalog != null ? _catalog.BattleProtagonistPrefab : null;
            if (protagonistPrefab == null)
            {
                Debug.LogError("[DefendStage] BattleProtagonist prefab missing.");
            }
            else
            {
                _battleProtagonistInstance = Instantiate(protagonistPrefab, _worldRoot);
                _battleProtagonistInstance.name = "BattleProtagonist";
                _battleProtagonistInstance.transform.position = _mapCenter;
                _deployedViews.Add(_battleProtagonistInstance);
            }

            if (_formation == null || _warriorPool == null || _catalog == null || _session == null || _configs == null)
            {
                return;
            }

            var retarget = _session.Config != null
                ? Mathf.Max(0.1f, _session.Config.TargetRetargetIntervalSeconds)
                : 1f;

            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!TryFindWarrior(entry.WarriorId, out var warrior))
                {
                    Debug.LogWarning($"[DefendStage] Warrior '{entry.WarriorId}' missing from pool — skip deploy.");
                    continue;
                }

                if (!_catalog.TryGetWarriorAppearance(warrior.AppearanceId, out var appearancePrefab)
                    || appearancePrefab == null)
                {
                    Debug.LogWarning(
                        $"[DefendStage] Appearance Prefab missing: Assets/Prefabs/Defend/Warriors/{warrior.AppearanceId}.prefab");
                    continue;
                }

                _configs.TryGetClass(warrior.ClassId, out var classRow);
                if (!_session.TryRegisterWarrior(warrior, classRow, out _, out var regError))
                {
                    Debug.LogWarning($"[DefendStage] RegisterWarrior failed: {regError}");
                    continue;
                }

                var bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
                var facingYawFlip = false;
                if (_configs.TryGetAppearance(warrior.AppearanceId, out var appearanceRow) &&
                    appearanceRow != null)
                {
                    bodyRadius = appearanceRow.BodyRadius;
                    facingYawFlip = appearanceRow.FacingYawFlip == 1;
                }

                var go = Instantiate(appearancePrefab, _worldRoot);
                go.name = $"Warrior_{warrior.Id}";
                var formationHome = new Vector3(
                    _mapCenter.x + entry.PositionX,
                    _mapCenter.y,
                    _mapCenter.z + entry.PositionZ);
                go.transform.position = formationHome;
                _deployedViews.Add(go);

                var agent = go.GetComponent<WarriorAgentView>();
                if (agent == null)
                {
                    agent = go.AddComponent<WarriorAgentView>();
                }

                var moveId = ++_nextMoveId;
                agent.Bind(
                    _session,
                    warrior.Id,
                    _engageZone,
                    () => _monsters,
                    retarget,
                    _catalog != null ? _catalog.ProjectilePrefab : null,
                    _worldRoot,
                    () => _warriorAgents,
                    () => _battleProtagonistInstance != null ? _battleProtagonistInstance.transform : null,
                    formationHome,
                    _moveScheduler,
                    _attackSlots,
                    moveId,
                    bodyRadius,
                    facingYawFlip);
                _warriorAgents.Add(agent);
            }

            Debug.Log($"[DefendStage] Deployed protagonist + {_warriorAgents.Count} warriors (MassCombatPathing).");
        }

        private void ClearMonsters()
        {
            for (var i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i] != null)
                {
                    Destroy(_monsters[i].gameObject);
                }
            }

            _monsters.Clear();
        }

        private void ClearDeployedViews()
        {
            ClearProjectiles();
            for (var i = 0; i < _deployedViews.Count; i++)
            {
                if (_deployedViews[i] != null)
                {
                    Destroy(_deployedViews[i]);
                }
            }

            _deployedViews.Clear();
            _warriorAgents.Clear();
            _battleProtagonistInstance = null;
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
            _moveSamples.Clear();
            _nextMoveId = 0;
            _slotGoalCursor = 0;
        }

        /// <summary>
        /// MP-06: budgeted AttackSlot / FormationHome + MassMoveScheduler steer (≤50 each, round-robin).
        /// Same GoalKind semantics as PushMap MP-05 (no dual destination stacks).
        /// </summary>
        private void TickMassCombatPathing()
        {
            if (_moveScheduler == null)
            {
                return;
            }

            TickAttackSlotGoals();

            _moveSamples.Clear();
            for (var i = 0; i < _warriorAgents.Count; i++)
            {
                var warrior = _warriorAgents[i];
                if (warrior != null && warrior.MoveId != 0)
                {
                    _moveSamples.Add(warrior.BuildSample());
                }
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null && monster.IsAlive && monster.MoveId != 0)
                {
                    _moveSamples.Add(monster.BuildSample());
                }
            }

            _moveScheduler.Tick(_moveSamples, Time.deltaTime);
        }

        private void TickAttackSlotGoals()
        {
            if (_attackSlots == null || _moveScheduler == null)
            {
                return;
            }

            var rosterCount = _warriorAgents.Count + _monsters.Count;
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

                if (_slotGoalCursor < _warriorAgents.Count)
                {
                    RefreshWarriorSlotGoal(_warriorAgents[_slotGoalCursor]);
                }
                else
                {
                    var mi = _slotGoalCursor - _warriorAgents.Count;
                    if (mi >= 0 && mi < _monsters.Count)
                    {
                        _monsters[mi]?.TryRefreshChaseGoal(_attackSlots, _moveScheduler);
                    }
                }

                _slotGoalCursor++;
            }
        }

        private void RefreshWarriorSlotGoal(WarriorAgentView warrior)
        {
            if (warrior == null || _moveScheduler == null || _attackSlots == null || warrior.MoveId == 0)
            {
                return;
            }

            if (_session == null || !_session.IsWarriorCombatActive(warrior.WarriorId))
            {
                _attackSlots.Release(warrior.AttackerId);
                _moveScheduler.SetPaused(warrior.MoveId, true);
                return;
            }

            if (warrior.IsRebel)
            {
                RefreshRebelSlotGoal(warrior);
                return;
            }

            var monster = warrior.FindNearestEngageMonster();
            if (monster == null)
            {
                // SPEC_03 §3.12: no EngageZone target → FormationHome; keep searching (abort on new target).
                _attackSlots.Release(warrior.AttackerId);
                _moveScheduler.SetPaused(warrior.MoveId, false);
                if (warrior.HasFormationHome)
                {
                    var home = warrior.FormationHome;
                    _moveScheduler.SetGoal(
                        warrior.MoveId,
                        GoalKind.FormationHome,
                        new Vector2(home.x, home.z));
                }

                return;
            }

            // SC-03: melee chase → Surround gap claim (B+); ranged → Chase (full ring).
            if (!_attackSlots.TryClaim(
                    warrior.AttackerId,
                    monster.RuntimeId,
                    warrior.AttackRange,
                    monster.transform.position,
                    out var slotPos,
                    warrior.AttackMode,
                    warrior.transform.position,
                    monster.BodyRadius,
                    CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, warrior.AttackMode)))
            {
                _moveScheduler.SetPaused(warrior.MoveId, true);
                return;
            }

            _moveScheduler.SetPaused(warrior.MoveId, false);
            _moveScheduler.SetGoal(
                warrior.MoveId,
                GoalKind.AttackSlot,
                new Vector2(slotPos.x, slotPos.z));
        }

        private void RefreshRebelSlotGoal(WarriorAgentView warrior)
        {
            if (!warrior.TryFindNearestRebelTarget(out var targetId, out var targetPos, out var bodyRadius))
            {
                _attackSlots.Release(warrior.AttackerId);
                _moveScheduler.SetPaused(warrior.MoveId, true);
                return;
            }

            var dist = Vector3.Distance(warrior.transform.position, targetPos);
            if (dist <= warrior.AttackRange)
            {
                _attackSlots.Release(warrior.AttackerId);
                _moveScheduler.SetPaused(warrior.MoveId, true);
                return;
            }

            if (!_attackSlots.TryClaim(
                    warrior.AttackerId,
                    targetId,
                    warrior.AttackRange,
                    targetPos,
                    out var slotPos,
                    warrior.AttackMode,
                    warrior.transform.position,
                    bodyRadius,
                    CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, warrior.AttackMode)))
            {
                var ring = AttackSlotService.ComputeRingRadius(warrior.AttackRange);
                var away = warrior.transform.position - targetPos;
                away.y = 0f;
                if (away.sqrMagnitude < 1e-6f)
                {
                    away = Vector3.forward;
                }

                slotPos = targetPos + away.normalized * ring;
            }

            _moveScheduler.SetPaused(warrior.MoveId, false);
            _moveScheduler.SetGoal(
                warrior.MoveId,
                GoalKind.AttackSlot,
                new Vector2(slotPos.x, slotPos.z));
        }

        private void ClearProjectiles()
        {
            if (_worldRoot == null)
            {
                return;
            }

            var projectiles = _worldRoot.GetComponentsInChildren<ProjectileView>(true);
            for (var i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null)
                {
                    Destroy(projectiles[i].gameObject);
                }
            }
        }

        private void ReleaseNavMesh()
        {
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
                _navMeshInstance = default;
            }
        }

        private void HandlePhaseChanged(DefendPhase phase)
        {
            RefreshHud();
        }

        private void HandleShieldChanged(int shield, int cap)
        {
            RefreshHud();
        }

        private void HandleRemainingChanged(int remaining)
        {
            RefreshHud();
        }

        private void HandleProgressOrFormationChanged()
        {
            if (_session != null && _session.Phase == DefendPhase.Prepare)
            {
                RefreshFormation();
                RefreshHud();
            }
        }

        private void RefreshHud()
        {
            if (_hudView == null || _session == null)
            {
                return;
            }

            var deployed = _formation != null ? _formation.Entries.Count : 0;
            var canStart = _session.CanStartBattle(deployed);
            _hudView.SetStartBattleInteractable(canStart);

            if (_session.Phase == DefendPhase.Prepare)
            {
                _hudView.SetPhaseText("DefendPhase：Prepare（可改布阵）");
                _hudView.SetCombatStatus(
                    $"上阵 {deployed} 人 · 护盾将取 ProtagonistMaxHP={_progress?.ProtagonistMaxHP ?? 0} · 倒计时初值={_session.Config?.CombatDurationSeconds ?? 0}s · Wave={_session.Config?.WaveConfigId}");
                _hudView.SetHint(canStart
                    ? "点击「开战」进入 Combat（须 ≥1 上阵）"
                    : "请先上阵至少 1 名士兵，再开战");
            }
            else if (_session.Phase == DefendPhase.Combat)
            {
                var clear = _session.IsClearVictoryConditionMet || _clearVictoryHintShown;
                var rebelCount = 0;
                for (var i = 0; i < _warriorAgents.Count; i++)
                {
                    var a = _warriorAgents[i];
                    if (a != null
                        && _session.TryGetWarrior(a.WarriorId, out var ws)
                        && ws != null
                        && ws.IsRebel
                        && _session.IsWarriorCombatActive(a.WarriorId))
                    {
                        rebelCount++;
                    }
                }

                _hudView.SetPhaseText("DefendPhase：Combat");
                _hudView.SetCombatStatus(
                    $"护盾 {_session.Shield}/{_session.ShieldCap} · 剩余 {_session.RemainingCombatSeconds}s · " +
                    $"存活怪 {_session.AliveMonsterCount}/{_session.RegisteredMonsterCount} · " +
                    $"Degree {_session.LockedLossOfControlDegree:0.##}/Tier{_session.LockedLossOfControlTierId} · Rebel {rebelCount} · " +
                    $"清场 {(clear ? "可检测✓" : "未满足")}");
            }
            else
            {
                _hudView.SetPhaseText("DefendPhase：Ended");
                _hudView.SetCombatStatus(
                    $"Ended · 最终护盾 {_session.Shield}/{_session.ShieldCap} · Exp={_progress?.LifetimeExperience ?? 0}");
            }
        }

        private void RefreshFormation()
        {
            // Legacy FormationPanelView path retired; FormationEditor owns Prepare UI.
        }

        private void HandleDeployRequested(string warriorId)
        {
            if (_session == null || _session.Phase != DefendPhase.Prepare || _formation == null
                || string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            if (!_formation.TryDeploy(warriorId, out var error))
            {
                Debug.Log($"[Defend Prepare] 上阵失败：{error}");
                RefreshFormation();
                return;
            }

            _selectedWarriorId = warriorId;
            Debug.Log($"[Defend Prepare] 上阵 {warriorId}");
        }

        private void HandleSelectRequested(string warriorId)
        {
            if (_session == null || _session.Phase != DefendPhase.Prepare || string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            _selectedWarriorId = warriorId;
            RefreshFormation();
        }

        private void HandleUndeployRequested()
        {
            if (_session == null || _session.Phase != DefendPhase.Prepare || _formation == null
                || string.IsNullOrEmpty(_selectedWarriorId))
            {
                return;
            }

            var id = _selectedWarriorId;
            if (!_formation.TryUndeploy(id, out var error))
            {
                Debug.Log($"[Defend Prepare] 下阵失败：{error}");
                RefreshFormation();
                return;
            }

            _selectedWarriorId = null;
            Debug.Log($"[Defend Prepare] 下阵 {id}");
        }

        private void HandleNudgeNegX()
        {
            Nudge(-BattleFormationService.DefaultNudgeStep, 0f);
        }

        private void HandleNudgePosX()
        {
            Nudge(BattleFormationService.DefaultNudgeStep, 0f);
        }

        private void HandleNudgeNegZ()
        {
            Nudge(0f, -BattleFormationService.DefaultNudgeStep);
        }

        private void HandleNudgePosZ()
        {
            Nudge(0f, BattleFormationService.DefaultNudgeStep);
        }

        private void Nudge(float dx, float dz)
        {
            if (_session == null || _session.Phase != DefendPhase.Prepare || _formation == null
                || string.IsNullOrEmpty(_selectedWarriorId))
            {
                return;
            }

            if (!_formation.TryNudge(_selectedWarriorId, dx, dz, out var error))
            {
                Debug.Log($"[Defend Prepare] 改位失败：{error}");
            }
        }

        private bool TryFindWarrior(string warriorId, out WarriorInstance warrior)
        {
            warrior = null;
            if (_warriorPool == null || string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            var list = _warriorPool.Warriors;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Id, warriorId, StringComparison.Ordinal))
                {
                    warrior = list[i];
                    return true;
                }
            }

            return false;
        }

        private void EnsureWorldRoot()
        {
            if (_worldRoot != null)
            {
                return;
            }

            var existing = transform.Find("WorldRoot");
            if (existing != null)
            {
                _worldRoot = existing;
                return;
            }

            var go = new GameObject("WorldRoot");
            go.transform.SetParent(transform, false);
            _worldRoot = go.transform;
        }

        private void EnsureDefendLight()
        {
            if (transform.Find("DefendLight") != null)
            {
                return;
            }

            var lightGo = new GameObject("DefendLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }
    }
}
