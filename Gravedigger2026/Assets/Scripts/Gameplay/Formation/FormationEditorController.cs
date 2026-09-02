using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.TacticalFormation;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.PushMap;
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
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _quickPreviewButton;
        [SerializeField] private Slider _cameraPathSlider;
        private Button _oneClickDeployButton;
        [SerializeField] private RectTransform _dragGhost;
        [SerializeField] private Image _dragGhostImage;
        [SerializeField] private FormationSoldierHoverTooltipView _hoverTooltip;
        [SerializeField] private FormationBondHudView _bondHud;
        [SerializeField] private TacticalFormationSquadBarView _tacticalSquadBar;

        private readonly List<string> _barIds = new List<string>();
        private readonly List<string> _barDisplayNames = new List<string>();
        private readonly List<int> _barClassLevels = new List<int>();
        private readonly List<Sprite> _barSprites = new List<Sprite>();
        private readonly List<bool> _barHighlighted = new List<bool>();
        private readonly List<TacticalFormationSquadSnapshot> _squadScratch =
            new List<TacticalFormationSquadSnapshot>(4);
        private readonly Dictionary<string, Sprite> _thumbnailCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private string _selectedTacticalFormationId;

        private DefendPrefabCatalog _defendCatalog;
        private ConfigCsvRepository _configs;
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

        private bool _suppressAutoDeployRefresh;
        private readonly List<FormationClassZoneSnapshot> _zonesScratch = new List<FormationClassZoneSnapshot>();
        private readonly List<float> _previewWaypointProgress = new List<float>(8);
        private PushMapCameraPath _cameraPath;
        private Coroutine _pathPreviewRoutine;
        private bool _suppressPathSliderCallback;
        private float _previewIntroSpeed = CombatConstantKeys.Safety.PushMapCameraIntroSpeed;
        private float _previewIntroDwell =
            CombatConstantKeys.Safety.PushMapCameraIntroWaypointDwellSeconds;

        private FormationPrefabCatalog _patternCatalog;
        private TacticalFormationLayoutService _layout;
        private bool _squadDrag;
        private readonly List<string> _squadDragIds = new List<string>(8);
        private readonly List<float> _squadDragOrigX = new List<float>(8);
        private readonly List<float> _squadDragOrigZ = new List<float>(8);
        private float _squadDragAnchorX;
        private float _squadDragAnchorZ;

        public event Action ReturnRequested;
        public event Action StartBattleRequested;
        public event Action CompleteRequested;

        public bool IsActive => _active;

        /// <summary>Prepare layout used to lock combat snapshot before <c>End</c> (SPEC_04 §9.7 TF-04b).</summary>
        public TacticalFormationLayoutService Layout => _layout;

        public bool TryCollectClassZones(List<FormationClassZoneSnapshot> into)
        {
            if (into == null)
            {
                return false;
            }

            into.Clear();
            var map = _ownedMapInstance != null ? _ownedMapInstance : _boundMap;
            if (map == null)
            {
                return false;
            }

            FormationClassZoneCollector.CollectFromMapInstance(map, into);
            return into.Count > 0 || map != null;
        }

        public void Begin(
            FormationEditorMode mode,
            DefendPrefabCatalog defendCatalog,
            WarriorPoolService pool,
            BattleFormationService formation,
            ProtagonistProgressService progress,
            ConfigCsvRepository configs,
            GameObject mapPrefabOrNull,
            GameObject existingMapOrNull,
            FormationPrefabCatalog patternCatalog = null,
            TacticalFormationLayoutService layoutService = null)
        {
            End();
            _mode = mode;
            _defendCatalog = defendCatalog;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _progress = progress;
            _configs = configs;
            _patternCatalog = patternCatalog;
            _layout = layoutService ?? new TacticalFormationLayoutService();
            _active = true;
            _dragKind = DragKind.None;
            _dragWarriorId = null;
            _leftBar = false;
            _wasDeployedBeforeDrag = false;
            _suppressAutoDeployRefresh = false;
            ClearSquadDrag();
            _selectedTacticalFormationId = null;
            if (_tacticalSquadBar != null)
            {
                _tacticalSquadBar.SetClickHandler(SelectTacticalSquad);
                _tacticalSquadBar.SetSelectedFormationId(null);
            }

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
                _startBattleButton.gameObject.SetActive(FormationEditorModeUtil.ShowsStartBattle(mode));
                _startBattleButton.onClick.AddListener(HandleStartBattle);
            }

            // Mode2 Prefab only: always visible (UM + Prepare). Hosts may ignore CompleteRequested.
            if (_completeButton != null)
            {
                _completeButton.gameObject.SetActive(true);
                _completeButton.onClick.AddListener(HandleComplete);
            }

            EnsureOneClickDeployButton();
            if (_oneClickDeployButton != null)
            {
                _oneClickDeployButton.onClick.AddListener(HandleOneClickDeploy);
            }

            SetupPathPreviewControls();

            if (_soldierBar != null)
            {
                _soldierBar.SlotLiftStarted += HandleBarSlotLiftStarted;
                _soldierBar.HoverChanged += HandleBarHoverChanged;
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

            HideHoverTooltip();
            HideUiGhost();
            ClearWorldDragPreview();
            if (_bondHud != null)
            {
                _bondHud.BindServices(_formation, _pool, _configs);
            }

            EvaluateTacticalLayout();
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

            if (_completeButton != null)
            {
                _completeButton.onClick.RemoveListener(HandleComplete);
            }

            if (_oneClickDeployButton != null)
            {
                _oneClickDeployButton.onClick.RemoveListener(HandleOneClickDeploy);
            }

            TeardownPathPreviewControls();

            if (_soldierBar != null)
            {
                _soldierBar.SlotLiftStarted -= HandleBarSlotLiftStarted;
                _soldierBar.HoverChanged -= HandleBarHoverChanged;
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

            HideHoverTooltip();
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
            _configs = null;
            _pool = null;
            _formation = null;
            _progress = null;
            _patternCatalog = null;
            _layout = null;
            _active = false;
            _dragKind = DragKind.None;
            ClearSquadDrag();
            _selectedTacticalFormationId = null;
            if (_tacticalSquadBar != null)
            {
                _tacticalSquadBar.SetClickHandler(null);
                _tacticalSquadBar.SetSelectedFormationId(null);
                _tacticalSquadBar.Refresh(null, null);
            }

            if (_oneClickDeployButton != null)
            {
                Destroy(_oneClickDeployButton.gameObject);
                _oneClickDeployButton = null;
            }
            HideUiGhost();
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_dragKind != DragKind.None)
            {
                HideHoverTooltip();
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
                    if (_squadDrag)
                    {
                        UpdateSquadPreviewFollow(mouse);
                    }
                    else
                    {
                        EnsureWorldDragPreview(_dragWarriorId);
                        if (_battlefieldPreview != null)
                        {
                            _battlefieldPreview.SetPreviewVisible(_dragWarriorId, false);
                        }
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
            HideHoverTooltip();
            if (slot == null || string.IsNullOrEmpty(slot.WarriorId) || _formation == null)
            {
                return;
            }

            _dragKind = DragKind.FromBar;
            _dragWarriorId = slot.WarriorId;
            _leftBar = false;
            _wasDeployedBeforeDrag = _formation.IsDeployed(slot.WarriorId);
            slot.SetHighlighted(true);
            TryCaptureSquadDrag(slot.WarriorId, requireDeployed: true);

            if (_wasDeployedBeforeDrag && _battlefieldPreview != null && !_squadDrag)
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
            TryCaptureSquadDrag(warriorId, requireDeployed: true);
            if (_squadDrag)
            {
                UpdateSquadPreviewFollow(screenPos);
                return;
            }

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
            var squadDrag = _squadDrag;
            _dragKind = DragKind.None;
            _dragWarriorId = null;
            _leftBar = false;
            _wasDeployedBeforeDrag = false;
            ClearWorldDragPreview();

            if (string.IsNullOrEmpty(warriorId) || _formation == null)
            {
                ClearSquadDrag();
                RefreshAll();
                return;
            }

            var overBar = _soldierBar != null && _soldierBar.ContainsScreenPoint(screenPos, null);

            // Released still inside bar without leaving → cancel lift (keep deploy state if already deployed).
            if (kind == DragKind.FromBar && !leftBar && overBar)
            {
                ClearSquadDrag();
                RefreshAll();
                return;
            }

            if (overBar)
            {
                _formation.TryUndeploy(warriorId, out _);
                ClearSquadDrag();
                EvaluateTacticalLayout();
                RefreshAll();
                return;
            }

            if (!TryScreenToMapXZ(screenPos, out var relX, out var relZ) || !IsInsideMap(relX, relZ))
            {
                if (wasDeployed || kind == DragKind.FromField)
                {
                    _formation.TryUndeploy(warriorId, out _);
                }

                ClearSquadDrag();
                EvaluateTacticalLayout();
                RefreshAll();
                return;
            }

            if (squadDrag && wasDeployed)
            {
                _suppressAutoDeployRefresh = true;
                try
                {
                    _layout.TryApplySquadCenterDelta(
                        _formation,
                        warriorId,
                        relX - _squadDragAnchorX,
                        relZ - _squadDragAnchorZ);
                }
                finally
                {
                    _suppressAutoDeployRefresh = false;
                }

                ClearSquadDrag();
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

            ClearSquadDrag();
            EvaluateTacticalLayout();
            RefreshAll();
        }

        private void UpdateDragFollow(Vector2 screenPos)
        {
            if (_squadDrag)
            {
                UpdateSquadPreviewFollow(screenPos);
                return;
            }

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
                WarriorInstance warrior = null;
                if (_pool != null)
                {
                    var warriors = _pool.Warriors;
                    for (var i = 0; i < warriors.Count; i++)
                    {
                        if (string.Equals(warriors[i].Id, warriorId, StringComparison.Ordinal))
                        {
                            warrior = warriors[i];
                            break;
                        }
                    }
                }

                WarriorAllIn1StyleView.ApplyTo(
                    _worldDragPreview,
                    _defendCatalog != null ? _defendCatalog.VisualStyleCatalog : null,
                    warrior);
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
            if (_dragKind != DragKind.None || _suppressAutoDeployRefresh)
            {
                return;
            }

            EvaluateTacticalLayout();
            RefreshAll();
        }

        private void EvaluateTacticalLayout()
        {
            if (_layout == null || _formation == null || _pool == null || _configs == null)
            {
                return;
            }

            var wasSuppressed = _suppressAutoDeployRefresh;
            _suppressAutoDeployRefresh = true;
            try
            {
                _layout.EvaluateAndApply(
                    _formation,
                    _pool,
                    _configs,
                    _patternCatalog,
                    BuildLayoutContext());
            }
            finally
            {
                _suppressAutoDeployRefresh = wasSuppressed;
            }
        }

        private TacticalFormationLayoutContext BuildLayoutContext()
        {
            TryCollectClassZones(_zonesScratch);
            var map = _ownedMapInstance != null ? _ownedMapInstance : _boundMap;
            return TacticalFormationLayoutContextFactory.Create(_mode, map, _mapCenter, _zonesScratch);
        }

        private void TryCaptureSquadDrag(string warriorId, bool requireDeployed)
        {
            ClearSquadDrag();
            if (_layout == null
                || _formation == null
                || string.IsNullOrEmpty(warriorId)
                || (requireDeployed && !_formation.IsDeployed(warriorId))
                || !_layout.TryGetSquadByMember(warriorId, out var squad)
                || squad == null
                || squad.MemberIds == null
                || squad.MemberIds.Length == 0)
            {
                return;
            }

            if (!_formation.TryGetEntry(warriorId, out var anchor) || anchor == null)
            {
                return;
            }

            _squadDragAnchorX = anchor.PositionX;
            _squadDragAnchorZ = anchor.PositionZ;
            for (var i = 0; i < squad.MemberIds.Length; i++)
            {
                var id = squad.MemberIds[i];
                if (!_formation.TryGetEntry(id, out var entry) || entry == null)
                {
                    continue;
                }

                _squadDragIds.Add(id);
                _squadDragOrigX.Add(entry.PositionX);
                _squadDragOrigZ.Add(entry.PositionZ);
            }

            _squadDrag = _squadDragIds.Count > 0;
        }

        private void UpdateSquadPreviewFollow(Vector2 screenPos)
        {
            if (!_squadDrag || _battlefieldPreview == null || !TryScreenToMapXZ(screenPos, out var relX, out var relZ))
            {
                return;
            }

            var dx = relX - _squadDragAnchorX;
            var dz = relZ - _squadDragAnchorZ;
            for (var i = 0; i < _squadDragIds.Count; i++)
            {
                _battlefieldPreview.SetPreviewMapRel(
                    _squadDragIds[i],
                    _squadDragOrigX[i] + dx,
                    _squadDragOrigZ[i] + dz);
            }
        }

        private void ClearSquadDrag()
        {
            _squadDrag = false;
            _squadDragIds.Clear();
            _squadDragOrigX.Clear();
            _squadDragOrigZ.Clear();
            _squadDragAnchorX = 0f;
            _squadDragAnchorZ = 0f;
        }

        private void EnsureOneClickDeployButton()
        {
            if (_oneClickDeployButton != null)
            {
                return;
            }

            if (_formation == null || _formation.BoundCampaignMode != CampaignMode.Mode2)
            {
                return;
            }

            if (_completeButton == null)
            {
                return;
            }

            var canvasParent = _completeButton.transform.parent;
            if (canvasParent == null)
            {
                return;
            }

            var completeRt = _completeButton.GetComponent<RectTransform>();
            if (completeRt == null)
            {
                return;
            }

            const float gap = 8f;
            const float width = 180f;

            // Runtime-UI: create the button next to CompleteButton to avoid prefab authoring dependency.
            var go = new GameObject("OneClickDeployButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasParent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.4f, 0.62f, 1f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = completeRt.anchorMin;
            rect.anchorMax = completeRt.anchorMax;
            rect.pivot = completeRt.pivot;
            rect.anchoredPosition = new Vector2(
                completeRt.anchoredPosition.x - completeRt.sizeDelta.x - gap,
                completeRt.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, completeRt.sizeDelta.y);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.GetComponent<Text>();
            txt.text = "一键上阵";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var textRt = txt.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            _oneClickDeployButton = go.GetComponent<Button>();
            _oneClickDeployButton.interactable = true;
        }

        private void HandleOneClickDeploy()
        {
            if (_pool == null || _formation == null || _configs == null)
            {
                return;
            }

            if (_formation.BoundCampaignMode != CampaignMode.Mode2 || _suppressAutoDeployRefresh)
            {
                return;
            }

            var hasZones = TryCollectClassZones(_zonesScratch);
            if (!hasZones || _zonesScratch.Count == 0)
            {
                Debug.LogWarning("[FormationEditor] OneClickDeploy: no FormationClassZone on current map.");
                return;
            }

            _suppressAutoDeployRefresh = true;
            try
            {
                var deployService = new OneClickFormationDeployService(_configs, _pool, _formation);
                var deployed = deployService.DeployNotYetDeployedRandom(_zonesScratch);
                Debug.Log($"[FormationEditor] OneClickDeploy deployed={deployed} (zones={_zonesScratch.Count})");
                EvaluateTacticalLayout();
            }
            finally
            {
                _suppressAutoDeployRefresh = false;
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            RefreshBar();
            RefreshHud();
            if (_battlefieldPreview != null && _formation != null)
            {
                _battlefieldPreview.Sync(_formation, _pool);
            }

            if (_startBattleButton != null
                && FormationEditorModeUtil.ShowsStartBattle(_mode))
            {
                _startBattleButton.interactable = _formation != null && _formation.Entries.Count >= 1;
            }

            if (_bondHud != null)
            {
                _bondHud.RefreshLive();
            }

            RefreshTacticalSquadBar();
        }

        private void RefreshTacticalSquadBar()
        {
            if (_tacticalSquadBar == null)
            {
                return;
            }

            _squadScratch.Clear();
            if (_layout != null)
            {
                _layout.CollectActiveSquads(_squadScratch);
            }

            if (!string.IsNullOrEmpty(_selectedTacticalFormationId))
            {
                var stillActive = false;
                for (var i = 0; i < _squadScratch.Count; i++)
                {
                    var s = _squadScratch[i];
                    if (s != null
                        && string.Equals(s.FormationId, _selectedTacticalFormationId, StringComparison.Ordinal))
                    {
                        stillActive = true;
                        break;
                    }
                }

                if (!stillActive)
                {
                    _selectedTacticalFormationId = null;
                }
            }

            _tacticalSquadBar.Refresh(_squadScratch, _configs);
            _tacticalSquadBar.SetSelectedFormationId(_selectedTacticalFormationId);
        }

        private void SelectTacticalSquad(string formationId)
        {
            if (string.IsNullOrEmpty(formationId))
            {
                return;
            }

            _selectedTacticalFormationId = formationId;
            if (_tacticalSquadBar != null)
            {
                _tacticalSquadBar.SetSelectedFormationId(formationId);
            }

            ApplySoldierBarHighlights();
        }

        private void ApplySoldierBarHighlights()
        {
            if (_soldierBar == null || _pool == null || _formation == null)
            {
                return;
            }

            TacticalFormationSquadSnapshot selectedSquad = null;
            if (!string.IsNullOrEmpty(_selectedTacticalFormationId) && _layout != null)
            {
                _squadScratch.Clear();
                _layout.CollectActiveSquads(_squadScratch);
                for (var i = 0; i < _squadScratch.Count; i++)
                {
                    var s = _squadScratch[i];
                    if (s != null
                        && string.Equals(s.FormationId, _selectedTacticalFormationId, StringComparison.Ordinal))
                    {
                        selectedSquad = s;
                        break;
                    }
                }
            }

            var warriors = _pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var id = warriors[i].Id;
                var highlight = selectedSquad != null
                    ? selectedSquad.Contains(id)
                    : _formation.IsDeployed(id);
                _soldierBar.SetSlotHighlighted(id, highlight);
            }
        }

        private void RefreshBar()
        {
            if (_soldierBar == null || _pool == null || _formation == null)
            {
                return;
            }

            TacticalFormationSquadSnapshot selectedSquad = null;
            if (!string.IsNullOrEmpty(_selectedTacticalFormationId) && _layout != null)
            {
                _squadScratch.Clear();
                _layout.CollectActiveSquads(_squadScratch);
                for (var i = 0; i < _squadScratch.Count; i++)
                {
                    var s = _squadScratch[i];
                    if (s != null
                        && string.Equals(s.FormationId, _selectedTacticalFormationId, StringComparison.Ordinal))
                    {
                        selectedSquad = s;
                        break;
                    }
                }

                if (selectedSquad == null)
                {
                    _selectedTacticalFormationId = null;
                }
            }

            _barIds.Clear();
            _barDisplayNames.Clear();
            _barClassLevels.Clear();
            _barSprites.Clear();
            _barHighlighted.Clear();
            var warriors = _pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var w = warriors[i];
                _barIds.Add(w.Id);
                _barDisplayNames.Add(ResolveClassName(w));
                _barClassLevels.Add(ResolveClassLevel(w));
                _barSprites.Add(ResolveThumbnail(w.AppearanceId));
                var highlight = selectedSquad != null
                    ? selectedSquad.Contains(w.Id)
                    : _formation.IsDeployed(w.Id);
                _barHighlighted.Add(highlight);
            }

            _soldierBar.SetSlots(_barIds, _barDisplayNames, _barClassLevels, _barSprites, _barHighlighted);
            RefreshHoverTooltip();
        }

        private string ResolveClassName(WarriorInstance warrior)
        {
            if (warrior == null)
            {
                return string.Empty;
            }

            if (_configs != null &&
                _configs.TryGetClass(warrior.ClassId, out var row) &&
                row != null &&
                !string.IsNullOrEmpty(row.ClassName))
            {
                return row.ClassName;
            }

            return warrior.ClassId ?? string.Empty;
        }

        private int ResolveClassLevel(WarriorInstance warrior)
        {
            if (warrior == null || _configs == null)
            {
                return 0;
            }

            if (_configs.TryGetClass(warrior.ClassId, out var row) && row != null)
            {
                return row.ClassLevel < 0 ? 0 : row.ClassLevel;
            }

            return 0;
        }

        private void HandleBarHoverChanged(FormationSoldierSlotView slot)
        {
            if (_dragKind != DragKind.None)
            {
                HideHoverTooltip();
                return;
            }

            ShowHoverTooltip(slot);
        }

        private void RefreshHoverTooltip()
        {
            if (_hoverTooltip == null || _soldierBar == null)
            {
                return;
            }

            if (_dragKind != DragKind.None)
            {
                HideHoverTooltip();
                return;
            }

            ShowHoverTooltip(_soldierBar.HoveredSlot);
        }

        private void ShowHoverTooltip(FormationSoldierSlotView slot)
        {
            if (_hoverTooltip == null)
            {
                return;
            }

            if (slot == null || string.IsNullOrEmpty(slot.WarriorId) || _pool == null || _configs == null)
            {
                HideHoverTooltip();
                return;
            }

            if (!_pool.TryGet(slot.WarriorId, out var warrior) || warrior == null)
            {
                HideHoverTooltip();
                return;
            }

            _configs.TryGetClass(warrior.ClassId, out var classRow);
            var content = new FormationSoldierHoverTooltipView.Content
            {
                ClassName = ResolveClassName(warrior),
                ClassLevel = classRow != null ? (classRow.ClassLevel < 0 ? 0 : classRow.ClassLevel) : 0,
                RaceDisplayName = ResolveRaceDisplayName(warrior.RaceId),
                BaseClassDisplay = classRow != null ? FormatBaseClass(classRow.BaseClass) : string.Empty,
                PromoteClass = classRow != null ? classRow.PromoteClass : string.Empty,
                PrimaryStat = classRow != null ? classRow.PrimaryStat : StatKind.Strength
            };

            var staticStats = WarriorStatMath.ComputeStaticStats(
                warrior.BaseStats,
                warrior.EquipStats,
                warrior.GemMult,
                warrior.RaceAdjustCoeff,
                WarriorCombatMath.ResolveClassBaseMoveSpeed(classRow));
            content.MaxHp = WarriorStatMath.ComputeMaxHP(
                warrior.BodyLife,
                staticStats.Strength,
                _configs.GetMaxHpStrengthMult());
            content.Strength = staticStats.Strength;
            content.Agility = staticStats.Agility;
            content.Intelligence = staticStats.Intelligence;

            var skills = warrior.SoldierSkills;
            if (skills != null)
            {
                for (var i = 0; i < skills.Count; i++)
                {
                    var entry = skills[i];
                    if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                    {
                        continue;
                    }

                    var displayName = entry.SkillId;
                    var effectImplemented = false;
                    if (_configs.TryGetSkill(entry.SkillId, entry.SkillLevel, out var skillRow) &&
                        skillRow != null)
                    {
                        if (!string.IsNullOrEmpty(skillRow.DisplayName))
                        {
                            displayName = skillRow.DisplayName;
                        }

                        effectImplemented = skillRow.EffectImplemented;
                    }

                    content.Skills.Add(new FormationSoldierHoverTooltipView.SkillItem
                    {
                        DisplayName = displayName,
                        Icon = FormationSoldierHoverTooltipView.LoadSkillIcon(entry.SkillId),
                        EffectImplemented = effectImplemented
                    });
                }
            }

            _hoverTooltip.Show(slot.RectTransform, content);
        }

        private void HideHoverTooltip()
        {
            if (_hoverTooltip != null)
            {
                _hoverTooltip.Hide();
            }
        }

        private string ResolveRaceDisplayName(string raceId)
        {
            if (_configs != null && _configs.TryGetRace(raceId, out var raceRow) && raceRow != null)
            {
                return string.IsNullOrEmpty(raceRow.DisplayNameKey) ? raceRow.RaceId : raceRow.DisplayNameKey;
            }

            return raceId ?? string.Empty;
        }

        private static string FormatBaseClass(BaseClassKind kind)
        {
            switch (kind)
            {
                case BaseClassKind.Warrior:
                    return "战士";
                case BaseClassKind.Archer:
                    return "射手";
                case BaseClassKind.Mage:
                    return "法师";
                case BaseClassKind.Thief:
                    return "刺客";
                default:
                    return string.Empty;
            }
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
            var cam = _configs != null
                ? _configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            var orthoSize = FormationEditorModeUtil.UsesPushMapPrepareFraming(_mode)
                ? cam.PushMapPrepareOrthoSize
                : cam.ResolveMapFitOrthoSize(half);
            cam.ApplyTopDownPose(_editorCamera, _mapCenter, orthoSize);
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
                if (go != null
                    && (go.GetComponentInParent<Button>() != null
                        || go.GetComponentInParent<Slider>() != null))
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

        private void HandleComplete()
        {
            CompleteRequested?.Invoke();
        }

        private void SetupPathPreviewControls()
        {
            TeardownPathPreviewControls();
            var pushMapPrepare = FormationEditorModeUtil.UsesPushMapPrepareFraming(_mode);
            if (!pushMapPrepare)
            {
                SetPathPreviewUiVisible(false);
                return;
            }

            EnsurePathPreviewUi();
            ResolveCameraPath();
            var camConsts = _configs != null
                ? _configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            _previewIntroSpeed = camConsts.PushMapIntroSpeed;
            _previewIntroDwell = camConsts.PushMapIntroWaypointDwellSeconds;

            var pathOk = _cameraPath != null
                         && _cameraPath.HasBakedPath
                         && _cameraPath.TryBuildAuthorWaypointProgresses(_previewWaypointProgress);
            SetPathPreviewUiVisible(pathOk);
            if (!pathOk)
            {
                return;
            }

            if (_quickPreviewButton != null)
            {
                _quickPreviewButton.onClick.AddListener(HandleQuickPreview);
            }

            if (_cameraPathSlider != null)
            {
                _cameraPathSlider.minValue = 0f;
                _cameraPathSlider.maxValue = 1f;
                _cameraPathSlider.wholeNumbers = false;
                _cameraPathSlider.onValueChanged.AddListener(HandleCameraPathSliderChanged);
                var initS = 0f;
                if (_editorCamera != null
                    && _cameraPath.TryProjectProgress(_editorCamera.transform.position, out var projected))
                {
                    initS = Mathf.Clamp01(projected);
                }

                SetSliderValueSilent(initS);
            }
        }

        private void TeardownPathPreviewControls()
        {
            StopPathPreviewRoutine();
            if (_quickPreviewButton != null)
            {
                _quickPreviewButton.onClick.RemoveListener(HandleQuickPreview);
            }

            if (_cameraPathSlider != null)
            {
                _cameraPathSlider.onValueChanged.RemoveListener(HandleCameraPathSliderChanged);
            }

            _cameraPath = null;
            _previewWaypointProgress.Clear();
            SetPathPreviewUiVisible(false);
        }

        private void ResolveCameraPath()
        {
            _cameraPath = null;
            var map = _ownedMapInstance != null ? _ownedMapInstance : _boundMap;
            if (map == null)
            {
                return;
            }

            _cameraPath = map.GetComponentInChildren<PushMapCameraPath>(true);
            if (_cameraPath == null)
            {
                return;
            }

            if (!_cameraPath.HasBakedPath)
            {
                if (!_cameraPath.TryBake(out var bakeError))
                {
                    Debug.LogWarning($"[FormationEditor] CameraFollowPath bake failed: {bakeError}");
                    _cameraPath = null;
                }
            }
        }

        private void EnsurePathPreviewUi()
        {
            if (_startBattleButton == null)
            {
                return;
            }

            var canvasParent = _startBattleButton.transform.parent;
            if (canvasParent == null)
            {
                return;
            }

            var startRt = _startBattleButton.GetComponent<RectTransform>();
            if (startRt == null)
            {
                return;
            }

            const float gap = 8f;
            var btnH = startRt.sizeDelta.y > 1f ? startRt.sizeDelta.y : 48f;
            var btnW = startRt.sizeDelta.x > 1f ? startRt.sizeDelta.x : 140f;

            if (_quickPreviewButton == null)
            {
                var existing = canvasParent.Find("QuickPreviewButton");
                if (existing != null)
                {
                    _quickPreviewButton = existing.GetComponent<Button>();
                }
            }

            if (_quickPreviewButton == null)
            {
                var go = new GameObject("QuickPreviewButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(canvasParent, false);
                go.GetComponent<Image>().color = new Color(0.32f, 0.42f, 0.55f, 1f);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = startRt.anchorMin;
                rt.anchorMax = startRt.anchorMax;
                rt.pivot = startRt.pivot;
                rt.anchoredPosition = new Vector2(
                    startRt.anchoredPosition.x,
                    startRt.anchoredPosition.y + btnH + gap);
                rt.sizeDelta = new Vector2(btnW, btnH);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                var txt = textGo.GetComponent<Text>();
                txt.text = "快速预览";
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.fontSize = 20;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                var textRt = txt.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                _quickPreviewButton = go.GetComponent<Button>();
            }
            else
            {
                var rt = _quickPreviewButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(
                        startRt.anchoredPosition.x,
                        startRt.anchoredPosition.y + btnH + gap);
                    rt.sizeDelta = new Vector2(btnW, btnH);
                }
            }

            if (_cameraPathSlider == null)
            {
                var existingSlider = canvasParent.Find("CameraPathSlider");
                if (existingSlider != null)
                {
                    _cameraPathSlider = existingSlider.GetComponent<Slider>();
                }
            }

            if (_cameraPathSlider == null)
            {
                _cameraPathSlider = CreateCameraPathSlider(canvasParent, startRt);
            }
            else
            {
                ApplyCameraPathSliderLayout(_cameraPathSlider.GetComponent<RectTransform>(), startRt);
            }
        }

        private static void ApplyCameraPathSliderLayout(RectTransform sliderRt, RectTransform startRt)
        {
            if (sliderRt == null || startRt == null)
            {
                return;
            }

            // SPEC_04 §6: Mode2 CameraPathSlider — bottom-right anchor; Pos=(-630,240); Width=700.
            sliderRt.anchorMin = startRt.anchorMin;
            sliderRt.anchorMax = startRt.anchorMax;
            sliderRt.pivot = startRt.pivot;
            sliderRt.anchoredPosition = new Vector2(-630f, 240f);
            sliderRt.sizeDelta = new Vector2(700f, 28f);
        }

        private static Slider CreateCameraPathSlider(Transform parent, RectTransform startRt)
        {
            var root = new GameObject("CameraPathSlider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            ApplyCameraPathSliderLayout(rootRt, startRt);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.2f, 0.95f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5f, 0f);
            fillAreaRt.offsetMax = new Vector2(-5f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.35f, 0.55f, 0.75f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, 20f);
            handle.GetComponent<Image>().color = new Color(0.85f, 0.88f, 0.92f, 1f);

            var slider = root.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 0f;
            return slider;
        }

        private void SetPathPreviewUiVisible(bool visible)
        {
            if (_quickPreviewButton != null)
            {
                _quickPreviewButton.gameObject.SetActive(visible);
            }

            if (_cameraPathSlider != null)
            {
                _cameraPathSlider.gameObject.SetActive(visible);
            }
        }

        private void HandleQuickPreview()
        {
            if (_editorCamera == null
                || _cameraPath == null
                || !_cameraPath.HasBakedPath
                || !_cameraPath.TryBuildAuthorWaypointProgresses(_previewWaypointProgress))
            {
                return;
            }

            StopPathPreviewRoutine();
            _pathPreviewRoutine = StartCoroutine(PathPreviewRoutine());
        }

        private IEnumerator PathPreviewRoutine()
        {
            yield return PushMapCameraPathRailTravel.ReverseAuthorWaypointSweep(
                _editorCamera,
                _cameraPath,
                _previewWaypointProgress,
                _previewIntroSpeed,
                _previewIntroDwell,
                SetSliderValueSilent);
            _pathPreviewRoutine = null;
        }

        private void HandleCameraPathSliderChanged(float value)
        {
            if (_suppressPathSliderCallback)
            {
                return;
            }

            StopPathPreviewRoutine();
            if (_editorCamera == null || _cameraPath == null || !_cameraPath.HasBakedPath)
            {
                return;
            }

            PushMapCameraPathRailTravel.SnapCameraToProgress(_editorCamera, _cameraPath, value);
        }

        private void SetSliderValueSilent(float s)
        {
            if (_cameraPathSlider == null)
            {
                return;
            }

            _suppressPathSliderCallback = true;
            _cameraPathSlider.SetValueWithoutNotify(Mathf.Clamp01(s));
            _suppressPathSliderCallback = false;
        }

        private void StopPathPreviewRoutine()
        {
            if (_pathPreviewRoutine != null)
            {
                StopCoroutine(_pathPreviewRoutine);
                _pathPreviewRoutine = null;
            }
        }
    }
}
