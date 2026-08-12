using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Mode2 UM read-only last-batch soldier list (SPEC_03 UI-015 / D-054).
    /// </summary>
    public sealed class ManufactureRecordModalView : MonoBehaviour
    {
        private const string EmptyCopy = "本批无士兵";

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _content;
        [SerializeField] private Text _rowTemplate;
        [SerializeField] private Text _emptyText;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _wired;

        public event Action CloseRequested;

        public Button CloseButton => _closeButton;

        private void Awake()
        {
            EnsureWired();
        }

        public void RuntimeWire(GameObject root, Button closeButton, Transform content, Text rowTemplate, Text emptyText)
        {
            _root = root;
            _closeButton = closeButton;
            _content = content;
            _rowTemplate = rowTemplate;
            _emptyText = emptyText;
            _wired = false;
            EnsureWired();
        }

        public void ShowRoot()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void HideRoot()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void Bind(IReadOnlyList<string> lines)
        {
            EnsureWired();
            ClearRows();

            var hasRows = lines != null && lines.Count > 0;
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(!hasRows);
                if (!hasRows)
                {
                    _emptyText.text = EmptyCopy;
                }
            }

            if (!hasRows || _content == null || _rowTemplate == null)
            {
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var row = Instantiate(_rowTemplate, _content);
                row.gameObject.SetActive(true);
                row.text = line;
                _rows.Add(row.gameObject);
            }
        }

        /// <summary>
        /// Builds Mode2 entry button + modal under UM root. Used by UmAssetBuilder and runtime Ensure.
        /// </summary>
        public static ManufactureRecordModalView Build(Transform umRoot, out Button recordButton)
        {
            if (umRoot == null)
            {
                throw new ArgumentNullException(nameof(umRoot));
            }

            var recordGo = CreateUiButton(umRoot, "ManufactureRecordButton", "制造记录",
                new Color(0.38f, 0.36f, 0.52f, 1f));
            Place(recordGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(308f, 28f), new Vector2(140f, 48f));
            SetButtonFontSize(recordGo, 18);
            recordButton = recordGo.GetComponent<Button>();

            var modal = CreateUiPanel(umRoot, "ManufactureRecordModal", new Color(0f, 0f, 0f, 0.55f));
            Stretch(modal.GetComponent<RectTransform>());
            var box = CreateUiPanel(modal.transform, "RecordZone", new Color(0.18f, 0.22f, 0.32f, 0.98f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f));

            var title = CreateUiText(box.transform, "Title", "制造记录", 24, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(400f, 40f));

            var closeX = CreateUiButton(box.transform, "CloseButton", "X", new Color(0.55f, 0.28f, 0.28f, 1f));
            Place(closeX.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(44f, 44f));
            SetButtonFontSize(closeX, 22);

            var empty = CreateUiText(box.transform, "EmptyText", EmptyCopy, 20, TextAnchor.MiddleCenter);
            StretchFill(empty.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.82f), 0f);

            var scrollRoot = CreateUiPanel(box.transform, "ScrollRoot", new Color(0f, 0f, 0f, 0.08f));
            StretchFill(scrollRoot.GetComponent<RectTransform>(), new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.84f), 0f);
            var content = CreateVerticalScroll(scrollRoot.transform);
            var rowTemplate = CreateRowTemplate(content);

            var view = modal.AddComponent<ManufactureRecordModalView>();
            view.RuntimeWire(
                modal,
                closeX.GetComponent<Button>(),
                content,
                rowTemplate,
                empty);
            modal.SetActive(false);
            return view;
        }

        private void EnsureWired()
        {
            if (_wired)
            {
                return;
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleClose);
            }

            if (_rowTemplate != null)
            {
                _rowTemplate.gameObject.SetActive(false);
            }

            _wired = true;
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleClose);
            }
        }

        private void HandleClose()
        {
            CloseRequested?.Invoke();
        }

        private void ClearRows()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                {
                    Destroy(_rows[i]);
                }
            }

            _rows.Clear();
        }

        private static RectTransform CreateVerticalScroll(Transform parent)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(parent, false);
            Stretch(scrollGo.GetComponent<RectTransform>());
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            Stretch(viewportGo.GetComponent<RectTransform>());
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            return content;
        }

        private static Text CreateRowTemplate(Transform content)
        {
            var go = new GameObject("RowTemplate", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(content, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.minHeight = 28f;
            go.SetActive(false);
            return text;
        }

        private static GameObject CreateUiPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateUiText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        private static GameObject CreateUiButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var text = CreateUiText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            return go;
        }

        private static void SetButtonFontSize(GameObject button, int fontSize)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontSize = fontSize;
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchFill(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float padding)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
