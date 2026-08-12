using System;
using Gravedigger2026.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-014: pick CampaignMode Mode1/Mode2 or cancel before enter-save (SPEC_03 §3.6).
    /// </summary>
    public sealed class CampaignModeSelectView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _mode1Button;
        [SerializeField] private Button _mode2Button;
        [SerializeField] private Button _cancelButton;

        private Action<CampaignMode> _onPicked;
        private Action _onCancel;

        private void Awake()
        {
            if (_mode1Button != null)
            {
                _mode1Button.onClick.AddListener(() => HandlePick(CampaignMode.Mode1));
            }

            if (_mode2Button != null)
            {
                _mode2Button.onClick.AddListener(() => HandlePick(CampaignMode.Mode2));
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(HandleCancel);
            }
        }

        public void Show(string message, Action<CampaignMode> onPicked, Action onCancel = null)
        {
            _onPicked = onPicked;
            _onCancel = onCancel;

            if (_messageText != null)
            {
                _messageText.text = message;
            }

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

            _onPicked = null;
            _onCancel = null;
        }

        private void HandlePick(CampaignMode mode)
        {
            var cb = _onPicked;
            Hide();
            cb?.Invoke(mode);
        }

        private void HandleCancel()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
