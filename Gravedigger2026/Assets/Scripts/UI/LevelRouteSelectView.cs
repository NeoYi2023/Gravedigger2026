using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Level;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-031: LevelId tabs atop Box + route map (1450-wide) or legacy Stage rows + unlock edges.
    /// Map mode: Icon-only options; hover Tips show Type/Title/Description/Reward.
    /// </summary>
    public sealed class LevelRouteSelectView : MonoBehaviour
    {
        public const float MapDisplayWidth = 1450f;
        public const float MapOptionIconSize = 150f;
        private const float ClearReturnHoldSeconds = 0.5f;
        private const float ClearReturnMoveSeconds = 0.5f;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _levelTabBar;
        [SerializeField] private GameObject _levelTabTemplate;
        [SerializeField] private Transform _stageListContent;
        [SerializeField] private GameObject _stageRowTemplate;
        [SerializeField] private GameObject _optionCardTemplate;
        [SerializeField] private RectTransform _edgeLayer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _stageScrollRoot;
        [SerializeField] private GameObject _mapScrollRoot;
        [SerializeField] private RectTransform _mapContent;
        [SerializeField] private Image _mapBackground;
        [SerializeField] private RectTransform _mapOptionsHost;
        [SerializeField] private ScrollRect _mapScroll;
        [SerializeField] private GameObject _optionHoverTipsRoot;
        [SerializeField] private Text _optionTipsType;
        [SerializeField] private Text _optionTipsTitle;
        [SerializeField] private Text _optionTipsDescription;
        [SerializeField] private Text _optionTipsReward;

        private readonly List<GameObject> _spawnedStages = new List<GameObject>();
        private readonly List<GameObject> _spawnedEdges = new List<GameObject>();
        private readonly List<GameObject> _spawnedTabs = new List<GameObject>();
        private readonly Dictionary<string, RectTransform> _optionRects =
            new Dictionary<string, RectTransform>(StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> _unlockByOption =
            new Dictionary<string, string[]>(StringComparer.Ordinal);

        private LevelRouteSnapshot _lastSnapshot;
        private string _selectedLevelId;
        private readonly List<string> _levelIds = new List<string>();
        private readonly Dictionary<string, string> _levelDisplayNames =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _unlockedLevelIds = new HashSet<string>(StringComparer.Ordinal);
        private GameObject _spawnedMapRoot;
        private Coroutine _clearReturnFocusRoutine;

        public event Action<string> OptionSelected;
        public event Action<string> LevelTabSelected;
        /// <summary>Locked LevelId tab clicked (gray still clickable → Toast).</summary>
        public event Action<string> LockedLevelTabClicked;
        public event Action Closed;

        private static readonly Color Locked = new Color(0.35f, 0.35f, 0.38f, 0.85f);
        private static readonly Color Selectable = new Color(0.22f, 0.48f, 0.32f, 1f);
        private static readonly Color Cleared = new Color(0.25f, 0.35f, 0.55f, 1f);
        private static readonly Color Running = new Color(0.55f, 0.42f, 0.18f, 1f);
        private static readonly Color EdgeColor = new Color(0.75f, 0.78f, 0.55f, 0.85f);
        private static readonly Color TabIdle = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color TabSelected = new Color(0.35f, 0.48f, 0.72f, 1f);
        private static readonly Color TabLocked = new Color(0.18f, 0.18f, 0.20f, 0.75f);

        private void Awake()
        {
            if (_root == null && _closeButton == null)
            {
                // Boot.unity leftover was saved with no serialized refs — never show it.
                gameObject.SetActive(false);
                return;
            }

            WireClose();

            if (_stageRowTemplate != null)
            {
                _stageRowTemplate.SetActive(false);
            }

            if (_optionCardTemplate != null)
            {
                _optionCardTemplate.SetActive(false);
            }

            if (_levelTabTemplate != null)
            {
                _levelTabTemplate.SetActive(false);
            }
        }

        public void BindRuntime(
            GameObject root,
            Text titleText,
            Transform stageListContent,
            GameObject stageRowTemplate,
            GameObject optionCardTemplate,
            RectTransform edgeLayer,
            Button closeButton,
            Transform levelTabBar = null,
            GameObject levelTabTemplate = null,
            GameObject stageScrollRoot = null,
            GameObject mapScrollRoot = null,
            RectTransform mapContent = null,
            Image mapBackground = null,
            RectTransform mapOptionsHost = null,
            ScrollRect mapScroll = null,
            GameObject optionHoverTipsRoot = null,
            Text optionTipsType = null,
            Text optionTipsTitle = null,
            Text optionTipsDescription = null,
            Text optionTipsReward = null)
        {
            _root = root;
            _titleText = titleText;
            _stageListContent = stageListContent;
            _stageRowTemplate = stageRowTemplate;
            _optionCardTemplate = optionCardTemplate;
            _edgeLayer = edgeLayer;
            _closeButton = closeButton;
            if (levelTabBar != null)
            {
                _levelTabBar = levelTabBar;
            }

            if (levelTabTemplate != null)
            {
                _levelTabTemplate = levelTabTemplate;
            }

            _stageScrollRoot = stageScrollRoot;
            _mapScrollRoot = mapScrollRoot;
            _mapContent = mapContent;
            _mapBackground = mapBackground;
            _mapOptionsHost = mapOptionsHost;
            _mapScroll = mapScroll;

            if (optionHoverTipsRoot != null)
            {
                _optionHoverTipsRoot = optionHoverTipsRoot;
            }

            if (optionTipsType != null)
            {
                _optionTipsType = optionTipsType;
            }

            if (optionTipsTitle != null)
            {
                _optionTipsTitle = optionTipsTitle;
            }

            if (optionTipsDescription != null)
            {
                _optionTipsDescription = optionTipsDescription;
            }

            if (optionTipsReward != null)
            {
                _optionTipsReward = optionTipsReward;
            }

            WireClose();
            HideOptionTips();

            if (_stageRowTemplate != null)
            {
                _stageRowTemplate.SetActive(false);
            }

            if (_optionCardTemplate != null)
            {
                _optionCardTemplate.SetActive(false);
            }

            if (_levelTabTemplate != null)
            {
                _levelTabTemplate.SetActive(false);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public string SelectedLevelId => _selectedLevelId;

        public void ConfigureLevelTabs(
            IReadOnlyList<string> levelIds,
            string selectedLevelId,
            IReadOnlyCollection<string> unlockedLevelIds = null,
            IReadOnlyDictionary<string, string> levelDisplayNames = null)
        {
            _levelIds.Clear();
            if (levelIds != null)
            {
                for (var i = 0; i < levelIds.Count; i++)
                {
                    var id = levelIds[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        _levelIds.Add(id);
                    }
                }
            }

            _levelDisplayNames.Clear();
            if (levelDisplayNames != null)
            {
                foreach (var pair in levelDisplayNames)
                {
                    if (!string.IsNullOrEmpty(pair.Key))
                    {
                        _levelDisplayNames[pair.Key] = pair.Value ?? string.Empty;
                    }
                }
            }

            _unlockedLevelIds.Clear();
            if (unlockedLevelIds != null)
            {
                foreach (var id in unlockedLevelIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _unlockedLevelIds.Add(id);
                    }
                }
            }
            else
            {
                // Backward compatible: treat all configured ids as unlocked.
                for (var i = 0; i < _levelIds.Count; i++)
                {
                    _unlockedLevelIds.Add(_levelIds[i]);
                }
            }

            _selectedLevelId = selectedLevelId;
            if (string.IsNullOrEmpty(_selectedLevelId) && _levelIds.Count > 0)
            {
                _selectedLevelId = _levelIds[_levelIds.Count - 1];
            }

            RebuildTabs();
        }

        public void ApplySnapshot(LevelRouteSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            if (snapshot == null || !snapshot.Visible)
            {
                Hide();
                return;
            }

            if (!string.IsNullOrEmpty(snapshot.LevelId))
            {
                _selectedLevelId = snapshot.LevelId;
            }

            if (_titleText != null)
            {
                _titleText.text = ResolveDisplayName(snapshot.LevelId, snapshot.LevelName);
            }

            RebuildTabs();
            Rebuild(snapshot);
            if (_root != null)
            {
                _root.SetActive(true);
            }

            Canvas.ForceUpdateCanvases();
            RebuildEdges();
            BeginMapFocusAfterApply(snapshot);
        }

        public void Hide()
        {
            StopClearReturnFocusCeremony();
            HideOptionTips();
            if (_root != null)
            {
                _root.SetActive(false);
            }

            // Scene leftover in Boot.unity has _root unassigned; still must hide the overlay.
            if (_root == null || _root == gameObject)
            {
                gameObject.SetActive(false);
            }
        }

        public void NotifyOptionPointerEnter(LevelRouteOptionSnapshot opt, RectTransform anchor)
        {
            ShowOptionTips(opt, anchor);
        }

        public void NotifyOptionPointerExit()
        {
            HideOptionTips();
        }

        private void LateUpdate()
        {
            if (IsOpen && _lastSnapshot != null && _lastSnapshot.Visible)
            {
                RebuildEdges();
            }
        }

        private void RebuildTabs()
        {
            ClearTabs();
            if (_levelTabBar == null || _levelTabTemplate == null)
            {
                return;
            }

            for (var i = 0; i < _levelIds.Count; i++)
            {
                var levelId = _levelIds[i];
                var tab = Instantiate(_levelTabTemplate, _levelTabBar);
                tab.name = "Tab_" + levelId;
                tab.SetActive(true);
                _spawnedTabs.Add(tab);

                var label = tab.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.text = ResolveDisplayName(levelId, null);
                }

                var selected = string.Equals(levelId, _selectedLevelId, StringComparison.Ordinal);
                var unlocked = _unlockedLevelIds.Contains(levelId);
                var bg = tab.GetComponent<Image>();
                if (bg != null)
                {
                    if (selected)
                    {
                        bg.color = TabSelected;
                    }
                    else if (!unlocked)
                    {
                        bg.color = TabLocked;
                    }
                    else
                    {
                        bg.color = TabIdle;
                    }
                }

                var button = tab.GetComponent<Button>();
                if (button != null)
                {
                    // Keep locked tabs clickable so Toast can fire; only disable current selection.
                    button.interactable = !selected;
                    var captured = levelId;
                    var capturedUnlocked = unlocked;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (!capturedUnlocked)
                        {
                            LockedLevelTabClicked?.Invoke(captured);
                            return;
                        }

                        LevelTabSelected?.Invoke(captured);
                    });
                }
            }
        }

        private void Rebuild(LevelRouteSnapshot snapshot)
        {
            HideOptionTips();
            ClearSpawned();
            if (_optionCardTemplate == null)
            {
                return;
            }

            var stages = snapshot.Stages;
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            var mapPrefab = LevelRouteMapLoader.LoadPrefab(snapshot.LevelId);
            var useMap = !string.IsNullOrEmpty(snapshot.RouteMapAssetId)
                         && mapPrefab != null
                         && _mapContent != null;

            if (!string.IsNullOrEmpty(snapshot.RouteMapAssetId) && mapPrefab == null)
            {
                Debug.LogWarning(
                    $"[LevelRouteSelect] Missing LevelRouteMap_{snapshot.LevelId} under Resources/Prefabs/Level/; falling back to Stage rows.");
            }

            SetScrollMode(useMap);

            if (useMap)
            {
                RebuildMapLayout(snapshot, stages, mapPrefab);
            }
            else
            {
                RebuildLegacyStageRows(stages);
            }
        }

        private void SetScrollMode(bool mapMode)
        {
            if (_stageScrollRoot != null)
            {
                _stageScrollRoot.SetActive(!mapMode);
            }

            if (_mapScrollRoot != null)
            {
                _mapScrollRoot.SetActive(mapMode);
            }
        }

        private void RebuildMapLayout(
            LevelRouteSnapshot snapshot,
            LevelRouteStageSnapshot[] stages,
            GameObject mapPrefab)
        {
            if (_mapBackground != null)
            {
                _mapBackground.gameObject.SetActive(false);
            }

            if (_mapOptionsHost != null)
            {
                _mapOptionsHost.gameObject.SetActive(false);
            }

            _spawnedMapRoot = Instantiate(mapPrefab, _mapContent);
            _spawnedMapRoot.name = "LevelRouteMap_" + snapshot.LevelId;
            var mapRt = _spawnedMapRoot.GetComponent<RectTransform>();
            var size = mapRt != null ? mapRt.sizeDelta : new Vector2(MapDisplayWidth, MapDisplayWidth);
            if (size.x < 1f)
            {
                size.x = MapDisplayWidth;
            }

            if (size.y < 1f)
            {
                size.y = Mathf.Max(MapDisplayWidth, 2200f);
            }

            _mapContent.anchorMin = Vector2.zero;
            _mapContent.anchorMax = Vector2.zero;
            _mapContent.pivot = Vector2.zero;
            _mapContent.anchoredPosition = Vector2.zero;
            _mapContent.sizeDelta = size;

            if (mapRt != null)
            {
                mapRt.anchorMin = Vector2.zero;
                mapRt.anchorMax = Vector2.zero;
                mapRt.pivot = Vector2.zero;
                mapRt.anchoredPosition = Vector2.zero;
                mapRt.sizeDelta = size;
                mapRt.localScale = Vector3.one;
                mapRt.localRotation = Quaternion.identity;
            }

            PlaceEdgeLayerUnderMapContent();

            HideMapPinVisuals(_spawnedMapRoot.transform);

            for (var i = 0; i < stages.Length; i++)
            {
                var options = stages[i].Options;
                if (options == null)
                {
                    continue;
                }

                for (var j = 0; j < options.Length; j++)
                {
                    var opt = options[j];
                    var pinPos = ResolvePinPosition(_spawnedMapRoot.transform, opt.GameplayOptionId, snapshot.LevelId);
                    SpawnOptionCard(opt, _spawnedMapRoot.transform, pinPos);
                }
            }
        }

        /// <summary>
        /// Map mode: after layout, scroll MapContent Y so the latest unlocked option pin
        /// is vertically centered in the viewport (X unchanged).
        /// Clear-return with JustClearedOptionId: snap cleared → hold 0.5s → smooth to frontier.
        /// </summary>
        private void BeginMapFocusAfterApply(LevelRouteSnapshot snapshot)
        {
            StopClearReturnFocusCeremony();
            if (_mapScrollRoot == null || !_mapScrollRoot.activeSelf
                || _mapContent == null || _mapScroll == null || snapshot == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(snapshot.JustClearedOptionId))
            {
                _clearReturnFocusRoutine = StartCoroutine(ClearReturnFocusCeremony(snapshot.JustClearedOptionId));
                return;
            }

            ScrollMapToLatestUnlocked();
        }

        private void StopClearReturnFocusCeremony()
        {
            if (_clearReturnFocusRoutine == null)
            {
                return;
            }

            StopCoroutine(_clearReturnFocusRoutine);
            _clearReturnFocusRoutine = null;
        }

        private IEnumerator ClearReturnFocusCeremony(string justClearedOptionId)
        {
            if (!TryGetOptionPinY(justClearedOptionId, out var clearedY))
            {
                Debug.LogWarning(
                    $"[LevelRouteSelect] Clear-return focus missing pin '{justClearedOptionId}'; jumping to frontier.");
                ScrollMapToLatestUnlocked();
                _clearReturnFocusRoutine = null;
                yield break;
            }

            ScrollMapContentToY(clearedY);
            yield return new WaitForSecondsRealtime(ClearReturnHoldSeconds);

            if (!TryResolveFocusOptionY(_lastSnapshot, out var frontierY))
            {
                _clearReturnFocusRoutine = null;
                yield break;
            }

            if (Mathf.Abs(frontierY - clearedY) < 0.5f)
            {
                ScrollMapContentToY(frontierY);
                _clearReturnFocusRoutine = null;
                yield break;
            }

            var startY = clearedY;
            var elapsed = 0f;
            while (elapsed < ClearReturnMoveSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / ClearReturnMoveSeconds);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                ScrollMapContentToY(Mathf.Lerp(startY, frontierY, eased));
                yield return null;
            }

            ScrollMapContentToY(frontierY);
            _clearReturnFocusRoutine = null;
        }

        private void ScrollMapToLatestUnlocked()
        {
            if (_mapScrollRoot == null || !_mapScrollRoot.activeSelf
                || _mapContent == null || _mapScroll == null || _lastSnapshot == null)
            {
                return;
            }

            if (!TryResolveFocusOptionY(_lastSnapshot, out var pinY))
            {
                _mapScroll.verticalNormalizedPosition = 0f;
                return;
            }

            ScrollMapContentToY(pinY);
        }

        private bool TryGetOptionPinY(string optionId, out float pinY)
        {
            pinY = 0f;
            if (string.IsNullOrEmpty(optionId)
                || !_optionRects.TryGetValue(optionId, out var rt)
                || rt == null)
            {
                return false;
            }

            pinY = rt.anchoredPosition.y;
            return true;
        }

        private bool TryResolveFocusOptionY(LevelRouteSnapshot snapshot, out float pinY)
        {
            pinY = 0f;
            if (snapshot?.Stages == null)
            {
                return false;
            }

            if (TryPickFocusPinY(snapshot, preferFrontier: true, out pinY))
            {
                return true;
            }

            return TryPickFocusPinY(snapshot, preferFrontier: false, out pinY);
        }

        private bool TryPickFocusPinY(LevelRouteSnapshot snapshot, bool preferFrontier, out float pinY)
        {
            pinY = 0f;
            var bestStage = int.MinValue;
            var bestY = float.NegativeInfinity;
            var found = false;

            for (var i = 0; i < snapshot.Stages.Length; i++)
            {
                var stage = snapshot.Stages[i];
                var options = stage?.Options;
                if (options == null)
                {
                    continue;
                }

                for (var j = 0; j < options.Length; j++)
                {
                    var opt = options[j];
                    if (opt == null || string.IsNullOrEmpty(opt.GameplayOptionId))
                    {
                        continue;
                    }

                    var state = opt.UiState;
                    if (preferFrontier)
                    {
                        if (state != LevelRouteOptionUiState.Selectable
                            && state != LevelRouteOptionUiState.Running)
                        {
                            continue;
                        }
                    }
                    else if (state != LevelRouteOptionUiState.Cleared)
                    {
                        continue;
                    }

                    if (!_optionRects.TryGetValue(opt.GameplayOptionId, out var rt) || rt == null)
                    {
                        continue;
                    }

                    var y = rt.anchoredPosition.y;
                    if (!found
                        || opt.StageNumber > bestStage
                        || (opt.StageNumber == bestStage && y > bestY))
                    {
                        found = true;
                        bestStage = opt.StageNumber;
                        bestY = y;
                    }
                }
            }

            if (!found)
            {
                return false;
            }

            pinY = bestY;
            return true;
        }

        private void ScrollMapContentToY(float pinY)
        {
            var viewport = _mapScroll.viewport != null
                ? _mapScroll.viewport
                : _mapScroll.GetComponent<RectTransform>();
            if (viewport == null)
            {
                return;
            }

            var contentH = _mapContent.rect.height;
            var viewportH = viewport.rect.height;
            var scrollable = Mathf.Max(0f, contentH - viewportH);
            var desiredOffset = Mathf.Clamp(pinY - viewportH * 0.5f, 0f, scrollable);

            var pos = _mapContent.anchoredPosition;
            pos.y = -desiredOffset;
            _mapContent.anchoredPosition = pos;

            if (scrollable > 0.01f)
            {
                _mapScroll.verticalNormalizedPosition = desiredOffset / scrollable;
            }
            else
            {
                _mapScroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Map mode: EdgeLayer reparents under LevelRouteMap, sibling after Background so edges
        /// scroll with the map and draw above the bg but below option Icons (cards still receive
        /// clicks; EdgeLayer CanvasGroup.blocksRaycasts=false).
        /// </summary>
        private void PlaceEdgeLayerUnderMapContent()
        {
            if (_edgeLayer == null || _spawnedMapRoot == null)
            {
                return;
            }

            var mapRoot = _spawnedMapRoot.transform;
            _edgeLayer.SetParent(mapRoot, false);
            _edgeLayer.anchorMin = Vector2.zero;
            _edgeLayer.anchorMax = Vector2.one;
            _edgeLayer.pivot = new Vector2(0.5f, 0.5f);
            _edgeLayer.offsetMin = Vector2.zero;
            _edgeLayer.offsetMax = Vector2.zero;
            _edgeLayer.localScale = Vector3.one;
            _edgeLayer.localRotation = Quaternion.identity;

            var background = mapRoot.Find("Background");
            if (background != null)
            {
                _edgeLayer.SetSiblingIndex(background.GetSiblingIndex() + 1);
            }
            else
            {
                _edgeLayer.SetAsFirstSibling();
            }
        }

        private void PlaceEdgeLayerForLegacyRows()
        {
            if (_edgeLayer == null)
            {
                return;
            }

            var box = _stageScrollRoot != null ? _stageScrollRoot.transform.parent : null;
            if (box == null && _mapScrollRoot != null)
            {
                box = _mapScrollRoot.transform.parent;
            }

            if (box != null)
            {
                _edgeLayer.SetParent(box, false);
            }

            _edgeLayer.anchorMin = Vector2.zero;
            _edgeLayer.anchorMax = Vector2.one;
            _edgeLayer.pivot = new Vector2(0.5f, 0.5f);
            _edgeLayer.offsetMin = new Vector2(24f, 24f);
            _edgeLayer.offsetMax = new Vector2(-24f, -120f);
            _edgeLayer.localScale = Vector3.one;
            _edgeLayer.localRotation = Quaternion.identity;

            var close = box != null ? box.Find("CloseButton") : null;
            if (close != null)
            {
                _edgeLayer.SetSiblingIndex(close.GetSiblingIndex());
            }
            else
            {
                _edgeLayer.SetAsLastSibling();
            }
        }

        private static Vector2 ResolvePinPosition(Transform mapRoot, string optionId, string levelId)
        {
            if (mapRoot == null || string.IsNullOrEmpty(optionId))
            {
                return Vector2.zero;
            }

            var pin = mapRoot.Find(optionId);
            if (pin == null)
            {
                Debug.LogWarning(
                    $"[LevelRouteSelect] Missing pin '{optionId}' on LevelRouteMap_{levelId}; card at (0,0).");
                return Vector2.zero;
            }

            var rt = pin.GetComponent<RectTransform>();
            return rt != null ? rt.anchoredPosition : Vector2.zero;
        }

        private static void HideMapPinVisuals(Transform mapRoot)
        {
            if (mapRoot == null)
            {
                return;
            }

            for (var i = 0; i < mapRoot.childCount; i++)
            {
                var child = mapRoot.GetChild(i);
                if (child == null
                    || string.Equals(child.name, "Background", StringComparison.Ordinal)
                    || string.Equals(child.name, "EdgeLayer", StringComparison.Ordinal))
                {
                    continue;
                }

                var img = child.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = false;
                    img.raycastTarget = false;
                }

                var label = child.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.enabled = false;
                }
            }
        }

        private void RebuildLegacyStageRows(LevelRouteStageSnapshot[] stages)
        {
            if (_stageListContent == null || _stageRowTemplate == null)
            {
                return;
            }

            PlaceEdgeLayerForLegacyRows();

            // Top = highest StageNumber; bottom = Stage1 (bottom-up map).
            for (var i = stages.Length - 1; i >= 0; i--)
            {
                var stage = stages[i];
                var rowGo = Instantiate(_stageRowTemplate, _stageListContent);
                rowGo.name = $"StageRow_{stage.StageNumber}";
                rowGo.SetActive(true);
                _spawnedStages.Add(rowGo);

                var label = rowGo.transform.Find("StageLabel")?.GetComponent<Text>();
                if (label != null)
                {
                    label.text = $"Stage {stage.StageNumber}";
                }

                var optionsHost = rowGo.transform.Find("OptionsHost");
                if (optionsHost == null)
                {
                    optionsHost = rowGo.transform;
                }

                var options = stage.Options;
                if (options == null)
                {
                    continue;
                }

                for (var j = 0; j < options.Length; j++)
                {
                    SpawnOptionCard(options[j], optionsHost, absoluteMapPos: null);
                }
            }
        }

        private void SpawnOptionCard(LevelRouteOptionSnapshot opt, Transform parent, Vector2? absoluteMapPos)
        {
            var card = Instantiate(_optionCardTemplate, parent);
            card.name = opt.GameplayOptionId;
            card.SetActive(true);
            _spawnedStages.Add(card);

            var mapMode = absoluteMapPos.HasValue;
            var rt = card.GetComponent<RectTransform>();
            if (rt != null)
            {
                _optionRects[opt.GameplayOptionId] = rt;
                if (mapMode)
                {
                    var le = card.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.enabled = false;
                    }

                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(MapOptionIconSize, MapOptionIconSize);
                    rt.anchoredPosition = absoluteMapPos.Value;
                }
            }

            _unlockByOption[opt.GameplayOptionId] = SplitPipe(opt.UnlockNextOptionIds);

            var title = card.transform.Find("Title")?.GetComponent<Text>();
            var desc = card.transform.Find("Description")?.GetComponent<Text>();
            var reward = card.transform.Find("Reward")?.GetComponent<Text>();
            var typeText = card.transform.Find("Type")?.GetComponent<Text>();

            if (mapMode)
            {
                SetChildActive(title != null ? title.gameObject : null, false);
                SetChildActive(desc != null ? desc.gameObject : null, false);
                SetChildActive(reward != null ? reward.gameObject : null, false);
                SetChildActive(typeText != null ? typeText.gameObject : null, false);
            }
            else
            {
                if (title != null)
                {
                    title.text = string.IsNullOrEmpty(opt.Title) ? opt.GameplayOptionId : opt.Title;
                }

                if (desc != null)
                {
                    desc.text = opt.Description ?? string.Empty;
                }

                if (reward != null)
                {
                    reward.text = FormatRewardLine(opt.Reward);
                }

                if (typeText != null)
                {
                    typeText.text = opt.GameplayType.ToString();
                }
            }

            var icon = card.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = LevelRouteIconLoader.Load(opt.IconAssetId);
                icon.enabled = icon.sprite != null;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                if (mapMode)
                {
                    var iconRt = icon.rectTransform;
                    iconRt.anchorMin = Vector2.zero;
                    iconRt.anchorMax = Vector2.one;
                    iconRt.pivot = new Vector2(0.5f, 0.5f);
                    iconRt.offsetMin = new Vector2(4f, 4f);
                    iconRt.offsetMax = new Vector2(-4f, -4f);
                }
            }

            var bg = card.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = mapMode
                    ? new Color(1f, 1f, 1f, 0f)
                    : ColorFor(opt.UiState);
                bg.raycastTarget = true;
            }

            var button = card.GetComponent<Button>();
            if (button != null)
            {
                var selectable = opt.UiState == LevelRouteOptionUiState.Selectable;
                button.interactable = selectable;
                var capturedId = opt.GameplayOptionId;
                button.onClick.RemoveAllListeners();
                if (selectable)
                {
                    button.onClick.AddListener(() => OptionSelected?.Invoke(capturedId));
                }
            }

            if (mapMode)
            {
                var hover = card.GetComponent<LevelRouteOptionHover>();
                if (hover == null)
                {
                    hover = card.AddComponent<LevelRouteOptionHover>();
                }

                hover.Bind(this, opt, rt);
            }
        }

        private void ShowOptionTips(LevelRouteOptionSnapshot opt, RectTransform anchor)
        {
            if (opt == null)
            {
                return;
            }

            EnsureOptionHoverTips();
            if (_optionHoverTipsRoot == null)
            {
                return;
            }

            if (_optionTipsType != null)
            {
                _optionTipsType.text = opt.GameplayType.ToString();
            }

            if (_optionTipsTitle != null)
            {
                _optionTipsTitle.text = string.IsNullOrEmpty(opt.Title) ? opt.GameplayOptionId : opt.Title;
            }

            if (_optionTipsDescription != null)
            {
                _optionTipsDescription.text = opt.Description ?? string.Empty;
            }

            if (_optionTipsReward != null)
            {
                _optionTipsReward.text = FormatRewardLine(opt.Reward);
            }

            _optionHoverTipsRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            PositionOptionTips(anchor);
        }

        private void HideOptionTips()
        {
            if (_optionHoverTipsRoot != null)
            {
                _optionHoverTipsRoot.SetActive(false);
            }
        }

        private void PositionOptionTips(RectTransform anchor)
        {
            if (anchor == null || _optionHoverTipsRoot == null)
            {
                return;
            }

            var tipsRt = _optionHoverTipsRoot.GetComponent<RectTransform>();
            var parentRt = tipsRt != null ? tipsRt.parent as RectTransform : null;
            if (tipsRt == null || parentRt == null)
            {
                return;
            }

            var canvas = parentRt.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            var topCenter = (corners[1] + corners[2]) * 0.5f;
            var screen = RectTransformUtility.WorldToScreenPoint(cam, topCenter);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screen, cam, out var local))
            {
                return;
            }

            tipsRt.anchorMin = new Vector2(0.5f, 0.5f);
            tipsRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipsRt.pivot = new Vector2(0.5f, 0f);
            var height = Mathf.Max(tipsRt.sizeDelta.y, tipsRt.rect.height);
            var width = Mathf.Max(tipsRt.sizeDelta.x, tipsRt.rect.width);
            var pos = local + new Vector2(0f, 12f);
            var parentRect = parentRt.rect;
            var halfW = width * 0.5f;
            pos.x = Mathf.Clamp(pos.x, parentRect.xMin + halfW + 8f, parentRect.xMax - halfW - 8f);
            if (pos.y + height > parentRect.yMax - 8f)
            {
                tipsRt.pivot = new Vector2(0.5f, 1f);
                pos = local - new Vector2(0f, 12f);
            }

            tipsRt.anchoredPosition = pos;
            tipsRt.SetAsLastSibling();
        }

        private void EnsureOptionHoverTips()
        {
            if (_optionHoverTipsRoot != null)
            {
                if (_optionTipsType == null)
                {
                    _optionTipsType = _optionHoverTipsRoot.transform.Find("Type")?.GetComponent<Text>();
                }

                if (_optionTipsTitle == null)
                {
                    _optionTipsTitle = _optionHoverTipsRoot.transform.Find("Title")?.GetComponent<Text>();
                }

                if (_optionTipsDescription == null)
                {
                    _optionTipsDescription = _optionHoverTipsRoot.transform.Find("Description")?.GetComponent<Text>();
                }

                if (_optionTipsReward == null)
                {
                    _optionTipsReward = _optionHoverTipsRoot.transform.Find("Reward")?.GetComponent<Text>();
                }

                return;
            }

            var box = ResolveBoxTransform();
            if (box == null)
            {
                return;
            }

            var existing = box.Find("OptionHoverTips");
            if (existing != null)
            {
                _optionHoverTipsRoot = existing.gameObject;
                _optionTipsType = existing.Find("Type")?.GetComponent<Text>();
                _optionTipsTitle = existing.Find("Title")?.GetComponent<Text>();
                _optionTipsDescription = existing.Find("Description")?.GetComponent<Text>();
                _optionTipsReward = existing.Find("Reward")?.GetComponent<Text>();
                _optionHoverTipsRoot.SetActive(false);
                return;
            }

            _optionHoverTipsRoot = BuildOptionHoverTips(box);
            _optionTipsType = _optionHoverTipsRoot.transform.Find("Type")?.GetComponent<Text>();
            _optionTipsTitle = _optionHoverTipsRoot.transform.Find("Title")?.GetComponent<Text>();
            _optionTipsDescription = _optionHoverTipsRoot.transform.Find("Description")?.GetComponent<Text>();
            _optionTipsReward = _optionHoverTipsRoot.transform.Find("Reward")?.GetComponent<Text>();
            _optionHoverTipsRoot.SetActive(false);
        }

        private Transform ResolveBoxTransform()
        {
            if (_titleText != null && _titleText.transform.parent != null)
            {
                return _titleText.transform.parent;
            }

            if (_closeButton != null && _closeButton.transform.parent != null)
            {
                return _closeButton.transform.parent;
            }

            if (_root != null)
            {
                var box = _root.transform.Find("Box") ?? _root.transform.Find("Panel/Box");
                if (box != null)
                {
                    return box;
                }
            }

            return transform.Find("Box") ?? transform.Find("Panel/Box");
        }

        public static GameObject BuildOptionHoverTips(Transform box)
        {
            var tips = new GameObject("OptionHoverTips", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tips.transform.SetParent(box, false);
            var rt = tips.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(320f, 160f);
            rt.anchoredPosition = Vector2.zero;
            var img = tips.GetComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);
            img.raycastTarget = false;

            CreateTipsText(tips.transform, "Type", "Type", 14, new Vector2(0f, -10f), new Vector2(300f, 22f));
            CreateTipsText(tips.transform, "Title", "Title", 18, new Vector2(0f, -36f), new Vector2(300f, 26f));
            CreateTipsText(tips.transform, "Description", "Description", 14, new Vector2(0f, -78f), new Vector2(300f, 48f));
            CreateTipsText(tips.transform, "Reward", "奖励：—", 13, new Vector2(0f, -132f), new Vector2(300f, 22f));
            return tips;
        }

        private static void CreateTipsText(
            Transform parent,
            string name,
            string sample,
            int fontSize,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = sample;
        }

        private static void SetChildActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static string FormatRewardLine(string reward)
        {
            return string.IsNullOrEmpty(reward) ? "奖励：—" : $"奖励：{reward}";
        }

        private void RebuildEdges()
        {
            ClearEdges();
            if (_edgeLayer == null)
            {
                return;
            }

            foreach (var kv in _unlockByOption)
            {
                if (!_optionRects.TryGetValue(kv.Key, out var from) || from == null)
                {
                    continue;
                }

                var targets = kv.Value;
                if (targets == null)
                {
                    continue;
                }

                for (var i = 0; i < targets.Length; i++)
                {
                    if (!_optionRects.TryGetValue(targets[i], out var to) || to == null)
                    {
                        continue;
                    }

                    SpawnEdge(from, to);
                }
            }
        }

        private void SpawnEdge(RectTransform from, RectTransform to)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_edgeLayer, false);
            var img = go.GetComponent<Image>();
            img.color = EdgeColor;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            var a = WorldToLayerLocal(from.position);
            var b = WorldToLayerLocal(to.position);
            var mid = (a + b) * 0.5f;
            var dir = b - a;
            var len = dir.magnitude;
            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(Mathf.Max(4f, len), 4f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            _spawnedEdges.Add(go);
        }

        private Vector2 WorldToLayerLocal(Vector3 world)
        {
            var canvas = _edgeLayer.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _edgeLayer,
                RectTransformUtility.WorldToScreenPoint(cam, world),
                cam,
                out var local);
            return local;
        }

        private void ClearSpawned()
        {
            HideOptionTips();
            for (var i = 0; i < _spawnedStages.Count; i++)
            {
                if (_spawnedStages[i] != null)
                {
                    Destroy(_spawnedStages[i]);
                }
            }

            _spawnedStages.Clear();
            _optionRects.Clear();
            _unlockByOption.Clear();
            ClearEdges();

            if (_edgeLayer != null && _spawnedMapRoot != null
                && _edgeLayer.IsChildOf(_spawnedMapRoot.transform))
            {
                var safeParent = _mapContent != null
                    ? _mapContent
                    : (_mapScrollRoot != null ? _mapScrollRoot.transform : null);
                if (safeParent != null)
                {
                    _edgeLayer.SetParent(safeParent, false);
                }
            }

            if (_spawnedMapRoot != null)
            {
                Destroy(_spawnedMapRoot);
                _spawnedMapRoot = null;
            }

            if (_mapBackground != null)
            {
                _mapBackground.gameObject.SetActive(true);
            }

            if (_mapOptionsHost != null)
            {
                _mapOptionsHost.gameObject.SetActive(true);
            }
        }

        private string ResolveDisplayName(string levelId, string snapshotLevelName)
        {
            if (!string.IsNullOrEmpty(snapshotLevelName))
            {
                return snapshotLevelName;
            }

            if (!string.IsNullOrEmpty(levelId)
                && _levelDisplayNames.TryGetValue(levelId, out var mapped)
                && !string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }

            return levelId ?? string.Empty;
        }

        private void ClearTabs()
        {
            for (var i = 0; i < _spawnedTabs.Count; i++)
            {
                if (_spawnedTabs[i] != null)
                {
                    Destroy(_spawnedTabs[i]);
                }
            }

            _spawnedTabs.Clear();
        }

        private void ClearEdges()
        {
            for (var i = 0; i < _spawnedEdges.Count; i++)
            {
                if (_spawnedEdges[i] != null)
                {
                    Destroy(_spawnedEdges[i]);
                }
            }

            _spawnedEdges.Clear();
        }

        private void WireClose()
        {
            if (_closeButton == null)
            {
                return;
            }

            _closeButton.onClick.RemoveListener(HandleClose);
            _closeButton.onClick.AddListener(HandleClose);
        }

        private void HandleClose()
        {
            Hide();
            Closed?.Invoke();
        }

        private static Color ColorFor(LevelRouteOptionUiState state)
        {
            switch (state)
            {
                case LevelRouteOptionUiState.Selectable:
                    return Selectable;
                case LevelRouteOptionUiState.Cleared:
                    return Cleared;
                case LevelRouteOptionUiState.Running:
                    return Running;
                default:
                    return Locked;
            }
        }

        private static string[] SplitPipe(string encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return Array.Empty<string>();
            }

            var parts = encoded.Split('|');
            var list = new List<string>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i] != null ? parts[i].Trim() : string.Empty;
                if (p.Length > 0)
                {
                    list.Add(p);
                }
            }

            return list.ToArray();
        }
    }

    /// <summary> Pointer hover bridge for map-mode route options (UI-031). </summary>
    public sealed class LevelRouteOptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private LevelRouteSelectView _host;
        private LevelRouteOptionSnapshot _opt;
        private RectTransform _anchor;

        public void Bind(LevelRouteSelectView host, LevelRouteOptionSnapshot opt, RectTransform anchor)
        {
            _host = host;
            _opt = opt;
            _anchor = anchor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _host?.NotifyOptionPointerEnter(_opt, _anchor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _host?.NotifyOptionPointerExit();
        }
    }

    public static class LevelRouteIconLoader
    {
        public static Sprite Load(string iconAssetId)
        {
            if (string.IsNullOrEmpty(iconAssetId))
            {
                return null;
            }

            return Resources.Load<Sprite>("UI/Levels/" + iconAssetId);
        }
    }

    public static class LevelRouteMapLoader
    {
        public const string PrefabResourcesFolder = "Prefabs/Level/";

        public static GameObject LoadPrefab(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            return Resources.Load<GameObject>(PrefabResourcesFolder + "LevelRouteMap_" + levelId);
        }

        public static Sprite Load(string routeMapAssetId)
        {
            if (string.IsNullOrEmpty(routeMapAssetId))
            {
                return null;
            }

            return Resources.Load<Sprite>("UI/SubLevelMaps/" + routeMapAssetId);
        }
    }
}
