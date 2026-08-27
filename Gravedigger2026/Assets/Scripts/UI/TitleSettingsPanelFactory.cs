using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Runtime fallback when TitleSettingsPanel Prefab is not yet Ensure'd (UI-028).
    /// </summary>
    public static class TitleSettingsPanelFactory
    {
        private const int ModalSortingOrder = 100;

        public static TitleSettingsPanelView Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("TitleSettingsPanel");
            if (existing != null)
            {
                var existingView = existing.GetComponent<TitleSettingsPanelView>();
                if (existingView != null)
                {
                    return existingView;
                }

                Object.Destroy(existing.gameObject);
            }

            var root = CreatePanel(parent, "TitleSettingsPanel", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = ModalSortingOrder;
            root.AddComponent<GraphicRaycaster>();

            var box = CreatePanel(root.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(640f, 620f));

            CreateText(box.transform, "Title", "设置", 28, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(560f, 40f));

            var displayTabBtn = CreateButton(box.transform, "DisplayTabButton", "显示",
                new Color(0.35f, 0.55f, 0.38f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -64f), new Vector2(120f, 40f));

            var displayRoot = CreatePanel(box.transform, "DisplayTab", new Color(0.12f, 0.13f, 0.16f, 1f));
            Place(displayRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(580f, 420f));

            CreateText(displayRoot.transform, "ResolutionLabel", "分辨率", 22, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(200f, 32f));

            var scrollGo = new GameObject("ResolutionScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(displayRoot.transform, false);
            Place(scrollGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(548f, 220f));
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rowTemplate = CreateButton(content.transform, "ResolutionRowTemplate", "1920 × 1080",
                new Color(0.22f, 0.28f, 0.36f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 40f));
            var rowLe = rowTemplate.AddComponent<LayoutElement>();
            rowLe.minHeight = 40f;
            rowLe.preferredHeight = 40f;
            rowTemplate.SetActive(false);

            CreateText(displayRoot.transform, "ModeLabel", "显示模式", 22, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(16f, 120f), new Vector2(200f, 32f));

            var modeWindowed = CreateButton(displayRoot.transform, "ModeWindowedButton", "窗口",
                new Color(0.28f, 0.38f, 0.52f, 1f),
                new Vector2(0f, 0f), new Vector2(16f, 64f), new Vector2(170f, 44f));
            var modeBorderless = CreateButton(displayRoot.transform, "ModeBorderlessButton", "无边框全屏",
                new Color(0.28f, 0.38f, 0.52f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(170f, 44f));
            var modeExclusive = CreateButton(displayRoot.transform, "ModeExclusiveButton", "独占全屏",
                new Color(0.28f, 0.38f, 0.52f, 1f),
                new Vector2(1f, 0f), new Vector2(-16f, 64f), new Vector2(170f, 44f));
            var apply = CreateButton(displayRoot.transform, "ApplyButton", "应用",
                new Color(0.25f, 0.55f, 0.35f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(160f, 44f));

            var displayTab = displayRoot.AddComponent<DisplaySettingsTabView>();
            displayTab.BindRuntime(
                displayRoot,
                content.transform,
                rowTemplate,
                modeWindowed.GetComponent<Button>(),
                modeBorderless.GetComponent<Button>(),
                modeExclusive.GetComponent<Button>(),
                apply.GetComponent<Button>());

            var close = CreateButton(box.transform, "CloseButton", "关闭",
                new Color(0.40f, 0.40f, 0.42f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(180f, 44f));

            var view = root.AddComponent<TitleSettingsPanelView>();
            view.BindRuntime(
                root,
                displayTabBtn.GetComponent<Button>(),
                displayTab,
                close.GetComponent<Button>());

            root.SetActive(false);
            return view;
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Place(go.GetComponent<RectTransform>(), anchor, anchor, anchor, anchoredPos, size);
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
            Place(go.GetComponent<RectTransform>(), anchor, anchor, anchor, anchoredPos, size);
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

        private static void Place(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
