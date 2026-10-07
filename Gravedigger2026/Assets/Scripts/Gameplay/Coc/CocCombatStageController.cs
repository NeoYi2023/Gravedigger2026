using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core.Coc;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.Sandbox;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.PushMap;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// COC map view (SPEC_03 §3.21). Slice 03d deploys by ClassId, fights with PushMap
    /// chase/AttackSlot, marches to the Final Boss, and settles victory / round loss.
    /// Slice 04b stamps temporary reveal and explored-dark on the fog grid.
    /// Slice 05 stamps permanent light for activated capture points.
    /// Slice 06 expands a class card's soldiers on hold without changing pool order.
    /// D-100 slice 01: observe-range fog and three-axis product lock (Approach A).
    /// D-100 slice 02: destructible obstacles enter targeting; hits deal 1 HP.
    /// D-100 slice 03b: treat-as-target monsters hit a wall only when it blocks the soldier.
    /// D-103: deploy-block polygons reject clicks outside activated capture circles.
    /// Soldier repath: out of attack range, blocked straight lines follow NavMesh corners.
    /// Monster hit FX: MonsterDamageSettled → red DamagePopup + HitFlash (soldiers deferred).
    /// </summary>
    public sealed class CocCombatStageController : MonoBehaviour
    {
        private const float DeploySampleRadius = 1.5f;
        private const int WalkableAreaMask = 1;
        private const string DeployBlockTip = "该区域无法投入士兵";

        private readonly List<PushMapAdvanceView> _soldiers = new List<PushMapAdvanceView>(16);
        private readonly List<PushMapMonsterAgentView> _monsters = new List<PushMapMonsterAgentView>(16);
        private readonly List<AirWall> _airWalls = new List<AirWall>(8);
        private AirWallTilemap[] _airWallTiles = Array.Empty<AirWallTilemap>();
        private readonly List<CocDestructibleObstacle> _obstacles = new List<CocDestructibleObstacle>(4);
        private readonly List<CocClassCardModel> _cardModels = new List<CocClassCardModel>(8);
        private readonly List<MassMoveSample> _moveSamples = new List<MassMoveSample>(32);
        private readonly MassMoveScheduler _scheduler = new MassMoveScheduler();
        private readonly AttackSlotService _attackSlots = new AttackSlotService();
        private readonly CocSoldierNavPath _soldierNavPath = new CocSoldierNavPath();

        private GameObject _mapInstance;
        private Camera _camera;
        private PushMapCameraFollowController _follow;
        private Camera _suppressedMain;
        private GameObject _hudRoot;
        private Button _returnButton;
        private CocClassCardBar _cards;
        private Action<CocRoundResult> _onRoundEnded;
        private bool _running;
        private bool _settled;
        private ConfigCsvRepository _configs;
        private DefendPrefabCatalog _catalog;
        private WarriorPoolService _pool;
        private CocFieldCombatSession _field;
        private PushMapMonsterAgentView _finalBoss;
        private string _selectedClassId;
        private float _deployInterval = 0.1f;
        private float _nextDeployTime;
        private bool _deployHeld;
        private int _nextMoveId = 1;
        private float _groundY;
        private readonly List<CocFogGrid.RevealStamp> _revealStamps = new List<CocFogGrid.RevealStamp>(16);
        private readonly List<CocAttackPriority.Candidate> _priorityCandidates =
            new List<CocAttackPriority.Candidate>(16);
        private NavMeshDataInstance _navMesh;
        private NavMeshDataInstance _specialNavMesh;
        private bool _loggedDeployReject;
        private CocFogGrid _fogGrid;
        private CocFogOverlayView _fogOverlay;
        private CocDeployBlockArea _deployBlock;
        private CocDeployBlockOutlineView _deployBlockOutline;
        private ToastView _deployToast;
        private readonly List<CocDeployBlockArea.ActivatedCircle> _activatedCircles =
            new List<CocDeployBlockArea.ActivatedCircle>(4);
        private float _revealRadius = 2f;
        private SandboxProgressService _progress;
        private readonly List<CaptureSite> _captures = new List<CaptureSite>(4);

        public void Begin(
            LevelStageContext context,
            DefendPrefabCatalog catalog,
            ConfigCsvRepository configs,
            CocCombatSessionService session,
            WarriorPoolService pool,
            SandboxProgressService progress,
            Action<CocRoundResult> onRoundEnded)
        {
            End();
            _onRoundEnded = onRoundEnded;
            _configs = configs;
            _catalog = catalog;
            _pool = pool;
            _progress = progress;
            _running = true;
            _settled = false;
            _deployInterval = context?.CocConfig != null && context.CocConfig.DeployHoldIntervalSeconds > 0f
                ? context.CocConfig.DeployHoldIntervalSeconds
                : 0.1f;

            var mapId = context?.CocConfig != null ? context.CocConfig.MapId : context?.ResolvedMapId;
            if (catalog == null || string.IsNullOrEmpty(mapId) || !catalog.TryGetMap(mapId, out var mapPrefab) || mapPrefab == null)
            {
                Debug.LogError($"[CocCombatStage] Map prefab missing for '{mapId}'.");
                return;
            }

            _mapInstance = Instantiate(mapPrefab, transform);
            _mapInstance.name = mapId;
            _airWalls.Clear();
            _mapInstance.GetComponentsInChildren(true, _airWalls);
            _airWallTiles = _mapInstance.GetComponentsInChildren<AirWallTilemap>(true);
            BakeWalkNavMesh();

            _field = new CocFieldCombatSession(configs);
            _field.SetMonsterWorldXZProvider(TryGetMonsterWorldXZ);
            _field.MonsterDied += HandleMonsterDied;
            _field.MonsterDamageSettled += HandleMonsterDamageSettled;
            _field.ObstacleDamaged += HandleObstacleDamaged;
            _field.ObstacleDestroyed += HandleObstacleDestroyed;
            _field.MonsterBerserkTriggered += HandleMonsterBerserkTriggered;
            BindObstacles();
            var bindings = new CocMonsterSpawnBindings
            {
                Scheduler = _scheduler,
                AttackSlots = _attackSlots,
                Warriors = ProvideSoldiers,
                OnHitWarrior = HandleMonsterHitWarrior,
                AllocateMoveId = AllocateMoveId,
                OnSpawned = HandleMonsterSpawned
            };
            var spawned = CocMonsterSpawner.Spawn(
                _mapInstance,
                catalog,
                configs,
                session != null ? session.Spawns : null,
                bindings,
                _monsters);
            ResolveFinalBoss();
            BindCaptures(context?.CocConfig != null ? context.CocConfig.GameplayConfigId : null);
            BuildFog(context);
            BuildDeployBlock();
            EnsureCamera(configs);
            EnsureHud();
            if (_cards != null)
            {
                var holdSeconds = context?.CocConfig != null && context.CocConfig.CardHoldSeconds > 0f
                    ? context.CocConfig.CardHoldSeconds
                    : 1f;
                _cards.SetHoldSeconds(holdSeconds);
            }

            if (_pool != null)
            {
                _pool.Changed += HandlePoolChanged;
            }

            RebuildCards();
            Debug.Log(
                $"[CocCombatStage] Map instantiated MapId={mapId}. Spawned={spawned}. " +
                $"FinalBoss={(_finalBoss != null)}. Obstacles={_obstacles.Count}. Deploy ready.");
            TryEvaluateWipeLoss();
        }

        public void End()
        {
            StopAllCoroutines();

            if (_pool != null)
            {
                _pool.Changed -= HandlePoolChanged;
            }

            if (_field != null)
            {
                _field.MonsterDied -= HandleMonsterDied;
                _field.MonsterDamageSettled -= HandleMonsterDamageSettled;
                _field.ObstacleDamaged -= HandleObstacleDamaged;
                _field.ObstacleDestroyed -= HandleObstacleDestroyed;
                _field.MonsterBerserkTriggered -= HandleMonsterBerserkTriggered;
                _field.Clear();
                _field = null;
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(HandleReturnClicked);
                _returnButton = null;
            }

            if (_cards != null)
            {
                _cards.ClassSelected -= HandleClassSelected;
                _cards.Clear();
                _cards = null;
            }

            if (_follow != null)
            {
                _follow.Disable();
                _follow = null;
            }

            if (_suppressedMain != null)
            {
                _suppressedMain.enabled = true;
                _suppressedMain = null;
            }

            if (_hudRoot != null)
            {
                Destroy(_hudRoot);
                _hudRoot = null;
            }

            _deployToast = null;

            if (_deployBlockOutline != null)
            {
                Destroy(_deployBlockOutline.gameObject);
                _deployBlockOutline = null;
            }

            _deployBlock = null;
            _activatedCircles.Clear();

            if (_camera != null)
            {
                Destroy(_camera.gameObject);
                _camera = null;
            }

            if (_mapInstance != null)
            {
                Destroy(_mapInstance);
                _mapInstance = null;
            }

            _soldiers.Clear();
            _monsters.Clear();
            _airWalls.Clear();
            _airWallTiles = Array.Empty<AirWallTilemap>();
            _obstacles.Clear();
            _cardModels.Clear();
            _moveSamples.Clear();
            _scheduler.Clear();
            _attackSlots.Clear();
            _soldierNavPath.Clear();
            ReleaseNavMesh();
            _finalBoss = null;
            _selectedClassId = null;
            _loggedDeployReject = false;
            _fogGrid = null;
            _fogOverlay = null;
            _revealStamps.Clear();
            _priorityCandidates.Clear();
            _captures.Clear();
            _progress = null;
            _deployHeld = false;
            _nextMoveId = 1;
            _onRoundEnded = null;
            _pool = null;
            _configs = null;
            _catalog = null;
            _running = false;
            _settled = false;
        }

        private void OnDestroy()
        {
            if (_running)
            {
                End();
            }
        }

        private void Update()
        {
            if (!_running || _settled || _mapInstance == null)
            {
                return;
            }

            TickDeployInput();
            TickGoals();
            TickMove();
            TickCaptures();
            TickFog();
            ApplyFogVisibility();
            TryEvaluateWipeLoss();
        }

        private void TickDeployInput()
        {
            if (!Input.GetMouseButton(0))
            {
                _deployHeld = false;
                return;
            }

            if (IsPointerOverUi())
            {
                return;
            }

            var now = Time.time;
            if (!_deployHeld)
            {
                _deployHeld = true;
                if (TryDeployAtCursor())
                {
                    _nextDeployTime = now + _deployInterval;
                }

                return;
            }

            if (now < _nextDeployTime)
            {
                return;
            }

            TryDeployAtCursor();
            _nextDeployTime = now + _deployInterval;
        }

        private bool TryDeployAtCursor()
        {
            if (string.IsNullOrEmpty(_selectedClassId) || _pool == null || _field == null)
            {
                return false;
            }

            var hasSpecialMove = SelectedClassHasSpecialMove();
            if (!TryPickDeployPoint(hasSpecialMove, out var point))
            {
                return false;
            }

            if (!TryTakeFrontWarrior(_selectedClassId, out var warrior) || warrior == null)
            {
                return false;
            }

            if (_catalog == null ||
                !_catalog.TryGetWarriorAppearance(warrior.AppearanceId, out var appearancePrefab) ||
                appearancePrefab == null)
            {
                Debug.LogWarning(
                    $"[CocCombatStage] Appearance Prefab missing for {warrior.Id} '{warrior.AppearanceId}'. Not deployed.");
                return false;
            }

            ClassConfigRow classRow = null;
            if (_configs != null && !string.IsNullOrEmpty(warrior.ClassId))
            {
                _configs.TryGetClass(warrior.ClassId, out classRow);
            }

            if (!_field.TryRegisterWarrior(warrior, classRow, out var state, out var error) || state == null)
            {
                Debug.LogWarning($"[CocCombatStage] Register warrior failed: {error}");
                return false;
            }

            if (!_pool.TryRemove(warrior.Id))
            {
                _field.ForgetWarrior(warrior.Id);
                Debug.LogWarning($"[CocCombatStage] Pool remove failed for {warrior.Id}.");
                return false;
            }

            SpawnSoldier(warrior, classRow, state, appearancePrefab, point);
            RebuildCards();
            return true;
        }

        private void SpawnSoldier(
            WarriorInstance warrior,
            ClassConfigRow classRow,
            Gravedigger2026.Core.Defend.DefendCombatWarriorState state,
            GameObject appearancePrefab,
            Vector3 point)
        {
            var go = Instantiate(appearancePrefab, _mapInstance.transform);
            go.name = "Warrior_" + warrior.Id;
            go.transform.position = point;
            WarriorAllIn1StyleView.ApplyTo(go, _catalog != null ? _catalog.VisualStyleCatalog : null, warrior);

            var advance = go.GetComponent<PushMapAdvanceView>();
            if (advance == null)
            {
                advance = go.AddComponent<PushMapAdvanceView>();
            }

            var bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
            var pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient;
            var repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale;
            var facingYawFlip = false;
            if (_configs != null &&
                _configs.TryGetAppearance(warrior.AppearanceId, out var appearanceRow) &&
                appearanceRow != null)
            {
                bodyRadius = appearanceRow.BodyRadius;
                pushCoefficient = appearanceRow.PushCoefficient;
                repulsionScale = appearanceRow.RepulsionScale;
                facingYawFlip = appearanceRow.FacingYawFlip == 1;
            }

            bodyRadius *= WarriorVisualModelScale.Resolve(warrior);
            var chaseMult = classRow != null
                ? classRow.ChaseMoveSpeedMult
                : ClassConfigRow.DefaultChaseMoveSpeedMult;
            var moveId = AllocateMoveId();
            advance.Bind(
                _scheduler,
                moveId,
                Mathf.Max(0.1f, state.MoveSpeed),
                ProvideMonsters,
                state.AttackRange,
                state.AttackMode,
                warrior.Id,
                _attackSlots,
                _field,
                _catalog != null ? _catalog.ProjectilePrefab : null,
                _mapInstance.transform,
                _catalog,
                bodyRadius,
                facingYawFlip,
                pushCoefficient,
                repulsionScale,
                chaseMult,
                classRow != null && classRow.HasSpecialMove);
            advance.SetParabolaCrowd(ProvideSoldiers, () => null);
            var observe = CocAttackPriority.ResolveObserveRange(
                classRow != null ? classRow.ObserveRange : 0f,
                _revealRadius);
            advance.EnableCocAttackPriority(classRow, observe);
            advance.SetCocObstacleAimResolver(TryResolveObstacleAim);
            _scheduler.SetPaused(moveId, true);
            _soldiers.Add(advance);
        }

        private bool TryTakeFrontWarrior(string classId, out WarriorInstance warrior)
        {
            warrior = null;
            var list = _pool.Warriors;
            for (var i = 0; i < list.Count; i++)
            {
                var candidate = list[i];
                if (candidate != null && string.Equals(candidate.ClassId, classId, StringComparison.Ordinal))
                {
                    warrior = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool SelectedClassHasSpecialMove()
        {
            return _configs != null
                && !string.IsNullOrEmpty(_selectedClassId)
                && _configs.TryGetClass(_selectedClassId, out var row)
                && row != null
                && row.HasSpecialMove;
        }

        private bool TryPickDeployPoint(bool hasSpecialMove, out Vector3 point)
        {
            point = default;
            if (_camera == null)
            {
                return false;
            }

            if (!_navMesh.valid)
            {
                RejectDeployOnce("NavMesh was not baked.");
                return false;
            }

            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, _groundY, 0f));
            if (!plane.Raycast(ray, out var enter) || enter < 0f)
            {
                RejectDeployOnce("Ground ray missed.");
                return false;
            }

            var hitPoint = ray.GetPoint(enter);
            var agentTypeId = SpecialMoveNavMesh.ResolveAgentTypeId(hasSpecialMove);
            var sampled = SpecialMoveNavMesh.SampleWalkable(
                hitPoint,
                DeploySampleRadius,
                agentTypeId,
                out var navHit,
                WalkableAreaMask);
            if (IsInsideLockedDeployBlock(hitPoint) ||
                (sampled && IsInsideLockedDeployBlock(navHit.position)))
            {
                ShowDeployBlockTip();
                return false;
            }

            if (!sampled)
            {
                RejectDeployOnce("Click is off the walkable map.");
                return false;
            }

            var dx = navHit.position.x - hitPoint.x;
            var dz = navHit.position.z - hitPoint.z;
            if (dx * dx + dz * dz > DeploySampleRadius * DeploySampleRadius)
            {
                RejectDeployOnce("Click is off the walkable map.");
                return false;
            }

            if (IsInsideAirWall(hitPoint, hasSpecialMove) ||
                IsInsideAirWall(navHit.position, hasSpecialMove) ||
                IsInsideDestructibleObstacle(hitPoint) ||
                IsInsideDestructibleObstacle(navHit.position))
            {
                RejectDeployOnce("Click is inside an air wall.");
                return false;
            }

            if (IsInsideFog(hitPoint) || IsInsideFog(navHit.position))
            {
                RejectDeployOnce("Click is inside fog.");
                return false;
            }

            point = navHit.position;
            return true;
        }

        private void BakeWalkNavMesh()
        {
            ReleaseNavMesh();
            var bounds = _mapInstance.GetComponentInChildren<DigMapBounds>(true);
            var center = bounds != null ? bounds.Center : _mapInstance.transform.position;
            var half = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            _groundY = center.y;

            var defaultBoxes = new List<DefendNavMeshBaker.NavMeshBoxObstacle>(8);
            var specialBoxes = new List<DefendNavMeshBaker.NavMeshBoxObstacle>(8);
            AirWallBakeCollector.Collect(
                _mapInstance != null ? _mapInstance.transform : null,
                defaultBoxes,
                specialBoxes,
                out var defaultTileMesh,
                out var specialTileMesh);

            _navMesh = DefendNavMeshBaker.Bake(
                center,
                half,
                defaultBoxes,
                DefendNavMeshBaker.CocCarveAgentRadius,
                SpecialMoveNavMesh.DefaultAgentTypeId,
                defaultTileMesh);
            if (!_navMesh.valid)
            {
                Debug.LogError("[CocCombatStage] NavMesh bake failed. Deploy cannot find a walkable point.");
            }

            if (!SpecialMoveNavMesh.TryResolveSpecialMoveAgentTypeId(out var specialId))
            {
                if (specialTileMesh != null)
                {
                    Destroy(specialTileMesh);
                }

                Debug.LogError(
                    "[CocCombatStage] NavMesh Agent Type 'SpecialMove' missing; " +
                    "special-move soldiers stay on the default mesh.");
                return;
            }

            _specialNavMesh = DefendNavMeshBaker.Bake(
                center,
                half,
                specialBoxes,
                DefendNavMeshBaker.CocCarveAgentRadius,
                specialId,
                specialTileMesh);
        }

        private void ReleaseNavMesh()
        {
            if (_navMesh.valid)
            {
                NavMesh.RemoveNavMeshData(_navMesh);
                _navMesh = default;
            }

            if (_specialNavMesh.valid)
            {
                NavMesh.RemoveNavMeshData(_specialNavMesh);
                _specialNavMesh = default;
            }
        }

        private void RejectDeployOnce(string reason)
        {
            if (_loggedDeployReject)
            {
                return;
            }

            _loggedDeployReject = true;
            Debug.LogWarning($"[CocCombatStage] Deploy rejected: {reason}");
        }

        private bool IsInsideAirWall(Vector3 world, bool ignoreSupportsSpecialMove)
        {
            for (var i = 0; i < _airWalls.Count; i++)
            {
                var wall = _airWalls[i];
                if (wall == null)
                {
                    continue;
                }

                if (ignoreSupportsSpecialMove && wall.SupportsSpecialMove)
                {
                    continue;
                }

                if (wall.ContainsXZ(world))
                {
                    return true;
                }
            }

            for (var i = 0; i < _airWallTiles.Length; i++)
            {
                var layer = _airWallTiles[i];
                if (layer != null && layer.Blocks(world.x, world.z, ignoreSupportsSpecialMove))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInsideDestructibleObstacle(Vector3 world)
        {
            for (var i = 0; i < _obstacles.Count; i++)
            {
                var obstacle = _obstacles[i];
                if (obstacle != null &&
                    obstacle.IsAlive &&
                    (_field == null || _field.IsObstacleTargetable(obstacle.RuntimeId)) &&
                    obstacle.ContainsXZ(world))
                {
                    return true;
                }
            }

            return false;
        }

        private void BindObstacles()
        {
            _obstacles.Clear();
            if (_mapInstance == null)
            {
                return;
            }

            _mapInstance.GetComponentsInChildren(true, _obstacles);
            for (var i = 0; i < _obstacles.Count; i++)
            {
                var obstacle = _obstacles[i];
                if (obstacle == null)
                {
                    continue;
                }

                obstacle.BindRuntimeId("CocObstacle_" + (i + 1).ToString("00"));
                _field?.TryRegisterObstacle(obstacle.RuntimeId, obstacle.MaxHp);
            }
        }

        private bool TryResolveObstacleAim(string runtimeId, out Transform transform, out float bodyRadius)
        {
            transform = null;
            bodyRadius = 0f;
            if (!TryFindObstacle(runtimeId, out var obstacle) ||
                obstacle == null ||
                !obstacle.IsAlive)
            {
                return false;
            }

            if (_field != null && !_field.IsObstacleTargetable(runtimeId))
            {
                return false;
            }

            transform = obstacle.transform;
            bodyRadius = obstacle.BodyRadius;
            return true;
        }

        private void BuildFog(LevelStageContext context)
        {
            var unexplored = context?.CocConfig != null ? context.CocConfig.UnexploredAlpha : 0.9f;
            var explored = context?.CocConfig != null ? context.CocConfig.ExploredAlpha : 0.7f;
            _revealRadius = context?.CocConfig != null && context.CocConfig.RevealRadius > 0f
                ? context.CocConfig.RevealRadius
                : 2f;
            _fogGrid = CocFogGrid.Build(CollectFogPolygons());
            if (_fogGrid == null || !_fogGrid.HasCoverage)
            {
                Debug.LogWarning("[CocCombatStage] No fog cells. The whole walkable map can receive soldiers.");
                return;
            }

            StampSavedCaptures();

            _fogOverlay = CocFogOverlayView.Create(
                _mapInstance.transform,
                _fogGrid,
                unexplored,
                explored,
                _groundY);
            ApplyFogVisibility();
            Debug.Log(
                $"[CocCombatStage] Fog cells={_fogGrid.UnseenCount} " +
                $"grid={_fogGrid.Width}x{_fogGrid.Height} " +
                $"unseenAlpha={unexplored:0.##} exploredAlpha={explored:0.##} " +
                $"revealRadius={_revealRadius:0.##}.");
        }

        private List<CocFogGrid.AuthoredPolygon> CollectFogPolygons()
        {
            var markers = _mapInstance.GetComponentsInChildren<CocFogPolygon>(true);
            var polygons = new List<CocFogGrid.AuthoredPolygon>(markers.Length);
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                var count = marker.PointCount;
                if (count < 3)
                {
                    Debug.LogWarning($"[CocCombatStage] CocFogPolygon '{marker.name}' has {count} points. Skip.");
                    continue;
                }

                var groupId = marker.FogGroupId;
                if (groupId < 0)
                {
                    Debug.LogWarning(
                        $"[CocCombatStage] CocFogPolygon '{marker.name}' FogGroupId={groupId}. Using 0.");
                    groupId = 0;
                }
                else if (groupId > CocFogGrid.MaxGroupId)
                {
                    Debug.LogWarning(
                        $"[CocCombatStage] CocFogPolygon '{marker.name}' FogGroupId={groupId} exceeds {CocFogGrid.MaxGroupId}. Clamped.");
                    groupId = CocFogGrid.MaxGroupId;
                }

                var poly = new Vector2[count];
                for (var p = 0; p < count; p++)
                {
                    var world = marker.transform.GetChild(p).position;
                    poly[p] = new Vector2(world.x, world.z);
                }

                polygons.Add(new CocFogGrid.AuthoredPolygon(poly, groupId));
            }

            return polygons;
        }

        private bool IsInsideFog(Vector3 world)
        {
            return _fogGrid != null && _fogGrid.BlocksDeploy(world.x, world.z);
        }

        private void BuildDeployBlock()
        {
            if (_mapInstance == null)
            {
                return;
            }

            _deployBlock = CocDeployBlockArea.Build(CollectDeployBlockPolygons());
            if (_deployBlock == null || !_deployBlock.HasCoverage)
            {
                return;
            }

            _deployBlockOutline = CocDeployBlockOutlineView.Create(
                _mapInstance.transform,
                _deployBlock,
                _groundY);
            Debug.Log($"[CocCombatStage] Deploy-block polygons={_deployBlock.Polygons.Count}.");
        }

        private List<Vector2[]> CollectDeployBlockPolygons()
        {
            var markers = _mapInstance.GetComponentsInChildren<CocDeployBlockPolygon>(true);
            var polygons = new List<Vector2[]>(markers.Length);
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                var count = marker.PointCount;
                if (count < 3)
                {
                    Debug.LogWarning(
                        $"[CocCombatStage] CocDeployBlockPolygon '{marker.name}' has {count} points. Skip.");
                    continue;
                }

                var poly = new Vector2[count];
                for (var p = 0; p < count; p++)
                {
                    var world = marker.transform.GetChild(p).position;
                    poly[p] = new Vector2(world.x, world.z);
                }

                polygons.Add(poly);
            }

            return polygons;
        }

        private bool IsInsideLockedDeployBlock(Vector3 world)
        {
            if (_deployBlock == null || !_deployBlock.HasCoverage)
            {
                return false;
            }

            _activatedCircles.Clear();
            for (var i = 0; i < _captures.Count; i++)
            {
                var site = _captures[i];
                if (!site.Activated)
                {
                    continue;
                }

                _activatedCircles.Add(new CocDeployBlockArea.ActivatedCircle(site.X, site.Z, site.Radius));
            }

            return _deployBlock.BlocksDeploy(world.x, world.z, _activatedCircles);
        }

        private void ShowDeployBlockTip()
        {
            if (_deployToast != null)
            {
                _deployToast.Show(DeployBlockTip);
            }

            RejectDeployOnce("Click is inside a deploy-block zone.");
        }

        private void BindCaptures(string gameplayConfigId)
        {
            _captures.Clear();
            if (_mapInstance == null || _configs == null || string.IsNullOrEmpty(gameplayConfigId))
            {
                return;
            }

            var markers = _mapInstance.GetComponentsInChildren<CocCapturePoint>(true);
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                var id = marker.CapturePointId;
                if (!_configs.TryGetCocCapture(id, out var row) || row == null)
                {
                    Debug.LogWarning($"[CocCombatStage] CocCapturePoint '{id}' has no config row. Skip.");
                    continue;
                }

                if (!string.Equals(row.GameplayConfigId, gameplayConfigId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (ContainsCapture(id))
                {
                    Debug.LogWarning($"[CocCombatStage] Duplicate CocCapturePoint '{id}'. Skip.");
                    continue;
                }

                var pos = marker.transform.position;
                _captures.Add(new CaptureSite
                {
                    Id = id,
                    X = pos.x,
                    Z = pos.z,
                    Radius = row.Radius,
                    Activated = _progress != null && _progress.IsCaptureActivated(id)
                });
            }

            var rows = _configs.GetCocCaptures(gameplayConfigId);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || ContainsCapture(row.CapturePointId))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"[CocCombatStage] Capture '{row.CapturePointId}' has no marker on the current map. Skip.");
            }
        }

        private bool ContainsCapture(string capturePointId)
        {
            for (var i = 0; i < _captures.Count; i++)
            {
                if (string.Equals(_captures[i].Id, capturePointId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void StampSavedCaptures()
        {
            if (_fogGrid == null || !_fogGrid.HasCoverage)
            {
                return;
            }

            for (var i = 0; i < _captures.Count; i++)
            {
                var site = _captures[i];
                if (!site.Activated)
                {
                    continue;
                }

                _fogGrid.StampPermanent(site.X, site.Z, site.Radius);
            }
        }

        private void TickCaptures()
        {
            if (_captures.Count == 0)
            {
                return;
            }

            var painted = false;
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier == null || !soldier.IsCombatActive)
                {
                    continue;
                }

                var pos = soldier.transform.position;
                for (var c = 0; c < _captures.Count; c++)
                {
                    var site = _captures[c];
                    if (site.Activated)
                    {
                        continue;
                    }

                    var dx = pos.x - site.X;
                    var dz = pos.z - site.Z;
                    if (dx * dx + dz * dz > site.Radius * site.Radius)
                    {
                        continue;
                    }

                    if (ActivateCapture(c))
                    {
                        painted = true;
                    }
                }
            }

            if (painted && _fogOverlay != null && _fogGrid != null)
            {
                _fogOverlay.Refresh(_fogGrid);
            }
        }

        private bool ActivateCapture(int index)
        {
            var site = _captures[index];
            if (site.Activated)
            {
                return false;
            }

            site.Activated = true;
            _captures[index] = site;

            var grant = false;
            if (_progress == null)
            {
                Debug.LogWarning($"[CocCombatStage] Capture '{site.Id}' activated without a save.");
            }
            else
            {
                grant = _progress.TryActivateCapture(site.Id);
            }

            var stamped = _fogGrid != null && _fogGrid.StampPermanent(site.X, site.Z, site.Radius);
            if (grant)
            {
                GrantCaptureRewards(site.Id);
            }

            Debug.Log($"[CocCombatStage] Capture '{site.Id}' activated. Stamped={stamped}. Grant={grant}.");
            return stamped;
        }

        private void GrantCaptureRewards(string capturePointId)
        {
            if (_progress == null || _configs == null)
            {
                return;
            }

            var rewards = _configs.GetCocCaptureRewards(capturePointId);
            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                if (reward == null || reward.AddCount <= 0 || string.IsNullOrEmpty(reward.TargetNodeId))
                {
                    continue;
                }

                if (_progress.TryGetNode(reward.TargetNodeId, out _, out var cleared))
                {
                    if (cleared)
                    {
                        Debug.Log(
                            $"[CocCombatStage] Capture '{capturePointId}' skips cleared node '{reward.TargetNodeId}'.");
                        continue;
                    }
                }
                else if (_configs.TryGetSandboxNode(reward.TargetNodeId, out var node) && node != null)
                {
                    _progress.EnsureSeen(reward.TargetNodeId, node.RepeatEnterCount);
                    _progress.FlushNewNodes();
                }
                else
                {
                    Debug.LogWarning(
                        $"[CocCombatStage] Capture '{capturePointId}' reward target '{reward.TargetNodeId}' is missing.");
                    continue;
                }

                if (_progress.TryAddRemainingEnterCount(reward.TargetNodeId, reward.AddCount))
                {
                    Debug.Log(
                        $"[CocCombatStage] Capture '{capturePointId}' added {reward.AddCount} to '{reward.TargetNodeId}'.");
                }
            }
        }

        private void TickFog()
        {
            if (_fogGrid == null || !_fogGrid.HasCoverage)
            {
                return;
            }

            _revealStamps.Clear();
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier == null || !soldier.IsCombatActive)
                {
                    continue;
                }

                var pos = soldier.transform.position;
                _revealStamps.Add(new CocFogGrid.RevealStamp(
                    new Vector2(pos.x, pos.z),
                    soldier.CocObserveRange));
            }

            if (!_fogGrid.ApplyReveal(_revealStamps))
            {
                return;
            }

            if (_fogOverlay != null)
            {
                _fogOverlay.Refresh(_fogGrid);
            }
        }

        private void ApplyFogVisibility()
        {
            if (_fogGrid == null || !_fogGrid.HasCoverage)
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

                var role = monster.GetComponent<CocMonsterSpawnRole>();
                if (role == null)
                {
                    continue;
                }

                var pos = monster.transform.position;
                role.ApplyFog(_fogGrid.GetWorldCell(pos.x, pos.z));
            }
        }

        private void TickGoals()
        {
            var living = CountLivingFriendlies();
            _soldierNavPath.BeginFrame();
            for (var i = 0; i < _soldiers.Count; i++)
            {
                RefreshSoldierGoal(_soldiers[i]);
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                RefreshMonster(living, _monsters[i]);
            }
        }

        private bool TryGetCocEngageTarget(
            PushMapAdvanceView soldier,
            out PushMapMonsterAgentView monster,
            out CocDestructibleObstacle obstacle)
        {
            monster = null;
            obstacle = null;
            if (soldier == null)
            {
                return false;
            }

            var observe = soldier.CocObserveRange;
            var lockedId = soldier.CocLockedTargetId;
            if (!string.IsNullOrEmpty(lockedId))
            {
                if (TryFindCocMonster(lockedId, out var locked) &&
                    IsCocEnemyCandidate(locked) &&
                    CocAttackPriority.IsInsideObserveRange(
                        CombatReach.DistanceXZ(soldier.transform.position, locked.transform.position),
                        observe))
                {
                    monster = locked;
                    return true;
                }

                if (TryFindObstacle(lockedId, out var lockedWall) &&
                    IsCocObstacleCandidate(lockedWall) &&
                    CocAttackPriority.IsInsideObserveRange(
                        CombatReach.DistanceXZ(soldier.transform.position, lockedWall.transform.position),
                        observe))
                {
                    obstacle = lockedWall;
                    return true;
                }
            }

            _priorityCandidates.Clear();
            for (var i = 0; i < _monsters.Count; i++)
            {
                var candidate = _monsters[i];
                if (!IsCocEnemyCandidate(candidate))
                {
                    continue;
                }

                var dist = CombatReach.DistanceXZ(
                    soldier.transform.position,
                    candidate.transform.position);
                if (!CocAttackPriority.IsInsideObserveRange(dist, observe))
                {
                    continue;
                }

                _priorityCandidates.Add(new CocAttackPriority.Candidate(
                    candidate.RuntimeTargetId,
                    dist,
                    candidate.TargetValue,
                    CocAttackPriority.TypeEnemyUnit));
            }

            for (var i = 0; i < _obstacles.Count; i++)
            {
                var wall = _obstacles[i];
                if (!IsCocObstacleCandidate(wall))
                {
                    continue;
                }

                var dist = CombatReach.DistanceXZ(
                    soldier.transform.position,
                    wall.transform.position);
                if (!CocAttackPriority.IsInsideObserveRange(dist, observe))
                {
                    continue;
                }

                _priorityCandidates.Add(new CocAttackPriority.Candidate(
                    wall.RuntimeId,
                    dist,
                    wall.TargetValue,
                    CocAttackPriority.TypeDestructibleObstacle));
            }

            if (!CocAttackPriority.TryPickBest(soldier.CocClass, _priorityCandidates, out var bestId))
            {
                soldier.SetCocLockedTargetId(null);
                return false;
            }

            if (TryFindCocMonster(bestId, out monster) && monster != null)
            {
                soldier.SetCocLockedTargetId(bestId);
                return true;
            }

            if (TryFindObstacle(bestId, out obstacle) && obstacle != null)
            {
                soldier.SetCocLockedTargetId(bestId);
                return true;
            }

            soldier.SetCocLockedTargetId(null);
            monster = null;
            obstacle = null;
            return false;
        }

        private bool IsCocEnemyCandidate(PushMapMonsterAgentView m)
        {
            if (m == null || !m.IsAlive || string.IsNullOrEmpty(m.RuntimeTargetId))
            {
                return false;
            }

            return _field == null || _field.IsMonsterTargetable(m.RuntimeTargetId);
        }

        private bool IsCocObstacleCandidate(CocDestructibleObstacle obstacle)
        {
            if (obstacle == null || !obstacle.IsAlive || string.IsNullOrEmpty(obstacle.RuntimeId))
            {
                return false;
            }

            return _field == null || _field.IsObstacleTargetable(obstacle.RuntimeId);
        }

        private bool TryFindObstacle(string runtimeId, out CocDestructibleObstacle obstacle)
        {
            obstacle = null;
            if (string.IsNullOrEmpty(runtimeId))
            {
                return false;
            }

            for (var i = 0; i < _obstacles.Count; i++)
            {
                var candidate = _obstacles[i];
                if (candidate != null &&
                    string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                {
                    obstacle = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindCocMonster(string runtimeId, out PushMapMonsterAgentView monster)
        {
            monster = null;
            if (string.IsNullOrEmpty(runtimeId))
            {
                return false;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var m = _monsters[i];
                if (m != null &&
                    string.Equals(m.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    monster = m;
                    return true;
                }
            }

            return false;
        }

        private void RefreshSoldierGoal(PushMapAdvanceView soldier)
        {
            if (soldier == null || !soldier.IsCombatActive || soldier.MoveId == 0)
            {
                if (soldier != null && soldier.MoveId != 0)
                {
                    _soldierNavPath.Release(soldier.MoveId);
                }

                return;
            }

            if (!TryGetCocEngageTarget(soldier, out var monster, out var obstacle) ||
                (monster == null && obstacle == null))
            {
                _attackSlots.Release(soldier.AttackerId);
                if (!TryGetLivingFinalBoss(out var boss))
                {
                    _soldierNavPath.Release(soldier.MoveId);
                    _scheduler.SetPaused(soldier.MoveId, true);
                    return;
                }

                // No observe-range target → march to the unique Final Boss along this
                // soldier's NavMesh when an air wall blocks the straight line.
                var marchDest = new Vector2(boss.transform.position.x, boss.transform.position.z);
                var steer = SteerAroundWalls(soldier, boss.RuntimeTargetId, marchDest);
                _scheduler.SetPaused(soldier.MoveId, false);
                _scheduler.SetGoal(soldier.MoveId, GoalKind.ChaseAnchor, steer);
                return;
            }

            if (obstacle != null)
            {
                RefreshSoldierAttackGoal(
                    soldier,
                    obstacle.RuntimeId,
                    obstacle.transform.position,
                    obstacle.BodyRadius);
                return;
            }

            RefreshSoldierAttackGoal(
                soldier,
                monster.RuntimeTargetId,
                monster.transform.position,
                monster.BodyRadius);
        }

        private void RefreshSoldierAttackGoal(
            PushMapAdvanceView soldier,
            string targetId,
            Vector3 targetPos,
            float targetBody)
        {
            var distXZ = CombatReach.DistanceXZ(soldier.transform.position, targetPos);
            var inRange = CombatReach.IsInAttackRange(
                distXZ,
                soldier.AttackRange,
                soldier.AgentRadius,
                targetBody);
            var claimed = _attackSlots.TryClaim(
                soldier.AttackerId,
                targetId,
                soldier.AttackRange,
                targetPos,
                out var slotPos,
                soldier.AttackMode,
                soldier.transform.position,
                targetBody,
                soldier.AgentRadius,
                CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, soldier.AttackMode));

            if (inRange)
            {
                _soldierNavPath.Release(soldier.MoveId);
                if (!claimed)
                {
                    _attackSlots.Release(soldier.AttackerId);
                }

                var hold = claimed
                    ? CombatReach.ChaseDestinationXZ(
                        soldier.transform.position,
                        targetPos,
                        slotPos,
                        soldier.AttackRange,
                        soldier.AgentRadius,
                        targetBody,
                        MassMoveScheduler.ArriveEpsilon)
                    : new Vector2(soldier.transform.position.x, soldier.transform.position.z);
                _scheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, hold);
                _scheduler.SetPaused(soldier.MoveId, true);
                return;
            }

            Vector3 chasePoint;
            if (claimed)
            {
                chasePoint = slotPos;
            }
            else
            {
                var away = soldier.transform.position - targetPos;
                away.y = 0f;
                if (away.sqrMagnitude < 1e-6f)
                {
                    away = Vector3.forward;
                }

                var ring = AttackSlotService.ComputeRingRadius(
                    soldier.AttackRange,
                    soldier.AgentRadius,
                    targetBody);
                chasePoint = targetPos + away.normalized * ring;
            }

            var dest = CombatReach.ChaseDestinationXZ(
                soldier.transform.position,
                targetPos,
                chasePoint,
                soldier.AttackRange,
                soldier.AgentRadius,
                targetBody,
                MassMoveScheduler.ArriveEpsilon);
            var steer = SteerAroundWalls(soldier, targetId, dest);
            _scheduler.SetPaused(soldier.MoveId, false);
            _scheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, steer);
        }

        private Vector2 SteerAroundWalls(PushMapAdvanceView soldier, string targetId, Vector2 destination)
        {
            var agent = soldier.GetComponent<NavMeshAgent>();
            var agentTypeId = agent != null
                ? agent.agentTypeID
                : SpecialMoveNavMesh.DefaultAgentTypeId;
            return _soldierNavPath.ResolveSteer(
                soldier.MoveId,
                targetId,
                soldier.transform.position,
                destination,
                agentTypeId,
                Time.deltaTime);
        }

        private void RefreshMonster(int livingFriendlies, PushMapMonsterAgentView monster)
        {
            if (monster == null)
            {
                return;
            }

            if (monster.IsCocReturningHome && monster.IsAlive)
            {
                monster.SetCombatGameplayEnabled(true);
                monster.ClearCocIntercept();
                monster.SetCocBodyTarget(null);
                monster.DriveCocReturnHome(_attackSlots, _scheduler);
                if (_field != null)
                {
                    _field.SetReturnHomeInvincible(monster.RuntimeTargetId, monster.IsCocReturningHome);
                }

                return;
            }

            if (_field != null)
            {
                _field.SetReturnHomeInvincible(monster.RuntimeTargetId, false);
            }

            if (livingFriendlies <= 0 || !monster.IsAlive)
            {
                monster.ClearCocIntercept();
                monster.SetCombatGameplayEnabled(false);
                if (monster.MoveId != 0)
                {
                    _scheduler.SetPaused(monster.MoveId, true);
                }

                HoldAgent(monster);
                return;
            }

            monster.SetCombatGameplayEnabled(true);
            if (monster.IsPassive)
            {
                TryProvoke(monster);
            }

            AssignCocBodyTarget(monster);
            RefreshTreatAsTarget(monster);
            monster.TryRefreshChaseGoal(_attackSlots, _scheduler);
        }

        /// <summary>
        /// ActiveChase, once a soldier has entered AlertRadius or landed a hit, chases the
        /// nearest living soldier measured from the monster body. A new deploy is not required.
        /// </summary>
        private void AssignCocBodyTarget(PushMapMonsterAgentView monster)
        {
            if (monster == null || !monster.IsAlive || monster.IsStationary || !monster.IsActiveChase)
            {
                monster?.SetCocBodyTarget(null);
                return;
            }

            if (!monster.CocChaseUnlocked && SoldierInsideAlert(monster))
            {
                monster.UnlockCocChase();
            }

            monster.SetCocBodyTarget(monster.CocChaseUnlocked
                ? NearestLivingSoldier(monster.transform.position)
                : null);
        }

        private bool SoldierInsideAlert(PushMapMonsterAgentView monster)
        {
            var alert = Mathf.Max(0.01f, monster.AlertRadius);
            var origin = monster.transform.position;
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (!IsLivingSoldier(soldier))
                {
                    continue;
                }

                if (CombatReach.DistanceXZ(origin, soldier.transform.position) <= alert)
                {
                    return true;
                }
            }

            return false;
        }

        private PushMapAdvanceView NearestLivingSoldier(Vector3 origin)
        {
            PushMapAdvanceView best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (!IsLivingSoldier(soldier))
                {
                    continue;
                }

                var dist = CombatReach.DistanceXZ(origin, soldier.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = soldier;
                }
            }

            return best;
        }

        private static bool IsLivingSoldier(PushMapAdvanceView soldier)
        {
            return soldier != null && soldier.IsCombatActive && !soldier.IsRebel;
        }

        private void RefreshTreatAsTarget(PushMapMonsterAgentView monster)
        {
            if (monster.ObstaclePathMode != CocObstaclePathMode.TreatAsTarget)
            {
                monster.ClearCocIntercept();
                return;
            }

            if (!monster.TryGetCocChaseAim(out var aim))
            {
                monster.ClearCocIntercept();
                return;
            }

            CocDestructibleObstacle best = null;
            var bestDist = float.MaxValue;
            var bestId = string.Empty;
            var from = monster.transform.position;
            var segment = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(aim.x, aim.z));
            for (var i = 0; i < _obstacles.Count; i++)
            {
                var wall = _obstacles[i];
                if (!IsCocObstacleCandidate(wall) || !wall.SegmentCrossesXZ(from, aim, out var tEnter))
                {
                    continue;
                }

                var dist = tEnter * segment;
                var id = wall.RuntimeId ?? string.Empty;
                if (best != null &&
                    (dist > bestDist + 0.0001f ||
                     (Mathf.Abs(dist - bestDist) <= 0.0001f &&
                      string.CompareOrdinal(id, bestId) >= 0)))
                {
                    continue;
                }

                best = wall;
                bestDist = dist;
                bestId = id;
            }

            if (best == null)
            {
                monster.ClearCocIntercept();
                return;
            }

            if (string.Equals(monster.CocInterceptTargetId, best.RuntimeId, StringComparison.Ordinal))
            {
                return;
            }

            var monsterId = monster.RuntimeTargetId;
            var wallId = best.RuntimeId;
            monster.SetCocIntercept(
                wallId,
                best.transform,
                best.BodyRadius,
                () => _field != null && _field.TryApplyMonsterObstacleHit(monsterId, wallId));
        }

        private void TryProvoke(PushMapMonsterAgentView monster)
        {
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier == null || !soldier.IsCombatActive)
                {
                    continue;
                }

                var dist = CombatReach.DistanceXZ(monster.transform.position, soldier.transform.position);
                if (CombatReach.IsInAttackRange(dist, monster.AttackRange, monster.BodyRadius, soldier.AgentRadius))
                {
                    monster.NotifyProvoked();
                    return;
                }
            }
        }

        private void TickMove()
        {
            _moveSamples.Clear();
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier != null)
                {
                    _moveSamples.Add(soldier.BuildSample());
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

            _scheduler.Tick(_moveSamples, Time.deltaTime);
        }

        private int CountLivingFriendlies()
        {
            var count = 0;
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier != null && soldier.IsCombatActive)
                {
                    count++;
                }
            }

            return count;
        }

        private void RebuildCards()
        {
            if (_cards == null)
            {
                return;
            }

            _cardModels.Clear();
            if (_pool != null)
            {
                var warriors = _pool.Warriors;
                for (var i = 0; i < warriors.Count; i++)
                {
                    var warrior = warriors[i];
                    if (warrior == null || string.IsNullOrEmpty(warrior.ClassId))
                    {
                        continue;
                    }

                    CocClassCardModel group = null;
                    for (var g = 0; g < _cardModels.Count; g++)
                    {
                        if (string.Equals(_cardModels[g].ClassId, warrior.ClassId, StringComparison.Ordinal))
                        {
                            group = _cardModels[g];
                            break;
                        }
                    }

                    GameObject memberPrefab = null;
                    if (_catalog != null)
                    {
                        _catalog.TryGetWarriorAppearance(warrior.AppearanceId, out memberPrefab);
                    }

                    if (group == null)
                    {
                        var className = warrior.ClassId;
                        if (_configs != null &&
                            _configs.TryGetClass(warrior.ClassId, out var classRow) &&
                            classRow != null &&
                            !string.IsNullOrEmpty(classRow.ClassName))
                        {
                            className = classRow.ClassName;
                        }

                        group = new CocClassCardModel
                        {
                            ClassId = warrior.ClassId,
                            ClassName = className,
                            Count = 0,
                            AppearancePrefab = memberPrefab
                        };
                        _cardModels.Add(group);
                    }

                    if (group.AppearancePrefab == null)
                    {
                        group.AppearancePrefab = memberPrefab;
                    }

                    var label = string.IsNullOrEmpty(warrior.WarriorName) ? "未命名" : warrior.WarriorName;
                    group.Members.Add(new CocClassCardMember
                    {
                        Label = label,
                        AppearancePrefab = memberPrefab
                    });
                    group.Count++;
                }
            }

            if (!string.IsNullOrEmpty(_selectedClassId))
            {
                var stillThere = false;
                for (var i = 0; i < _cardModels.Count; i++)
                {
                    if (string.Equals(_cardModels[i].ClassId, _selectedClassId, StringComparison.Ordinal) &&
                        _cardModels[i].Count > 0)
                    {
                        stillThere = true;
                        break;
                    }
                }

                if (!stillThere)
                {
                    _selectedClassId = null;
                }
            }

            _cards.Rebuild(_cardModels, _selectedClassId);
        }

        private void HandleClassSelected(string classId)
        {
            _selectedClassId = classId;
            RebuildCards();
        }

        private void HandlePoolChanged()
        {
            if (!_running || _settled)
            {
                return;
            }

            RebuildCards();
            TryEvaluateWipeLoss();
        }

        private void HandleMonsterSpawned(PushMapMonsterAgentView view, MonsterConfigRow row)
        {
            if (_field == null || view == null || row == null)
            {
                return;
            }

            _field.TryRegisterMonster(view.RuntimeTargetId, row.MonsterId, row.MaxHP);
            view.SetCocDriveStraight(CocMonsterObstaclePath.ShouldDriveStraight(row.ObstaclePathMode));
            view.SetCocActiveChaseLatch(true);
        }

        private void HandleMonsterDamageSettled(string runtimeId, float damage)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null || !string.Equals(monster.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    continue;
                }

                monster.NotifyProvoked();
                monster.NotifyCocSoldierHit();
                monster.UnlockCocChase();

                var flash = monster.GetComponent<HitFlashView>();
                if (flash == null)
                {
                    flash = monster.gameObject.AddComponent<HitFlashView>();
                }

                flash.Play(HitFlashView.MonsterFlashColor);
                SpawnDamagePopup(monster.transform.position, damage, DamagePopupStyle.Monster);
                return;
            }
        }

        private void SpawnDamagePopup(Vector3 worldPos, float damage, DamagePopupStyle style)
        {
            var prefab = _catalog != null ? _catalog.DamagePopupPrefab : null;
            if (prefab == null)
            {
                Debug.LogWarning("[CocCombatStage] DamagePopup prefab missing in DefendPrefabCatalog — popup skipped.");
                return;
            }

            var parent = _mapInstance != null ? _mapInstance.transform : transform;
            DamagePopupView.Spawn(prefab, parent, worldPos, damage, style);
        }

        private void HandleMonsterBerserkTriggered(string runtimeId, AggroMode mode, bool provoke)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null || !string.Equals(monster.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    continue;
                }

                monster.SetRuntimeAggroMode(mode);
                if (provoke)
                {
                    monster.NotifyProvoked();
                }

                return;
            }
        }

        private Vector2? TryGetMonsterWorldXZ(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null || !string.Equals(monster.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    continue;
                }

                var p = monster.transform.position;
                return new Vector2(p.x, p.z);
            }

            return null;
        }

        private void ResolveFinalBoss()
        {
            _finalBoss = null;
            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null || !monster.IsBoss)
                {
                    continue;
                }

                if (_finalBoss != null)
                {
                    Debug.LogWarning(
                        "[CocCombatStage] Multiple FinalBoss markers found; keeping the first.");
                    continue;
                }

                _finalBoss = monster;
            }

            if (_finalBoss == null)
            {
                Debug.LogWarning("[CocCombatStage] No FinalBoss spawned; soldiers will idle with no enemy.");
            }
        }

        private bool TryGetLivingFinalBoss(out PushMapMonsterAgentView boss)
        {
            boss = _finalBoss;
            return boss != null && boss.IsAlive;
        }

        private bool HandleMonsterHitWarrior(string monsterRuntimeId, string warriorId, float attackPower)
        {
            return _field != null && _field.TryApplyMonsterHit(monsterRuntimeId, warriorId, attackPower);
        }

        private void HandleMonsterDied(string runtimeId, string killerWarriorId, float damage)
        {
            if (_settled)
            {
                return;
            }

            PushMapMonsterAgentView monster = null;
            for (var i = 0; i < _monsters.Count; i++)
            {
                var candidate = _monsters[i];
                if (candidate != null && string.Equals(candidate.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    monster = candidate;
                    break;
                }
            }

            if (monster == null)
            {
                return;
            }

            Vector3? killerPos = null;
            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier != null && string.Equals(soldier.AttackerId, killerWarriorId, StringComparison.Ordinal))
                {
                    killerPos = soldier.transform.position;
                    break;
                }
            }

            var wasFinalBoss = _finalBoss != null && ReferenceEquals(monster, _finalBoss);
            monster.NotifyKilled(killerPos, 0f, killerWarriorId, damage);
            if (wasFinalBoss)
            {
                SettleRound(CocRoundResult.Victory);
            }
        }

        private void HandleObstacleDamaged(string runtimeId, float remaining, float maxHp)
        {
            if (!TryFindObstacle(runtimeId, out var obstacle) || obstacle == null)
            {
                return;
            }

            obstacle.NotifyDamaged(remaining, maxHp);
        }

        private void HandleObstacleDestroyed(string runtimeId, string killerWarriorId)
        {
            if (_settled || !TryFindObstacle(runtimeId, out var obstacle) || obstacle == null)
            {
                return;
            }

            obstacle.NotifyDestroyed();
            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null &&
                    string.Equals(monster.CocInterceptTargetId, runtimeId, StringComparison.Ordinal))
                {
                    monster.ClearCocIntercept();
                    _attackSlots.Release(monster.RuntimeTargetId);
                }
            }

            for (var i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier != null &&
                    string.Equals(soldier.CocLockedTargetId, runtimeId, StringComparison.Ordinal))
                {
                    soldier.SetCocLockedTargetId(null);
                    _attackSlots.Release(soldier.AttackerId);
                }
            }
        }

        private void TryEvaluateWipeLoss()
        {
            if (_settled || !_running)
            {
                return;
            }

            if (CountLivingFriendlies() > 0 || !IsPoolEmpty())
            {
                return;
            }

            SettleRound(CocRoundResult.RoundLoss);
        }

        private bool IsPoolEmpty()
        {
            return _pool == null || _pool.Warriors == null || _pool.Warriors.Count == 0;
        }

        private void SettleRound(CocRoundResult result)
        {
            if (_settled)
            {
                return;
            }

            _settled = true;
            _running = false;
            if (_field != null)
            {
                _field.IsCombatGameplayActive = false;
            }

            Debug.Log($"[CocCombatStage] Round settled: {result}");
            // Defer so Exit/Destroy does not run inside MonsterDied / UI click stacks.
            StartCoroutine(CoEmitRoundEnded(result));
        }

        private IEnumerator CoEmitRoundEnded(CocRoundResult result)
        {
            var callback = _onRoundEnded;
            yield return null;
            callback?.Invoke(result);
        }

        private IReadOnlyList<PushMapAdvanceView> ProvideSoldiers()
        {
            return _soldiers;
        }

        private IReadOnlyList<PushMapMonsterAgentView> ProvideMonsters()
        {
            return _monsters;
        }

        private int AllocateMoveId()
        {
            return _nextMoveId++;
        }

        private static void HoldAgent(PushMapMonsterAgentView view)
        {
            if (view == null)
            {
                return;
            }

            var agent = view.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        private static bool IsPointerOverUi()
        {
            var system = EventSystem.current;
            return system != null && system.IsPointerOverGameObject();
        }

        private void EnsureCamera(ConfigCsvRepository configs)
        {
            var camGo = new GameObject("CocCombatCamera");
            camGo.transform.SetParent(transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            _camera.depth = 5f;

            var presentation = configs != null
                ? configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            _camera.nearClipPlane = presentation.NearClip;
            _camera.farClipPlane = presentation.FarClip;
            _camera.transparencySortMode = TransparencySortMode.CustomAxis;
            _camera.transparencySortAxis = Vector3.forward;

            var lookAt = _mapInstance != null ? _mapInstance.transform.position : Vector3.zero;
            var path = _mapInstance != null
                ? _mapInstance.GetComponentInChildren<PushMapCameraPath>(true)
                : null;
            string bakeError = null;
            var baked = path != null && path.TryBake(out bakeError);
            if (baked && path.TryEvaluate(0f, out var start))
            {
                lookAt = start;
            }
            else if (!string.IsNullOrEmpty(bakeError))
            {
                Debug.LogWarning($"[CocCombatStage] CameraFollowPath bake failed: {bakeError}");
            }

            presentation.ApplyCombatCameraPose(_camera, lookAt, presentation.PushMapOrthoSize);
            _follow = camGo.AddComponent<PushMapCameraFollowController>();
            _follow.ApplyPresentationConstants(presentation);
            _follow.Bind(_camera, _soldiers, () => null, path);
            _follow.SetPanMouseButton(1);
            _follow.EnableForCombat();

            var main = Camera.main;
            if (main != null && main != _camera)
            {
                _suppressedMain = main;
                _suppressedMain.enabled = false;
            }
        }

        private void EnsureHud()
        {
            _hudRoot = new GameObject("CocReturnHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _hudRoot.transform.SetParent(transform, false);
            var canvas = _hudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;
            var scaler = _hudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (EventSystem.current == null)
            {
                var eventGo = new GameObject("CocEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventGo.transform.SetParent(_hudRoot.transform, false);
            }

            _cards = _hudRoot.AddComponent<CocClassCardBar>();
            _cards.Ensure(_hudRoot.transform);
            _cards.ClassSelected += HandleClassSelected;

            var buttonGo = new GameObject("ReturnButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(_hudRoot.transform, false);
            var rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(220f, 56f);
            rt.anchoredPosition = new Vector2(0f, 36f);
            buttonGo.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.28f, 0.92f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "退出";
            label.raycastTarget = false;

            _returnButton = buttonGo.GetComponent<Button>();
            _returnButton.onClick.AddListener(HandleReturnClicked);
            EnsureDeployToast();
        }

        private void EnsureDeployToast()
        {
            var toastGo = new GameObject(
                "DeployBlockToast",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            toastGo.transform.SetParent(_hudRoot.transform, false);
            var rt = toastGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 64f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            var bg = toastGo.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.05f, 0.05f, 0.9f);
            bg.raycastTarget = false;

            var textGo = new GameObject(
                "Message",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textGo.transform.SetParent(toastGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.86f, 0.86f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            _deployToast = toastGo.AddComponent<ToastView>();
            _deployToast.RuntimeConfigure(toastGo, text, 1.6f);
        }

        private void HandleReturnClicked()
        {
            SettleRound(CocRoundResult.RoundLoss);
        }

        private struct CaptureSite
        {
            public string Id;
            public float X;
            public float Z;
            public float Radius;
            public bool Activated;
        }
    }
}
