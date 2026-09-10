using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>UI-032 SearchExtract point decision: Continue Gather / Leave (SPEC_03 §3.19).</summary>
    public sealed class SearchExtractDecisionPanelView : MonoBehaviour
    {
        private const string DefaultLeaveLabel = "离开";

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Text _titleLabel;

        private Text _leaveLabel;
        private string _leaveBaseText = DefaultLeaveLabel;

        public event Action ContinueClicked;
        public event Action LeaveClicked;

        public void Bind(GameObject root, Button continueButton, Button leaveButton, Text titleLabel)
        {
            _root = root;
            _continueButton = continueButton;
            _leaveButton = leaveButton;
            _titleLabel = titleLabel;
            CacheLeaveLabel();
            WireButtons();
            Hide();
        }

        private void Awake()
        {
            CacheLeaveLabel();
            WireButtons();
        }

        private void CacheLeaveLabel()
        {
            if (_leaveButton == null)
            {
                return;
            }

            _leaveLabel = _leaveButton.GetComponentInChildren<Text>(true);
            if (_leaveLabel != null && !string.IsNullOrWhiteSpace(_leaveLabel.text))
            {
                // Strip any leftover countdown suffix when caching base.
                var text = _leaveLabel.text;
                var paren = text.IndexOf(" (", StringComparison.Ordinal);
                _leaveBaseText = paren > 0 ? text.Substring(0, paren) : text;
            }
            else
            {
                _leaveBaseText = DefaultLeaveLabel;
            }
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

            SetLeaveCountdownSeconds(null);
        }

        /// <summary>
        /// Leave button label: null restores base text; otherwise「离开 (N)」.
        /// </summary>
        public void SetLeaveCountdownSeconds(int? seconds)
        {
            if (_leaveLabel == null)
            {
                CacheLeaveLabel();
            }

            if (_leaveLabel == null)
            {
                return;
            }

            if (seconds == null || seconds.Value < 1)
            {
                _leaveLabel.text = _leaveBaseText;
                return;
            }

            _leaveLabel.text = $"{_leaveBaseText} ({seconds.Value})";
        }

        public void Hide()
        {
            SetLeaveCountdownSeconds(null);

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
