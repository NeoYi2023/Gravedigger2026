using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Upgrade + Complete + Formation entry (SPEC_03 UI-010 / D-030 / D-032).
    /// </summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _inject100Button;
        [SerializeField] private Button _inject500Button;
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _formationButton;

        public event Action Inject100Requested;
        public event Action Inject500Requested;
        public event Action CompleteRequested;
        public event Action FormationRequested;

        private void OnEnable()
        {
            if (_inject100Button != null)
            {
                _inject100Button.onClick.AddListener(HandleInject100);
            }

            if (_inject500Button != null)
            {
                _inject500Button.onClick.AddListener(HandleInject500);
            }

            if (_completeButton != null)
            {
                _completeButton.onClick.AddListener(HandleComplete);
            }

            if (_formationButton != null)
            {
                _formationButton.onClick.AddListener(HandleFormation);
            }
        }

        private void OnDisable()
        {
            if (_inject100Button != null)
            {
                _inject100Button.onClick.RemoveListener(HandleInject100);
            }

            if (_inject500Button != null)
            {
                _inject500Button.onClick.RemoveListener(HandleInject500);
            }

            if (_completeButton != null)
            {
                _completeButton.onClick.RemoveListener(HandleComplete);
            }

            if (_formationButton != null)
            {
                _formationButton.onClick.RemoveListener(HandleFormation);
            }
        }

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

        public void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text ?? string.Empty;
            }
        }

        private void HandleInject100()
        {
            Inject100Requested?.Invoke();
        }

        private void HandleInject500()
        {
            Inject500Requested?.Invoke();
        }

        private void HandleComplete()
        {
            CompleteRequested?.Invoke();
        }

        private void HandleFormation()
        {
            FormationRequested?.Invoke();
        }
    }
}
