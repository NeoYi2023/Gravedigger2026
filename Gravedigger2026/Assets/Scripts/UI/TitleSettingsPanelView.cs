using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-028 Title settings modal with Display tab (SPEC_03 §3.6).
    /// </summary>
    public sealed class TitleSettingsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _displayTabButton;
        [SerializeField] private DisplaySettingsTabView _displayTab;
        [SerializeField] private Button _closeButton;

        public event Action Closed;
        public event Action Applied;

        public DisplaySettingsTabView DisplayTab => _displayTab;

        public void BindRuntime(
            GameObject root,
            Button displayTabButton,
            DisplaySettingsTabView displayTab,
            Button closeButton)
        {
            if (_displayTab != null)
            {
                _displayTab.Applied -= HandleDisplayApplied;
            }

            _root = root;
            _displayTabButton = displayTabButton;
            _displayTab = displayTab;
            _closeButton = closeButton;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_displayTabButton != null)
            {
                _displayTabButton.onClick.RemoveListener(ShowDisplayTab);
                _displayTabButton.onClick.AddListener(ShowDisplayTab);
            }

            if (_displayTab != null)
            {
                _displayTab.Applied -= HandleDisplayApplied;
                _displayTab.Applied += HandleDisplayApplied;
            }
        }

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_displayTabButton != null)
            {
                _displayTabButton.onClick.AddListener(ShowDisplayTab);
            }

            if (_displayTab != null)
            {
                _displayTab.Applied += HandleDisplayApplied;
            }
        }

        private void OnDestroy()
        {
            if (_displayTab != null)
            {
                _displayTab.Applied -= HandleDisplayApplied;
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            ShowDisplayTab();
            _displayTab?.RefreshFromService();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void ShowDisplayTab()
        {
            _displayTab?.Show();
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private void HandleDisplayApplied()
        {
            Applied?.Invoke();
        }
    }
}
