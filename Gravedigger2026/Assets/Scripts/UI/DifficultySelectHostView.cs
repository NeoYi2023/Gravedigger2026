using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-029: InSaveShell difficulty host — collapsed three columns; Normal only expands (D-081).
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
        [SerializeField] private RectTransform _columnsRoot;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;
        [SerializeField] private Button _hellButton;
        [SerializeField] private Image _normalImage;
        [SerializeField] private Image _hardImage;
        [SerializeField] private Image _hellImage;
        [SerializeField] private Text _normalLabel;
        [SerializeField] private Text _hardLabel;
        [SerializeField] private Text _hellLabel;
        [SerializeField] private RectTransform _mapHost;
        [SerializeField] private GameObject _mapHostRoot;

        private bool _expandedNormal;
        private Color _hardDefault = new Color(0.85f, 0.75f, 0.35f, 1f);
        private Color _hellDefault = new Color(0.90f, 0.55f, 0.35f, 1f);

        public event Action LockedDifficultyClicked;

        public bool IsOpen => _root != null && _root.activeSelf;

        public RectTransform MapHost => _mapHost;

        public void BindRuntime(
            GameObject root,
            RectTransform columnsRoot,
            Button normalButton,
            Button hardButton,
            Button hellButton,
            Image normalImage,
            Image hardImage,
            Image hellImage,
            Text normalLabel,
            Text hardLabel,
            Text hellLabel,
            RectTransform mapHost,
            GameObject mapHostRoot)
        {
            _root = root;
            _columnsRoot = columnsRoot;
            _normalButton = normalButton;
            _hardButton = hardButton;
            _hellButton = hellButton;
            _normalImage = normalImage;
            _hardImage = hardImage;
            _hellImage = hellImage;
            _normalLabel = normalLabel;
            _hardLabel = hardLabel;
            _hellLabel = hellLabel;
            _mapHost = mapHost;
            _mapHostRoot = mapHostRoot;
            WireButtons();
            CacheDimDefaults();
        }

        private void Awake()
        {
            WireButtons();
            CacheDimDefaults();
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

        private void CacheDimDefaults()
        {
            if (_hardImage != null)
            {
                _hardDefault = _hardImage.color;
            }

            if (_hellImage != null)
            {
                _hellDefault = _hellImage.color;
            }
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
            if (_mapHostRoot != null)
            {
                _mapHostRoot.SetActive(_expandedNormal);
            }

            SetColumnLayout(_normalButton != null ? _normalButton.transform as RectTransform : null, 0f, _expandedNormal ? 0.18f : 0.333f);
            SetColumnLayout(_hardButton != null ? _hardButton.transform as RectTransform : null, _expandedNormal ? 0.82f : 0.333f, _expandedNormal ? 0.91f : 0.666f);
            SetColumnLayout(_hellButton != null ? _hellButton.transform as RectTransform : null, _expandedNormal ? 0.91f : 0.666f, 1f);

            if (_mapHost != null)
            {
                _mapHost.anchorMin = new Vector2(0.18f, 0f);
                _mapHost.anchorMax = new Vector2(0.82f, 1f);
                _mapHost.offsetMin = Vector2.zero;
                _mapHost.offsetMax = Vector2.zero;
                _mapHost.gameObject.SetActive(_expandedNormal);
            }

            if (_hardImage != null)
            {
                _hardImage.color = _expandedNormal ? Dim(_hardDefault) : _hardDefault;
            }

            if (_hellImage != null)
            {
                _hellImage.color = _expandedNormal ? Dim(_hellDefault) : _hellDefault;
            }
        }

        private static Color Dim(Color c)
        {
            return new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, c.a);
        }

        private static void SetColumnLayout(RectTransform rt, float anchorMinX, float anchorMaxX)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(anchorMinX, 0f);
            rt.anchorMax = new Vector2(anchorMaxX, 1f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
        }
    }
}
