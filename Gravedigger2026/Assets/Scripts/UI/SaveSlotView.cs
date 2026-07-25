using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class SaveSlotView : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Text _primaryButtonLabel;

        public int SlotIndex => _slotIndex;

        public event Action<int> PrimaryClicked;
        public event Action<int> DeleteClicked;

        private void Awake()
        {
            if (_primaryButton != null)
            {
                _primaryButton.onClick.AddListener(() => PrimaryClicked?.Invoke(_slotIndex));
            }

            if (_deleteButton != null)
            {
                _deleteButton.onClick.AddListener(() => DeleteClicked?.Invoke(_slotIndex));
            }
        }

        public void BindIndex(int slotIndex)
        {
            _slotIndex = slotIndex;
            if (_titleText != null)
            {
                _titleText.text = $"存档槽 {slotIndex + 1}";
            }
        }

        public void Refresh(bool occupied)
        {
            if (_statusText != null)
            {
                _statusText.text = occupied ? "状态：已占用" : "状态：空";
            }

            if (_primaryButtonLabel != null)
            {
                _primaryButtonLabel.text = occupied ? "进入" : "新建";
            }

            if (_deleteButton != null)
            {
                _deleteButton.gameObject.SetActive(occupied);
            }
        }
    }
}
