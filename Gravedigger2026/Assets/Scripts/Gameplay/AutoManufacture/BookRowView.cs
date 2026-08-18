using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Shared 6-slot MagicBook row (UI-016 / UI-023). Nested by AM presentation and MagicBookSlotsPanel.
    /// </summary>
    public sealed class BookRowView : MonoBehaviour
    {
        public const float SlotSpacing = 12f;

        [SerializeField] private AutoMfgMagicBookSlotView[] _slots =
            new AutoMfgMagicBookSlotView[SpecialEquipSlotsService.SlotCount];
        [SerializeField] private bool _allowReorder;

        private SpecialEquipSlotsService _equipSlots;
        private ConfigCsvRepository _configs;

        public AutoMfgMagicBookSlotView[] Slots => _slots;

        public bool AllowReorder => _allowReorder;

        public bool HasWiredSlots =>
            _slots != null && _slots.Length == SpecialEquipSlotsService.SlotCount;

        public static float RowWidth =>
            SpecialEquipSlotsService.SlotCount * AutoMfgMagicBookSlotView.SlotWidth
            + (SpecialEquipSlotsService.SlotCount - 1) * SlotSpacing;

        public void RuntimeWire(AutoMfgMagicBookSlotView[] slots, bool allowReorder)
        {
            _slots = slots;
            _allowReorder = allowReorder;
            ApplyReorderHandlers();
        }

        public void SetAllowReorder(bool allow)
        {
            _allowReorder = allow;
            ApplyReorderHandlers();
        }

        public void Bind(SpecialEquipSlotsService equipSlots, ConfigCsvRepository configs)
        {
            _equipSlots = equipSlots;
            _configs = configs;
            EnsureSlotArray();
            ApplyReorderHandlers();
            Refresh();
        }

        public void Refresh()
        {
            EnsureSlotArray();
            if (_slots == null)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                BindSlot(i);
            }
        }

        public bool TryReorder(int indexA, int indexB)
        {
            if (_equipSlots == null)
            {
                return false;
            }

            return _equipSlots.TrySwap(indexA, indexB, out _);
        }

        public static BookRowView CreateHierarchy(Transform parent)
        {
            var go = new GameObject("BookRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(RowWidth, AutoMfgMagicBookSlotView.SlotHeight);

            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = SlotSpacing;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;

            var slots = new AutoMfgMagicBookSlotView[SpecialEquipSlotsService.SlotCount];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = CreateBookSlot(go.transform, i);
            }

            var view = go.AddComponent<BookRowView>();
            view.RuntimeWire(slots, allowReorder: false);
            return view;
        }

        private void BindSlot(int index)
        {
            var view = _slots[index];
            if (view == null)
            {
                return;
            }

            view.ResetScale();
            var bookId = _equipSlots != null ? _equipSlots.GetSlot(index) : string.Empty;
            if (string.IsNullOrEmpty(bookId) || _configs == null || !_configs.TryGetMagicBook(bookId, out var row))
            {
                view.BindEmpty();
                return;
            }

            Sprite icon = null;
            if (!string.IsNullOrEmpty(row.IconAssetId))
            {
                icon = Resources.Load<Sprite>(row.IconAssetId);
            }

            var name = !string.IsNullOrEmpty(row.DisplayName) ? row.DisplayName : bookId;
            view.BindBook(name, icon);
        }

        private void EnsureSlotArray()
        {
            if (_slots != null && _slots.Length == SpecialEquipSlotsService.SlotCount)
            {
                return;
            }

            var found = GetComponentsInChildren<AutoMfgMagicBookSlotView>(true);
            if (found == null || found.Length == 0)
            {
                return;
            }

            _slots = new AutoMfgMagicBookSlotView[SpecialEquipSlotsService.SlotCount];
            for (var i = 0; i < found.Length && i < _slots.Length; i++)
            {
                _slots[i] = found[i];
            }
        }

        private void ApplyReorderHandlers()
        {
            EnsureSlotArray();
            if (_slots == null)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null)
                {
                    continue;
                }

                var handler = slot.GetComponent<MagicBookSlotDragHandler>();
                var bg = slot.GetComponent<Image>();
                if (_allowReorder)
                {
                    if (handler == null)
                    {
                        handler = slot.gameObject.AddComponent<MagicBookSlotDragHandler>();
                    }

                    handler.enabled = true;
                    handler.Wire(this, i);
                    if (bg != null)
                    {
                        bg.raycastTarget = true;
                    }
                }
                else
                {
                    if (handler != null)
                    {
                        handler.enabled = false;
                    }

                    if (bg != null)
                    {
                        bg.raycastTarget = false;
                    }
                }
            }
        }

        private static AutoMfgMagicBookSlotView CreateBookSlot(Transform parent, int index)
        {
            var go = new GameObject("BookSlot_" + index, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.28f, 0.3f, 0.4f, 0.95f);
            bg.raycastTarget = true;

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = AutoMfgMagicBookSlotView.SlotWidth;
            le.preferredHeight = AutoMfgMagicBookSlotView.SlotHeight;
            le.minWidth = AutoMfgMagicBookSlotView.SlotWidth;
            le.minHeight = AutoMfgMagicBookSlotView.SlotHeight;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.35f);
            iconRt.anchorMax = new Vector2(0.85f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var icon = iconGo.GetComponent<Image>();
            icon.enabled = false;
            icon.raycastTarget = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(go.transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0.02f);
            nameRt.anchorMax = new Vector2(0.95f, 0.32f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var name = nameGo.GetComponent<Text>();
            name.text = string.Empty;
            name.fontSize = 18;
            name.alignment = TextAnchor.LowerCenter;
            name.color = Color.white;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Overflow;
            name.raycastTarget = false;
            name.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameGo.SetActive(false);

            var view = go.AddComponent<AutoMfgMagicBookSlotView>();
            view.RuntimeWire(bg, icon, name);
            return view;
        }
    }
}
