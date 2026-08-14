using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>UI-018: PushMap reward popup — already-credited Exp + CaptureLoot; Continue.</summary>
    public sealed class PushMapRewardPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _continueButton;

        private Action _onContinue;

        public void Bind(GameObject root, Text titleText, Text bodyText, Button continueButton)
        {
            _root = root;
            _titleText = titleText;
            _bodyText = bodyText;
            _continueButton = continueButton;
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinue);
                _continueButton.onClick.AddListener(HandleContinue);
            }
        }

        private void Awake()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinue);
                _continueButton.onClick.AddListener(HandleContinue);
            }
        }

        public void Show(string body, Action onContinue)
        {
            _onContinue = onContinue;
            if (_titleText != null)
            {
                _titleText.text = "奖励";
            }

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

            _onContinue = null;
        }

        private void HandleContinue()
        {
            var cb = _onContinue;
            Hide();
            cb?.Invoke();
        }
    }
}
