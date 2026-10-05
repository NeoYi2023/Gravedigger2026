using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-036 shell. Prefab is authoritative after Ensure; this builds the same hierarchy when the Prefab is missing.
    /// </summary>
    public static class SandboxRuntimeFactory
    {
        public const int ModalSortingOrder = 200;

        public static SandboxRootView Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("SandboxRoot");
            if (existing != null)
            {
                var existingView = existing.GetComponent<SandboxRootView>();
                if (existingView != null)
                {
                    return existingView;
                }

                Object.Destroy(existing.gameObject);
            }

            var root = new GameObject("SandboxRoot");
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            var rootRt = root.AddComponent<RectTransform>();
            StretchFull(rootRt);
            var dim = root.AddComponent<Image>();
            dim.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = ModalSortingOrder;
            root.AddComponent<GraphicRaycaster>();
            var view = root.AddComponent<SandboxRootView>();

            var title = CreateText(root.transform, "Title", "沙盘", 36, TextAnchor.MiddleCenter);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(800f, 56f);

            var backGo = new GameObject("BackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backGo.transform.SetParent(root.transform, false);
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 1f);
            backRt.anchorMax = new Vector2(0f, 1f);
            backRt.pivot = new Vector2(0f, 1f);
            backRt.anchoredPosition = new Vector2(36f, -24f);
            backRt.sizeDelta = new Vector2(160f, 48f);
            backGo.GetComponent<Image>().color = new Color(0.35f, 0.38f, 0.42f, 1f);
            var backLabel = CreateText(backGo.transform, "Label", "返回", 24, TextAnchor.MiddleCenter);
            StretchFull(backLabel.rectTransform);

            var scrollGo = new GameObject("NodeScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.sizeDelta = new Vector2(1680f, 420f);
            scrollRt.anchoredPosition = new Vector2(0f, -20f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

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
            contentRt.sizeDelta = new Vector2(0f, 320f);
            var layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;

            var empty = CreateText(root.transform, "Empty", "该难度没有沙盘节点", 26, TextAnchor.MiddleCenter);
            var emptyRt = empty.rectTransform;
            emptyRt.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRt.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRt.sizeDelta = new Vector2(640f, 48f);
            empty.gameObject.SetActive(false);

            view.BindRuntime(root, title, backGo.GetComponent<Button>(), contentRt, empty);
            return view;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
