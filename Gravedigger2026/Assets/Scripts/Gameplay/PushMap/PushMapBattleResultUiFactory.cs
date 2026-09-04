using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Runtime-builds UI-017/018 under a Canvas on the stage root (Demo; Prefab optional).
    /// Shared by PushMap and SearchExtract.
    /// </summary>
    public static class PushMapBattleResultUiFactory
    {
        private const string IconWarrior = "Assets/Art/UI/Icons/WarriorIcon.png";
        private const string IconArcher = "Assets/Art/UI/Icons/ArcherIcon.png";
        private const string IconMage = "Assets/Art/UI/Icons/MageIcon.png";
        private const string IconAssassin = "Assets/Art/UI/Icons/AssassinIcon.png";

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

            if (settlement == null)
            {
                settlement = BuildSettlement(canvasGo.transform);
            }

            if (reward == null)
            {
                reward = BuildReward(canvasGo.transform);
            }
        }

        /// <summary>Ensure settlement only (SearchExtract may omit reward popup).</summary>
        public static PushMapBattleSettlementView EnsureSettlement(Transform parent)
        {
            Ensure(parent, out var settlement, out _);
            return settlement;
        }

        private static PushMapBattleSettlementView BuildSettlement(Transform canvas)
        {
            var root = CreatePanel(canvas, "BattleSettlementRoot", new Color(0.05f, 0.06f, 0.09f, 0.88f));
            Place(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                Vector2.zero);

            var panel = CreatePanel(root.transform, "Panel", new Color(0.12f, 0.14f, 0.18f, 0.96f));
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 620f));

            var result = CreateText(panel.transform, "ResultText", "胜利", 48, TextAnchor.MiddleCenter);
            Place(result.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(680f, 56f));

            var elapsed = CreateText(panel.transform, "ElapsedText", "战斗耗时：00:00", 26, TextAnchor.MiddleCenter);
            Place(elapsed.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(680f, 36f));

            var kills = CreateText(panel.transform, "KillsText", "击杀怪物总数：0", 26, TextAnchor.MiddleCenter);
            Place(kills.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(680f, 36f));

            var casualty = CreateText(panel.transform, "CasualtyTotalText", "阵亡士兵总数：0", 26,
                TextAnchor.MiddleCenter);
            Place(casualty.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(680f, 36f));

            var classRow = new GameObject("ClassCasualtyRow", typeof(RectTransform));
            classRow.transform.SetParent(panel.transform, false);
            Place(classRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 88f));

            var warriorCount = CreateClassCell(classRow.transform, "Warrior", IconWarrior, -255f);
            var archerCount = CreateClassCell(classRow.transform, "Archer", IconArcher, -85f);
            var mageCount = CreateClassCell(classRow.transform, "Mage", IconMage, 85f);
            var thiefCount = CreateClassCell(classRow.transform, "Assassin", IconAssassin, 255f);

            var victoryButtons = new GameObject("VictoryButtons", typeof(RectTransform));
            victoryButtons.transform.SetParent(panel.transform, false);
            Place(victoryButtons.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(680f, 72f));

            var continueBtn = CreateButton(victoryButtons.transform, "ContinueButton", "继续",
                new Color(0.28f, 0.55f, 0.35f, 1f));
            Place(continueBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 64f));

            var defeatButtons = new GameObject("DefeatButtons", typeof(RectTransform));
            defeatButtons.transform.SetParent(panel.transform, false);
            Place(defeatButtons.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(680f, 72f));

            var returnBtn = CreateButton(defeatButtons.transform, "ReturnTitleButton", "返回主界面",
                new Color(0.35f, 0.38f, 0.48f, 1f));
            Place(returnBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-140f, 0f), new Vector2(240f, 64f));

            var restartBtn = CreateButton(defeatButtons.transform, "RestartButton", "重新开始",
                new Color(0.55f, 0.32f, 0.28f, 1f));
            Place(restartBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(140f, 0f), new Vector2(240f, 64f));

            defeatButtons.SetActive(false);

            var view = root.AddComponent<PushMapBattleSettlementView>();
            view.Bind(
                root,
                result,
                elapsed,
                kills,
                casualty,
                classRow,
                warriorCount,
                archerCount,
                mageCount,
                thiefCount,
                continueBtn.GetComponent<Button>(),
                returnBtn.GetComponent<Button>(),
                restartBtn.GetComponent<Button>(),
                victoryButtons,
                defeatButtons);
            root.SetActive(false);
            return view;
        }

        private static Text CreateClassCell(Transform parent, string name, string iconPath, float x)
        {
            var cell = new GameObject(name + "Cell", typeof(RectTransform));
            cell.transform.SetParent(parent, false);
            Place(cell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(140f, 88f));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cell.transform, false);
            Place(iconGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(48f, 48f));
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = LoadIconSprite(iconPath);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var count = CreateText(cell.transform, "Count", "0", 24, TextAnchor.MiddleCenter);
            Place(count.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(120f, 28f));
            return count;
        }

        private static Sprite LoadIconSprite(string assetPath)
        {
            var resourceName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var fromResources = Resources.Load<Sprite>("UI/Icons/" + resourceName);
            if (fromResources != null)
            {
                return fromResources;
            }

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
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
            rt.sizeDelta = size;
        }
    }
}
