using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.EditorTools.Level
{
    /// <summary>
    /// Generates LevelRouteSelectRoot Prefab (UI-031 / D-086).
    /// </summary>
    public static class LevelRouteSelectAssetBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Level/LevelRouteSelectRoot.prefab";
        private const string MenuPath = "Gravedigger2026/Level/Ensure LevelRouteSelectRoot Prefab (UI-031)";

        [MenuItem(MenuPath)]
        public static void EnsurePrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Level");

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject rootGo;
            LevelRouteSelectView view;
            if (existing != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(existing) as GameObject;
                rootGo = instance;
                view = rootGo.GetComponent<LevelRouteSelectView>();
                if (view == null)
                {
                    view = rootGo.AddComponent<LevelRouteSelectView>();
                }
            }
            else
            {
                rootGo = BuildFresh();
                view = rootGo.GetComponent<LevelRouteSelectView>();
            }

            WireView(view, rootGo);
            PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelRouteSelect] Ensured Prefab at {PrefabPath}");
        }

        private static GameObject BuildFresh()
        {
            var canvasGo = new GameObject(
                "LevelRouteSelectRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(LevelRouteSelectView));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = CreateUi(canvasGo.transform, "Panel");
            StretchFull(panel.GetComponent<RectTransform>());
            panel.SetActive(false);

            var backdrop = CreateUi(panel.transform, "Backdrop", typeof(Image));
            StretchFull(backdrop.GetComponent<RectTransform>());
            backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var box = CreateUi(panel.transform, "Box", typeof(Image));
            var boxRt = box.GetComponent<RectTransform>();
            Place(boxRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 860f));
            box.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

            var title = CreateText(box.transform, "Title", "路线选择", 30, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 44f));

            var closeGo = CreateUi(box.transform, "CloseButton", typeof(Image), typeof(Button));
            Place(closeGo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(48f, 48f));
            closeGo.GetComponent<Image>().color = new Color(0.45f, 0.22f, 0.22f, 1f);
            var closeLabel = CreateText(closeGo.transform, "Label", "X", 22, TextAnchor.MiddleCenter);
            StretchFull(closeLabel.GetComponent<RectTransform>());

            var edgeLayer = CreateUi(box.transform, "EdgeLayer", typeof(RectTransform));
            StretchFull(edgeLayer.GetComponent<RectTransform>());
            // leave space under title
            var edgeRt = edgeLayer.GetComponent<RectTransform>();
            edgeRt.offsetMin = new Vector2(24f, 24f);
            edgeRt.offsetMax = new Vector2(-24f, -70f);
            var edgeCg = edgeLayer.AddComponent<CanvasGroup>();
            edgeCg.blocksRaycasts = false;
            edgeCg.interactable = false;

            var scrollGo = CreateUi(box.transform, "StageScroll", typeof(Image), typeof(ScrollRect));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1040f, 740f));
            scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 1f);

            var viewport = CreateUi(scrollGo.transform, "Viewport", typeof(Image), typeof(Mask));
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = CreateUi(viewport.transform, "Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
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
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            // EdgeLayer above scroll for line visibility; edge Images use raycastTarget=false.
            edgeLayer.transform.SetAsLastSibling();

            var stageRow = CreateUi(content.transform, "StageRowTemplate", typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            stageRow.GetComponent<Image>().color = new Color(0.15f, 0.17f, 0.22f, 0.9f);
            stageRow.GetComponent<LayoutElement>().preferredHeight = 180f;
            stageRow.GetComponent<LayoutElement>().minHeight = 160f;
            var rowVlg = stageRow.GetComponent<VerticalLayoutGroup>();
            rowVlg.padding = new RectOffset(12, 12, 8, 8);
            rowVlg.spacing = 8f;
            rowVlg.childAlignment = TextAnchor.UpperCenter;
            rowVlg.childControlHeight = true;
            rowVlg.childControlWidth = true;
            rowVlg.childForceExpandWidth = true;
            rowVlg.childForceExpandHeight = false;

            var stageLabel = CreateText(stageRow.transform, "StageLabel", "Stage", 20, TextAnchor.MiddleLeft);
            var stageLabelLe = stageLabel.GetComponent<LayoutElement>();
            if (stageLabelLe == null)
            {
                stageLabelLe = stageLabel.AddComponent<LayoutElement>();
            }

            stageLabelLe.preferredHeight = 28f;

            var optionsHost = CreateUi(stageRow.transform, "OptionsHost", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            optionsHost.GetComponent<LayoutElement>().preferredHeight = 130f;
            var hlg = optionsHost.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;

            var optionCard = CreateUi(optionsHost.transform, "OptionCardTemplate", typeof(Image), typeof(Button), typeof(LayoutElement));
            optionCard.GetComponent<LayoutElement>().preferredWidth = 200f;
            optionCard.GetComponent<LayoutElement>().preferredHeight = 120f;
            optionCard.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.32f, 1f);

            var icon = CreateUi(optionCard.transform, "Icon", typeof(Image));
            Place(icon.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(36f, 36f));

            var type = CreateText(optionCard.transform, "Type", "Dig", 14, TextAnchor.MiddleRight);
            Place(type.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(100f, 22f));

            var optTitle = CreateText(optionCard.transform, "Title", "标题", 18, TextAnchor.UpperLeft);
            Place(optTitle.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(-16f, 24f));

            var desc = CreateText(optionCard.transform, "Description", "描述", 14, TextAnchor.UpperLeft);
            Place(desc.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(-16f, 36f));

            var reward = CreateText(optionCard.transform, "Reward", "奖励：—", 13, TextAnchor.LowerLeft);
            Place(reward.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-16f, 22f));

            stageRow.SetActive(false);
            optionCard.SetActive(false);

            return canvasGo;
        }

        private static void WireView(LevelRouteSelectView view, GameObject rootGo)
        {
            // Prefer Panel visual root (Canvas+View stay active). Legacy: Box under canvas.
            var panelTf = rootGo.transform.Find("Panel");
            if (panelTf == null)
            {
                EnsureLegacyMigratedToPanel(rootGo);
                panelTf = rootGo.transform.Find("Panel");
            }

            var panel = panelTf != null ? panelTf.gameObject : rootGo;
            var boxPrefix = panelTf != null ? "Panel/Box" : "Box";
            var title = rootGo.transform.Find(boxPrefix + "/Title")?.GetComponent<Text>();
            var content = rootGo.transform.Find(boxPrefix + "/StageScroll/Viewport/Content");
            var stageRow = content != null ? content.Find("StageRowTemplate")?.gameObject : null;
            var optionCard = stageRow != null ? stageRow.transform.Find("OptionsHost/OptionCardTemplate")?.gameObject : null;
            var edge = rootGo.transform.Find(boxPrefix + "/EdgeLayer")?.GetComponent<RectTransform>();
            var close = rootGo.transform.Find(boxPrefix + "/CloseButton")?.GetComponent<Button>();
            view.BindRuntime(panel, title, content, stageRow, optionCard, edge, close);
            if (panel != null && panel != rootGo)
            {
                panel.SetActive(false);
            }

            EditorUtility.SetDirty(view);
        }

        private static void EnsureLegacyMigratedToPanel(GameObject rootGo)
        {
            var backdrop = rootGo.transform.Find("Backdrop");
            var box = rootGo.transform.Find("Box");
            if (backdrop == null && box == null)
            {
                return;
            }

            var panel = CreateUi(rootGo.transform, "Panel");
            StretchFull(panel.GetComponent<RectTransform>());
            if (backdrop != null)
            {
                backdrop.SetParent(panel.transform, false);
            }

            if (box != null)
            {
                box.SetParent(panel.transform, false);
            }

            panel.SetActive(false);
        }

        private static GameObject CreateUi(Transform parent, string name, params System.Type[] types)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] == typeof(RectTransform))
                {
                    continue;
                }

                go.AddComponent(types[i]);
            }

            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string value, int size, TextAnchor anchor)
        {
            var go = CreateUi(parent, name, typeof(Text));
            var text = go.GetComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
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
            if (Mathf.Approximately(anchorMin.x, anchorMax.x) || Mathf.Approximately(anchorMin.y, anchorMax.y))
            {
                rt.sizeDelta = size;
            }
            else
            {
                rt.offsetMin = new Vector2(size.x * -0.5f, size.y * -0.5f);
                rt.offsetMax = new Vector2(size.x * 0.5f, size.y * 0.5f);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var cur = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }

                cur = next;
            }
        }
    }
}
