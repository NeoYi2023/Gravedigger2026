using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-027: Boot title / login menu (SPEC_03 §3.6).
    /// </summary>
    public sealed class TitleMenuView : MonoBehaviour
    {
        private const string LabelStart = "开始游戏";
        private const string LabelContinue = "继续游戏";

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Text _primaryButtonLabel;
        [SerializeField] private Button _loadSaveButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Text _versionText;

        public event Action PrimaryClicked;
        public event Action LoadSaveClicked;
        public event Action SettingsClicked;
        public event Action CreditsClicked;

        private void Awake()
        {
            if (_primaryButton != null)
            {
                _primaryButton.onClick.AddListener(() => PrimaryClicked?.Invoke());
            }

            if (_loadSaveButton != null)
            {
                _loadSaveButton.onClick.AddListener(() => LoadSaveClicked?.Invoke());
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            }

            if (_creditsButton != null)
            {
                _creditsButton.onClick.AddListener(() => CreditsClicked?.Invoke());
            }
        }

        public void Show(bool hasAnySave)
        {
            if (_primaryButtonLabel != null)
            {
                _primaryButtonLabel.text = hasAnySave ? LabelContinue : LabelStart;
            }

            if (_versionText != null)
            {
                _versionText.text = $"版本 v{Application.version}";
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
        }

        public bool IsVisible => _root != null && _root.activeSelf;
    }
}
