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
    /// Mode2 AutoManufacture presentation Step1–2 (SPEC_03 UI-016 / D-055 Approach A).
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
        private const float BookSpacing = 12f;

        [SerializeField] private RectTransform _bookRow;
        [SerializeField] private ScrollRect _soldierScroll;
        [SerializeField] private RectTransform _soldierContent;
        [SerializeField] private AutoMfgSoldierCardView _cardTemplate;
        [SerializeField] private AutoMfgMagicBookSlotView[] _bookSlots =
            new AutoMfgMagicBookSlotView[SpecialEquipSlotsService.SlotCount];

        private readonly List<AutoMfgSoldierCardView> _cards = new List<AutoMfgSoldierCardView>();
        private readonly List<string> _batchIds = new List<string>();

        private ConfigCsvRepository _configs;
        private WarriorPoolService _warriorPool;
        private DefendPrefabCatalog _defendCatalog;
        private Action _onComplete;
        private Coroutine _playRoutine;
        private Canvas _canvas;

        public bool IsWired =>
            _soldierScroll != null
            && _soldierContent != null
            && _cardTemplate != null
            && _bookSlots != null
            && _bookSlots.Length == SpecialEquipSlotsService.SlotCount;

        public void Begin(
            IReadOnlyList<string> batchWarriorIds,
            SpecialEquipSlotsService specialEquipSlots,
            ConfigCsvRepository configs,
            WarriorPoolService warriorPool,
            DefendPrefabCatalog defendCatalog,
            Action onComplete)
        {
            End();
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

        public void End()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            ClearCards();
            _configs = null;
            _warriorPool = null;
            _defendCatalog = null;
            _onComplete = null;
            _batchIds.Clear();
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

            var dim = CreatePanel(root.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            Stretch(dim.GetComponent<RectTransform>());

            var bookRow = CreatePanel(root.transform, "BookRow", new Color(0f, 0f, 0f, 0f));
            var bookRt = bookRow.GetComponent<RectTransform>();
            bookRt.anchorMin = new Vector2(0.5f, 0.5f);
            bookRt.anchorMax = new Vector2(0.5f, 0.5f);
            bookRt.pivot = new Vector2(0.5f, 0.5f);
            bookRt.anchoredPosition = new Vector2(0f, 220f);
            bookRt.sizeDelta = new Vector2(
                SpecialEquipSlotsService.SlotCount * AutoMfgMagicBookSlotView.SlotWidth
                + (SpecialEquipSlotsService.SlotCount - 1) * BookSpacing,
                AutoMfgMagicBookSlotView.SlotHeight);

            var hlg = bookRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = BookSpacing;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;

            var bookSlots = new AutoMfgMagicBookSlotView[SpecialEquipSlotsService.SlotCount];
            for (var i = 0; i < bookSlots.Length; i++)
            {
                bookSlots[i] = CreateBookSlot(bookRow.transform, i);
            }

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
            controller._soldierScroll = scroll;
            controller._soldierContent = contentRt;
            controller._cardTemplate = cardTemplate;
            controller._bookSlots = bookSlots;
            return controller;
        }

        private void BindBooks(SpecialEquipSlotsService slots)
        {
            for (var i = 0; i < _bookSlots.Length; i++)
            {
                var view = _bookSlots[i];
                if (view == null)
                {
                    continue;
                }

                view.ResetScale();
                var bookId = slots != null ? slots.GetSlot(i) : string.Empty;
                if (string.IsNullOrEmpty(bookId) || _configs == null || !_configs.TryGetMagicBook(bookId, out var row))
                {
                    view.BindEmpty();
                    continue;
                }

                Sprite icon = null;
                if (!string.IsNullOrEmpty(row.IconAssetId))
                {
                    icon = Resources.Load<Sprite>(row.IconAssetId);
                }

                var name = !string.IsNullOrEmpty(row.DisplayName) ? row.DisplayName : bookId;
                view.BindBook(name, icon);
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
            if (_soldierScroll != null)
            {
                _soldierScroll.horizontalNormalizedPosition = 0f;
            }
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
            yield return new WaitForSeconds(Step1HoldSeconds);

            var completed = 0;
            for (var i = 0; i < _cards.Count; i++)
            {
                var speed = Mathf.Pow(SpeedStep, completed / SpeedEveryN);
                var focusDur = BaseFocusSeconds / speed;
                var pulseDur = BaseBookPulseSeconds / speed;
                var revealHold = BaseRevealHoldSeconds / speed;

                yield return CoFocusCard(i, focusDur);

                var card = _cards[i];
                card.SetAmplifyVisible(true);

                for (var b = 0; b < _bookSlots.Length; b++)
                {
                    var slot = _bookSlots[b];
                    if (slot == null)
                    {
                        continue;
                    }

                    yield return CoPulseBook(slot, pulseDur);
                }

                card.SetAmplifyVisible(false);
                card.RevealIdle(ResolveIdleSprite(card.WarriorId));
                yield return new WaitForSeconds(revealHold);

                completed++;
            }

            _playRoutine = null;
            var done = _onComplete;
            _onComplete = null;
            done?.Invoke();
        }

        private IEnumerator CoFocusCard(int index, float duration)
        {
            if (_soldierScroll == null || _soldierContent == null || index < 0 || index >= _cards.Count)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();
            var target = ComputeFocusNormalized(index);
            var start = _soldierScroll.horizontalNormalizedPosition;
            if (duration <= 0.001f)
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
            if (_cards.Count <= 1 || _soldierScroll == null || _soldierScroll.viewport == null)
            {
                return 0f;
            }

            var content = _soldierContent;
            var viewport = _soldierScroll.viewport;
            var card = _cards[index].RectTransform;
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

            var cardCenter = card.anchoredPosition.x + card.rect.width * 0.5f;
            var desired = cardCenter - viewWidth * 0.5f;
            return Mathf.Clamp01(desired / scrollable);
        }

        private static IEnumerator CoPulseBook(AutoMfgMagicBookSlotView slot, float duration)
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

        private static AutoMfgMagicBookSlotView CreateBookSlot(Transform parent, int index)
        {
            var go = CreatePanel(parent, "BookSlot_" + index, new Color(0.28f, 0.3f, 0.4f, 0.95f));
            var le = go.AddComponent<LayoutElement>();
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

            var name = CreateText(go.transform, "Name", string.Empty, 18, TextAnchor.LowerCenter);
            var nameRt = name.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0.02f);
            nameRt.anchorMax = new Vector2(0.95f, 0.32f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            name.gameObject.SetActive(false);

            var view = go.AddComponent<AutoMfgMagicBookSlotView>();
            view.RuntimeWire(go.GetComponent<Image>(), icon, name);
            return view;
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
