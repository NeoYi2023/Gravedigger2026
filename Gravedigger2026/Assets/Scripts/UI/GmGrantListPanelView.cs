using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public readonly struct GmGrantListItem
    {
        public readonly string Id;
        public readonly string Label;

        public GmGrantListItem(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    /// <summary>
    /// UI-019 / D-061: ToolsPanel GM grant list (layout aligned with LevelSelectPanel).
    /// Equipment: nested LevelPicker overlay (sorting 111).
    /// </summary>
    public sealed class GmGrantListPanelView : MonoBehaviour
    {
        private const int LevelPickerSortingOrder = 111;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _listContent;
        [SerializeField] private GameObject _rowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _emptyHintText;
        [SerializeField] private GameObject _levelPickerRoot;
        [SerializeField] private Text _levelPickerTitleText;
        [SerializeField] private Transform _levelPickerContent;
        [SerializeField] private Button _levelPickerCloseButton;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private readonly List<GameObject> _spawnedLevelRows = new List<GameObject>();
        private bool _levelPickerCloseBound;

        public event Action<string> ItemPicked;
        public event Action<int> LevelPicked;
        public event Action Closed;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            BindLevelPickerClose();

            if (_rowTemplate != null)
            {
                _rowTemplate.SetActive(false);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void BindRuntime(
            GameObject root,
            Text titleText,
            Transform listContent,
            GameObject rowTemplate,
            Button closeButton,
            Text emptyHintText)
        {
            _root = root;
            _titleText = titleText;
            _listContent = listContent;
            _rowTemplate = rowTemplate;
            _closeButton = closeButton;
            _emptyHintText = emptyHintText;
        }

        public void BindLevelPicker(
            GameObject pickerRoot,
            Text titleText,
            Transform listContent,
            Button closeButton)
        {
            _levelPickerRoot = pickerRoot;
            _levelPickerTitleText = titleText;
            _levelPickerContent = listContent;
            _levelPickerCloseButton = closeButton;
            BindLevelPickerClose();
        }

        public void Show(string title, IReadOnlyList<GmGrantListItem> items)
        {
            HideLevelPicker();
            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrEmpty(title) ? "发放" : title;
            }

            RebuildRows(items);
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            HideLevelPicker();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void ShowLevelPicker(string title, IReadOnlyList<int> levels)
        {
            EnsureLevelPicker();
            if (_levelPickerTitleText != null)
            {
                _levelPickerTitleText.text = string.IsNullOrEmpty(title) ? "选择等级" : title;
            }

            RebuildLevelRows(levels);
            if (_levelPickerRoot != null)
            {
                _levelPickerRoot.transform.SetAsLastSibling();
                ApplyModalSorting(_levelPickerRoot, LevelPickerSortingOrder);
                _levelPickerRoot.SetActive(true);
            }
        }

        public void HideLevelPicker()
        {
            ClearLevelRows();
            if (_levelPickerRoot != null)
            {
                _levelPickerRoot.SetActive(false);
            }
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private void BindLevelPickerClose()
        {
            if (_levelPickerCloseBound || _levelPickerCloseButton == null)
            {
                return;
            }

            _levelPickerCloseButton.onClick.AddListener(HideLevelPicker);
            _levelPickerCloseBound = true;
        }

        private void RebuildRows(IReadOnlyList<GmGrantListItem> items)
        {
            ClearRows();
            var count = items != null ? items.Count : 0;
            if (_emptyHintText != null)
            {
                _emptyHintText.gameObject.SetActive(count == 0);
                if (count == 0)
                {
                    _emptyHintText.text = "当前模式无可用项";
                }
            }

            if (count == 0 || _listContent == null || _rowTemplate == null)
            {
                return;
            }

            _rowTemplate.SetActive(false);
            for (var i = 0; i < count; i++)
            {
                var item = items[i];
                if (string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                var go = Instantiate(_rowTemplate, _listContent);
                go.name = "GrantRow_" + item.Id;
                go.SetActive(true);
                var text = go.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = string.IsNullOrEmpty(item.Label) ? item.Id : item.Label;
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = item.Id;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => HandleRowClicked(captured));
                }

                _spawnedRows.Add(go);
            }
        }

        private void RebuildLevelRows(IReadOnlyList<int> levels)
        {
            ClearLevelRows();
            var count = levels != null ? levels.Count : 0;
            if (count == 0 || _levelPickerContent == null || _rowTemplate == null)
            {
                return;
            }

            _rowTemplate.SetActive(false);
            for (var i = 0; i < count; i++)
            {
                var level = levels[i];
                var go = Instantiate(_rowTemplate, _levelPickerContent);
                go.name = "GrantLevel_" + level;
                go.SetActive(true);
                var le = go.GetComponent<LayoutElement>();
                if (le == null)
                {
                    le = go.AddComponent<LayoutElement>();
                }

                le.minHeight = 45f;
                le.preferredHeight = 45f;
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var size = rt.sizeDelta;
                    size.y = 45f;
                    rt.sizeDelta = size;
                }
                var text = go.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = "Lv." + level;
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = level;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => HandleLevelClicked(captured));
                }

                _spawnedLevelRows.Add(go);
            }
        }

        private void HandleRowClicked(string id)
        {
            ItemPicked?.Invoke(id);
        }

        private void HandleLevelClicked(int level)
        {
            HideLevelPicker();
            LevelPicked?.Invoke(level);
        }

        private void ClearRows()
        {
            DestroySpawned(_spawnedRows);
        }

        private void ClearLevelRows()
        {
            DestroySpawned(_spawnedLevelRows);
        }

        private static void DestroySpawned(List<GameObject> spawned)
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    Destroy(spawned[i]);
                }
            }

            spawned.Clear();
        }

        private void EnsureLevelPicker()
        {
            if (_levelPickerRoot != null && _levelPickerContent != null)
            {
                BindLevelPickerClose();
                return;
            }

            var host = _root != null ? _root.transform : transform;
            var picker = new GameObject("LevelPicker", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
            picker.transform.SetParent(host, false);
            StretchFull(picker.GetComponent<RectTransform>());
            picker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(picker.transform, false);
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 420f));
            box.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f, 1f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(box.transform, false);
            Place(titleGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(320f, 36f));
            var title = titleGo.GetComponent<Text>();
            ApplyDefaultText(title, 24, TextAnchor.MiddleCenter);
            title.text = "选择等级";

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(box.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = new Vector2(0f, 16f);
            contentRt.sizeDelta = new Vector2(300f, 280f);
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(box.transform, false);
            Place(closeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(140f, 40f));
            closeGo.GetComponent<Image>().color = new Color(0.40f, 0.40f, 0.42f, 1f);
            var closeLabel = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeLabel.transform.SetParent(closeGo.transform, false);
            StretchFull(closeLabel.GetComponent<RectTransform>());
            var closeText = closeLabel.GetComponent<Text>();
            ApplyDefaultText(closeText, 20, TextAnchor.MiddleCenter);
            closeText.text = "关闭";

            picker.SetActive(false);
            BindLevelPicker(picker, title, contentGo.transform, closeGo.GetComponent<Button>());
        }

        private static void ApplyModalSorting(GameObject root, int sortingOrder)
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }
        }

        private static void ApplyDefaultText(Text text, int size, TextAnchor align)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
    }
}
