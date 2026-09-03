using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Runtime UI-031 panel. Parent must be MetaCanvas (same nested-Canvas pattern as TitleSettings).
    /// The whole root starts inactive so boot/title never shows Box.
    /// </summary>
    public static class LevelRouteSelectRuntimeFactory
    {
        private const int ModalSortingOrder = 220;
        private const float MapDisplayWidth = LevelRouteSelectView.MapDisplayWidth;
        private const float BoxWidth = 1520f;
        private const float BoxHeight = 860f;

        public static LevelRouteSelectView Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("LevelRouteSelectRoot");
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            // Inactive before components/children so nothing flashes on boot.
            var root = new GameObject("LevelRouteSelectRoot");
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            root.AddComponent<CanvasRenderer>();
            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.72f);
            StretchFull(root.GetComponent<RectTransform>());

            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = ModalSortingOrder;
            root.AddComponent<GraphicRaycaster>();

            var box = CreatePanel(root.transform, "Box", new Color(0.12f, 0.14f, 0.18f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BoxWidth, BoxHeight));

            CreateText(box.transform, "Title", "路线选择", 30, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 44f));

            var closeGo = CreateButton(box.transform, "CloseButton", "X",
                new Color(0.45f, 0.22f, 0.22f, 1f),
                new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(48f, 48f));

            var tabBarGo = new GameObject("LevelTabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBarGo.transform.SetParent(box.transform, false);
            Place(tabBarGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1400f, 48f));
            var tabHlg = tabBarGo.GetComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 8f;
            tabHlg.childAlignment = TextAnchor.MiddleLeft;
            tabHlg.childControlWidth = false;
            tabHlg.childControlHeight = true;
            tabHlg.childForceExpandWidth = false;
            tabHlg.childForceExpandHeight = true;
            tabHlg.padding = new RectOffset(8, 8, 4, 4);

            var tabTemplate = new GameObject(
                "LevelTabTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            tabTemplate.transform.SetParent(tabBarGo.transform, false);
            tabTemplate.GetComponent<LayoutElement>().preferredWidth = 140f;
            tabTemplate.GetComponent<LayoutElement>().preferredHeight = 40f;
            tabTemplate.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.30f, 1f);
            CreateText(tabTemplate.transform, "Label", "Level", 18, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 36f));
            tabTemplate.SetActive(false);

            var stageScrollGo = BuildStageScroll(box.transform, out var content, out var stageRow, out var optionCard);
            var mapScrollGo = BuildMapScroll(box.transform, out var mapContent, out var mapBg, out var mapHost);

            var edgeLayer = new GameObject("EdgeLayer", typeof(RectTransform));
            edgeLayer.transform.SetParent(mapContent, false);
            StretchFull(edgeLayer.GetComponent<RectTransform>());
            var edgeRt = edgeLayer.GetComponent<RectTransform>();
            edgeRt.offsetMin = Vector2.zero;
            edgeRt.offsetMax = Vector2.zero;
            var edgeCg = edgeLayer.AddComponent<CanvasGroup>();
            edgeCg.blocksRaycasts = false;
            edgeCg.interactable = false;

            var tips = LevelRouteSelectView.BuildOptionHoverTips(box.transform);
            tips.SetActive(false);

            // Close above scroll so X always receives clicks.
            closeGo.transform.SetAsLastSibling();

            var view = root.AddComponent<LevelRouteSelectView>();
            view.BindRuntime(
                root,
                root.transform.Find("Box/Title")?.GetComponent<Text>(),
                content.transform,
                stageRow,
                optionCard,
                edgeRt,
                closeGo.GetComponent<Button>(),
                tabBarGo.transform,
                tabTemplate,
                stageScrollGo,
                mapScrollGo,
                mapContent,
                mapBg,
                mapHost,
                mapScrollGo.GetComponent<ScrollRect>(),
                tips,
                tips.transform.Find("Type")?.GetComponent<Text>(),
                tips.transform.Find("Title")?.GetComponent<Text>(),
                tips.transform.Find("Description")?.GetComponent<Text>(),
                tips.transform.Find("Reward")?.GetComponent<Text>());
            return view;
        }

        private static GameObject BuildStageScroll(
            Transform box,
            out GameObject content,
            out GameObject stageRow,
            out GameObject optionCard)
        {
            var scrollGo = new GameObject("StageScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box, false);
            Place(scrollGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1460f, 680f));
            scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 28f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            stageRow = CreatePanel(content.transform, "StageRowTemplate", new Color(0.15f, 0.17f, 0.22f, 0.9f));
            var rowLe = stageRow.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 180f;
            rowLe.minHeight = 160f;
            var rowVlg = stageRow.AddComponent<VerticalLayoutGroup>();
            rowVlg.padding = new RectOffset(12, 12, 8, 8);
            rowVlg.spacing = 8f;
            rowVlg.childControlHeight = true;
            rowVlg.childControlWidth = true;
            rowVlg.childForceExpandWidth = true;
            rowVlg.childForceExpandHeight = false;

            var stageLabel = CreateText(stageRow.transform, "StageLabel", "Stage", 20, TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 28f));
            var stageLabelLe = stageLabel.gameObject.AddComponent<LayoutElement>();
            stageLabelLe.preferredHeight = 28f;

            var optionsHost = new GameObject("OptionsHost", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            optionsHost.transform.SetParent(stageRow.transform, false);
            optionsHost.GetComponent<LayoutElement>().preferredHeight = 130f;
            var hlg = optionsHost.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;

            optionCard = new GameObject(
                "OptionCardTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            optionCard.transform.SetParent(optionsHost.transform, false);
            optionCard.GetComponent<LayoutElement>().preferredWidth = 200f;
            optionCard.GetComponent<LayoutElement>().preferredHeight = 120f;
            optionCard.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.32f, 1f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(optionCard.transform, false);
            Place(icon.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(36f, 36f));
            icon.GetComponent<Image>().raycastTarget = false;

            CreateText(optionCard.transform, "Type", "Dig", 14, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(100f, 22f));
            CreateText(optionCard.transform, "Title", "标题", 18, TextAnchor.UpperLeft,
                new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(168f, 24f));
            CreateText(optionCard.transform, "Description", "描述", 14, TextAnchor.UpperLeft,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(168f, 36f));
            CreateText(optionCard.transform, "Reward", "奖励：—", 13, TextAnchor.LowerLeft,
                new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(168f, 22f));

            stageRow.SetActive(false);
            optionCard.SetActive(false);
            return scrollGo;
        }

        private static GameObject BuildMapScroll(
            Transform box,
            out RectTransform mapContent,
            out Image mapBg,
            out RectTransform mapHost)
        {
            var scrollGo = new GameObject("MapScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box, false);
            Place(scrollGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1460f, 680f));
            scrollGo.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("MapContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            mapContent = contentGo.GetComponent<RectTransform>();
            mapContent.anchorMin = Vector2.zero;
            mapContent.anchorMax = Vector2.zero;
            mapContent.pivot = Vector2.zero;
            mapContent.anchoredPosition = Vector2.zero;
            mapContent.sizeDelta = new Vector2(MapDisplayWidth, MapDisplayWidth);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(contentGo.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            mapBg = bgGo.GetComponent<Image>();
            mapBg.color = new Color(0.1f, 0.12f, 0.14f, 1f);
            mapBg.raycastTarget = false;

            var hostGo = new GameObject("OptionsHost", typeof(RectTransform));
            hostGo.transform.SetParent(contentGo.transform, false);
            mapHost = hostGo.GetComponent<RectTransform>();
            StretchFull(mapHost);
            mapHost.pivot = Vector2.zero;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = mapContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollGo.SetActive(false);
            return scrollGo;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPos, size);
            return text;
        }

        private static GameObject CreateButton(
            Transform parent,
            string name,
            string label,
            Color color,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPos, size);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
