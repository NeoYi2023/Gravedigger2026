using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Runtime CombatIndicator HUD (UI-033 / D-089 Approach A) when Prefab not yet authored.
    /// </summary>
    public static class CombatIndicatorHudRuntimeFactory
    {
        public const float RootAnchoredY = -10f;
        public const float HudScale = 0.75f;
        public const int DefaultSortingOrder = 68;

        public static readonly Vector2 CenterSize = new Vector2(102f, 143f);
        public static readonly Vector2 SlotSize = new Vector2(42f, 72f);
        public static readonly Vector2 DeathMarkSize = new Vector2(36f, 60f);

        public static CombatIndicatorHudView Create(Transform parent, int sortingOrder = DefaultSortingOrder)
        {
            var canvasGo = new GameObject(
                "CombatIndicatorCanvas",
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
            scaler.matchWidthOrHeight = 0.5f;

            var rootGo = new GameObject("CombatIndicatorHud", typeof(RectTransform), typeof(CombatIndicatorHudView));
            rootGo.transform.SetParent(canvasGo.transform, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, RootAnchoredY);
            rootRt.sizeDelta = new Vector2(1920f, 400f);
            rootRt.localScale = Vector3.one * HudScale;

            var centerBg = CreateImage(rootGo.transform, "CenterBg", CenterSize, Vector2.zero);
            centerBg.sprite = LoadIcon("HPPK_UI_1");
            centerBg.preserveAspect = true;

            // Alive counts live inside CenterBg left/right halves (not outside beside slots).
            var allyCount = CreateText(
                centerBg.transform,
                "AllyAliveCount",
                "0",
                42,
                TextAnchor.MiddleCenter,
                new Color(0.45f, 0.7f, 1f, 1f));
            ApplyAliveCountBestFit(allyCount);
            var allyCountRt = allyCount.rectTransform;
            allyCountRt.anchorMin = new Vector2(0.5f, 1f);
            allyCountRt.anchorMax = new Vector2(0.5f, 1f);
            allyCountRt.pivot = new Vector2(0.5f, 0.5f);
            allyCountRt.anchoredPosition = new Vector2(-CenterSize.x * 0.25f, -CenterSize.y * 0.5f);
            allyCountRt.sizeDelta = new Vector2(56f, 48f);

            var enemyCount = CreateText(
                centerBg.transform,
                "EnemyAliveCount",
                "?",
                42,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.4f, 0.4f, 1f));
            ApplyAliveCountBestFit(enemyCount);
            var enemyCountRt = enemyCount.rectTransform;
            enemyCountRt.anchorMin = new Vector2(0.5f, 1f);
            enemyCountRt.anchorMax = new Vector2(0.5f, 1f);
            enemyCountRt.pivot = new Vector2(0.5f, 0.5f);
            enemyCountRt.anchoredPosition = new Vector2(CenterSize.x * 0.25f, -CenterSize.y * 0.5f);
            enemyCountRt.sizeDelta = new Vector2(56f, 48f);

            var allyRow = CreateSlotHost(rootGo.transform, "AllySlots");
            var enemyRow = CreateSlotHost(rootGo.transform, "EnemySlots");
            var poolHost = new GameObject("SlotPool", typeof(RectTransform));
            poolHost.transform.SetParent(rootGo.transform, false);
            poolHost.SetActive(false);

            var allyTemplate = CreateSlotTemplate(poolHost.transform, "AllySlotTemplate", "HPPK_UI_2");
            var enemyTemplate = CreateSlotTemplate(poolHost.transform, "EnemySlotTemplate", "HPPK_UI_3");
            var deathSprite = LoadIcon("HPPK_UI_4");

            var view = rootGo.GetComponent<CombatIndicatorHudView>();
            view.Configure(
                canvasGo,
                allyCount,
                enemyCount,
                allyRow,
                enemyRow,
                allyTemplate,
                enemyTemplate,
                deathSprite,
                SlotSize,
                CenterSize.x,
                CenterSize.y);
            canvasGo.SetActive(false);
            return view;
        }

        private static void ApplyAliveCountBestFit(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 26;
            text.resizeTextMaxSize = 42;
        }

        private static RectTransform CreateSlotHost(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        private static RectTransform CreateSlotTemplate(Transform parent, string name, string frameIcon)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = SlotSize;
            var frame = go.GetComponent<Image>();
            frame.sprite = LoadIcon(frameIcon);
            frame.preserveAspect = true;
            frame.raycastTarget = false;
            frame.color = Color.white;

            var silGo = new GameObject("Silhouette", typeof(RectTransform), typeof(Image));
            silGo.transform.SetParent(go.transform, false);
            var silRt = silGo.GetComponent<RectTransform>();
            silRt.anchorMin = new Vector2(0.5f, 0.5f);
            silRt.anchorMax = new Vector2(0.5f, 0.5f);
            silRt.pivot = new Vector2(0.5f, 0.5f);
            silRt.sizeDelta = new Vector2(SlotSize.x - 6f, SlotSize.y - 10f);
            var sil = silGo.GetComponent<Image>();
            sil.preserveAspect = true;
            sil.raycastTarget = false;
            sil.color = Color.white;

            var deathGo = new GameObject("DeathMark", typeof(RectTransform), typeof(Image));
            deathGo.transform.SetParent(go.transform, false);
            var deathRt = deathGo.GetComponent<RectTransform>();
            deathRt.anchorMin = new Vector2(0.5f, 0.5f);
            deathRt.anchorMax = new Vector2(0.5f, 0.5f);
            deathRt.pivot = new Vector2(0.5f, 0.5f);
            deathRt.sizeDelta = DeathMarkSize;
            var death = deathGo.GetComponent<Image>();
            death.preserveAspect = true;
            death.raycastTarget = false;
            death.enabled = false;
            deathGo.SetActive(false);

            go.SetActive(false);
            return rt;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Sprite LoadIcon(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return null;
            }

            return Resources.Load<Sprite>($"UI/Icons/{assetId}");
        }
    }
}
