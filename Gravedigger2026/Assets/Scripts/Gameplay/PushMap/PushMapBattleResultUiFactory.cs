using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Runtime-builds UI-017/018 under a Canvas on the PushMap stage root (Demo; Prefab optional).
    /// </summary>
    public static class PushMapBattleResultUiFactory
    {
        public static void Ensure(
            Transform parent,
            out PushMapBattleSettlementView settlement,
            out PushMapRewardPopupView reward)
        {
            settlement = parent != null
                ? parent.GetComponentInChildren<PushMapBattleSettlementView>(true)
                : null;
            reward = parent != null
                ? parent.GetComponentInChildren<PushMapRewardPopupView>(true)
                : null;
            if (settlement != null && reward != null)
            {
                return;
            }

            var canvasGo = new GameObject("PushMapBattleResultCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
            {
                canvasGo.transform.SetParent(parent, false);
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            settlement = BuildSettlement(canvasGo.transform);
            reward = BuildReward(canvasGo.transform);
        }

        private static PushMapBattleSettlementView BuildSettlement(Transform canvas)
        {
            var root = CreatePanel(canvas, "BattleSettlementRoot", new Color(0.05f, 0.06f, 0.09f, 0.88f));
            Place(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                Vector2.zero);

            var panel = CreatePanel(root.transform, "Panel", new Color(0.12f, 0.14f, 0.18f, 0.96f));
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f));

            var result = CreateText(panel.transform, "ResultText", "胜利", 48, TextAnchor.MiddleCenter);
            Place(result.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(640f, 64f));

            var elapsed = CreateText(panel.transform, "ElapsedText", "战斗耗时：00:00", 28, TextAnchor.MiddleCenter);
            Place(elapsed.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(640f, 40f));

            var kills = CreateText(panel.transform, "KillsText", "击杀怪物总数：0", 28, TextAnchor.MiddleCenter);
            Place(kills.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(640f, 40f));

            var btn = CreateButton(panel.transform, "ContinueButton", "继续", new Color(0.28f, 0.55f, 0.35f, 1f));
            Place(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(220f, 64f));

            var view = root.AddComponent<PushMapBattleSettlementView>();
            view.Bind(root, result, elapsed, kills, btn.GetComponent<Button>());
            root.SetActive(false);
            return view;
        }

        private static PushMapRewardPopupView BuildReward(Transform canvas)
        {
            var root = CreatePanel(canvas, "RewardPopupRoot", new Color(0.05f, 0.06f, 0.09f, 0.88f));
            Place(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                Vector2.zero);

            var panel = CreatePanel(root.transform, "Panel", new Color(0.12f, 0.14f, 0.18f, 0.96f));
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 560f));

            var title = CreateText(panel.transform, "TitleText", "奖励", 40, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(640f, 56f));

            var body = CreateText(panel.transform, "BodyText", "", 26, TextAnchor.UpperLeft);
            Place(body.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(620f, 340f));

            var btn = CreateButton(panel.transform, "ContinueButton", "继续", new Color(0.28f, 0.55f, 0.35f, 1f));
            Place(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(220f, 64f));

            var view = root.AddComponent<PushMapRewardPopupView>();
            view.Bind(root, title, body, btn.GetComponent<Button>());
            root.SetActive(false);
            return view;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return t;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var textGo = CreateText(go.transform, "Label", label, 28, TextAnchor.MiddleCenter);
            Place(textGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                Vector2.zero);
            return go;
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
            if (size.sqrMagnitude > 0.01f)
            {
                rt.sizeDelta = size;
            }
            else if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
            {
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }
}
