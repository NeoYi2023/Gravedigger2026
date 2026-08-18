using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.AutoManufacture;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-023 / D-068: InSaveShell MagicBook slots modal with shared BookRow + LMB drag TrySwap.
    /// </summary>
    public sealed class MagicBookSlotsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _bookRowHost;
        [SerializeField] private BookRowView _bookRow;

        private SpecialEquipSlotsService _equipSlots;
        private ConfigCsvRepository _configs;
        private bool _changedSubscribed;

        public event System.Action Closed;

        public Transform BookRowHost => _bookRowHost;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            EnsureBookRow();
        }

        private void OnEnable()
        {
            SubscribeChanged();
            RefreshBooks();
        }

        private void OnDisable()
        {
            UnsubscribeChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeChanged();
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Bind(SpecialEquipSlotsService equipSlots, ConfigCsvRepository configs)
        {
            UnsubscribeChanged();
            _equipSlots = equipSlots;
            _configs = configs;
            EnsureBookRow();
            if (_bookRow != null)
            {
                _bookRow.SetAllowReorder(true);
                _bookRow.Bind(_equipSlots, _configs);
            }

            if (isActiveAndEnabled)
            {
                SubscribeChanged();
                RefreshBooks();
            }
        }

        public void Show()
        {
            EnsureBookRow();
            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }

            RefreshBooks();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void EnsureBookRow()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_bookRowHost == null)
            {
                var box = transform.Find("Box");
                var host = box != null ? box.Find("BookRowHost") : null;
                if (host != null)
                {
                    _bookRowHost = host;
                }
            }

            if (_bookRow == null && _bookRowHost != null)
            {
                _bookRow = _bookRowHost.GetComponentInChildren<BookRowView>(true);
            }

            if (_bookRow == null && _bookRowHost != null)
            {
                _bookRow = BookRowView.CreateHierarchy(_bookRowHost);
                StretchToHost(_bookRow.GetComponent<RectTransform>());
            }

            if (_bookRow != null)
            {
                _bookRow.SetAllowReorder(true);
            }
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private void SubscribeChanged()
        {
            if (_changedSubscribed || _equipSlots == null)
            {
                return;
            }

            _equipSlots.Changed += HandleSlotsChanged;
            _changedSubscribed = true;
        }

        private void UnsubscribeChanged()
        {
            if (!_changedSubscribed || _equipSlots == null)
            {
                _changedSubscribed = false;
                return;
            }

            _equipSlots.Changed -= HandleSlotsChanged;
            _changedSubscribed = false;
        }

        private void HandleSlotsChanged()
        {
            RefreshBooks();
        }

        private void RefreshBooks()
        {
            EnsureBookRow();
            if (_bookRow == null)
            {
                return;
            }

            _bookRow.SetAllowReorder(true);
            if (_equipSlots != null || _configs != null)
            {
                _bookRow.Bind(_equipSlots, _configs);
            }
            else
            {
                _bookRow.Refresh();
            }
        }

        private static void StretchToHost(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(BookRowView.RowWidth, AutoMfgMagicBookSlotView.SlotHeight);
        }
    }
}
