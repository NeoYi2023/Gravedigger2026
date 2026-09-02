using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-029: InSaveShell difficulty host — equal-width columns in a horizontal scroll;
    /// Normal centers with in-column LevelSelect (D-081). No MapHost; no sibling shrink/dim.
    /// </summary>
    public sealed class DifficultySelectHostView : MonoBehaviour
    {
        public enum DifficultyKind
        {
            Normal = 0,
            Hard = 1,
            Hell = 2
        }

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

        private bool _expandedNormal;
        private float _lastViewportWidth = -1f;

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
            Text hellLabel)
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
            WireButtons();
            ApplyNormalColumnImageAspect();
        }

        private void Awake()
        {
            WireButtons();
            ApplyNormalColumnImageAspect();
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

        public void ShowExpandedNormal()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            _expandedNormal = true;
            ApplyLayout();
        }

        public void ShowCollapsed()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            _expandedNormal = false;
            ApplyLayout();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void HandleNormalClicked()
        {
            _expandedNormal = true;
            ApplyLayout();
        }

        private void HandleLockedClicked()
        {
            LockedDifficultyClicked?.Invoke();
        }

        private void ApplyLayout()
        {
            RefreshColumnWidths();
            if (_normalLevelHost != null)
            {
                _normalLevelHost.gameObject.SetActive(_expandedNormal);
            }

            if (_expandedNormal)
            {
                ScrollToColumn(_normalColumn);
            }
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
            if (_expandedNormal)
            {
                ScrollToColumn(_normalColumn);
            }
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

            var columnWidth = Mathf.Max(1f, viewport.rect.width);
            _lastViewportWidth = columnWidth;
            SetColumnWidth(_normalColumn, columnWidth);
            SetColumnWidth(_hardColumn, columnWidth);
            SetColumnWidth(_hellColumn, columnWidth);

            var totalWidth = columnWidth * 3f;
            _columnsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
            _columnsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewport.rect.height);
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

        private void ScrollToColumn(RectTransform column)
        {
            if (_columnsScroll == null || _columnsContent == null || column == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_columnsContent);

            var childCount = _columnsContent.childCount;
            if (childCount <= 1)
            {
                _columnsScroll.horizontalNormalizedPosition = 0f;
                return;
            }

            var index = column.GetSiblingIndex();
            _columnsScroll.horizontalNormalizedPosition =
                Mathf.Clamp01(index / (float)(childCount - 1));
        }
    }
}
