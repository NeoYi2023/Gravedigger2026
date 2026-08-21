using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig stage presentation bridge (Approach A). Rules live in DigSessionService.
    /// </summary>
    public sealed class DigStageController : MonoBehaviour
    {
        public const string IronShovelEquipId = "Equip_IronShovel";
        public const string MinerLampEquipId = "Equip_MinerLamp";
        public const string ExplosivesEquipId = "Equip_Explosives";
        public const string LightningEquipId = "Equip_Elctr";
        public const string DetectorEquipId = "Equip_Detector";
        public const string HumanTokenEquipId = "Equip_HumanToken";
        public const string ElfTokenEquipId = "Equip_ElfToken";
        public const string OrcTokenEquipId = "Equip_OrcToken";
        private const int GmEquipCommonExpAmount = 50;
        private const int GmSpendIronShovelExpAmount = 1;
        private const int GmSpendMinerLampExpAmount = 1;
        private const int GmSpendExplosivesExpAmount = 1;
        private const int GmSpendLightningExpAmount = 1;
        private const int GmSpendDetectorExpAmount = 1;
        private const int GmSpendHumanTokenExpAmount = 1;
        private const int GmSpendElfTokenExpAmount = 1;
        private const int GmSpendOrcTokenExpAmount = 1;

        [SerializeField] private DigPrefabCatalog _catalog;
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Camera _digCamera;
        [SerializeField] private DigCursorView _cursorView;
        [SerializeField] private DigHudView _hudView;
        [SerializeField] private DigStageSummaryView _summaryView;

        private DigSessionService _session;
        private ConfigCsvRepository _configs;
        private SpecialEquipSlotsService _specialEquipSlots;
        private ProtagonistEquipmentService _protagonistEquipment;
        private DefendPrefabCatalog _defendCatalog;
        private Action _onSummaryConfirmed;
        private readonly Dictionary<int, DigGraveView> _graveViews = new Dictionary<int, DigGraveView>();
        private readonly Dictionary<int, DigExplosiveBarrelView> _barrelViews =
            new Dictionary<int, DigExplosiveBarrelView>();
        private Transform _effectsParent;
        private GameObject _mapInstance;
        private Transform _gravesParent;
        private float _mapPlaneY;
        private bool _running;

        public void ConfigureCatalog(DigPrefabCatalog catalog)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }
        }

        public void Begin(
            LevelStageContext context,
            ConfigCsvRepository configs,
            WarehouseService warehouse,
            DigProtagonistCapabilities caps,
            Action onSummaryConfirmed,
            SpecialEquipSlotsService specialEquipSlots = null,
            ProtagonistEquipmentService protagonistEquipment = null,
            GmSoldierGrantService soldierGrant = null,
            DefendPrefabCatalog defendCatalog = null)
        {
            EndInternal(destroyWorld: true);

            if (context?.DigConfig == null)
            {
                Debug.LogError("[DigStageController] Missing DigConfig.");
                return;
            }

            if (_catalog == null)
            {
                Debug.LogError("[DigStageController] DigPrefabCatalog missing.");
                return;
            }

            _onSummaryConfirmed = onSummaryConfirmed;
            _configs = configs;
            _specialEquipSlots = specialEquipSlots;
            _protagonistEquipment = protagonistEquipment;
            _defendCatalog = defendCatalog;

            if (!_catalog.TryGetMap(context.DigConfig.DigMapId, out var mapPrefab))
            {
                Debug.LogError($"[DigStageController] Map prefab missing for '{context.DigConfig.DigMapId}'.");
                return;
            }

            EnsureWorldRoot();
            _mapInstance = Instantiate(mapPrefab, _worldRoot);
            _mapInstance.name = context.DigConfig.DigMapId;

            var bounds = _mapInstance.GetComponent<DigMapBounds>();
            var half = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            var center = bounds != null ? bounds.Center : _mapInstance.transform.position;

            _mapPlaneY = center.y;
            _gravesParent = new GameObject("Graves").transform;
            _gravesParent.SetParent(_worldRoot, false);
            _effectsParent = new GameObject("DigEffects").transform;
            _effectsParent.SetParent(_worldRoot, false);

            var ledger = new DigStageRewardLedger();
            _session = new DigSessionService(configs, warehouse, ledger, caps, protagonistEquipment, soldierGrant);
            SubscribeSession(_session);

            if (_digCamera != null)
            {
                _digCamera.gameObject.SetActive(true);
                var cam = _configs != null
                    ? _configs.GetCameraPresentationConstants()
                    : CameraPresentationConstants.SafetyDefaults;
                cam.ApplyTopDownPose(_digCamera, center, cam.ResolveMapFitOrthoSize(half));
                // Top-down: sort transparent Tilemap vs Sprite by view depth (higher Y draws in front).
                _digCamera.transparencySortMode = TransparencySortMode.CustomAxis;
                _digCamera.transparencySortAxis = Vector3.up;
            }

            EnsureDigLight();

            if (_cursorView != null)
            {
                _cursorView.gameObject.SetActive(true);
                _cursorView.SetUiRingPrefab(_catalog.UiDigCursorRingPrefab);
                ApplyCursorRadius(caps.DigCursorRadius);
            }

            _hudView?.Show();
            _summaryView?.Hide();
            var mode2 = _configs != null && _configs.LoadedCampaignMode == CampaignMode.Mode2;
            _hudView?.SetWarriorEnhanceGmVisible(mode2 && _specialEquipSlots != null);

            if (_hudView != null)
            {
                _hudView.AddGravesRequested += HandleGmAddGraves;
                _hudView.AddBodyPartsRequested += HandleGmAddBodyParts;
                _hudView.EquipWarriorEnhanceRequested += HandleGmEquipWarriorEnhance;
                _hudView.AcquireDigRingRequested += HandleGmAcquireDigRing;
                _hudView.GrantEquipCommonExpRequested += HandleGmGrantEquipCommonExp;
                _hudView.SpendDigRingCommonExpRequested += HandleGmSpendDigRingCommonExp;
                _hudView.AcquireMinerLampRequested += HandleGmAcquireMinerLamp;
                _hudView.SpendMinerLampCommonExpRequested += HandleGmSpendMinerLampCommonExp;
                _hudView.AcquireExplosivesRequested += HandleGmAcquireExplosives;
                _hudView.SpendExplosivesCommonExpRequested += HandleGmSpendExplosivesCommonExp;
                _hudView.AcquireLightningRequested += HandleGmAcquireLightning;
                _hudView.SpendLightningCommonExpRequested += HandleGmSpendLightningCommonExp;
                _hudView.AcquireDetectorRequested += HandleGmAcquireDetector;
                _hudView.SpendDetectorCommonExpRequested += HandleGmSpendDetectorCommonExp;
                _hudView.AcquireHumanTokenRequested += HandleGmAcquireHumanToken;
                _hudView.SpendHumanTokenCommonExpRequested += HandleGmSpendHumanTokenCommonExp;
                _hudView.AcquireElfTokenRequested += HandleGmAcquireElfToken;
                _hudView.SpendElfTokenCommonExpRequested += HandleGmSpendElfTokenCommonExp;
                _hudView.AcquireOrcTokenRequested += HandleGmAcquireOrcToken;
                _hudView.SpendOrcTokenCommonExpRequested += HandleGmSpendOrcTokenCommonExp;
            }

            _session.Begin(context.DigConfig, center, half);
            RefreshWarehouseHud();
            _running = true;

            context.MapResolveNote =
                $"DigMapId={context.DigConfig.DigMapId} → Instantiated {MapPrefabPaths.PrefabFolder}/{context.DigConfig.DigMapId}.prefab";
        }

        public void End()
        {
            EndInternal(destroyWorld: true);
        }

        /// <summary>
        /// Mid-session caps refresh after tech/gear recalc (PE-03). Session holds the same caps instance when mutated in place.
        /// </summary>
        public void RefreshCapabilities(DigProtagonistCapabilities caps)
        {
            if (!_running || caps == null)
            {
                return;
            }

            ApplyCursorRadius(caps.DigCursorRadius);
            Debug.Log(
                $"[DigStageController] Caps refreshed DigDamage={caps.DigDamage} Cursor={caps.DigCursorRadius:0.###} StageBonus={caps.DigStageDurationBonus}");
        }

        private void ApplyCursorRadius(float radius)
        {
            if (_cursorView == null)
            {
                return;
            }

            Canvas hudCanvas = null;
            if (_hudView != null)
            {
                hudCanvas = _hudView.GetComponentInParent<Canvas>();
            }

            _cursorView.Configure(_digCamera, radius, _mapPlaneY, hudCanvas);
            Debug.Log(
                $"[DigStageController] Cursor radius={radius:0.###} camera ortho={(_digCamera != null ? _digCamera.orthographicSize : -1f):0.##} canvasScale={(hudCanvas != null ? hudCanvas.scaleFactor : -1f):0.###}");
        }

        private void Update()
        {
            if (!_running || _session == null || !_session.IsActive)
            {
                return;
            }

            if (_cursorView != null)
            {
                _cursorView.SampleFromScreen(Input.mousePosition);
                _session.SetCursorWorld(_cursorView.WorldPosition, _cursorView.IsValid);
            }

            _session.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            EndInternal(destroyWorld: false);
        }

        private void SubscribeSession(DigSessionService session)
        {
            session.RemainingTimeChanged += HandleTime;
            session.GraveSpawned += HandleGraveSpawned;
            session.GraveUpdated += HandleGraveUpdated;
            session.DigActionStarted += HandleDigStarted;
            session.DigActionEnded += HandleDigEnded;
            session.GraveClearedForReward += HandleGraveCleared;
            session.ExplosiveBarrelQueued += HandleExplosiveBarrelQueued;
            session.ExplosiveBlastStarted += HandleExplosiveBlastStarted;
            session.GraveRemovedWithoutLoot += HandleGraveRemovedWithoutLoot;
            session.LightningStrikeQueued += HandleLightningStrikeQueued;
            session.LightningSoldierPreview += HandleLightningSoldierPreview;
            session.StageTimeUp += HandleTimeUp;
            session.WarehouseChanged += RefreshWarehouseHud;
        }

        private void UnsubscribeSession(DigSessionService session)
        {
            if (session == null)
            {
                return;
            }

            session.RemainingTimeChanged -= HandleTime;
            session.GraveSpawned -= HandleGraveSpawned;
            session.GraveUpdated -= HandleGraveUpdated;
            session.DigActionStarted -= HandleDigStarted;
            session.DigActionEnded -= HandleDigEnded;
            session.GraveClearedForReward -= HandleGraveCleared;
            session.ExplosiveBarrelQueued -= HandleExplosiveBarrelQueued;
            session.ExplosiveBlastStarted -= HandleExplosiveBlastStarted;
            session.GraveRemovedWithoutLoot -= HandleGraveRemovedWithoutLoot;
            session.LightningStrikeQueued -= HandleLightningStrikeQueued;
            session.LightningSoldierPreview -= HandleLightningSoldierPreview;
            session.StageTimeUp -= HandleTimeUp;
            session.WarehouseChanged -= RefreshWarehouseHud;
        }

        private void HandleTime(float remaining, float total)
        {
            _hudView?.SetTimer(remaining, total);
        }

        private void HandleGraveSpawned(DigGraveRuntime grave)
        {
            if (_catalog == null || !_catalog.TryGetGrave(grave.QualityId, out var prefab) || prefab == null)
            {
                Debug.LogWarning(
                    $"[DigStageController] No Grave prefab for QualityId='{grave.QualityId}'. Check DigPrefabCatalog grave refs.");
                return;
            }

            var go = Instantiate(prefab, _gravesParent);
            go.transform.position = grave.WorldPosition;
            var radiusComp = go.GetComponent<DigObstacleRadius>();
            if (radiusComp != null)
            {
                grave.ObstacleRadius = radiusComp.Radius;
            }

            var hitShape = go.GetComponent<DigHitShape>();
            if (hitShape != null && hitShape.HasValidPolygon)
            {
                var src = hitShape.LocalXZ;
                var copy = new Vector2[src.Length];
                Array.Copy(src, copy, src.Length);
                grave.HitLocalXZ = copy;
                grave.HitBoundingRadius = hitShape.BoundingRadius;
            }
            else
            {
                grave.HitLocalXZ = null;
                grave.HitBoundingRadius = Mathf.Max(0.05f, grave.ObstacleRadius);
            }

            var view = go.GetComponent<DigGraveView>();
            if (view == null)
            {
                view = go.AddComponent<DigGraveView>();
            }

            view.Bind(grave.InstanceId, grave.QualityId);
            view.SetIconTier(grave.IconStyleTier);
            _graveViews[grave.InstanceId] = view;
        }

        private void HandleGraveUpdated(DigGraveRuntime grave)
        {
            if (_graveViews.TryGetValue(grave.InstanceId, out var view))
            {
                view.SetIconTier(grave.IconStyleTier);
            }
        }

        private void HandleDigStarted(DigGraveRuntime grave)
        {
            if (_graveViews.TryGetValue(grave.InstanceId, out var view))
            {
                view.SetBusy(true);
            }
        }

        private void HandleDigEnded(DigGraveRuntime grave)
        {
            if (_graveViews.TryGetValue(grave.InstanceId, out var view))
            {
                view.SetBusy(false);
            }
        }

        private void HandleGraveCleared(DigGraveRuntime grave, string lootEncoded)
        {
            if (_graveViews.TryGetValue(grave.InstanceId, out var view))
            {
                _graveViews.Remove(grave.InstanceId);
                Destroy(view.gameObject);
            }

            SpawnRewardFlyer(grave.WorldPosition, lootEncoded);
        }

        private void HandleGraveRemovedWithoutLoot(DigGraveRuntime grave)
        {
            if (grave == null)
            {
                return;
            }

            if (_graveViews.TryGetValue(grave.InstanceId, out var view))
            {
                _graveViews.Remove(grave.InstanceId);
                Destroy(view.gameObject);
            }
        }

        private void HandleLightningStrikeQueued(Vector3 worldPosition, float frameSeconds)
        {
            var go = new GameObject("LightningBolt");
            if (_effectsParent != null)
            {
                go.transform.SetParent(_effectsParent, false);
            }

            var view = go.AddComponent<DigLightningBoltView>();
            view.Play(worldPosition, _catalog != null ? _catalog.LightningFrames : null, frameSeconds);
        }

        private void HandleLightningSoldierPreview(string appearanceId, Vector3 worldPosition, float previewSeconds)
        {
            GameObject prefab = null;
            if (_defendCatalog != null)
            {
                _defendCatalog.TryGetWarriorAppearance(appearanceId, out prefab);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[DigStageController] Lightning soldier preview missing appearance '{appearanceId}'.");
                return;
            }

            var go = new GameObject("LightningSoldierPreview");
            if (_effectsParent != null)
            {
                go.transform.SetParent(_effectsParent, false);
            }

            var preview = go.AddComponent<DigSoldierIdlePreviewView>();
            preview.Play(prefab, worldPosition, previewSeconds);
        }

        private void HandleExplosiveBarrelQueued(int barrelId, Vector3 origin, Vector3 target, float flightSeconds)
        {
            if (_effectsParent == null)
            {
                return;
            }

            var go = new GameObject("ExplosiveBarrel_" + barrelId);
            go.transform.SetParent(_effectsParent, false);
            var view = go.AddComponent<DigExplosiveBarrelView>();
            view.Launch(origin, target, flightSeconds, _catalog != null ? _catalog.ExplosiveBarrelSprite : null);
            _barrelViews[barrelId] = view;
        }

        private void HandleExplosiveBlastStarted(int barrelId, Vector3 center, float blastRadius, float ringSeconds)
        {
            if (_barrelViews.TryGetValue(barrelId, out var barrel) && barrel != null)
            {
                _barrelViews.Remove(barrelId);
                Destroy(barrel.gameObject);
            }

            if (_effectsParent == null)
            {
                return;
            }

            var ringGo = new GameObject("ExplosionRadius_" + barrelId);
            ringGo.transform.SetParent(_effectsParent, false);
            var ring = ringGo.AddComponent<DigExplosionRadiusView>();
            ring.Play(center, blastRadius, ringSeconds);
        }

        private void SpawnRewardFlyer(Vector3 from, string lootEncoded)
        {
            var target = ResolvePortraitWorldTarget(from);
            var flyerPrefab = _catalog != null ? _catalog.RewardFlyerPrefab : null;
            if (flyerPrefab == null)
            {
                _session?.CreditPendingLoot(lootEncoded);
                return;
            }

            var go = Instantiate(flyerPrefab, _worldRoot);
            var flyer = go.GetComponent<DigRewardFlyerView>();
            if (flyer == null)
            {
                flyer = go.AddComponent<DigRewardFlyerView>();
            }

            var label = string.IsNullOrEmpty(lootEncoded) ? "★" : lootEncoded.Split('|')[0];
            flyer.Play(from + Vector3.up * 0.5f, target, label, () =>
            {
                _session?.CreditPendingLoot(lootEncoded);
            });
        }

        private Vector3 ResolvePortraitWorldTarget(Vector3 fallback)
        {
            if (_hudView == null || _digCamera == null)
            {
                return fallback + Vector3.up * 0.5f;
            }

            Canvas canvas = _hudView.GetComponentInParent<Canvas>();
            Camera uiCam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCam = canvas.worldCamera;
            }

            if (!_hudView.TryGetPortraitScreenPoint(uiCam, out var screenPoint))
            {
                return fallback + Vector3.up * 0.5f;
            }

            var ray = _digCamera.ScreenPointToRay(screenPoint);
            var plane = new Plane(Vector3.up, new Vector3(0f, _mapPlaneY, 0f));
            if (plane.Raycast(ray, out var enter))
            {
                return ray.GetPoint(enter) + Vector3.up * 0.5f;
            }

            return fallback + Vector3.up * 0.5f;
        }

        private void HandleTimeUp()
        {
            if (_cursorView != null)
            {
                _cursorView.DestroySpawnedUiRing();
                _cursorView.gameObject.SetActive(false);
            }
            var body = _session != null ? _session.Ledger.BuildSummaryText(_configs) : "本阶段未获得奖励。";
            if (_summaryView != null)
            {
                _summaryView.Show(body, () =>
                {
                    var cb = _onSummaryConfirmed;
                    _onSummaryConfirmed = null;
                    cb?.Invoke();
                });
            }
            else
            {
                _onSummaryConfirmed?.Invoke();
                _onSummaryConfirmed = null;
            }
        }

        private void RefreshWarehouseHud()
        {
            if (_session == null || _hudView == null)
            {
                return;
            }

            var wh = _session.Warehouse;
            var sb = new StringBuilder();
            sb.Append($"精魂 {wh.SpiritEssence:0.##}");
            foreach (var kv in wh.Materials)
            {
                sb.Append($" | {kv.Key} {kv.Value}");
            }

            _hudView.SetWarehouse(sb.ToString());
        }

        private void HandleGmAddGraves()
        {
            if (_session == null || !_session.IsActive || _session.IsTimeUp)
            {
                return;
            }

            var spawned = _session.DebugSpawnGraves(10);
            Debug.Log($"[DigStageController] GM Add Graves → spawned {spawned}/10");
        }

        private void HandleGmAddBodyParts()
        {
            if (_session == null || !_session.IsActive || _session.IsTimeUp)
            {
                return;
            }

            _session.DebugGrantAllBodyParts(10);
            Debug.Log("[DigStageController] GM Add Body Parts → +10 each BodyPartConfig row");
        }

        private void HandleGmEquipWarriorEnhance()
        {
            if (_specialEquipSlots == null)
            {
                Debug.LogWarning("[DigStageController] GM Equip Warrior Enhance — no SpecialEquipSlots bound.");
                return;
            }

            if (!_specialEquipSlots.TryEquip(SoldierManufactureMagicBookHook.WarriorEnhanceBookId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Equip Warrior Enhance failed: {error}");
                return;
            }

            Debug.Log("[DigStageController] GM Equip Warrior Enhance → MagicBook_WarriorEnhance");
        }

        private void HandleGmAcquireDigRing()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire Iron Shovel — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(IronShovelEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire Iron Shovel failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire Iron Shovel");
        }

        private void HandleGmGrantEquipCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Grant EquipCommonExp — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.DebugGrantCommonExp(GmEquipCommonExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Grant EquipCommonExp failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Grant EquipCommonExp +50");
        }

        private void HandleGmSpendDigRingCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend Iron Shovel — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(IronShovelEquipId, GmSpendIronShovelExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend Iron Shovel failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend Iron Shovel CommonExp");
        }

        private void HandleGmAcquireMinerLamp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire Miner Lamp — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(MinerLampEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire Miner Lamp failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire Miner Lamp");
        }

        private void HandleGmSpendMinerLampCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend Miner Lamp — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(MinerLampEquipId, GmSpendMinerLampExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend Miner Lamp failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend Miner Lamp CommonExp");
        }

        private void HandleGmAcquireExplosives()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire Explosives — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(ExplosivesEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire Explosives failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire Explosives");
        }

        private void HandleGmSpendExplosivesCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend Explosives — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(ExplosivesEquipId, GmSpendExplosivesExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend Explosives failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend Explosives CommonExp");
        }

        private void HandleGmAcquireLightning()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire Lightning — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(LightningEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire Lightning failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire Lightning");
        }

        private void HandleGmSpendLightningCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend Lightning — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(LightningEquipId, GmSpendLightningExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend Lightning failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend Lightning CommonExp");
        }

        private void HandleGmAcquireDetector()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire Detector — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(DetectorEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire Detector failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire Detector");
        }

        private void HandleGmSpendDetectorCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend Detector — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(DetectorEquipId, GmSpendDetectorExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend Detector failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend Detector CommonExp");
        }

        private void HandleGmAcquireHumanToken()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire HumanToken — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(HumanTokenEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire HumanToken failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire HumanToken");
        }

        private void HandleGmSpendHumanTokenCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend HumanToken — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(HumanTokenEquipId, GmSpendHumanTokenExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend HumanToken failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend HumanToken CommonExp");
        }

        private void HandleGmAcquireElfToken()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire ElfToken — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(ElfTokenEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire ElfToken failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire ElfToken");
        }

        private void HandleGmSpendElfTokenCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend ElfToken — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(ElfTokenEquipId, GmSpendElfTokenExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend ElfToken failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend ElfToken CommonExp");
        }

        private void HandleGmAcquireOrcToken()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Acquire OrcToken — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TryAcquire(OrcTokenEquipId, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Acquire OrcToken failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Acquire OrcToken");
        }

        private void HandleGmSpendOrcTokenCommonExp()
        {
            if (_protagonistEquipment == null)
            {
                Debug.LogWarning("[DigStageController] GM Spend OrcToken — no ProtagonistEquipment bound.");
                return;
            }

            if (!_protagonistEquipment.TrySpendCommonExp(OrcTokenEquipId, GmSpendOrcTokenExpAmount, out var error))
            {
                Debug.LogWarning($"[DigStageController] GM Spend OrcToken failed: {error}");
                return;
            }

            LogProtagonistEquipmentGmState("Spend OrcToken CommonExp");
        }

        private void LogProtagonistEquipmentGmState(string action)
        {
            var caps = _session != null ? _session.Capabilities : null;
            var cursor = caps != null ? caps.DigCursorRadius : -1f;
            var spawnBonus = caps != null ? caps.DigProcessSpawnCountBonus : 0f;
            var q4 = caps != null ? caps.GetGraveSpawnWeightBonus("Q4") : 0f;
            var q5 = caps != null ? caps.GetGraveSpawnWeightBonus("Q5") : 0f;
            var q6 = caps != null ? caps.GetGraveSpawnWeightBonus("Q6") : 0f;
            var q16 = caps != null ? caps.GetGraveSpawnWeightBonus("Q16") : 0f;
            var q19 = caps != null ? caps.GetGraveSpawnWeightBonus("Q19") : 0f;
            var q20 = caps != null ? caps.GetGraveSpawnWeightBonus("Q20") : 0f;
            var q23 = caps != null ? caps.GetGraveSpawnWeightBonus("Q23") : 0f;
            var q24 = caps != null ? caps.GetGraveSpawnWeightBonus("Q24") : 0f;
            var q27 = caps != null ? caps.GetGraveSpawnWeightBonus("Q27") : 0f;
            var common = _protagonistEquipment != null ? _protagonistEquipment.EquipCommonExp : -1;
            var shovel = FormatOwnedSummary(IronShovelEquipId);
            var lamp = FormatOwnedSummary(MinerLampEquipId);
            var explosives = FormatOwnedSummary(ExplosivesEquipId);
            var lightning = FormatOwnedSummary(LightningEquipId);
            var detector = FormatOwnedSummary(DetectorEquipId);
            var human = FormatOwnedSummary(HumanTokenEquipId);
            var elf = FormatOwnedSummary(ElfTokenEquipId);
            var orc = FormatOwnedSummary(OrcTokenEquipId);
            var blast = FormatExplosiveBlastDamage();
            var interval = FormatLightningInterval();
            Debug.Log(
                $"[DigStageController] GM {action} → {shovel}; {lamp}; {explosives}; {lightning}; {detector}; " +
                $"{human}; {elf}; {orc}; " +
                $"EquipCommonExp={common} DigCursorRadius={cursor:0.###} ExplosiveBlastDamage={blast} " +
                $"DigLightningIntervalSec={interval} DigProcessSpawnCountBonus={spawnBonus:0.###} " +
                $"GraveSpawnWeightBonus Q4={q4:0.###} Q5={q5:0.###} Q6={q6:0.###} " +
                $"Q16={q16:0.###} Q19={q19:0.###} Q20={q20:0.###} Q23={q23:0.###} Q24={q24:0.###} Q27={q27:0.###}");
        }

        private string FormatExplosiveBlastDamage()
        {
            if (_protagonistEquipment == null ||
                !_protagonistEquipment.TryGetOwned(ExplosivesEquipId, out var owned) ||
                owned == null ||
                _configs == null ||
                !_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level, out var row) ||
                row == null ||
                !DigExplosiveEffectConfig.TryParse(row, out var effect))
            {
                return "n/a";
            }

            return effect.BlastDamage.ToString("0.###");
        }

        private string FormatLightningInterval()
        {
            if (_protagonistEquipment == null ||
                !_protagonistEquipment.TryGetOwned(LightningEquipId, out var owned) ||
                owned == null ||
                _configs == null ||
                !_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level, out var row) ||
                row == null ||
                !DigLightningEffectConfig.TryParse(row, out var effect))
            {
                return "n/a";
            }

            return effect.IntervalSeconds.ToString("0.###");
        }

        private string FormatOwnedSummary(string equipId)
        {
            if (_protagonistEquipment != null &&
                _protagonistEquipment.TryGetOwned(equipId, out var owned) &&
                owned != null)
            {
                return $"{equipId} L{owned.Level} Exp{owned.CurrentExp}";
            }

            return $"{equipId} (not owned)";
        }

        private void EnsureWorldRoot()
        {
            if (_worldRoot == null)
            {
                var go = new GameObject("DigWorld");
                go.transform.SetParent(transform, false);
                _worldRoot = go.transform;
            }
        }

        private void EnsureDigLight()
        {
            const string lightName = "DigStageLight";
            var existing = transform.Find(lightName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var lightGo = new GameObject(lightName);
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = Color.white;
            light.shadows = LightShadows.None;
        }

        private void EndInternal(bool destroyWorld)
        {
            _running = false;
            if (_hudView != null)
            {
                _hudView.AddGravesRequested -= HandleGmAddGraves;
                _hudView.AddBodyPartsRequested -= HandleGmAddBodyParts;
                _hudView.EquipWarriorEnhanceRequested -= HandleGmEquipWarriorEnhance;
                _hudView.AcquireDigRingRequested -= HandleGmAcquireDigRing;
                _hudView.GrantEquipCommonExpRequested -= HandleGmGrantEquipCommonExp;
                _hudView.SpendDigRingCommonExpRequested -= HandleGmSpendDigRingCommonExp;
                _hudView.AcquireMinerLampRequested -= HandleGmAcquireMinerLamp;
                _hudView.SpendMinerLampCommonExpRequested -= HandleGmSpendMinerLampCommonExp;
                _hudView.AcquireExplosivesRequested -= HandleGmAcquireExplosives;
                _hudView.SpendExplosivesCommonExpRequested -= HandleGmSpendExplosivesCommonExp;
                _hudView.AcquireLightningRequested -= HandleGmAcquireLightning;
                _hudView.SpendLightningCommonExpRequested -= HandleGmSpendLightningCommonExp;
                _hudView.AcquireDetectorRequested -= HandleGmAcquireDetector;
                _hudView.SpendDetectorCommonExpRequested -= HandleGmSpendDetectorCommonExp;
                _hudView.AcquireHumanTokenRequested -= HandleGmAcquireHumanToken;
                _hudView.SpendHumanTokenCommonExpRequested -= HandleGmSpendHumanTokenCommonExp;
                _hudView.AcquireElfTokenRequested -= HandleGmAcquireElfToken;
                _hudView.SpendElfTokenCommonExpRequested -= HandleGmSpendElfTokenCommonExp;
                _hudView.AcquireOrcTokenRequested -= HandleGmAcquireOrcToken;
                _hudView.SpendOrcTokenCommonExpRequested -= HandleGmSpendOrcTokenCommonExp;
            }

            UnsubscribeSession(_session);
            _session?.Stop();
            _session = null;
            _summaryView?.Hide();
            _hudView?.Hide();
            if (_cursorView != null)
            {
                _cursorView.DestroySpawnedUiRing();
                _cursorView.gameObject.SetActive(false);
            }

            if (_digCamera != null)
            {
                _digCamera.gameObject.SetActive(false);
            }

            _graveViews.Clear();
            _barrelViews.Clear();

            if (destroyWorld && _worldRoot != null)
            {
                for (var i = _worldRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(_worldRoot.GetChild(i).gameObject);
                }
            }

            _mapInstance = null;
            _gravesParent = null;
            _effectsParent = null;
            _onSummaryConfirmed = null;
            _configs = null;
            _specialEquipSlots = null;
            _protagonistEquipment = null;
            _defendCatalog = null;
        }
    }
}
