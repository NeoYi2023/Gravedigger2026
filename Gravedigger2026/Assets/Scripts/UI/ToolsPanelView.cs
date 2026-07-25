using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class ToolsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _levelButton;
        [SerializeField] private Button _closeButton;

        public event Action SettingsClicked;
        public event Action LevelClicked;

        private void Awake()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            }

            if (_levelButton != null)
            {
                _levelButton.onClick.AddListener(() => LevelClicked?.Invoke());
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }
}
