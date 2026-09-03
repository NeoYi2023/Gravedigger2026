using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-029: InSaveShell difficulty host — three equal-width columns same-screen;
    /// hover shows description; Normal click enters RouteSelect; Hard/Hell Toast (D-081).
    /// </summary>
    public sealed class DifficultySelectHostView : MonoBehaviour
    {
        public enum DifficultyKind
        {
            Normal = 0,
            Hard = 1,
            Hell = 2
        }

        private const string DefaultHint = "将鼠标移到难度栏上查看说明";
        private const string DefaultNormalDesc = "普通：适合熟悉流程与主线关卡。";
        private const string DefaultHardDesc = "困难：尚未开放。";
        private const string DefaultHellDesc = "地狱：尚未开放。";

        [SerializeField] private GameObject _root;
        [SerializeField] private ScrollRect _columnsScroll;
        [SerializeField] private RectTransform _columnsContent;
        [SerializeField] private RectTransform _normalColumn;
        [SerializeField] private RectTransform _hardColumn;
        [SerializeField] private RectTransform _hellColumn;
        [SerializeField] private RectTransform _normalLevelHost;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;
        [SerializeField] private Button _hellButton;
        [SerializeField] private Text _normalLabel;
        [SerializeField] private Text _hardLabel;
        [SerializeField] private Text _hellLabel;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private string _normalDescription = DefaultNormalDesc;
        [SerializeField] private string _hardDescription = DefaultHardDesc;
        [SerializeField] private string _hellDescription = DefaultHellDesc;

        private float _lastViewportWidth = -1f;
        private DifficultyKind? _hoveredKind;

        public event Action DifficultySelected;
        public event Action LockedDifficultyClicked;

        public bool IsOpen => _root != null && _root.activeSelf;

        public RectTransform NormalLevelHost => _normalLevelHost;

        public void BindRuntime(
            GameObject root,
            ScrollRect columnsScroll,
            RectTransform columnsContent,
            RectTransform normalColumn,
            RectTransform hardColumn,
            RectTransform hellColumn,
            RectTransform normalLevelHost,
            Button normalButton,
            Button hardButton,
            Button hellButton,
            Text normalLabel,
            Text hardLabel,
            Text hellLabel,
            Text descriptionText = null)
        {
            _root = root;
            _columnsScroll = columnsScroll;
            _columnsContent = columnsContent;
            _normalColumn = normalColumn;
            _hardColumn = hardColumn;
            _hellColumn = hellColumn;
            _normalLevelHost = normalLevelHost;
            _normalButton = normalButton;
            _hardButton = hardButton;
            _hellButton = hellButton;
            _normalLabel = normalLabel;
            _hardLabel = hardLabel;
            _hellLabel = hellLabel;
            if (descriptionText != null)
            {
                _descriptionText = descriptionText;
            }

            EnsureDescriptionDefaults();
            WireButtons();
            WireHover();
            ApplyNormalColumnImageAspect();
            HideLevelHost();
            ResetDescription();
        }

        private void Awake()
        {
            EnsureDescriptionDefaults();
            WireButtons();
            WireHover();
            ApplyNormalColumnImageAspect();
            HideLevelHost();
            ResetDescription();
        }

        private void EnsureDescriptionDefaults()
        {
            if (string.IsNullOrWhiteSpace(_normalDescription))
            {
                _normalDescription = DefaultNormalDesc;
            }

            if (string.IsNullOrWhiteSpace(_hardDescription))
            {
                _hardDescription = DefaultHardDesc;
            }

            if (string.IsNullOrWhiteSpace(_hellDescription))
            {
                _hellDescription = DefaultHellDesc;
            }
        }

        private void WireButtons()
        {
            if (_normalButton != null)
            {
                _normalButton.onClick.RemoveAllListeners();
                _normalButton.onClick.AddListener(HandleNormalClicked);
            }

            if (_hardButton != null)
            {
                _hardButton.onClick.RemoveAllListeners();
                _hardButton.onClick.AddListener(HandleLockedClicked);
            }

            if (_hellButton != null)
            {
                _hellButton.onClick.RemoveAllListeners();
                _hellButton.onClick.AddListener(HandleLockedClicked);
            }
        }

        private void WireHover()
        {
            AttachHover(_normalColumn, DifficultyKind.Normal);
            AttachHover(_hardColumn, DifficultyKind.Hard);
            AttachHover(_hellColumn, DifficultyKind.Hell);
        }

        private void AttachHover(RectTransform column, DifficultyKind kind)
        {
            if (column == null)
            {
                return;
            }

            var hover = column.GetComponent<DifficultyColumnHover>();
            if (hover == null)
            {
                hover = column.gameObject.AddComponent<DifficultyColumnHover>();
            }

            hover.Bind(this, kind);
        }

        private void ApplyNormalColumnImageAspect()
        {
            if (_normalColumn == null)
            {
                return;
            }

            var image = _normalColumn.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        private void HideLevelHost()
        {
            if (_normalLevelHost != null)
            {
                _normalLevelHost.gameObject.SetActive(false);
            }
        }

        public void ShowAllColumns()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            HideLevelHost();
            ResetDescription();
            ApplyLayout();
        }

        /// <summary>Backward-compatible alias — always shows three columns same-screen. </summary>
        public void ShowExpandedNormal()
        {
            ShowAllColumns();
        }

        /// <summary> Backward-compatible alias — always shows three columns same-screen. </summary>
        public void ShowCollapsed()
        {
            ShowAllColumns();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        internal void NotifyPointerEnter(DifficultyKind kind)
        {
            _hoveredKind = kind;
            SetDescription(DescriptionFor(kind));
        }

        internal void NotifyPointerExit(DifficultyKind kind)
        {
            if (_hoveredKind != kind)
            {
                return;
            }

            _hoveredKind = null;
            ResetDescription();
        }

        private void HandleNormalClicked()
        {
            DifficultySelected?.Invoke();
        }

        private void HandleLockedClicked()
        {
            LockedDifficultyClicked?.Invoke();
        }

        private void ApplyLayout()
        {
            RefreshColumnWidths();
        }

        private void LateUpdate()
        {
            if (!IsOpen || _columnsScroll == null)
            {
                return;
            }

            var viewport = _columnsScroll.viewport != null
                ? _columnsScroll.viewport
                : _columnsScroll.transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            var width = viewport.rect.width;
            if (Mathf.Abs(width - _lastViewportWidth) <= 0.5f)
            {
                return;
            }

            RefreshColumnWidths();
        }

        private void RefreshColumnWidths()
        {
            if (_columnsScroll == null || _columnsContent == null)
            {
                return;
            }

            var viewport = _columnsScroll.viewport != null
                ? _columnsScroll.viewport
                : _columnsScroll.transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            var viewportWidth = Mathf.Max(1f, viewport.rect.width);
            _lastViewportWidth = viewportWidth;
            var columnWidth = viewportWidth / 3f;
            SetColumnWidth(_normalColumn, columnWidth);
            SetColumnWidth(_hardColumn, columnWidth);
            SetColumnWidth(_hellColumn, columnWidth);

            _columnsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
            _columnsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewport.rect.height);

            if (_columnsScroll != null)
            {
                _columnsScroll.horizontalNormalizedPosition = 0f;
            }
        }

        private static void SetColumnWidth(RectTransform column, float width)
        {
            if (column == null)
            {
                return;
            }

            var le = column.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = column.gameObject.AddComponent<LayoutElement>();
            }

            le.minWidth = width;
            le.preferredWidth = width;
            le.flexibleWidth = 0f;
            column.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private void ResetDescription()
        {
            SetDescription(DefaultHint);
        }

        private void SetDescription(string text)
        {
            if (_descriptionText != null)
            {
                _descriptionText.text = text ?? string.Empty;
            }
        }

        private string DescriptionFor(DifficultyKind kind)
        {
            switch (kind)
            {
                case DifficultyKind.Hard:
                    return _hardDescription;
                case DifficultyKind.Hell:
                    return _hellDescription;
                default:
                    return _normalDescription;
            }
        }
    }

    /// <summary> Pointer hover bridge for a difficulty column. </summary>
    public sealed class DifficultyColumnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private DifficultySelectHostView _host;
        private DifficultySelectHostView.DifficultyKind _kind;

        public void Bind(DifficultySelectHostView host, DifficultySelectHostView.DifficultyKind kind)
        {
            _host = host;
            _kind = kind;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _host?.NotifyPointerEnter(_kind);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _host?.NotifyPointerExit(_kind);
        }
    }
}
