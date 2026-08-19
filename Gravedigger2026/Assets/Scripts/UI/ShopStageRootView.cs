using System;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class ShopStageRootView : MonoBehaviour
    {
        public event Action Closed;

        private sealed class OfferSlotUi
        {
            public GameObject Root;
            public Button BuyButton;
            public Image IconImage;
            public Text NameText;
            public Text PriceText;
            public Text SoldText;
        }

        private ShopProgressService _progress;
        private ProtagonistEquipmentService _protagonistEquipment;
        private SpecialEquipSlotsService _magicBookSlots;
        private WarehouseService _warehouse;
        private ConfigCsvRepository _configs;
        private ShopOfferRefreshService _refreshService;
        private ShopPurchaseService _purchaseService;
        private ToastView _toastView;

        private Canvas _canvas;
        private OfferSlotUi[] _slotUis = new OfferSlotUi[6];
        private Text _spiritText;
        private Text _equipSummaryText;
        private Text _magicBookSummaryText;
        private Text _refreshPriceText;
        private Button _closeButton;
        private Button _refreshButton;
        private bool _uiBuilt;

        public void Bind(
            ShopProgressService progress,
            ProtagonistEquipmentService protagonistEquipment,
            SpecialEquipSlotsService magicBookSlots,
            WarehouseService warehouse,
            ConfigCsvRepository configs,
            ShopOfferRefreshService refreshService,
            ShopPurchaseService purchaseService,
            ToastView toastView)
        {
            _progress = progress;
            _protagonistEquipment = protagonistEquipment;
            _magicBookSlots = magicBookSlots;
            _warehouse = warehouse;
            _configs = configs;
            _refreshService = refreshService;
            _purchaseService = purchaseService;
            _toastView = toastView;
        }

        public void Open()
        {
            if (_progress == null)
            {
                Debug.LogError("[ShopStageRootView] Open() failed: progress is null.");
                return;
            }

            if (!_uiBuilt)
            {
                BuildUi();
            }

            // SS-04 规则：首次打开前应先消化 pending 解锁（由 TryAutoRefreshOnceIfPending 兜底只生成一次）。
            if (_configs != null && _refreshService != null)
            {
                try
                {
                    _refreshService.TryAutoRefreshOnceIfPending(_progress, _configs);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ShopStageRootView] Auto refresh pending failed: {e}");
                }
            }

            RefreshUi();
        }

        private void BuildUi()
        {
            _uiBuilt = true;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Backdrop (blocks click when open).
            var backdropGo = new GameObject("ShopBackdrop");
            backdropGo.transform.SetParent(transform, false);
            var backdropRect = backdropGo.AddComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            var backdropImage = backdropGo.AddComponent<Image>();
            backdropImage.color = new Color(0, 0, 0, 0.4f);
            backdropImage.raycastTarget = true;

            // Box panel.
            var boxGo = new GameObject("ShopBox");
            boxGo.transform.SetParent(transform, false);
            var boxRect = boxGo.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(980, 650);

            var boxImage = boxGo.AddComponent<Image>();
            boxImage.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
            boxImage.raycastTarget = true;

            // Close button.
            _closeButton = CreateButton(boxGo.transform, "CloseButton", new Vector2(0.5f, 0.5f), new Vector2(320, 305), new Vector2(90, 40), "关闭", font);
            _closeButton.onClick.AddListener(HandleCloseClicked);

            // Header.
            CreateText(boxGo.transform, "TitleText", new Vector2(0.5f, 0.5f), new Vector2(-220, 300), new Vector2(440, 32),
                "商店", font, 24, TextAnchor.MiddleLeft);

            // Left: player summary.
            CreateText(boxGo.transform, "PlayerTitleText", new Vector2(0.5f, 0.5f), new Vector2(-360, 240), new Vector2(260, 28),
                "玩家信息", font, 18, TextAnchor.MiddleLeft);

            _spiritText = CreateText(boxGo.transform, "SpiritText", new Vector2(0.5f, 0.5f), new Vector2(-360, 205), new Vector2(300, 28),
                "", font, 18, TextAnchor.MiddleLeft);
            _equipSummaryText = CreateText(boxGo.transform, "EquipSummaryText", new Vector2(0.5f, 0.5f), new Vector2(-360, 175), new Vector2(300, 28),
                "", font, 18, TextAnchor.MiddleLeft);
            _magicBookSummaryText = CreateText(boxGo.transform, "MagicBookSummaryText", new Vector2(0.5f, 0.5f), new Vector2(-360, 145), new Vector2(300, 28),
                "", font, 18, TextAnchor.MiddleLeft);

            // Right: offers title.
            CreateText(boxGo.transform, "OffersTitleText", new Vector2(0.5f, 0.5f), new Vector2(60, 240), new Vector2(260, 28),
                "待售商品", font, 18, TextAnchor.MiddleLeft);

            // Offer slots grid (6 items, 3 columns x 2 rows).
            const float slotW = 190f;
            const float slotH = 138f;
            const float gap = 14f;
            var gridStart = new Vector2(-50f, 150f); // box local coords

            for (var i = 0; i < 6; i++)
            {
                var row = i / 3; // 0,1
                var col = i % 3; // 0..2
                var slotX = gridStart.x + col * (slotW + gap);
                var slotY = gridStart.y - row * (slotH + gap);

                var slot = new GameObject($"OfferSlot_{i}");
                slot.transform.SetParent(boxGo.transform, false);
                var slotRect = slot.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = new Vector2(slotX, slotY);
                slotRect.sizeDelta = new Vector2(slotW, slotH);

                var slotBg = slot.AddComponent<Image>();
                slotBg.color = new Color(0.15f, 0.18f, 0.23f, 0.95f);
                slotBg.raycastTarget = true;

                // Buy button overlay.
                var buyButton = slot.AddComponent<Button>();
                buyButton.transition = Selectable.Transition.ColorTint;
                buyButton.targetGraphic = slotBg;

                // Icon.
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slot.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.6f);
                iconRect.anchorMax = new Vector2(0.35f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.preserveAspect = true;

                // Name.
                var nameText = CreateText(slot.transform, "NameText",
                    new Vector2(0.5f, 0.5f), new Vector2(10, 40), new Vector2(140, 24),
                    "", font, 16, TextAnchor.UpperLeft);

                // Price.
                var priceText = CreateText(slot.transform, "PriceText",
                    new Vector2(0.5f, 0.5f), new Vector2(10, 16), new Vector2(140, 24),
                    "", font, 16, TextAnchor.UpperLeft);

                // Sold label.
                var soldText = CreateText(slot.transform, "SoldText",
                    new Vector2(0.5f, 0.5f), new Vector2(0, -55), new Vector2(180, 24),
                    "已售", font, 18, TextAnchor.MiddleCenter);
                soldText.color = new Color(1f, 0.85f, 0.25f, 1f);

                _slotUis[i] = new OfferSlotUi
                {
                    Root = slot,
                    BuyButton = buyButton,
                    IconImage = iconImage,
                    NameText = nameText,
                    PriceText = priceText,
                    SoldText = soldText
                };

                var captureIndex = i;
                buyButton.onClick.AddListener(() => HandleBuyClicked(captureIndex));
            }

            // Refresh button.
            _refreshButton = CreateButton(boxGo.transform, "RefreshButton", new Vector2(0.5f, 0.5f), new Vector2(160, -260), new Vector2(260, 54),
                "刷新", font, 20);
            _refreshButton.onClick.AddListener(HandleRefreshClicked);

            _refreshPriceText = CreateText(boxGo.transform, "RefreshPriceText", new Vector2(0.5f, 0.5f), new Vector2(-40, -260),
                new Vector2(260, 54), "", font, 18, TextAnchor.MiddleLeft);
        }

        private void RefreshUi()
        {
            if (_progress == null) return;

            if (_warehouse != null)
            {
                // WarehouseService: 精魂总量。
                _spiritText.text = $"精魂：{_warehouse.SpiritEssence}";
            }
            else
            {
                _spiritText.text = "精魂：?";
            }

            if (_protagonistEquipment != null)
            {
                _equipSummaryText.text = $"装备：{_protagonistEquipment.OwnedEquips.Count} / 6";
            }
            else
            {
                _equipSummaryText.text = "装备：?";
            }

            if (_magicBookSlots != null)
            {
                var occupied = 0;
                for (var i = 0; i < SpecialEquipSlotsService.SlotCount; i++)
                {
                    if (!string.IsNullOrEmpty(_magicBookSlots.GetSlot(i)))
                    {
                        occupied++;
                    }
                }

                _magicBookSummaryText.text = $"魔法书槽：{occupied} / {SpecialEquipSlotsService.SlotCount}";
            }
            else
            {
                _magicBookSummaryText.text = "魔法书槽：?";
            }

            // Refresh button price + interactivity.
            if (_refreshService != null && _configs != null)
            {
                var nextRefreshCount = _progress.CurrentRefreshCount + 1;
                if (!_configs.TryGetShopRefreshPrice(nextRefreshCount, out var refreshRow) || refreshRow == null)
                {
                    _refreshButton.interactable = false;
                    _refreshPriceText.text = "下一次刷新：配置缺失";
                }
                else
                {
                    var price = refreshRow.RefreshPrice;
                    _refreshPriceText.text = $"下一次刷新：{price}";

                    var canRefresh = true;
                    if (price > 0 && _warehouse != null)
                    {
                        canRefresh = _warehouse.SpiritEssence >= price;
                    }

                    _refreshButton.interactable = canRefresh;
                }
            }
            else
            {
                _refreshButton.interactable = false;
                _refreshPriceText.text = "刷新：配置缺失";
            }

            for (var i = 0; i < 6; i++)
            {
                var slotUi = _slotUis[i];
                if (slotUi == null) continue;

                var offer = _progress.CurrentOffers[i];
                var isEmpty = string.IsNullOrEmpty(offer.ItemId);
                var isSold = offer.IsSold;

                slotUi.SoldText.gameObject.SetActive(isSold && !isEmpty);

                slotUi.BuyButton.interactable = !isEmpty && !isSold;

                var slotBg = slotUi.Root.GetComponent<Image>();
                if (slotBg != null)
                {
                    slotBg.color = offer.Category == ShopPoolItemCategory.A
                        ? new Color(0.15f, 0.18f, 0.23f, 0.95f)
                        : new Color(0.2f, 0.15f, 0.28f, 0.95f);
                }

                if (isEmpty)
                {
                    slotUi.IconImage.sprite = null;
                    slotUi.NameText.text = "-";
                    slotUi.PriceText.text = "";
                    continue;
                }

                _configs.TryGetItemCatalog(offer.ItemId, out var itemRow);

                slotUi.NameText.text = !string.IsNullOrEmpty(itemRow?.DisplayName) ? itemRow.DisplayName : offer.ItemId;
                slotUi.PriceText.text = offer.PriceSpirit > 0 ? $"价格：{offer.PriceSpirit}" : "价格：-";

                var iconAssetId = itemRow?.IconAssetId;
                if (!string.IsNullOrEmpty(iconAssetId))
                {
                    // 注意：这里假定 IconAssetId 为 Resources 路径（与项目既有约定保持一致）。
                    slotUi.IconImage.sprite = Resources.Load<Sprite>(iconAssetId);
                }
                else
                {
                    slotUi.IconImage.sprite = null;
                }
            }
        }

        private void HandleCloseClicked()
        {
            Closed?.Invoke();
            Destroy(gameObject);
        }

        private void HandleRefreshClicked()
        {
            if (_refreshService == null || _configs == null)
                return;

            var ok = false;
            try
            {
                ok = _refreshService.TryManualRefresh(_progress, _warehouse, _configs);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopStageRootView] Manual refresh failed: {e}");
            }

            if (!ok)
            {
                ShowToast("刷新失败（配置缺失或精魂不足）");
            }

            RefreshUi();
        }

        private void HandleBuyClicked(int slotIndex)
        {
            if (_purchaseService == null)
                return;

            var ok = false;
            var error = string.Empty;
            try
            {
                ok = _purchaseService.TryPurchase(_progress, slotIndex, out error);
            }
            catch (Exception e)
            {
                error = e.ToString();
                ok = false;
            }

            if (!ok)
            {
                ShowToast(string.IsNullOrEmpty(error) ? "购买失败" : error);
            }

            RefreshUi();
        }

        private void ShowToast(string message)
        {
            if (_toastView != null)
            {
                _toastView.Show(message);
                return;
            }

            Debug.LogWarning($"[ShopStageRootView] Toast: {message}");
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            string text,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = alignment;
            t.text = text;
            return t;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            string label,
            Font font,
            int fontSize = 18)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.35f, 0.5f, 0.95f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var txt = labelGo.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = label;

            return button;
        }
    }
}

