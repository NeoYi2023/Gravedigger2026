using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-031 OptionHoverTips: layout by GameplayType (SPEC_04 §9.31).
    /// </summary>
    public sealed class OptionHoverTipsController : MonoBehaviour
    {
        private const int MaxSlots = 3;
        private const string IconsFolder = "UI/Icons/";

        [SerializeField] private Text _title;
        [SerializeField] private GameObject _messageRow;
        [SerializeField] private GameObject[] _messageItems = Array.Empty<GameObject>();
        [SerializeField] private Image _tipsIcon;
        [SerializeField] private GameObject _rewardRow;
        [SerializeField] private GameObject[] _rewardItems = Array.Empty<GameObject>();
        [SerializeField] private Text _description;

        private ConfigCsvRepository _configs;
        private static Sprite _cachedUp;
        private static Sprite _cachedDown;
        private static bool _scaleSpritesLoaded;

        public void BindRefs(
            Text title,
            GameObject messageRow,
            GameObject[] messageItems,
            Image tipsIcon,
            GameObject rewardRow,
            GameObject[] rewardItems,
            Text description)
        {
            _title = title;
            _messageRow = messageRow;
            _messageItems = messageItems ?? Array.Empty<GameObject>();
            _tipsIcon = tipsIcon;
            _rewardRow = rewardRow;
            _rewardItems = rewardItems ?? Array.Empty<GameObject>();
            _description = description;
        }

        public void BindConfigs(ConfigCsvRepository configs)
        {
            _configs = configs;
        }

        public void Show(LevelRouteOptionSnapshot opt)
        {
            if (opt == null)
            {
                return;
            }

            if (_title != null)
            {
                _title.text = string.IsNullOrEmpty(opt.Title) ? opt.GameplayOptionId : opt.Title;
            }

            var showMessages = opt.GameplayType == GameplayState.Dig;
            var showTipsIcon =
                opt.GameplayType == GameplayState.Shop
                || opt.GameplayType == GameplayState.AutoManufacture
                || opt.GameplayType == GameplayState.UpgradeManufacture
                || opt.GameplayType == GameplayState.PushMap
                || opt.GameplayType == GameplayState.SearchExtract
                || opt.GameplayType == GameplayState.Defend;
            var showRewards =
                opt.GameplayType == GameplayState.PushMap
                || opt.GameplayType == GameplayState.SearchExtract
                || opt.GameplayType == GameplayState.Defend;

            if (_messageRow != null)
            {
                _messageRow.SetActive(showMessages);
            }

            if (showMessages)
            {
                BindMessages(opt.TipMessages);
            }

            BindTipsIcon(showTipsIcon ? opt.IconAssetId2 : null);
            if (_rewardRow != null)
            {
                _rewardRow.SetActive(showRewards);
            }

            if (showRewards)
            {
                BindRewards(opt.Reward);
            }

            BindDescription(opt.Description);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BindDescription(string description)
        {
            if (_description == null)
            {
                return;
            }

            var has = !string.IsNullOrEmpty(description);
            _description.gameObject.SetActive(has);
            if (has)
            {
                _description.text = description;
            }
        }

        private void BindTipsIcon(string iconAssetId2)
        {
            if (_tipsIcon == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(iconAssetId2))
            {
                _tipsIcon.gameObject.SetActive(false);
                return;
            }

            var sprite = LevelRouteIconLoader.Load(iconAssetId2);
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(IconsFolder + iconAssetId2);
            }

            if (sprite == null)
            {
                _tipsIcon.gameObject.SetActive(false);
                return;
            }

            _tipsIcon.sprite = sprite;
            _tipsIcon.preserveAspect = true;
            _tipsIcon.gameObject.SetActive(true);
        }

        private void BindMessages(string encoded)
        {
            var list = SubLevelTipMessageCatalog.Parse(encoded, SubLevelTipMessageCatalog.WarnDefault);
            EnsureScaleSprites();
            for (var i = 0; i < _messageItems.Length; i++)
            {
                var item = _messageItems[i];
                if (item == null)
                {
                    continue;
                }

                if (i >= list.Count)
                {
                    item.SetActive(false);
                    continue;
                }

                item.SetActive(true);
                var msg = list[i];
                var typeName = item.transform.Find("TypeName")?.GetComponent<Text>();
                var icon = item.transform.Find("Icon")?.GetComponent<Image>();
                var scaleRow = item.transform.Find("ScaleRow");

                var displayName = msg.MsgType;
                if (_configs != null
                    && _configs.TryGetLocalizedText(msg.TypeNameKey, out var zh)
                    && !string.IsNullOrEmpty(zh))
                {
                    displayName = zh;
                }

                if (typeName != null)
                {
                    typeName.text = displayName;
                }

                if (icon != null)
                {
                    icon.sprite = Resources.Load<Sprite>(IconsFolder + msg.IconAssetId);
                    icon.enabled = icon.sprite != null;
                    icon.preserveAspect = true;
                }

                BindScaleArrows(scaleRow, msg.StockScale);
            }

            if (_messageRow != null)
            {
                _messageRow.SetActive(list.Count > 0);
            }
        }

        private static void BindScaleArrows(Transform scaleRow, int stockScale)
        {
            if (scaleRow == null)
            {
                return;
            }

            EnsureScaleSprites();
            var abs = Math.Abs(stockScale);
            var up = stockScale > 0;
            for (var i = 0; i < MaxSlots; i++)
            {
                var arrow = scaleRow.Find("Arrow" + i)?.GetComponent<Image>();
                if (arrow == null)
                {
                    continue;
                }

                if (i < abs)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.sprite = up ? _cachedUp : _cachedDown;
                    arrow.enabled = arrow.sprite != null;
                    arrow.preserveAspect = true;
                }
                else
                {
                    arrow.gameObject.SetActive(false);
                }
            }
        }

        private void BindRewards(string rewardEncoded)
        {
            var entries = LootDropParser.ParseIdSemicolonCount(
                rewardEncoded,
                msg => Debug.LogWarning("[OptionHoverTips] " + msg));
            var count = Math.Min(entries.Count, MaxSlots);
            for (var i = 0; i < _rewardItems.Length; i++)
            {
                var item = _rewardItems[i];
                if (item == null)
                {
                    continue;
                }

                if (i >= count)
                {
                    item.SetActive(false);
                    continue;
                }

                item.SetActive(true);
                var entry = entries[i];
                var icon = item.transform.Find("Icon")?.GetComponent<Image>();
                var qty = item.transform.Find("Quantity")?.GetComponent<Text>();
                if (icon != null)
                {
                    icon.sprite = ItemIconLoader.LoadFromCatalog(_configs, entry.Id);
                    icon.enabled = true;
                    icon.preserveAspect = true;
                }

                if (qty != null)
                {
                    qty.text = entry.Count.ToString();
                }
            }

            if (_rewardRow != null)
            {
                _rewardRow.SetActive(count > 0);
            }
        }

        private static void EnsureScaleSprites()
        {
            if (_scaleSpritesLoaded)
            {
                return;
            }

            _scaleSpritesLoaded = true;
            _cachedUp = Resources.Load<Sprite>(IconsFolder + "Up_1");
            _cachedDown = Resources.Load<Sprite>(IconsFolder + "Down_1");
        }

        public static GameObject BuildHierarchy(Transform parent)
        {
            var tips = new GameObject(
                "OptionHoverTips",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(OptionHoverTipsController));
            tips.transform.SetParent(parent, false);
            var rt = tips.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(360f, 220f);
            rt.anchoredPosition = Vector2.zero;
            var img = tips.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.96f);
            img.raycastTarget = false;
            var vlg = tips.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            tips.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = CreateText(tips.transform, "Title", "标题", 20, FontStyle.Bold);
            title.GetComponent<LayoutElement>().preferredHeight = 28f;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.12f, 0.12f, 0.14f, 1f);

            var messageRow = new GameObject(
                "MessageRow",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            messageRow.transform.SetParent(tips.transform, false);
            messageRow.GetComponent<LayoutElement>().preferredHeight = 110f;
            var mH = messageRow.GetComponent<HorizontalLayoutGroup>();
            mH.spacing = 8f;
            mH.childAlignment = TextAnchor.MiddleCenter;
            mH.childControlWidth = true;
            mH.childControlHeight = true;
            mH.childForceExpandWidth = true;
            mH.childForceExpandHeight = true;

            var messageItems = new GameObject[MaxSlots];
            for (var i = 0; i < MaxSlots; i++)
            {
                messageItems[i] = BuildMessageItem(messageRow.transform, "MessageItem" + i);
            }

            var tipsIconGo = new GameObject(
                "TipsIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            tipsIconGo.transform.SetParent(tips.transform, false);
            var tipsIconLe = tipsIconGo.GetComponent<LayoutElement>();
            tipsIconLe.preferredHeight = 72f;
            tipsIconLe.minHeight = 56f;
            var tipsIcon = tipsIconGo.GetComponent<Image>();
            tipsIcon.color = new Color(0.85f, 0.85f, 0.88f, 1f);
            tipsIcon.preserveAspect = true;
            tipsIcon.raycastTarget = false;
            tipsIconGo.SetActive(false);

            var rewardRow = new GameObject(
                "RewardRow",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            rewardRow.transform.SetParent(tips.transform, false);
            rewardRow.GetComponent<LayoutElement>().preferredHeight = 72f;
            var rH = rewardRow.GetComponent<HorizontalLayoutGroup>();
            rH.spacing = 10f;
            rH.childAlignment = TextAnchor.MiddleCenter;
            rH.childControlWidth = false;
            rH.childControlHeight = true;
            rH.childForceExpandWidth = false;
            rH.childForceExpandHeight = true;

            var rewardItems = new GameObject[MaxSlots];
            for (var i = 0; i < MaxSlots; i++)
            {
                rewardItems[i] = BuildRewardItem(rewardRow.transform, "RewardItem" + i);
            }

            var desc = CreateText(tips.transform, "Description", "这里是描述.........", 14, FontStyle.Normal);
            desc.GetComponent<LayoutElement>().preferredHeight = 40f;
            desc.alignment = TextAnchor.UpperLeft;
            desc.color = new Color(0.2f, 0.2f, 0.22f, 1f);

            var ctrl = tips.GetComponent<OptionHoverTipsController>();
            ctrl.BindRefs(
                title,
                messageRow,
                messageItems,
                tipsIcon,
                rewardRow,
                rewardItems,
                desc);
            return tips;
        }

        private static GameObject BuildMessageItem(Transform parent, string name)
        {
            var item = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            item.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.94f, 1f);
            item.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var v = item.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(4, 4, 4, 4);
            v.spacing = 2f;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var typeName = CreateText(item.transform, "TypeName", "类型名", 13, FontStyle.Normal);
            typeName.GetComponent<LayoutElement>().preferredHeight = 18f;
            typeName.alignment = TextAnchor.MiddleCenter;
            typeName.color = new Color(0.15f, 0.15f, 0.18f, 1f);

            var iconGo = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            iconGo.transform.SetParent(item.transform, false);
            iconGo.GetComponent<LayoutElement>().preferredHeight = 48f;
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var scaleRow = new GameObject(
                "ScaleRow",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            scaleRow.transform.SetParent(item.transform, false);
            scaleRow.GetComponent<LayoutElement>().preferredHeight = 22f;
            var sh = scaleRow.GetComponent<HorizontalLayoutGroup>();
            sh.spacing = 2f;
            sh.childAlignment = TextAnchor.MiddleCenter;
            sh.childControlWidth = false;
            sh.childControlHeight = true;
            sh.childForceExpandWidth = false;
            sh.childForceExpandHeight = true;
            for (var i = 0; i < MaxSlots; i++)
            {
                var arrow = new GameObject(
                    "Arrow" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(LayoutElement));
                arrow.transform.SetParent(scaleRow.transform, false);
                arrow.GetComponent<LayoutElement>().preferredWidth = 18f;
                arrow.GetComponent<LayoutElement>().preferredHeight = 18f;
                var aImg = arrow.GetComponent<Image>();
                aImg.raycastTarget = false;
                aImg.preserveAspect = true;
                arrow.SetActive(false);
            }

            return item;
        }

        private static GameObject BuildRewardItem(Transform parent, string name)
        {
            var item = new GameObject(
                name,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            item.GetComponent<LayoutElement>().preferredWidth = 64f;
            var v = item.GetComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var iconGo = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            iconGo.transform.SetParent(item.transform, false);
            iconGo.GetComponent<LayoutElement>().preferredHeight = 48f;
            var icon = iconGo.GetComponent<Image>();
            icon.color = new Color(1f, 0.9f, 0.35f, 1f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var qty = CreateText(item.transform, "Quantity", "0", 13, FontStyle.Normal);
            qty.GetComponent<LayoutElement>().preferredHeight = 18f;
            qty.alignment = TextAnchor.MiddleCenter;
            qty.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            return item;
        }

        private static Text CreateText(Transform parent, string name, string sample, int fontSize, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = sample;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
