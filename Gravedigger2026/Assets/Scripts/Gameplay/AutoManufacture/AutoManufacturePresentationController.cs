using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Mode2 AutoManufacture presentation Step1–2 (SPEC_03 UI-016 / D-055).
    /// Soldier row conveyor: enter slides card 0 from one pitch right of viewport center;
    /// after each soldier's books+Idle, shift left one pitch. Book pulse peak invokes Core apply.
    /// </summary>
    public sealed class AutoManufacturePresentationController : MonoBehaviour
    {
        private const float Step1HoldSeconds = 0.6f;
        private const float BaseBookPulseSeconds = 0.28f;
        private const float BaseFocusSeconds = 0.35f;
        private const float BaseRevealHoldSeconds = 0.2f;
        private const float SpeedStep = 1.25f;
        private const int SpeedEveryN = 3;
        private const float CardSpacing = 16f;

        [SerializeField] private RectTransform _bookRow;
        [SerializeField] private ScrollRect _soldierScroll;
        [SerializeField] private RectTransform _soldierContent;
        [SerializeField] private AutoMfgSoldierCardView _cardTemplate;
        [SerializeField] private AutoMfgMagicBookSlotView[] _bookSlots =
            new AutoMfgMagicBookSlotView[SpecialEquipSlotsService.SlotCount];
        [SerializeField] private BookRowView _bookRowView;

        private readonly List<AutoMfgSoldierCardView> _cards = new List<AutoMfgSoldierCardView>();
        private readonly List<string> _batchIds = new List<string>();

        private ConfigCsvRepository _configs;
        private WarriorPoolService _warriorPool;
        private DefendPrefabCatalog _defendCatalog;
        private SpecialEquipSlotsService _boundBooks;
        private Action _onComplete;
        private Action<string, int> _onBookPulsePeak;
        private Coroutine _playRoutine;
        private Canvas _canvas;
        private AutoMfgSoldierPreviewFarm _previewFarm;
        private bool _booksChangedSubscribed;

        public bool IsWired
        {
            get
            {
                EnsureBookRowView();
                return _soldierScroll != null
                    && _soldierContent != null
                    && _cardTemplate != null
                    && _bookSlots != null
                    && _bookSlots.Length == SpecialEquipSlotsService.SlotCount;
            }
        }

        public void Begin(
            IReadOnlyList<string> batchWarriorIds,
            SpecialEquipSlotsService specialEquipSlots,
            ConfigCsvRepository configs,
            WarriorPoolService warriorPool,
            DefendPrefabCatalog defendCatalog,
            Action onComplete,
            Action<string, int> onBookPulsePeak = null)
        {
            End();
            EnsureBookRowView();
            if (_soldierScroll == null || _soldierContent == null || _cardTemplate == null
                || _bookSlots == null || _bookSlots.Length != SpecialEquipSlotsService.SlotCount)
            {
                Debug.LogError(
                    "[AutoMfgPresentation] UI refs missing — use AutoManufacturePresentationController.Build.");
                return;
            }

            _configs = configs;
            _warriorPool = warriorPool;
            _defendCatalog = defendCatalog;
            _onComplete = onComplete;
            _onBookPulsePeak = onBookPulsePeak;

            _batchIds.Clear();
            if (batchWarriorIds != null)
            {
                for (var i = 0; i < batchWarriorIds.Count; i++)
                {
                    var id = batchWarriorIds[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        _batchIds.Add(id);
                    }
                }
            }

            BindBooks(specialEquipSlots);
            BindSoldierCards();
            gameObject.SetActive(true);
            _playRoutine = StartCoroutine(CoPlay());
        }

        /// <summary>Refresh class name / Lv on the card for <paramref name="warriorId"/> after a book apply.</summary>
        public void RefreshFocusedCardClass(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || !string.Equals(card.WarriorId, warriorId, StringComparison.Ordinal))
                {
                    continue;
                }

                card.RefreshClass(ResolveClassName(warriorId), ResolveClassLevel(warriorId));
                return;
            }
        }

        /// <summary>
        /// Step2 pulse peak: show/update soldier-card live preview when VisualStyle is baked (UI-016 / D-055).
        /// </summary>
        public void RefreshFocusedCardVisual(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId) || _warriorPool == null || _defendCatalog == null)
            {
                return;
            }

            if (!_warriorPool.TryGet(warriorId, out var warrior) || warrior == null)
            {
                return;
            }

            if (!AutoMfgSoldierPreviewFarm.HasBakedVisual(warrior))
            {
                return;
            }

            AutoMfgSoldierCardView card = null;
            for (var i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c != null && string.Equals(c.WarriorId, warriorId, StringComparison.Ordinal))
                {
                    card = c;
                    break;
                }
            }

            if (card == null)
            {
                return;
            }

            EnsureFarm();
            var styleCatalog = _defendCatalog.VisualStyleCatalog;
            _previewFarm?.RefreshVisual(card, warrior, _defendCatalog, styleCatalog);
        }

        public void End()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            DestroyFarm();
            ClearCards();
            UnsubscribeBooksChanged();
            _boundBooks = null;
            _configs = null;
            _warriorPool = null;
            _defendCatalog = null;
            _onComplete = null;
            _onBookPulsePeak = null;
            _batchIds.Clear();
        }

        private void OnEnable()
        {
            SubscribeBooksChanged();
            if (_boundBooks != null)
            {
                RefreshBooks();
            }
        }

        private void OnDisable()
        {
            UnsubscribeBooksChanged();
        }

        /// <summary>
        /// Builds a full-screen presentation hierarchy under parent (runtime / AmAssetBuilder).
        /// </summary>
        public static AutoManufacturePresentationController Build(Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var root = new GameObject(
                "AutoManufacturePresentationRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(AutoManufacturePresentationController));
            root.transform.SetParent(parent, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var rt = root.GetComponent<RectTransform>();
            Stretch(rt);

            CreateBackground(root.transform);

            var dim = CreatePanel(root.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            Stretch(dim.GetComponent<RectTransform>());

            var bookRowView = BookRowView.CreateHierarchy(root.transform);
            var bookRt = bookRowView.GetComponent<RectTransform>();
            bookRt.anchoredPosition = new Vector2(0f, 220f);
            bookRowView.SetAllowReorder(false);

            var scrollGo = new GameObject("SoldierScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0f, -40f);
            scrollRt.sizeDelta = new Vector2(900f, AutoMfgSoldierCardView.CardHeight + 24f);
            scrollGo.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.35f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0.5f);
            contentRt.anchorMax = new Vector2(0f, 0.5f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, AutoMfgSoldierCardView.CardHeight);

            var contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.spacing = CardSpacing;
            contentLayout.padding = new RectOffset(12, 12, 0, 0);
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.scrollSensitivity = 40f;

            var cardTemplate = CreateSoldierCard(content.transform);
            cardTemplate.gameObject.SetActive(false);

            var controller = root.GetComponent<AutoManufacturePresentationController>();
            controller._canvas = canvas;
            controller._bookRow = bookRt;
            controller._bookRowView = bookRowView;
            controller._soldierScroll = scroll;
            controller._soldierContent = contentRt;
            controller._cardTemplate = cardTemplate;
            controller._bookSlots = bookRowView.Slots;
            return controller;
        }

        private void BindBooks(SpecialEquipSlotsService slots)
        {
            EnsureBookRowView();
            UnsubscribeBooksChanged();
            _boundBooks = slots;
            if (_bookRowView != null)
            {
                _bookRowView.SetAllowReorder(false);
                _bookRowView.Bind(slots, _configs);
                _bookSlots = _bookRowView.Slots;
            }
            else
            {
                RefreshBooksFallback();
            }

            SubscribeBooksChanged();
        }

        private void RefreshBooks()
        {
            if (_bookRowView != null)
            {
                _bookRowView.Refresh();
                _bookSlots = _bookRowView.Slots;
                return;
            }

            RefreshBooksFallback();
        }

        private void RefreshBooksFallback()
        {
            if (_bookSlots == null)
            {
                return;
            }

            for (var i = 0; i < _bookSlots.Length; i++)
            {
                var view = _bookSlots[i];
                if (view == null)
                {
                    continue;
                }

                view.ResetScale();
                var bookId = _boundBooks != null ? _boundBooks.GetSlot(i) : string.Empty;
                if (string.IsNullOrEmpty(bookId) || _configs == null || !_configs.TryGetMagicBook(bookId, out var row))
                {
                    view.BindEmpty();
                    continue;
                }

                Sprite icon = null;
                if (!string.IsNullOrEmpty(row.IconAssetId))
                {
                    var assetPath = row.IconAssetId.Trim();
                    if (assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        assetPath = assetPath.Substring(0, assetPath.Length - 4);
                    }

                    // CSV/IconAssetId 可能是两种写法：
                    // 1) 仅文件名：MagicBook_Restore -> Resources/UI/MagicBooks/MagicBook_Restore
                    // 2) 已是 Resources 相对路径：UI/MagicBooks/MagicBook_Restore -> 直接 Resources.Load
                    icon = assetPath.Contains("/")
                        ? Resources.Load<Sprite>(assetPath)
                        : Resources.Load<Sprite>($"UI/MagicBooks/{assetPath}");
                }

                var name = !string.IsNullOrEmpty(row.DisplayName) ? row.DisplayName : bookId;
                view.BindBook(name, icon);
            }
        }

        private void HandleBooksChanged()
        {
            RefreshBooks();
        }

        private void SubscribeBooksChanged()
        {
            if (_booksChangedSubscribed || _boundBooks == null)
            {
                return;
            }

            _boundBooks.Changed += HandleBooksChanged;
            _booksChangedSubscribed = true;
        }

        private void UnsubscribeBooksChanged()
        {
            if (!_booksChangedSubscribed || _boundBooks == null)
            {
                _booksChangedSubscribed = false;
                return;
            }

            _boundBooks.Changed -= HandleBooksChanged;
            _booksChangedSubscribed = false;
        }

        private void EnsureBookRowView()
        {
            if (_bookRowView != null && _bookRowView.HasWiredSlots)
            {
                _bookSlots = _bookRowView.Slots;
                if (_bookRow == null)
                {
                    _bookRow = _bookRowView.transform as RectTransform;
                }

                return;
            }

            if (_bookRowView == null && _bookRow != null)
            {
                _bookRowView = _bookRow.GetComponent<BookRowView>();
            }

            if (_bookRowView == null)
            {
                _bookRowView = GetComponentInChildren<BookRowView>(true);
            }

            if (_bookRowView == null
                && _bookRow != null
                && _bookSlots != null
                && _bookSlots.Length == SpecialEquipSlotsService.SlotCount)
            {
                _bookRowView = _bookRow.gameObject.AddComponent<BookRowView>();
                _bookRowView.RuntimeWire(_bookSlots, allowReorder: false);
            }

            if (_bookRowView != null)
            {
                _bookSlots = _bookRowView.Slots;
                _bookRow = _bookRowView.transform as RectTransform;
            }
        }

        private void BindSoldierCards()
        {
            ClearCards();
            if (_cardTemplate == null || _soldierContent == null)
            {
                return;
            }

            for (var i = 0; i < _batchIds.Count; i++)
            {
                var id = _batchIds[i];
                var card = Instantiate(_cardTemplate, _soldierContent);
                card.gameObject.SetActive(true);
                card.gameObject.name = "SoldierCard_" + i;
                var className = ResolveClassName(id);
                var classLevel = ResolveClassLevel(id);
                card.BindMystery(id, className, classLevel);
                _cards.Add(card);
            }

            Canvas.ForceUpdateCanvases();
            ApplyCenterPadding();
            Canvas.ForceUpdateCanvases();
            if (_soldierScroll != null)
            {
                _soldierScroll.StopMovement();
                _soldierScroll.inertia = false;
                _soldierScroll.horizontalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Left pad = (viewportW - cardW)/2 + pitch so scroll=0 places card 0 one pitch right of center.
        /// Right pad = (viewportW - cardW)/2 so the last card can center.
        /// Card width is measured from the laid-out template instance (Prefab may differ from 150).
        /// </summary>
        private void ApplyCenterPadding()
        {
            if (_soldierScroll == null || _soldierScroll.viewport == null || _soldierContent == null)
            {
                return;
            }

            var layout = _soldierContent.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                return;
            }

            var viewWidth = _soldierScroll.viewport.rect.width;
            var cardWidth = ResolveLaidOutCardWidth();
            if (viewWidth <= 1f || cardWidth <= 1f)
            {
                return;
            }

            var pitch = cardWidth + layout.spacing;
            var side = Mathf.Max(0f, (viewWidth - cardWidth) * 0.5f);
            layout.padding = new RectOffset(
                Mathf.RoundToInt(side + pitch),
                Mathf.RoundToInt(side),
                layout.padding.top,
                layout.padding.bottom);
        }

        private float ResolveLaidOutCardWidth()
        {
            if (_cards.Count > 0 && _cards[0] != null)
            {
                var rt = _cards[0].RectTransform;
                if (rt != null && rt.rect.width > 1f)
                {
                    return rt.rect.width;
                }
            }

            if (_cardTemplate != null)
            {
                var templateRt = _cardTemplate.RectTransform;
                if (templateRt != null && templateRt.rect.width > 1f)
                {
                    return templateRt.rect.width;
                }
            }

            return AutoMfgSoldierCardView.CardWidth;
        }

        private string ResolveClassName(string warriorId)
        {
            if (_warriorPool == null || !_warriorPool.TryGet(warriorId, out var warrior) || warrior == null)
            {
                return string.Empty;
            }

            if (_configs != null && _configs.TryGetClass(warrior.ClassId, out var row) && row != null)
            {
                return row.ClassName ?? string.Empty;
            }

            return warrior.ClassId ?? string.Empty;
        }

        private int ResolveClassLevel(string warriorId)
        {
            if (_warriorPool == null || !_warriorPool.TryGet(warriorId, out var warrior) || warrior == null)
            {
                return 0;
            }

            if (_configs != null && _configs.TryGetClass(warrior.ClassId, out var row) && row != null)
            {
                return row.ClassLevel < 0 ? 0 : row.ClassLevel;
            }

            return 0;
        }

        private void RevealCard(AutoMfgSoldierCardView card)
        {
            if (card == null)
            {
                return;
            }

            EnsureFarm();
            if (_previewFarm != null && _previewFarm.HasActivePreview(card.WarriorId))
            {
                _previewFarm.TryPlayTaunt(card.WarriorId);
                return;
            }

            GameObject prefab = null;
            WarriorInstance warrior = null;
            if (_warriorPool != null)
            {
                _warriorPool.TryGet(card.WarriorId, out warrior);
            }

            if (warrior != null && _defendCatalog != null)
            {
                _defendCatalog.TryGetWarriorAppearance(warrior.AppearanceId, out prefab);
            }

            var styleCatalog = _defendCatalog != null ? _defendCatalog.VisualStyleCatalog : null;
            if (_previewFarm != null && _previewFarm.TryReveal(card, prefab, warrior, styleCatalog))
            {
                return;
            }

            card.RevealIdle(ResolveIdleSprite(card.WarriorId));
        }

        private void EnsureFarm()
        {
            if (_previewFarm != null)
            {
                return;
            }

            Transform worldParent = transform.parent;
            var canvas = GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                worldParent = transform;
            }

            _previewFarm = AutoMfgSoldierPreviewFarm.Ensure(worldParent);
        }

        private void DestroyFarm()
        {
            if (_previewFarm == null)
            {
                return;
            }

            _previewFarm.ClearAll();
            Destroy(_previewFarm.gameObject);
            _previewFarm = null;
        }

        private Sprite ResolveIdleSprite(string warriorId)
        {
            if (_warriorPool == null || !_warriorPool.TryGet(warriorId, out var warrior) || warrior == null)
            {
                return null;
            }

            if (_defendCatalog == null
                || !_defendCatalog.TryGetWarriorAppearance(warrior.AppearanceId, out var prefab)
                || prefab == null)
            {
                return null;
            }

            return FormationBattlefieldPreview.SampleIdleSprite(prefab);
        }

        private IEnumerator CoPlay()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyCenterPadding();
            Canvas.ForceUpdateCanvases();
            if (_soldierScroll != null)
            {
                _soldierScroll.StopMovement();
                _soldierScroll.inertia = false;
                _soldierScroll.horizontalNormalizedPosition = 0f;
            }

            LockSoldierScrollDrag();

            yield return CoFocusCard(0, BaseFocusSeconds);
            yield return new WaitForSeconds(Step1HoldSeconds);

            var completed = 0;
            for (var i = 0; i < _cards.Count; i++)
            {
                var speed = Mathf.Pow(SpeedStep, completed / SpeedEveryN);
                var pulseDur = BaseBookPulseSeconds / speed;
                var revealHold = BaseRevealHoldSeconds / speed;
                var focusDur = BaseFocusSeconds / speed;

                var card = _cards[i];
                card.SetAmplifyVisible(true);

                for (var b = 0; b < _bookSlots.Length; b++)
                {
                    var slot = _bookSlots[b];
                    if (slot == null)
                    {
                        _onBookPulsePeak?.Invoke(card.WarriorId, b);
                        RefreshFocusedCardVisual(card.WarriorId);
                        continue;
                    }

                    yield return CoPulseBook(slot, pulseDur, card.WarriorId, b);
                }

                card.SetAmplifyVisible(false);
                RevealCard(card);
                yield return new WaitForSeconds(revealHold);

                completed++;
                if (i + 1 < _cards.Count)
                {
                    yield return CoFocusCard(i + 1, focusDur);
                }
            }

            _playRoutine = null;
            var done = _onComplete;
            _onComplete = null;
            done?.Invoke();
        }

        private void LockSoldierScrollDrag()
        {
            if (_soldierScroll == null)
            {
                return;
            }

            _soldierScroll.StopMovement();
            _soldierScroll.inertia = false;
            var scrollImage = _soldierScroll.GetComponent<Image>();
            if (scrollImage != null)
            {
                scrollImage.raycastTarget = false;
            }

            if (_soldierScroll.viewport != null)
            {
                var viewportImage = _soldierScroll.viewport.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.raycastTarget = false;
                }
            }
        }

        private IEnumerator CoFocusCard(int index, float duration)
        {
            if (_soldierScroll == null || _soldierContent == null || index < 0 || index >= _cards.Count)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();
            _soldierScroll.StopMovement();
            _soldierScroll.inertia = false;

            var target = ComputeFocusNormalized(index);
            var start = _soldierScroll.horizontalNormalizedPosition;
            if (duration <= 0.001f || Mathf.Abs(target - start) <= 0.0001f)
            {
                _soldierScroll.horizontalNormalizedPosition = target;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / duration);
                u = u * u * (3f - 2f * u);
                _soldierScroll.horizontalNormalizedPosition = Mathf.Lerp(start, target, u);
                yield return null;
            }

            _soldierScroll.horizontalNormalizedPosition = target;
        }

        private float ComputeFocusNormalized(int index)
        {
            if (_soldierScroll == null || _soldierScroll.viewport == null || index < 0 || index >= _cards.Count)
            {
                return 0f;
            }

            var content = _soldierContent;
            var viewport = _soldierScroll.viewport;
            var cardView = _cards[index];
            var card = cardView != null ? cardView.RectTransform : null;
            if (content == null || card == null)
            {
                return 0f;
            }

            var contentWidth = content.rect.width;
            var viewWidth = viewport.rect.width;
            var scrollable = contentWidth - viewWidth;
            if (scrollable <= 1f)
            {
                return 0f;
            }

            var cardCenterLocal = content.InverseTransformPoint(card.TransformPoint(card.rect.center));
            var desired = cardCenterLocal.x - viewWidth * 0.5f;
            return Mathf.Clamp01(desired / scrollable);
        }

        private IEnumerator CoPulseBook(
            AutoMfgMagicBookSlotView slot,
            float duration,
            string warriorId,
            int slotIndex)
        {
            if (slot == null)
            {
                yield break;
            }

            var half = Mathf.Max(0.01f, duration * 0.5f);
            var t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / half);
                slot.SetPulseScale(Mathf.Lerp(1f, 1.18f, u));
                yield return null;
            }

            slot.SetPulseScale(1.18f);
            // Peak scale: apply only this slot's MagicBook (SPEC_03 §3.15 Step2).
            _onBookPulsePeak?.Invoke(warriorId, slotIndex);
            RefreshFocusedCardVisual(warriorId);

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / half);
                slot.SetPulseScale(Mathf.Lerp(1.18f, 1f, u));
                yield return null;
            }

            slot.ResetScale();
        }

        private void ClearCards()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    Destroy(_cards[i].gameObject);
                }
            }

            _cards.Clear();
        }

        private void OnDestroy()
        {
            End();
        }

        private static AutoMfgSoldierCardView CreateSoldierCard(Transform parent)
        {
            var go = CreatePanel(parent, "SoldierCardTemplate", new Color(0.2f, 0.22f, 0.3f, 0.98f));
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = AutoMfgSoldierCardView.CardWidth;
            le.preferredHeight = AutoMfgSoldierCardView.CardHeight;
            le.minWidth = AutoMfgSoldierCardView.CardWidth;
            le.minHeight = AutoMfgSoldierCardView.CardHeight;

            var thumbGo = new GameObject("IdleThumb", typeof(RectTransform), typeof(Image));
            thumbGo.transform.SetParent(go.transform, false);
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0.08f, 0.28f);
            thumbRt.anchorMax = new Vector2(0.92f, 0.92f);
            thumbRt.offsetMin = Vector2.zero;
            thumbRt.offsetMax = Vector2.zero;
            var thumb = thumbGo.GetComponent<Image>();
            thumb.enabled = false;
            thumb.raycastTarget = false;

            var q = CreateText(go.transform, "Question", "?", 42, TextAnchor.MiddleCenter);
            var qRt = q.GetComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0f, 0.35f);
            qRt.anchorMax = new Vector2(1f, 0.85f);
            qRt.offsetMin = Vector2.zero;
            qRt.offsetMax = Vector2.zero;

            var className = CreateText(go.transform, "ClassName", string.Empty, 32, TextAnchor.MiddleCenter);
            var cRt = className.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.05f, 0.16f);
            cRt.anchorMax = new Vector2(0.95f, 0.32f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            var classLevel = CreateText(go.transform, "ClassLevel", "Lv.0", 24, TextAnchor.MiddleCenter);
            var lRt = classLevel.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.05f, 0.02f);
            lRt.anchorMax = new Vector2(0.95f, 0.16f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;

            var amplify = CreateText(go.transform, "Amplify", "加强", 28, TextAnchor.UpperCenter);
            amplify.color = new Color(1f, 0.85f, 0.35f, 1f);
            var aRt = amplify.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.1f, 0.72f);
            aRt.anchorMax = new Vector2(0.9f, 0.95f);
            aRt.offsetMin = Vector2.zero;
            aRt.offsetMax = Vector2.zero;
            amplify.gameObject.SetActive(false);

            var view = go.AddComponent<AutoMfgSoldierCardView>();
            view.RuntimeWire(go.GetComponent<Image>(), q, className, classLevel, thumb, amplify);
            return view;
        }

        /// <summary>
        /// Full-screen keep-aspect cover background (UI-016). Sprite assigned by AmAssetBuilder.
        /// </summary>
        public static GameObject CreateBackground(Transform parent)
        {
            var go = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image),
                typeof(AspectRatioFitter));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1920f, 1080f);

            var image = go.GetComponent<Image>();
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            var aspect = go.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 16f / 9f;
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
