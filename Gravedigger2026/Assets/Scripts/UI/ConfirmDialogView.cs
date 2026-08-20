using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class ConfirmDialogView : MonoBehaviour
    {
        private const int ConfirmDialogSortingOrder = 110;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(HandleCancel);
            }
        }

        public void Show(string message, Action onConfirm, Action onCancel = null)
        {
            Show(message, onConfirm, onCancel, ConfirmDialogSortingOrder);
        }

        public void Show(string message, Action onConfirm, Action onCancel, int sortingOrder)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                ApplyModalSorting(_root, sortingOrder);
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
            _onCancel = null;
        }

        private static void ApplyModalSorting(GameObject root, int sortingOrder = ConfirmDialogSortingOrder)
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }
        }

        private void HandleConfirm()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void HandleCancel()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
