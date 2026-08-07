using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// One soldier cell in PoolPanel: summary + optional Remake button when selected.
    /// </summary>
    public sealed class PoolSoldierFrameView : MonoBehaviour
    {
        [SerializeField] private Button _frameButton;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Button _remakeButton;
        [SerializeField] private Image _background;

        private string _warriorId;
        private bool _canRemake;
        private Color _normalColor = new Color(0.2f, 0.24f, 0.22f, 0.95f);
        private Color _selectedColor = new Color(0.28f, 0.38f, 0.3f, 0.98f);
        private bool _wired;

        public string WarriorId => _warriorId;

        public event Action<string> Selected;
        public event Action<string> RemakeRequested;

        private void Awake()
        {
            // Instantiate copies _wired=true from template; always re-bind on the clone.
            _wired = false;
            EnsureWired();
        }

        public void RuntimeWire(Button frameButton, Text summaryText, Button remakeButton, Image background)
        {
            _frameButton = frameButton;
            _summaryText = summaryText;
            _remakeButton = remakeButton;
            _background = background;
            EnsureWired();
        }

        private void EnsureWired()
        {
            if (_wired)
            {
                return;
            }

            if (_frameButton != null)
            {
                _frameButton.onClick.AddListener(HandleFrameClicked);
            }

            if (_remakeButton != null)
            {
                _remakeButton.onClick.AddListener(HandleRemakeClicked);
                _remakeButton.gameObject.SetActive(false);
            }

            _wired = true;
        }

        private void OnDestroy()
        {
            if (_frameButton != null)
            {
                _frameButton.onClick.RemoveListener(HandleFrameClicked);
            }

            if (_remakeButton != null)
            {
                _remakeButton.onClick.RemoveListener(HandleRemakeClicked);
            }
        }

        public void Bind(string warriorId, string summary, bool canRemake)
        {
            EnsureWired();
            _warriorId = warriorId ?? string.Empty;
            _canRemake = canRemake;
            if (_summaryText != null)
            {
                _summaryText.text = summary ?? string.Empty;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_background != null)
            {
                _background.color = selected ? _selectedColor : _normalColor;
            }

            if (_remakeButton != null)
            {
                _remakeButton.gameObject.SetActive(selected && _canRemake);
            }
        }

        private void HandleFrameClicked()
        {
            if (string.IsNullOrEmpty(_warriorId))
            {
                return;
            }

            Selected?.Invoke(_warriorId);
        }

        private void HandleRemakeClicked()
        {
            if (string.IsNullOrEmpty(_warriorId) || !_canRemake)
            {
                return;
            }

            RemakeRequested?.Invoke(_warriorId);
        }
    }
}
