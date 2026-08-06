using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Mode + level select UI before Defend Prepare (SPEC_03 §3.12 / UI-013 / D-044).
    /// Builds a minimal overlay at runtime when Prefab slots are unbound.
    /// </summary>
    public sealed class BattleModeSelectView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _defendModeButton;
        [SerializeField] private Button _pushMapModeButton;
        [SerializeField] private Transform _levelListContent;
        [SerializeField] private GameObject _levelRowTemplate;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _titleText;

        private readonly List<DefendGameplayConfigRow> _defendRows = new List<DefendGameplayConfigRow>();
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private BattleMode _selectedMode = BattleMode.Defend;
        private string _selectedConfigId;
        private string _recommendedConfigId;
        private bool _built;

        public event Action<BattleMode, string> ConfirmRequested;

        public void Show(
            IReadOnlyList<DefendGameplayConfigRow> defendRows,
            string recommendedConfigId)
        {
            EnsureUi();
            _defendRows.Clear();
            if (defendRows != null)
            {
                for (var i = 0; i < defendRows.Count; i++)
                {
                    if (defendRows[i] != null)
                    {
                        _defendRows.Add(defendRows[i]);
                    }
                }
            }

            _recommendedConfigId = recommendedConfigId ?? string.Empty;
            _selectedMode = BattleMode.Defend;
            _selectedConfigId = ResolveDefaultConfigId();
            if (_root != null)
            {
                _root.SetActive(true);
            }

            gameObject.SetActive(true);
            WireButtons(true);
            Refresh();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            WireButtons(true);
        }

        private void OnDisable()
        {
            WireButtons(false);
        }

        private void WireButtons(bool on)
        {
            if (_defendModeButton != null)
            {
                _defendModeButton.onClick.RemoveListener(OnDefendModeClicked);
                if (on)
                {
                    _defendModeButton.onClick.AddListener(OnDefendModeClicked);
                }
            }

            if (_pushMapModeButton != null)
            {
                _pushMapModeButton.onClick.RemoveListener(OnPushMapModeClicked);
                if (on)
                {
                    _pushMapModeButton.onClick.AddListener(OnPushMapModeClicked);
                }
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
                if (on)
                {
                    _confirmButton.onClick.AddListener(OnConfirmClicked);
                }
            }
        }

        private void OnDefendModeClicked()
        {
            _selectedMode = BattleMode.Defend;
            if (string.IsNullOrEmpty(_selectedConfigId) || !HasDefendConfig(_selectedConfigId))
            {
                _selectedConfigId = ResolveDefaultConfigId();
            }

            Refresh();
        }

        private void OnPushMapModeClicked()
        {
            _selectedMode = BattleMode.PushMap;
            _selectedConfigId = null;
            Refresh();
        }

        private void OnConfirmClicked()
        {
            if (_selectedMode == BattleMode.PushMap)
            {
                SetStatus("推图战规则待录入（Demo 占位，不可进入）");
                return;
            }

            if (string.IsNullOrEmpty(_selectedConfigId) || !HasDefendConfig(_selectedConfigId))
            {
                SetStatus("请选择保卫战关卡");
                return;
            }

            ConfirmRequested?.Invoke(BattleMode.Defend, _selectedConfigId);
        }

        private void SelectLevel(string configId)
        {
            if (_selectedMode != BattleMode.Defend)
            {
                return;
            }

            _selectedConfigId = configId;
            Refresh();
        }

        private void Refresh()
        {
            RebuildLevelRows();
            var confirmInteractable = _selectedMode == BattleMode.Defend
                && !string.IsNullOrEmpty(_selectedConfigId);
            if (_confirmButton != null)
            {
                _confirmButton.interactable = confirmInteractable;
                var label = _confirmButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = _selectedMode == BattleMode.PushMap
                        ? "进入（推图战不可用）"
                        : "进入保卫战";
                }
            }

            if (_selectedMode == BattleMode.PushMap)
            {
                SetStatus("模式：推图战 — 规则待录入，确认禁用");
            }
            else
            {
                SetStatus(
                    string.IsNullOrEmpty(_selectedConfigId)
                        ? "模式：保卫战 — 请选择关卡"
                        : $"模式：保卫战 — 已选 {_selectedConfigId}");
            }

            TintModeButton(_defendModeButton, _selectedMode == BattleMode.Defend);
            TintModeButton(_pushMapModeButton, _selectedMode == BattleMode.PushMap);
        }

        private void RebuildLevelRows()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                {
                    Destroy(_spawnedRows[i]);
                }
            }

            _spawnedRows.Clear();
            if (_levelListContent == null || _levelRowTemplate == null)
            {
                return;
            }

            _levelRowTemplate.SetActive(false);
            if (_selectedMode != BattleMode.Defend)
            {
                var stub = Instantiate(_levelRowTemplate, _levelListContent);
                stub.name = "PushMapStubRow";
                stub.SetActive(true);
                var stubText = stub.GetComponentInChildren<Text>();
                if (stubText != null)
                {
                    stubText.text = "（推图战关卡表未接入）";
                }

                var stubBtn = stub.GetComponent<Button>();
                if (stubBtn != null)
                {
                    stubBtn.interactable = false;
                }

                _spawnedRows.Add(stub);
                return;
            }

            for (var i = 0; i < _defendRows.Count; i++)
            {
                var row = _defendRows[i];
                var go = Instantiate(_levelRowTemplate, _levelListContent);
                go.name = "Level_" + row.GameplayConfigId;
                go.SetActive(true);
                var text = go.GetComponentInChildren<Text>();
                if (text != null)
                {
                    var mark = string.Equals(row.GameplayConfigId, _selectedConfigId, StringComparison.Ordinal)
                        ? "▶ "
                        : "  ";
                    var rec = string.Equals(row.GameplayConfigId, _recommendedConfigId, StringComparison.Ordinal)
                        ? " [推荐]"
                        : string.Empty;
                    text.text =
                        $"{mark}{row.GameplayConfigId}  Map={row.BattleMapId}  Wave={row.WaveConfigId}  {row.CombatDurationSeconds}s{rec}";
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = row.GameplayConfigId;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => SelectLevel(captured));
                }

                _spawnedRows.Add(go);
            }
        }

        private string ResolveDefaultConfigId()
        {
            if (!string.IsNullOrEmpty(_recommendedConfigId) && HasDefendConfig(_recommendedConfigId))
            {
                return _recommendedConfigId;
            }

            return _defendRows.Count > 0 ? _defendRows[0].GameplayConfigId : null;
        }

        private bool HasDefendConfig(string configId)
        {
            for (var i = 0; i < _defendRows.Count; i++)
            {
                if (string.Equals(_defendRows[i].GameplayConfigId, configId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message ?? string.Empty;
            }
        }

        private static void TintModeButton(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.45f, 0.32f, 0.18f, 1f)
                    : new Color(0.22f, 0.24f, 0.28f, 1f);
            }
        }

        private void EnsureUi()
        {
            if (_built && _root != null)
            {
                return;
            }

            if (_root != null && _confirmButton != null && _levelListContent != null)
            {
                _built = true;
                return;
            }

            BuildRuntimeUi();
            _built = true;
        }

        private void BuildRuntimeUi()
        {
            var canvasGo = new GameObject(
                "BattleModeSelectCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _root = CreatePanel(canvasGo.transform, "Panel", new Color(0.06f, 0.07f, 0.1f, 0.94f));
            StretchFull(_root.GetComponent<RectTransform>());

            _titleText = CreateText(_root.transform, "Title", "选择战斗模式与关卡", 32, TextAnchor.UpperCenter);
            Place(_titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(900f, 48f));

            _defendModeButton = CreateButton(_root.transform, "DefendModeButton", "模式1 保卫战",
                new Color(0.45f, 0.32f, 0.18f, 1f));
            Place(_defendModeButton.GetComponent<RectTransform>(), new Vector2(0.32f, 0.82f), new Vector2(0.32f, 0.82f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 56f));

            _pushMapModeButton = CreateButton(_root.transform, "PushMapModeButton", "模式2 推图战",
                new Color(0.22f, 0.24f, 0.28f, 1f));
            Place(_pushMapModeButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.82f), new Vector2(0.68f, 0.82f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 56f));

            var listHost = CreatePanel(_root.transform, "LevelListHost", new Color(0.1f, 0.12f, 0.15f, 0.95f));
            StretchFill(listHost.GetComponent<RectTransform>(), new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.74f), 0f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(listHost.transform, false);
            StretchFull(scrollGo.GetComponent<RectTransform>());
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;
            scroll.viewport = scrollGo.GetComponent<RectTransform>();
            _levelListContent = contentGo.transform;

            _levelRowTemplate = CreateButton(contentGo.transform, "LevelRowTemplate", "Level",
                new Color(0.18f, 0.2f, 0.24f, 1f)).gameObject;
            var rowLe = _levelRowTemplate.AddComponent<LayoutElement>();
            rowLe.minHeight = 48f;
            rowLe.preferredHeight = 48f;
            _levelRowTemplate.SetActive(false);

            _confirmButton = CreateButton(_root.transform, "ConfirmButton", "进入保卫战",
                new Color(0.55f, 0.32f, 0.22f, 1f));
            Place(_confirmButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 60f));

            _statusText = CreateText(_root.transform, "Status", string.Empty, 18, TextAnchor.LowerCenter);
            Place(_statusText.rectTransform, new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000f, 36f));
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return t;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            var go = CreatePanel(parent, name, color);
            var button = go.AddComponent<Button>();
            var text = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
            StretchFull(text.rectTransform);
            return button;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchFill(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float pad)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static void Place(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

#if UNITY_EDITOR
        public void EditorBind(
            GameObject root,
            Button defendMode,
            Button pushMapMode,
            Transform levelContent,
            GameObject levelRowTemplate,
            Button confirm,
            Text status,
            Text title)
        {
            _root = root;
            _defendModeButton = defendMode;
            _pushMapModeButton = pushMapMode;
            _levelListContent = levelContent;
            _levelRowTemplate = levelRowTemplate;
            _confirmButton = confirm;
            _statusText = status;
            _titleText = title;
            _built = true;
        }
#endif
    }
}
