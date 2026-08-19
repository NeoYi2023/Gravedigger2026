using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Runtime builder for combat bond HUD when no prefab canvas exists (PushMap).
    /// </summary>
    public static class FormationBondHudRuntimeFactory
    {
        public static FormationBondHudView Create(Transform parent, int sortingOrder = 65)
        {
            var canvasGo = new GameObject(
                "CombatBondCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var detailView = CreateDetailModal(canvasGo.transform);

            var hudGo = new GameObject("BondHudRoot", typeof(RectTransform), typeof(FormationBondHudView));
            hudGo.transform.SetParent(canvasGo.transform, false);
            var hudRt = hudGo.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0f, 1f);
            hudRt.anchorMax = new Vector2(0f, 1f);
            hudRt.pivot = new Vector2(0f, 1f);
            // PushMap/Combat HUD placement: top-left, but slightly lower to avoid other left-top widgets overlap.
            hudRt.anchoredPosition = new Vector2(24f, -70f);
            hudRt.sizeDelta = new Vector2(160f, 400f);

            var viewBtnGo = CreateButton(hudGo.transform, "ViewBondsButton", "查看阵容羁绊",
                new Color(0.28f, 0.36f, 0.48f, 1f), new Vector2(0f, 0f), new Vector2(160f, 36f));

            var iconRowGo = new GameObject(
                "ActiveBondIconsRow",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            iconRowGo.transform.SetParent(hudGo.transform, false);
            var iconRt = iconRowGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 1f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot = new Vector2(0f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -42f);
            iconRt.sizeDelta = new Vector2(40f, 0f);
            var vlg = iconRowGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            var iconFitter = iconRowGo.GetComponent<ContentSizeFitter>();
            iconFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            iconFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hudView = hudGo.GetComponent<FormationBondHudView>();
            hudView.Configure(viewBtnGo.GetComponent<Button>(), iconRt, detailView);
            return hudView;
        }

        private static FormationBondDetailView CreateDetailModal(Transform parent)
        {
            var detailGo = new GameObject("BondDetailModal", typeof(RectTransform), typeof(Image));
            detailGo.transform.SetParent(parent, false);
            var detailRt = detailGo.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.5f, 0.5f);
            detailRt.anchorMax = new Vector2(0.5f, 0.5f);
            detailRt.pivot = new Vector2(0.5f, 0.5f);
            detailRt.sizeDelta = new Vector2(720f, 520f);
            detailGo.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

            var detailView = detailGo.AddComponent<FormationBondDetailView>();
            var title = CreateText(detailGo.transform, "Title", "阵容羁绊", 24, TextAnchor.UpperCenter);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(0f, 40f);

            var body = CreateText(detailGo.transform, "Body", string.Empty, 16, TextAnchor.UpperLeft);
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(16f, 56f);
            bodyRt.offsetMax = new Vector2(-16f, -72f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;

            var closeBtnGo = CreateButton(detailGo.transform, "CloseButton", "关闭",
                new Color(0.35f, 0.4f, 0.5f, 1f), new Vector2(0f, 28f), new Vector2(140f, 40f));
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);

            detailView.Configure(detailGo, title, body, closeBtnGo.GetComponent<Button>());
            return detailView;
        }

        private static GameObject CreateButton(
            Transform parent,
            string name,
            string label,
            Color color,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            var text = CreateText(go.transform, "Label", label, 14, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            return go;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
