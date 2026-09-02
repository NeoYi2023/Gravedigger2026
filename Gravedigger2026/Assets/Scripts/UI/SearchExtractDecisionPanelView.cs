using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>UI-032 SearchExtract point decision: Continue Gather / Leave (SPEC_03 §3.6).</summary>
    public sealed class SearchExtractDecisionPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Text _titleLabel;

        public event Action ContinueClicked;
        public event Action LeaveClicked;

        public void Bind(GameObject root, Button continueButton, Button leaveButton, Text titleLabel)
        {
            _root = root;
            _continueButton = continueButton;
            _leaveButton = leaveButton;
            _titleLabel = titleLabel;
            WireButtons();
            Hide();
        }

        private void Awake()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueClicked);
                _continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (_leaveButton != null)
            {
                _leaveButton.onClick.RemoveListener(OnLeaveClicked);
                _leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnContinueClicked()
        {
            ContinueClicked?.Invoke();
        }

        private void OnLeaveClicked()
        {
            LeaveClicked?.Invoke();
        }

        public void Show(bool showContinue, int gatherOrder, int gatherCount)
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            gameObject.SetActive(true);

            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(showContinue);
            }

            if (_titleLabel != null)
            {
                _titleLabel.text = showContinue
                    ? $"搜集点 {gatherOrder}/{gatherCount} 完成"
                    : $"搜集点 {gatherOrder}/{gatherCount} 全部完成";
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
