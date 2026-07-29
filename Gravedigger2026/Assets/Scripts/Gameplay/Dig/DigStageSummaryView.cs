using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    public sealed class DigStageSummaryView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _confirmButton;

        private Action _onConfirm;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(HandleConfirm);
            }
        }

        public void Show(string body, Action onConfirm)
        {
            _onConfirm = onConfirm;
            if (_bodyText != null)
            {
                _bodyText.text = body ?? string.Empty;
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

            _onConfirm = null;
        }

        private void HandleConfirm()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }
    }
}
