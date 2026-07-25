using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class InSaveShellView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _backdropImage;
        [SerializeField] private Text _slotLabel;
        [SerializeField] private Button _toolsButton;
        [SerializeField] private Button _backToSaveSelectButton;
        [SerializeField] private Button _debugCycleStateButton;
        [SerializeField] private Button _debugAdvanceStageButton;
        [SerializeField] private ToolsPanelView _toolsPanel;
        [SerializeField] private GameplayStatePlaceholderView _placeholderView;

        private Color _backdropDefault = new Color(0.10f, 0.12f, 0.16f, 0.96f);

        public event Action ToolsToggleRequested;
        public event Action BackToSaveSelectRequested;
        public event Action DebugCycleStateRequested;
        public event Action DebugAdvanceStageRequested;
        public event Action SettingsRequested;
        public event Action LevelRequested;

        private void Awake()
        {
            if (_backdropImage != null)
            {
                _backdropDefault = _backdropImage.color;
            }

            if (_toolsButton != null)
            {
                _toolsButton.onClick.AddListener(() => ToolsToggleRequested?.Invoke());
            }

            if (_backToSaveSelectButton != null)
            {
                _backToSaveSelectButton.onClick.AddListener(() => BackToSaveSelectRequested?.Invoke());
            }

            if (_debugCycleStateButton != null)
            {
                _debugCycleStateButton.onClick.AddListener(() => DebugCycleStateRequested?.Invoke());
            }

            if (_debugAdvanceStageButton != null)
            {
                _debugAdvanceStageButton.onClick.AddListener(() => DebugAdvanceStageRequested?.Invoke());
            }

            if (_toolsPanel != null)
            {
                _toolsPanel.SettingsClicked += () => SettingsRequested?.Invoke();
                _toolsPanel.LevelClicked += () => LevelRequested?.Invoke();
            }
        }

        public void Show(int slotIndex)
        {
            if (_slotLabel != null)
            {
                _slotLabel.text = $"进档壳 — 槽 {slotIndex + 1}";
            }

            SetShellBackdropVisible(true);

            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.Hide();
            }

            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void ToggleToolsPanel()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.Toggle();
            }
        }

        public void HideToolsPanel()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.Hide();
            }
        }

        public void SetModePanelsSuppressed(bool suppressed)
        {
            if (_placeholderView != null)
            {
                _placeholderView.SetModePanelsSuppressed(suppressed);
            }

            // Dig camera needs a clear view through the shell panel.
            SetShellBackdropVisible(!suppressed);
        }

        public void SetShellBackdropVisible(bool visible)
        {
            if (_backdropImage == null)
            {
                return;
            }

            var c = _backdropDefault;
            if (!visible)
            {
                c.a = 0f;
            }

            _backdropImage.color = c;
        }

        public void ShowGameplayState(GameplayState state)
        {
            if (_placeholderView != null)
            {
                _placeholderView.ShowState(state);
            }
        }

        public void ShowStageInfo(LevelStageContext context)
        {
            if (_placeholderView != null)
            {
                _placeholderView.ShowStageInfo(context);
            }
        }
    }
}
