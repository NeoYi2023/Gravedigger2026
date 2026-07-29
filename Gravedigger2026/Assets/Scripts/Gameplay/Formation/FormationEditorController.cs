using System;
using System.Collections.Generic;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Shared formation editor: map + soldier bar drag + ControlPower HUD (SPEC_03 §3.11 / D-032 / D-040).
    /// </summary>
    public sealed class FormationEditorController : MonoBehaviour
    {
        private enum DragKind
        {
            None,
            FromBar,
            FromField
        }

        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Camera _editorCamera;
        [SerializeField] private FormationSoldierBarView _soldierBar;
        [SerializeField] private FormationBattlefieldPreview _battlefieldPreview;
        [SerializeField] private Text _controlPowerText;
        [SerializeField] private Button _returnButton;
        [SerializeField] private Button _startBattleButton;
        [SerializeField] private RectTransform _dragGhost;
        [SerializeField] private Image _dragGhostImage;

        private readonly List<string> _barIds = new List<string>();
        private readonly List<Sprite> _barSprites = new List<Sprite>();
        private readonly List<bool> _barHighlighted = new List<bool>();
        private readonly Dictionary<string, Sprite> _thumbnailCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private DefendPrefabCatalog _defendCatalog;
        private WarriorPoolService _pool;
        private BattleFormationService _formation;
        private ProtagonistProgressService _progress;
        private FormationEditorMode _mode;
        private GameObject _ownedMapInstance;
        private GameObject _boundMap;
        private Transform _previewParent;
        private DigMapBounds _mapBounds;
        private Vector3 _mapCenter;
        private bool _ownsMap;
        private bool _active;

        private DragKind _dragKind;
        private string _dragWarriorId;
        private bool _leftBar;
        private bool _wasDeployedBeforeDrag;
        private GameObject _worldDragPreview;

        public event Action ReturnRequested;
        public event Action StartBattleRequested;

        public void Begin(
            FormationEditorMode mode,
            DefendPrefabCatalog defendCatalog,
            WarriorPoolService pool,
            BattleFormationService formation,
            ProtagonistProgressService progress,
            GameObject mapPrefabOrNull,
            GameObject existingMapOrNull)
        {
            End();
            _mode = mode;
            _defendCatalog = defendCatalog;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _progress = progress;
            _active = true;
            _dragKind = DragKind.None;
            _dragWarriorId = null;
            _leftBar = false;
            _wasDeployedBeforeDrag = false;

            EnsureWorldRoot();
            if (existingMapOrNull != null)
            {
                _ownsMap = false;
                _ownedMapInstance = null;
                BindMap(existingMapOrNull);
            }
            else if (mapPrefabOrNull != null)
            {
                _ownsMap = true;
                _ownedMapInstance = Instantiate(mapPrefabOrNull, _worldRoot);
                _ownedMapInstance.name = mapPrefabOrNull.name;
                BindMap(_ownedMapInstance);
            }
            else
            {
                Debug.LogError("[FormationEditor] No map provided.");
                return;
            }

            _previewParent = _boundMap != null ? _boundMap.transform : _worldRoot;

            SetupCamera();
            EnsureLight();

            if (_battlefieldPreview == null)
            {
                _battlefieldPreview = gameObject.AddComponent<FormationBattlefieldPreview>();
            }

            _battlefieldPreview.Configure(_defendCatalog, _previewParent, _mapCenter);

            if (_returnButton != null)
            {
                _returnButton.gameObject.SetActive(mode == FormationEditorMode.UpgradeManufacture);
                _returnButton.onClick.AddListener(HandleReturn);
            }

            if (_startBattleButton != null)
            {
                _startBattleButton.gameObject.SetActive(mode == FormationEditorMode.DefendPrepare);
                _startBattleButton.onClick.AddListener(HandleStartBattle);
            }

            if (_soldierBar != null)
            {
                _soldierBar.SlotLiftStarted += HandleBarSlotLiftStarted;
            }

            _formation.Changed += HandleFormationChanged;
            if (_pool != null)
            {
                _pool.Changed += HandleFormationChanged;
            }

            if (_progress != null)
            {
                _progress.Changed += HandleFormationChanged;
            }

            HideUiGhost();
            ClearWorldDragPreview();
            RefreshAll();
        }

        public void End()
        {
            if (!_active && _ownedMapInstance == null && _formation == null)
            {
                return;
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(HandleReturn);
            }

            if (_startBattleButton != null)
            {
                _startBattleButton.onClick.RemoveListener(HandleStartBattle);
            }

            if (_soldierBar != null)
            {
                _soldierBar.SlotLiftStarted -= HandleBarSlotLiftStarted;
            }

            if (_formation != null)
            {
                _formation.Changed -= HandleFormationChanged;
            }

            if (_pool != null)
            {
                _pool.Changed -= HandleFormationChanged;
            }

            if (_progress != null)
            {
                _progress.Changed -= HandleFormationChanged;
            }

            ClearWorldDragPreview();
            if (_battlefieldPreview != null)
            {
                _battlefieldPreview.Clear();
            }

            if (_ownsMap && _ownedMapInstance != null)
            {
                Destroy(_ownedMapInstance);
            }

            _ownedMapInstance = null;
            _boundMap = null;
            _previewParent = null;
            _ownsMap = false;
            _mapBounds = null;
            _defendCatalog = null;
            _pool = null;
            _formation = null;
            _progress = null;
            _active = false;
            _dragKind = DragKind.None;
            HideUiGhost();
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_dragKind == DragKind.None)
            {
                if (Input.GetMouseButtonDown(0) && !IsPointerOverSoldierBar() && !IsPointerOverBlockingUi())
                {
                    TryBeginFieldDrag(Input.mousePosition);
                }

                return;
            }

            var mouse = (Vector2)Input.mousePosition;
            if (_dragKind == DragKind.FromBar && !_leftBar && _soldierBar != null)
            {
                if (!_soldierBar.ContainsScreenPoint(mouse, null))
                {
                    _leftBar = true;
                    EnsureWorldDragPreview(_dragWarriorId);
                    if (_battlefieldPreview != null)
                    {
                        _battlefieldPreview.SetPreviewVisible(_dragWarriorId, false);
                    }
                }
            }

            UpdateDragFollow(mouse);

            if (Input.GetMouseButtonUp(0))
            {
                FinishDrag(mouse);
            }
        }

        private void OnDestroy()
        {
            End();
        }

        private void HandleBarSlotLiftStarted(FormationSoldierSlotView slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.WarriorId) || _formation == null)
            {
                return;
            }

            _dragKind = DragKind.FromBar;
            _dragWarriorId = slot.WarriorId;
            _leftBar = false;
            _wasDeployedBeforeDrag = _formation.IsDeployed(slot.WarriorId);
            slot.SetHighlighted(true);

            if (_wasDeployedBeforeDrag && _battlefieldPreview != null)
            {
                _battlefieldPreview.SetPreviewVisible(slot.WarriorId, false);
            }

            HideUiGhost();
        }

        private void TryBeginFieldDrag(Vector2 screenPos)
        {
            if (_editorCamera == null || _battlefieldPreview == null || _formation == null)
            {
                return;
            }

            var ray = _editorCamera.ScreenPointToRay(screenPos);
            if (!_battlefieldPreview.TryPickWarrior(ray, out var warriorId))
            {
                return;
            }

            _dragKind = DragKind.FromField;
            _dragWarriorId = warriorId;
            _leftBar = true;
            _wasDeployedBeforeDrag = true;
            _battlefieldPreview.SetPreviewVisible(warriorId, false);
            if (_soldierBar != null)
            {
                _soldierBar.SetSlotHighlighted(warriorId, true);
            }

            EnsureWorldDragPreview(warriorId);
            UpdateDragFollow(screenPos);
        }

        private void FinishDrag(Vector2 screenPos)
        {
            var kind = _dragKind;
            var warriorId = _dragWarriorId;
            var wasDeployed = _wasDeployedBeforeDrag;
            var leftBar = _leftBar;
            _dragKind = DragKind.None;
            _dragWarriorId = null;
            _leftBar = false;
            _wasDeployedBeforeDrag = false;
            ClearWorldDragPreview();

            if (string.IsNullOrEmpty(warriorId) || _formation == null)
            {
                RefreshAll();
                return;
            }

            var overBar = _soldierBar != null && _soldierBar.ContainsScreenPoint(screenPos, null);

            // Released still inside bar without leaving → cancel lift (keep deploy state if already deployed).
            if (kind == DragKind.FromBar && !leftBar && overBar)
            {
                RefreshAll();
                return;
            }

            if (overBar)
            {
                _formation.TryUndeploy(warriorId, out _);
                RefreshAll();
                return;
            }

            if (!TryScreenToMapXZ(screenPos, out var relX, out var relZ) || !IsInsideMap(relX, relZ))
            {
                if (wasDeployed || kind == DragKind.FromField)
                {
                    _formation.TryUndeploy(warriorId, out _);
                }

                RefreshAll();
                return;
            }

            if (_formation.IsDeployed(warriorId))
            {
                if (!_formation.TrySetPosition(warriorId, relX, relZ, out var err))
                {
                    Debug.Log($"[FormationEditor] 改位失败：{err}");
                }
            }
            else if (!_formation.TryDeployAt(warriorId, relX, relZ, out var deployErr))
            {
                Debug.Log($"[FormationEditor] 上阵失败：{deployErr}");
            }

            RefreshAll();
        }

        private void UpdateDragFollow(Vector2 screenPos)
        {
            if (_worldDragPreview == null)
            {
                return;
            }

            if (TryScreenToMapXZ(screenPos, out var relX, out var relZ))
            {
                _worldDragPreview.SetActive(true);
                _worldDragPreview.transform.position = new Vector3(
                    _mapCenter.x + relX,
                    _mapCenter.y,
                    _mapCenter.z + relZ);
            }
        }

        private void EnsureWorldDragPreview(string warriorId)
        {
            if (_worldDragPreview != null || string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            var appearanceId = ResolveAppearanceId(warriorId);
            GameObject prefab = null;
            if (_defendCatalog != null && !string.IsNullOrEmpty(appearanceId))
            {
                _defendCatalog.TryGetWarriorAppearance(appearanceId, out prefab);
            }

            var parent = _previewParent != null ? _previewParent : _worldRoot;
            if (prefab != null)
            {
                _worldDragPreview = Instantiate(prefab, parent);
            }
            else
            {
                _worldDragPreview = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _worldDragPreview.transform.SetParent(parent, false);
                _worldDragPreview.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            }

            _worldDragPreview.name = $"FormationDragPreview_{warriorId}";
            FormationBattlefieldPreview.PreparePreviewVisual(_worldDragPreview);
            UpdateDragFollow(Input.mousePosition);
        }

        private void ClearWorldDragPreview()
        {
            if (_worldDragPreview != null)
            {
                Destroy(_worldDragPreview);
                _worldDragPreview = null;
            }
        }

        private bool TryScreenToMapXZ(Vector2 screenPos, out float relX, out float relZ)
        {
            relX = 0f;
            relZ = 0f;
            if (_editorCamera == null)
            {
                return false;
            }

            var ray = _editorCamera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0f, _mapCenter.y, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            var hit = ray.GetPoint(enter);
            relX = hit.x - _mapCenter.x;
            relZ = hit.z - _mapCenter.z;
            return true;
        }

        private bool IsInsideMap(float relX, float relZ)
        {
            var world = new Vector3(_mapCenter.x + relX, _mapCenter.y, _mapCenter.z + relZ);
            if (_mapBounds != null)
            {
                return _mapBounds.ContainsXZ(world);
            }

            return Mathf.Abs(relX) <= 5f && Mathf.Abs(relZ) <= 2.5f;
        }

        private void HandleFormationChanged()
        {
            if (_dragKind != DragKind.None)
            {
                return;
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshBar();
            RefreshHud();
            if (_battlefieldPreview != null && _formation != null)
            {
                _battlefieldPreview.Sync(_formation, _pool);
            }

            if (_startBattleButton != null && _mode == FormationEditorMode.DefendPrepare)
            {
                _startBattleButton.interactable = _formation != null && _formation.Entries.Count >= 1;
            }
        }

        private void RefreshBar()
        {
            if (_soldierBar == null || _pool == null || _formation == null)
            {
                return;
            }

            _barIds.Clear();
            _barSprites.Clear();
            _barHighlighted.Clear();
            var warriors = _pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var w = warriors[i];
                _barIds.Add(w.Id);
                _barSprites.Add(ResolveThumbnail(w.AppearanceId));
                _barHighlighted.Add(_formation.IsDeployed(w.Id));
            }

            _soldierBar.SetSlots(_barIds, _barSprites, _barHighlighted);
        }

        private void RefreshHud()
        {
            if (_controlPowerText == null || _formation == null)
            {
                return;
            }

            var used = _formation.SumControlPowerCost();
            var cap = _progress != null ? _progress.ControlPowerCap : 0f;
            _controlPowerText.text = $"{used:0.##} / {cap:0.##}";
        }

        private string ResolveAppearanceId(string warriorId)
        {
            if (_pool == null)
            {
                return null;
            }

            var warriors = _pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                if (string.Equals(warriors[i].Id, warriorId, StringComparison.Ordinal))
                {
                    return warriors[i].AppearanceId;
                }
            }

            return null;
        }

        private Sprite ResolveThumbnail(string appearanceId)
        {
            if (string.IsNullOrEmpty(appearanceId))
            {
                return null;
            }

            if (_thumbnailCache.TryGetValue(appearanceId, out var cached))
            {
                return cached;
            }

            if (_defendCatalog == null || !_defendCatalog.TryGetWarriorAppearance(appearanceId, out var prefab)
                || prefab == null)
            {
                return null;
            }

            var sprite = FormationBattlefieldPreview.SampleIdleSprite(prefab);
            if (sprite != null)
            {
                _thumbnailCache[appearanceId] = sprite;
            }

            return sprite;
        }

        private void BindMap(GameObject map)
        {
            _boundMap = map;
            _mapBounds = map != null ? map.GetComponent<DigMapBounds>() : null;
            _mapCenter = _mapBounds != null
                ? _mapBounds.Center
                : (map != null ? map.transform.position : Vector3.zero);
        }

        private void SetupCamera()
        {
            if (_editorCamera == null)
            {
                return;
            }

            _editorCamera.gameObject.SetActive(true);
            var half = _mapBounds != null ? _mapBounds.HalfExtents : new Vector2(5f, 2.5f);
            _editorCamera.transform.position = _mapCenter + new Vector3(0f, 18f, 0f);
            _editorCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _editorCamera.orthographic = true;
            _editorCamera.orthographicSize = Mathf.Max(half.x, half.y) - 1.5f;
            _editorCamera.nearClipPlane = 0.1f;
            _editorCamera.farClipPlane = 100f;
            _editorCamera.clearFlags = CameraClearFlags.SolidColor;
            _editorCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            _editorCamera.depth = 30;
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

        private void EnsureLight()
        {
            if (transform.Find("FormationLight") != null)
            {
                return;
            }

            var lightGo = new GameObject("FormationLight");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.1f;
        }

        private void HideUiGhost()
        {
            if (_dragGhost != null)
            {
                _dragGhost.gameObject.SetActive(false);
            }
        }

        private bool IsPointerOverSoldierBar()
        {
            return _soldierBar != null && _soldierBar.ContainsScreenPoint(Input.mousePosition, null);
        }

        private static bool IsPointerOverBlockingUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            // Allow field pick when not over interactive buttons; bar handled separately.
            var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);
            for (var i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go != null && go.GetComponentInParent<Button>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleReturn()
        {
            ReturnRequested?.Invoke();
        }

        private void HandleStartBattle()
        {
            StartBattleRequested?.Invoke();
        }
    }
}
