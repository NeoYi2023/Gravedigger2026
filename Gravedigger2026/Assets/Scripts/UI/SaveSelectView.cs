using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class SaveSelectView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private SaveSlotView[] _slotViews;
        [SerializeField] private Button _backButton;

        public event Action<int> CreateRequested;
        public event Action<int> EnterRequested;
        public event Action<int> DeleteRequested;
        public event Action BackRequested;

        private Func<int, bool> _isOccupied;

        private void Awake()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(() => BackRequested?.Invoke());
            }

            if (_slotViews == null)
            {
                return;
            }

            for (var i = 0; i < _slotViews.Length; i++)
            {
                var view = _slotViews[i];
                if (view == null)
                {
                    continue;
                }

                view.BindIndex(i);
                view.PrimaryClicked += HandlePrimary;
                view.DeleteClicked += HandleDelete;
            }
        }

        public void SetOccupiedQuery(Func<int, bool> isOccupied)
        {
            _isOccupied = isOccupied;
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            RefreshAll();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void RefreshAll()
        {
            if (_slotViews == null || _isOccupied == null)
            {
                return;
            }

            foreach (var view in _slotViews)
            {
                if (view == null)
                {
                    continue;
                }

                view.Refresh(_isOccupied(view.SlotIndex));
            }
        }

        private void HandlePrimary(int slotIndex)
        {
            if (_isOccupied == null)
            {
                return;
            }

            if (_isOccupied(slotIndex))
            {
                EnterRequested?.Invoke(slotIndex);
            }
            else
            {
                CreateRequested?.Invoke(slotIndex);
            }
        }

        private void HandleDelete(int slotIndex)
        {
            DeleteRequested?.Invoke(slotIndex);
        }
    }
}
