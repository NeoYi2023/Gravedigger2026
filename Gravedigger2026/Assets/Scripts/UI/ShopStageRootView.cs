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
    /// <summary>
    /// Full-screen Shop UI (UI-026 / D-075). Prefab-authored; BuildUi is the Editor/fallback hierarchy.
    /// </summary>
    public sealed class ShopStageRootView : MonoBehaviour
    {
        public event Action Closed;

        [Serializable]
        private sealed class OfferSlotUi
        {
            public GameObject Root;
            public Button BuyButton;
            public Image IconImage;
            public Text NameText;
            public Text PriceText;
            public Text SoldText;
        }

        [Serializable]
        private sealed class OwnedIconSlotUi
        {
            public GameObject Root;
            public Button Button;
            public Image IconImage;
        }

        private enum OwnedSelectionKind
        {
            None,
            Equipment,
            MagicBook
        }

        private const float OwnedIconSize = 64f;
        private const float OwnedIconGap = 8f;
        private const int ShopConfirmSortingOrder = 201;
        private const int MaxOwnedEquipIcons = 6;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _spiritText;
        [SerializeField] private Text _equipSummaryText;
        [SerializeField] private Text _magicBookSummaryText;
        [SerializeField] private Text _refreshPriceText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private OfferSlotUi[] _slotUis = new OfferSlotUi[6];
        [SerializeField] private OwnedIconSlotUi[] _equipIconUis = new OwnedIconSlotUi[MaxOwnedEquipIcons];
        [SerializeField] private OwnedIconSlotUi[] _magicBookIconUis =
            new OwnedIconSlotUi[SpecialEquipSlotsService.SlotCount];
        [SerializeField] private RectTransform _sellPopoverRect;
        [SerializeField] private Button _sellButton;
        [SerializeField] private Text _sellPriceText;

        private ShopProgressService _progress;
        private ProtagonistEquipmentService _protagonistEquipment;
        private SpecialEquipSlotsService _magicBookSlots;
        private WarehouseService _warehouse;
        private ConfigCsvRepository _configs;
        private ShopOfferRefreshService _refreshService;
        private ShopPurchaseService _purchaseService;
        private ShopSellService _sellService;
        private ConfirmDialogView _confirmDialog;
        private ToastView _toastView;
        private bool _listenersWired;
        private bool _inventoryChangedSubscribed;
        private OwnedSelectionKind _selectionKind = OwnedSelectionKind.None;
        private string _selectedEquipId;
        private int _selectedMagicSlot = -1;
        private RectTransform _selectedIconRect;

        public void Bind(
            ShopProgressService progress,
            ProtagonistEquipmentService protagonistEquipment,
            SpecialEquipSlotsService magicBookSlots,
            WarehouseService warehouse,
            ConfigCsvRepository configs,
            ShopOfferRefreshService refreshService,
            ShopPurchaseService purchaseService,
            ShopSellService sellService,
            ConfirmDialogView confirmDialog,
            ToastView toastView)
        {
            UnsubscribeInventoryChanged();
            _progress = progress;
            _protagonistEquipment = protagonistEquipment;
            _magicBookSlots = magicBookSlots;
            _warehouse = warehouse;
            _configs = configs;
            _refreshService = refreshService;
            _purchaseService = purchaseService;
            _sellService = sellService;
            _confirmDialog = confirmDialog;
            _toastView = toastView;
            if (isActiveAndEnabled)
            {
                SubscribeInventoryChanged();
            }
        }

        private void OnEnable()
        {
            SubscribeInventoryChanged();
        }

        private void OnDisable()
        {
            UnsubscribeInventoryChanged();
            HideSellPopover();
        }

        public void Open()
        {
            if (_progress == null)
            {
                Debug.LogError("[ShopStageRootView] Open() failed: progress is null.");
                return;
            }

            EnsureUi();

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

            gameObject.SetActive(true);
            RefreshUi();
        }

        /// <summary>
        /// Editor/builder: assemble full-screen hierarchy on this GameObject and assign serialized refs.
        /// </summary>
        public void BuildFullscreenHierarchy()
        {
            BuildUi();
        }

        private void EnsureUi()
        {
            if (_closeButton == null || !HasOwnedIconUi())
            {
                BuildUi();
            }

            WireListeners();
        }

        private bool HasOwnedIconUi()
        {
            return _equipIconUis != null && _equipIconUis.Length > 0 && _equipIconUis[0]?.Root != null
                   && _magicBookIconUis != null && _magicBookIconUis.Length > 0
                   && _magicBookIconUis[0]?.Root != null;
        }

        private void WireListeners()
        {
            if (_listenersWired)
            {
                return;
            }

            _listenersWired = true;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_refreshButton != null)
            {
                _refreshButton.onClick.RemoveListener(HandleRefreshClicked);
                _refreshButton.onClick.AddListener(HandleRefreshClicked);
            }

            if (_slotUis == null)
            {
                return;
            }

            for (var i = 0; i < _slotUis.Length; i++)
            {
                var slot = _slotUis[i];
                if (slot == null || slot.BuyButton == null)
                {
                    continue;
                }

                var captureIndex = i;
                slot.BuyButton.onClick.RemoveAllListeners();
                slot.BuyButton.onClick.AddListener(() => HandleBuyClicked(captureIndex));
            }

            EnsureSellPopover();
            if (_sellButton != null)
            {
                _sellButton.onClick.RemoveListener(HandleSellClicked);
                _sellButton.onClick.AddListener(HandleSellClicked);
            }

            WireOwnedIconListeners();
        }

        private void WireOwnedIconListeners()
        {
            if (_equipIconUis != null)
            {
                for (var i = 0; i < _equipIconUis.Length; i++)
                {
                    var slot = _equipIconUis[i];
                    if (slot?.Button == null)
                    {
                        continue;
                    }

                    var captureIndex = i;
                    slot.Button.onClick.RemoveAllListeners();
                    slot.Button.onClick.AddListener(() => HandleEquipIconClicked(captureIndex));
                }
            }

            if (_magicBookIconUis != null)
            {
                for (var i = 0; i < _magicBookIconUis.Length; i++)
                {
                    var slot = _magicBookIconUis[i];
                    if (slot?.Button == null)
                    {
                        continue;
                    }

                    var captureIndex = i;
                    slot.Button.onClick.RemoveAllListeners();
                    slot.Button.onClick.AddListener(() => HandleMagicBookIconClicked(captureIndex));
                }
            }
        }

        private void BuildUi()
        {
            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var rootRect = gameObject.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                StretchFull(rootRect);
            }

            var backdropGo = EnsureChild("ShopBackdrop");
            StretchFull(backdropGo.GetComponent<RectTransform>());
            var backdropImage = GetOrAdd<Image>(backdropGo);
            backdropImage.color = new Color(0.06f, 0.08f, 0.1f, 0.96f);
            backdropImage.raycastTarget = true;

            var boxGo = EnsureChild("ShopBox");
            StretchFull(boxGo.GetComponent<RectTransform>());
            var boxImage = GetOrAdd<Image>(boxGo);
            boxImage.color = new Color(0.1f, 0.13f, 0.17f, 1f);
            boxImage.raycastTarget = true;

            _closeButton = CreateButton(
                boxGo.transform,
                "CloseButton",
                new Vector2(1f, 1f),
                new Vector2(-28f, -24f),
                new Vector2(110f, 48f),
                "关闭",
                font,
                20);
            var closeRect = _closeButton.GetComponent<RectTransform>();
            closeRect.pivot = new Vector2(1f, 1f);

            CreateText(
                boxGo.transform,
                "TitleText",
                new Vector2(0f, 1f),
                new Vector2(36f, -28f),
                new Vector2(480f, 48f),
                "商店",
                font,
                32,
                TextAnchor.MiddleLeft).GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            CreateText(
                boxGo.transform,
                "PlayerTitleText",
                new Vector2(0f, 1f),
                new Vector2(36f, -96f),
                new Vector2(420f, 36f),
                "玩家信息",
                font,
                22,
                TextAnchor.MiddleLeft).GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            _spiritText = CreateText(
                boxGo.transform,
                "SpiritText",
                new Vector2(0f, 1f),
                new Vector2(36f, -144f),
                new Vector2(480f, 32f),
                "",
                font,
                20,
                TextAnchor.MiddleLeft);
            _spiritText.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            _equipSummaryText = CreateText(
                boxGo.transform,
                "EquipSummaryText",
                new Vector2(0f, 1f),
                new Vector2(36f, -184f),
                new Vector2(480f, 32f),
                "",
                font,
                20,
                TextAnchor.MiddleLeft);
            _equipSummaryText.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            BuildOwnedIconGrid(
                boxGo.transform,
                "EquipIconArea",
                new Vector2(36f, -220f),
                3,
                2,
                MaxOwnedEquipIcons,
                font,
                out _equipIconUis);

            _magicBookSummaryText = CreateText(
                boxGo.transform,
                "MagicBookSummaryText",
                new Vector2(0f, 1f),
                new Vector2(36f, -368f),
                new Vector2(480f, 32f),
                "",
                font,
                20,
                TextAnchor.MiddleLeft);
            _magicBookSummaryText.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            BuildOwnedIconGrid(
                boxGo.transform,
                "MagicBookIconArea",
                new Vector2(36f, -404f),
                6,
                1,
                SpecialEquipSlotsService.SlotCount,
                font,
                out _magicBookIconUis);

            BuildSellPopover(boxGo.transform, font);

            CreateText(
                boxGo.transform,
                "OffersTitleText",
                new Vector2(0.36f, 1f),
                new Vector2(0f, -96f),
                new Vector2(420f, 36f),
                "待售商品",
                font,
                22,
                TextAnchor.MiddleLeft).GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            var offersGo = EnsureChild("OffersArea", boxGo.transform);
            var offersRect = offersGo.GetComponent<RectTransform>();
            offersRect.anchorMin = new Vector2(0.36f, 0.18f);
            offersRect.anchorMax = new Vector2(0.97f, 0.86f);
            offersRect.offsetMin = Vector2.zero;
            offersRect.offsetMax = Vector2.zero;

            _slotUis = new OfferSlotUi[6];
            for (var i = 0; i < 6; i++)
            {
                var col = i % 3;
                var row = i / 3;
                var slot = EnsureChild($"OfferSlot_{i}", offersGo.transform);
                var slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(col / 3f + 0.01f, 1f - (row + 1) / 2f + 0.02f);
                slotRect.anchorMax = new Vector2((col + 1) / 3f - 0.01f, 1f - row / 2f - 0.02f);
                slotRect.offsetMin = Vector2.zero;
                slotRect.offsetMax = Vector2.zero;

                var slotBg = GetOrAdd<Image>(slot);
                slotBg.color = new Color(0.15f, 0.18f, 0.23f, 0.95f);
                slotBg.raycastTarget = true;

                var buyButton = GetOrAdd<Button>(slot);
                buyButton.transition = Selectable.Transition.ColorTint;
                buyButton.targetGraphic = slotBg;

                var iconGo = EnsureChild("Icon", slot.transform);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.08f, 0.55f);
                iconRect.anchorMax = new Vector2(0.38f, 0.92f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var iconImage = GetOrAdd<Image>(iconGo);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                var nameText = CreateText(
                    slot.transform,
                    "NameText",
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    "",
                    font,
                    18,
                    TextAnchor.UpperLeft);
                StretchText(nameText.rectTransform, new Vector2(0.42f, 0.62f), new Vector2(0.96f, 0.92f));

                var priceText = CreateText(
                    slot.transform,
                    "PriceText",
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    "",
                    font,
                    16,
                    TextAnchor.UpperLeft);
                StretchText(priceText.rectTransform, new Vector2(0.42f, 0.38f), new Vector2(0.96f, 0.62f));

                var soldText = CreateText(
                    slot.transform,
                    "SoldText",
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    "已售",
                    font,
                    22,
                    TextAnchor.MiddleCenter);
                StretchText(soldText.rectTransform, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.32f));
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
            }

            _refreshButton = CreateButton(
                boxGo.transform,
                "RefreshButton",
                new Vector2(0.5f, 0f),
                new Vector2(140f, 36f),
                new Vector2(280f, 56f),
                "刷新",
                font,
                22);
            var refreshRect = _refreshButton.GetComponent<RectTransform>();
            refreshRect.pivot = new Vector2(0.5f, 0f);

            _refreshPriceText = CreateText(
                boxGo.transform,
                "RefreshPriceText",
                new Vector2(0.5f, 0f),
                new Vector2(-160f, 36f),
                new Vector2(360f, 56f),
                "",
                font,
                20,
                TextAnchor.MiddleRight);
            _refreshPriceText.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        }

        private void RefreshUi()
        {
            if (_progress == null)
            {
                return;
            }

            if (_warehouse != null)
            {
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

            RefreshOwnedIcons();

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

            if (_slotUis == null)
            {
                return;
            }

            for (var i = 0; i < 6; i++)
            {
                var slotUi = i < _slotUis.Length ? _slotUis[i] : null;
                if (slotUi == null)
                {
                    continue;
                }

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
                    slotUi.IconImage.color = new Color(1f, 1f, 1f, 0.12f);
                    slotUi.NameText.text = "-";
                    slotUi.PriceText.text = "";
                    continue;
                }

                _configs.TryGetItemCatalog(offer.ItemId, out var itemRow);

                slotUi.NameText.text = !string.IsNullOrEmpty(itemRow?.DisplayName)
                    ? itemRow.DisplayName
                    : offer.ItemId;
                slotUi.PriceText.text = offer.PriceSpirit > 0 ? $"价格：{offer.PriceSpirit}" : "价格：-";

                var sprite = ItemIconLoader.LoadForShopOffer(_configs, offer.ItemId, offer.Category);
                slotUi.IconImage.sprite = sprite;
                slotUi.IconImage.enabled = true;
                slotUi.IconImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
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
            {
                return;
            }

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
            {
                return;
            }

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

        private void SubscribeInventoryChanged()
        {
            if (_inventoryChangedSubscribed)
            {
                return;
            }

            if (_protagonistEquipment != null)
            {
                _protagonistEquipment.Changed += HandleInventoryChanged;
            }

            if (_magicBookSlots != null)
            {
                _magicBookSlots.Changed += HandleInventoryChanged;
            }

            _inventoryChangedSubscribed = _protagonistEquipment != null || _magicBookSlots != null;
        }

        private void UnsubscribeInventoryChanged()
        {
            if (!_inventoryChangedSubscribed)
            {
                return;
            }

            if (_protagonistEquipment != null)
            {
                _protagonistEquipment.Changed -= HandleInventoryChanged;
            }

            if (_magicBookSlots != null)
            {
                _magicBookSlots.Changed -= HandleInventoryChanged;
            }

            _inventoryChangedSubscribed = false;
        }

        private void HandleInventoryChanged()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            RefreshUi();
        }

        private void RefreshOwnedIcons()
        {
            HideSellPopover();

            if (_equipIconUis != null && _protagonistEquipment != null)
            {
                var owned = _protagonistEquipment.OwnedEquips;
                for (var i = 0; i < _equipIconUis.Length; i++)
                {
                    var slotUi = _equipIconUis[i];
                    if (slotUi?.Root == null)
                    {
                        continue;
                    }

                    if (owned != null && i < owned.Count && owned[i] != null
                        && !string.IsNullOrEmpty(owned[i].EquipId))
                    {
                        var equipId = owned[i].EquipId;
                        slotUi.Root.SetActive(true);
                        var sprite = ItemIconLoader.LoadForShopOffer(
                            _configs, equipId, ShopPoolItemCategory.A);
                        slotUi.IconImage.sprite = sprite;
                        slotUi.IconImage.enabled = true;
                        slotUi.IconImage.color = sprite != null
                            ? Color.white
                            : new Color(1f, 1f, 1f, 0.12f);
                    }
                    else
                    {
                        slotUi.Root.SetActive(false);
                    }
                }
            }
            else if (_equipIconUis != null)
            {
                for (var i = 0; i < _equipIconUis.Length; i++)
                {
                    if (_equipIconUis[i]?.Root != null)
                    {
                        _equipIconUis[i].Root.SetActive(false);
                    }
                }
            }

            if (_magicBookIconUis != null && _magicBookSlots != null)
            {
                for (var i = 0; i < _magicBookIconUis.Length; i++)
                {
                    var slotUi = _magicBookIconUis[i];
                    if (slotUi?.Root == null)
                    {
                        continue;
                    }

                    var bookId = _magicBookSlots.GetSlot(i);
                    if (!string.IsNullOrEmpty(bookId))
                    {
                        slotUi.Root.SetActive(true);
                        var sprite = ItemIconLoader.LoadForShopOffer(
                            _configs, bookId, ShopPoolItemCategory.B);
                        slotUi.IconImage.sprite = sprite;
                        slotUi.IconImage.enabled = true;
                        slotUi.IconImage.color = sprite != null
                            ? Color.white
                            : new Color(1f, 1f, 1f, 0.12f);
                    }
                    else
                    {
                        slotUi.Root.SetActive(false);
                    }
                }
            }
            else if (_magicBookIconUis != null)
            {
                for (var i = 0; i < _magicBookIconUis.Length; i++)
                {
                    if (_magicBookIconUis[i]?.Root != null)
                    {
                        _magicBookIconUis[i].Root.SetActive(false);
                    }
                }
            }
        }

        private void HandleEquipIconClicked(int displayIndex)
        {
            if (_protagonistEquipment?.OwnedEquips == null || displayIndex >= _protagonistEquipment.OwnedEquips.Count)
            {
                return;
            }

            var piece = _protagonistEquipment.OwnedEquips[displayIndex];
            if (piece == null || string.IsNullOrEmpty(piece.EquipId))
            {
                return;
            }

            if (_selectionKind == OwnedSelectionKind.Equipment
                && string.Equals(_selectedEquipId, piece.EquipId, StringComparison.Ordinal))
            {
                HideSellPopover();
                return;
            }

            _selectionKind = OwnedSelectionKind.Equipment;
            _selectedEquipId = piece.EquipId;
            _selectedMagicSlot = -1;
            _selectedIconRect = _equipIconUis[displayIndex]?.IconImage?.rectTransform;
            ShowSellPopoverForItem(piece.EquipId);
        }

        private void HandleMagicBookIconClicked(int slotIndex)
        {
            if (_magicBookSlots == null)
            {
                return;
            }

            var bookId = _magicBookSlots.GetSlot(slotIndex);
            if (string.IsNullOrEmpty(bookId))
            {
                return;
            }

            if (_selectionKind == OwnedSelectionKind.MagicBook && _selectedMagicSlot == slotIndex)
            {
                HideSellPopover();
                return;
            }

            _selectionKind = OwnedSelectionKind.MagicBook;
            _selectedMagicSlot = slotIndex;
            _selectedEquipId = null;
            _selectedIconRect = _magicBookIconUis[slotIndex]?.IconImage?.rectTransform;
            ShowSellPopoverForItem(bookId);
        }

        private void ShowSellPopoverForItem(string itemId)
        {
            EnsureSellPopover();
            if (_sellPopoverRect == null || _sellService == null)
            {
                return;
            }

            if (!_sellService.TryResolveSellPrice(itemId, out var sellPrice, out _))
            {
                HideSellPopover();
                ShowToast("无法出售（配置缺失）");
                return;
            }

            if (_sellPriceText != null)
            {
                _sellPriceText.text = $"获得精魂：{sellPrice}";
            }

            PositionSellPopoverUnderIcon(_selectedIconRect);
            _sellPopoverRect.gameObject.SetActive(true);
        }

        private void HideSellPopover()
        {
            _selectionKind = OwnedSelectionKind.None;
            _selectedEquipId = null;
            _selectedMagicSlot = -1;
            _selectedIconRect = null;
            if (_sellPopoverRect != null)
            {
                _sellPopoverRect.gameObject.SetActive(false);
            }
        }

        private void HandleSellClicked()
        {
            if (_sellService == null || _selectionKind == OwnedSelectionKind.None)
            {
                return;
            }

            string itemId;
            string displayName;
            if (_selectionKind == OwnedSelectionKind.Equipment)
            {
                itemId = _selectedEquipId;
                displayName = ResolveDisplayName(itemId, ShopPoolItemCategory.A);
            }
            else
            {
                itemId = _magicBookSlots?.GetSlot(_selectedMagicSlot);
                displayName = ResolveDisplayName(itemId, ShopPoolItemCategory.B);
            }

            if (string.IsNullOrEmpty(itemId))
            {
                ShowToast("无法出售");
                return;
            }

            if (!_sellService.TryResolveSellPrice(itemId, out var sellPrice, out var priceError))
            {
                ShowToast(string.IsNullOrEmpty(priceError) ? "无法出售" : priceError);
                return;
            }

            if (_confirmDialog == null)
            {
                ConfirmSell();
                return;
            }

            var kind = _selectionKind;
            var equipId = _selectedEquipId;
            var magicSlot = _selectedMagicSlot;
            _confirmDialog.Show(
                $"确认出售「{displayName}」？将获得 {sellPrice} 精魂。",
                () => ConfirmSell(kind, equipId, magicSlot),
                null,
                ShopConfirmSortingOrder);
        }

        private void ConfirmSell()
        {
            ConfirmSell(_selectionKind, _selectedEquipId, _selectedMagicSlot);
        }

        private void ConfirmSell(OwnedSelectionKind kind, string equipId, int magicSlot)
        {
            if (_sellService == null)
            {
                return;
            }

            var ok = false;
            var error = string.Empty;
            try
            {
                ok = kind == OwnedSelectionKind.Equipment
                    ? _sellService.TrySellEquipment(equipId, out error)
                    : _sellService.TrySellMagicBook(magicSlot, out error);
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            if (!ok)
            {
                ShowToast(string.IsNullOrEmpty(error) ? "出售失败" : error);
                return;
            }

            HideSellPopover();
            RefreshUi();
        }

        private string ResolveDisplayName(string itemId, ShopPoolItemCategory category)
        {
            if (_configs != null && _configs.TryGetItemCatalog(itemId, out var catalog)
                && catalog != null && !string.IsNullOrEmpty(catalog.DisplayName))
            {
                return catalog.DisplayName;
            }

            if (_configs != null && category == ShopPoolItemCategory.A
                && _configs.TryGetProtagonistEquipment(itemId, 1, out var equip) && equip != null
                && !string.IsNullOrEmpty(equip.DisplayName))
            {
                return equip.DisplayName;
            }

            if (_configs != null && category == ShopPoolItemCategory.B
                && _configs.TryGetMagicBook(itemId, out var book) && book != null
                && !string.IsNullOrEmpty(book.DisplayName))
            {
                return book.DisplayName;
            }

            return itemId ?? string.Empty;
        }

        private void EnsureSellPopover()
        {
            if (_sellPopoverRect != null && _sellButton != null && _sellPriceText != null)
            {
                return;
            }

            var box = transform.Find("ShopBox");
            if (box == null)
            {
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildSellPopover(box, font);
        }

        private void BuildSellPopover(Transform boxParent, Font font)
        {
            var existing = boxParent.Find("SellPopover");
            GameObject popoverGo;
            if (existing != null)
            {
                popoverGo = existing.gameObject;
            }
            else
            {
                popoverGo = new GameObject(
                    "SellPopover",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(VerticalLayoutGroup));
                popoverGo.transform.SetParent(boxParent, false);
            }

            _sellPopoverRect = popoverGo.GetComponent<RectTransform>();
            _sellPopoverRect.sizeDelta = new Vector2(140f, 72f);
            _sellPopoverRect.pivot = new Vector2(0.5f, 1f);

            var bg = popoverGo.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
            bg.raycastTarget = true;

            var vlg = popoverGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            _sellPriceText = CreateText(
                popoverGo.transform,
                "SellPriceText",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(120f, 24f),
                "",
                font,
                16,
                TextAnchor.MiddleCenter);
            _sellPriceText.raycastTarget = false;

            _sellButton = CreateButton(
                popoverGo.transform,
                "SellButton",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(120f, 32f),
                "出售",
                font,
                18);

            popoverGo.SetActive(false);
        }

        private void PositionSellPopoverUnderIcon(RectTransform iconRect)
        {
            if (_sellPopoverRect == null || iconRect == null)
            {
                return;
            }

            var parentRt = _sellPopoverRect.parent as RectTransform;
            if (parentRt == null)
            {
                return;
            }

            var canvas = parentRt.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var corners = new Vector3[4];
            iconRect.GetWorldCorners(corners);
            var bottomCenter = (corners[0] + corners[3]) * 0.5f;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt, bottomCenter, cam, out var localPoint))
            {
                _sellPopoverRect.anchoredPosition = localPoint + new Vector2(0f, -8f);
            }
        }

        private void BuildOwnedIconGrid(
            Transform boxParent,
            string areaName,
            Vector2 topLeftAnchored,
            int columns,
            int rows,
            int slotCount,
            Font font,
            out OwnedIconSlotUi[] slots)
        {
            slots = new OwnedIconSlotUi[slotCount];
            var areaGo = EnsureChild(areaName, boxParent);
            var areaRect = areaGo.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0f, 1f);
            areaRect.anchorMax = new Vector2(0f, 1f);
            areaRect.pivot = new Vector2(0f, 1f);
            areaRect.anchoredPosition = topLeftAnchored;
            var width = columns * OwnedIconSize + (columns - 1) * OwnedIconGap;
            var height = rows * OwnedIconSize + (rows - 1) * OwnedIconGap;
            areaRect.sizeDelta = new Vector2(width, height);

            for (var i = 0; i < slotCount; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var slotGo = EnsureChild($"OwnedIcon_{i}", areaGo.transform);
                var slotRect = slotGo.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 1f);
                slotRect.anchorMax = new Vector2(0f, 1f);
                slotRect.pivot = new Vector2(0f, 1f);
                slotRect.sizeDelta = new Vector2(OwnedIconSize, OwnedIconSize);
                slotRect.anchoredPosition = new Vector2(
                    col * (OwnedIconSize + OwnedIconGap),
                    -row * (OwnedIconSize + OwnedIconGap));

                var slotBg = GetOrAdd<Image>(slotGo);
                slotBg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
                slotBg.raycastTarget = true;

                var button = GetOrAdd<Button>(slotGo);
                button.targetGraphic = slotBg;

                var iconGo = EnsureChild("Icon", slotGo.transform);
                StretchFull(iconGo.GetComponent<RectTransform>());
                var iconImage = GetOrAdd<Image>(iconGo);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                slotGo.SetActive(false);
                slots[i] = new OwnedIconSlotUi
                {
                    Root = slotGo,
                    Button = button,
                    IconImage = iconImage
                };
            }
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

        private GameObject EnsureChild(string name, Transform parent = null)
        {
            var host = parent != null ? parent : transform;
            var existing = host.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(host, false);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchText(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var t = GetOrAdd<Text>(go);
            t.font = font;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = alignment;
            t.raycastTarget = false;
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
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = GetOrAdd<Image>(go);
            image.color = new Color(0.24f, 0.35f, 0.5f, 0.95f);
            image.raycastTarget = true;

            var button = GetOrAdd<Button>(go);
            button.targetGraphic = image;

            Transform labelTf = go.transform.Find("Label");
            GameObject labelGo;
            if (labelTf != null)
            {
                labelGo = labelTf.gameObject;
            }
            else
            {
                labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
            }

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var txt = GetOrAdd<Text>(labelGo);
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            txt.text = label;

            return button;
        }
    }
}
