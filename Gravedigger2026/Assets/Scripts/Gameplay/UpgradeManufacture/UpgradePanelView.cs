using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Main shell: Complete / Formation + GM Upgrade Modal (SPEC_03 UI-010).
    /// </summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _upgradeModal;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _gmUpgradeButton;
        [SerializeField] private Button _closeModalButton;
        [SerializeField] private Button _inject100Button;
        [SerializeField] private Button _inject500Button;
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _formationButton;

        public event Action Inject100Requested;
        public event Action Inject500Requested;
        public event Action CompleteRequested;
        public event Action FormationRequested;
        public event Action OpenUpgradeModalRequested;
        public event Action CloseUpgradeModalRequested;

        private void OnEnable()
        {
            Wire(_gmUpgradeButton, HandleOpenModal);
            Wire(_closeModalButton, HandleCloseModal);
            Wire(_inject100Button, HandleInject100);
            Wire(_inject500Button, HandleInject500);
            Wire(_completeButton, HandleComplete);
            Wire(_formationButton, HandleFormation);
        }

        private void OnDisable()
        {
            Unwire(_gmUpgradeButton, HandleOpenModal);
            Unwire(_closeModalButton, HandleCloseModal);
            Unwire(_inject100Button, HandleInject100);
            Unwire(_inject500Button, HandleInject500);
            Unwire(_completeButton, HandleComplete);
            Unwire(_formationButton, HandleFormation);
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            HideUpgradeModal();
        }

        public void Hide()
        {
            HideUpgradeModal();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void ShowUpgradeModal()
        {
            if (_upgradeModal != null)
            {
                _upgradeModal.SetActive(true);
            }
        }

        public void HideUpgradeModal()
        {
            if (_upgradeModal != null)
            {
                _upgradeModal.SetActive(false);
            }
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text ?? string.Empty;
            }
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
            {
                button.onClick.AddListener(handler);
            }
        }

        private static void Unwire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(handler);
            }
        }

        private void HandleOpenModal()
        {
            OpenUpgradeModalRequested?.Invoke();
        }

        private void HandleCloseModal()
        {
            CloseUpgradeModalRequested?.Invoke();
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
