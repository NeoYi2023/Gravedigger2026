using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig stage presentation bridge (Approach A). Rules live in DigSessionService.
    /// </summary>
    public sealed class DigStageController : MonoBehaviour
    {
        [SerializeField] private DigPrefabCatalog _catalog;
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Camera _digCamera;
        [SerializeField] private DigCursorView _cursorView;
        [SerializeField] private DigHudView _hudView;
        [SerializeField] private DigStageSummaryView _summaryView;

        private DigSessionService _session;
        private Action _onSummaryConfirmed;
        private readonly Dictionary<int, DigGraveView> _graveViews = new Dictionary<int, DigGraveView>();
        private GameObject _mapInstance;
        private DigDiggerView _diggerView;
        private Transform _gravesParent;
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
            Action onSummaryConfirmed)
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

            var diggerPrefab = _catalog.DiggerPrefab;
            if (diggerPrefab == null)
            {
                Debug.LogError("[DigStageController] Digger prefab missing.");
                return;
            }

            var diggerGo = Instantiate(diggerPrefab, _worldRoot);
            diggerGo.transform.position = center;
            _diggerView = diggerGo.GetComponent<DigDiggerView>();
            if (_diggerView == null)
            {
                _diggerView = diggerGo.AddComponent<DigDiggerView>();
            }

            var diggerRadius = diggerGo.GetComponent<DigObstacleRadius>();
            var diggerR = diggerRadius != null ? diggerRadius.Radius : 0.8f;

            _gravesParent = new GameObject("Graves").transform;
            _gravesParent.SetParent(_worldRoot, false);

            var ledger = new DigStageRewardLedger();
            _session = new DigSessionService(configs, warehouse, ledger, caps);
            SubscribeSession(_session);

            if (_digCamera != null)
            {
                _digCamera.gameObject.SetActive(true);
                _digCamera.transform.position = center + new Vector3(0f, 18f, 0f);
                _digCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _digCamera.orthographic = true;
                _digCamera.orthographicSize = Mathf.Max(half.x, half.y) - 1.5f;
                _digCamera.nearClipPlane = 0.1f;
                _digCamera.farClipPlane = 100f;
                // Top-down: sort transparent Tilemap vs Sprite by view depth (higher Y draws in front).
                _digCamera.transparencySortMode = TransparencySortMode.CustomAxis;
                _digCamera.transparencySortAxis = Vector3.up;
            }

            EnsureDigLight();

            if (_cursorView != null)
            {
                _cursorView.gameObject.SetActive(true);
                _cursorView.SetUiRingPrefab(_catalog.UiDigCursorRingPrefab);
                Canvas hudCanvas = null;
                if (_hudView != null)
                {
                    hudCanvas = _hudView.GetComponentInParent<Canvas>();
                }

                _cursorView.Configure(_digCamera, caps.DigCursorRadius, center.y, hudCanvas);
                Debug.Log($"[DigStageController] Cursor radius={caps.DigCursorRadius:0.###} camera ortho={_digCamera.orthographicSize:0.##} canvasScale={(hudCanvas != null ? hudCanvas.scaleFactor : -1f):0.###}");
            }

            _hudView?.Show();
            _summaryView?.Hide();

            _session.Begin(context.DigConfig, center, diggerR, half);
            RefreshWarehouseHud();
            _running = true;

            context.MapResolveNote =
                $"DigMapId={context.DigConfig.DigMapId} → Instantiated {MapPrefabPaths.PrefabFolder}/{context.DigConfig.DigMapId}.prefab";
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
            session.DiggingPresenceChanged += HandleDiggingPresence;
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
            session.DiggingPresenceChanged -= HandleDiggingPresence;
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

        private void SpawnRewardFlyer(Vector3 from, string lootEncoded)
        {
            var target = _diggerView != null ? _diggerView.transform.position : from;
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
            flyer.Play(from + Vector3.up * 0.5f, target + Vector3.up * 0.5f, label, () =>
            {
                _session?.CreditPendingLoot(lootEncoded);
            });
        }

        private void HandleDiggingPresence(bool digging)
        {
            _diggerView?.SetDigging(digging);
        }

        private void HandleTimeUp()
        {
            if (_cursorView != null)
            {
                _cursorView.DestroySpawnedUiRing();
                _cursorView.gameObject.SetActive(false);
            }
            var body = _session != null ? _session.Ledger.BuildSummaryText() : "本阶段未获得奖励。";
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

            if (destroyWorld && _worldRoot != null)
            {
                for (var i = _worldRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(_worldRoot.GetChild(i).gameObject);
                }
            }

            _mapInstance = null;
            _diggerView = null;
            _gravesParent = null;
            _onSummaryConfirmed = null;
        }
    }
}
