using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.EditorTools.Level
{
    /// <summary>
    /// Generates LevelRouteSelectRoot Prefab (UI-031 / D-086) + Resources map copy.
    /// </summary>
    public static class LevelRouteSelectAssetBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Level/LevelRouteSelectRoot.prefab";
        private const string MenuPath = "Gravedigger2026/Level/Ensure LevelRouteSelectRoot Prefab (UI-031)";
        private const string MapMenuPath = "Gravedigger2026/Level/Ensure Route Map Resources (UI-031)";
        private const string ArtMapDir = "Assets/Art/UI/SubLevelMaps";
        private const string ResourcesMapDir = "Assets/Resources/UI/SubLevelMaps";
        private const float MapDisplayWidth = LevelRouteSelectView.MapDisplayWidth;
        private const float BoxWidth = 1520f;
        private const float BoxHeight = 860f;

        [MenuItem(MenuPath)]
        public static void EnsurePrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Level");
            EnsureRouteMapResources();

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

        [MenuItem(MapMenuPath)]
        public static void EnsureRouteMapResources()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            EnsureFolder(ResourcesMapDir);

            if (!AssetDatabase.IsValidFolder(ArtMapDir))
            {
                Debug.LogWarning($"[LevelRouteSelect] Art map folder missing: {ArtMapDir}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtMapDir });
            var copied = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var src = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(src) || !src.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = System.IO.Path.GetFileName(src);
                var dst = ResourcesMapDir + "/" + fileName;
                if (AssetDatabase.LoadAssetAtPath<Object>(dst) == null)
                {
                    AssetDatabase.CopyAsset(src, dst);
                    copied++;
                }

                var importer = AssetImporter.GetAtPath(dst) as TextureImporter;
                if (importer != null)
                {
                    var dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }

                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }

                    if (importer.maxTextureSize < 4096)
                    {
                        importer.maxTextureSize = 4096;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[LevelRouteSelect] Ensured Resources maps under {ResourcesMapDir} (copied={copied})");
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
            Place(boxRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BoxWidth, BoxHeight));
            box.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

            var title = CreateText(box.transform, "Title", "路线选择", 30, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 44f));

            var closeGo = CreateUi(box.transform, "CloseButton", typeof(Image), typeof(Button));
            Place(closeGo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(48f, 48f));
            closeGo.GetComponent<Image>().color = new Color(0.45f, 0.22f, 0.22f, 1f);
            var closeLabel = CreateText(closeGo.transform, "Label", "X", 22, TextAnchor.MiddleCenter);
            StretchFull(closeLabel.GetComponent<RectTransform>());

            BuildTabBar(box.transform);
            BuildStageScroll(box.transform);
            BuildMapScroll(box.transform);
            LevelRouteSelectView.BuildOptionHoverTips(box.transform);
            var mapContent = box.transform.Find("MapScroll/Viewport/MapContent");
            if (mapContent != null)
            {
                BuildEdgeLayer(mapContent);
            }

            return canvasGo;
        }

        private static void BuildTabBar(Transform box)
        {
            var tabBar = CreateUi(box, "LevelTabBar", typeof(HorizontalLayoutGroup));
            Place(tabBar.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1400f, 48f));
            var tabHlg = tabBar.GetComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 8f;
            tabHlg.childAlignment = TextAnchor.MiddleLeft;
            tabHlg.childControlWidth = false;
            tabHlg.childControlHeight = true;
            tabHlg.childForceExpandWidth = false;
            tabHlg.childForceExpandHeight = true;
            tabHlg.padding = new RectOffset(8, 8, 4, 4);

            var tabTemplate = CreateUi(tabBar.transform, "LevelTabTemplate", typeof(Image), typeof(Button), typeof(LayoutElement));
            tabTemplate.GetComponent<LayoutElement>().preferredWidth = 140f;
            tabTemplate.GetComponent<LayoutElement>().preferredHeight = 40f;
            tabTemplate.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.30f, 1f);
            var tabLabel = CreateText(tabTemplate.transform, "Label", "Level", 18, TextAnchor.MiddleCenter);
            StretchFull(tabLabel.GetComponent<RectTransform>());
            tabTemplate.SetActive(false);
        }

        private static void BuildEdgeLayer(Transform mapContent)
        {
            // Prefab default under MapContent; runtime map mode reparents under LevelRouteMap
            // (after Background, before option Icons).
            var edgeLayer = CreateUi(mapContent, "EdgeLayer", typeof(RectTransform));
            StretchFull(edgeLayer.GetComponent<RectTransform>());
            var edgeRt = edgeLayer.GetComponent<RectTransform>();
            edgeRt.offsetMin = Vector2.zero;
            edgeRt.offsetMax = Vector2.zero;
            var edgeCg = edgeLayer.AddComponent<CanvasGroup>();
            edgeCg.blocksRaycasts = false;
            edgeCg.interactable = false;
            edgeLayer.transform.SetAsLastSibling();
        }

        private static GameObject BuildStageScroll(Transform box)
        {
            var scrollGo = CreateUi(box, "StageScroll", typeof(Image), typeof(ScrollRect));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1460f, 680f));
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
            var stageLabelLe = stageLabel.GetComponent<LayoutElement>() ?? stageLabel.AddComponent<LayoutElement>();
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

            var optionCard = CreateOptionCard(optionsHost.transform);
            stageRow.SetActive(false);
            optionCard.SetActive(false);
            return scrollGo;
        }

        private static void BuildMapScroll(Transform box)
        {
            var scrollGo = CreateUi(box, "MapScroll", typeof(Image), typeof(ScrollRect));
            Place(scrollGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1460f, 680f));
            scrollGo.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 1f);

            var viewport = CreateUi(scrollGo.transform, "Viewport", typeof(Image), typeof(Mask));
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = CreateUi(viewport.transform, "MapContent", typeof(RectTransform));
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.zero;
            contentRt.pivot = Vector2.zero;
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(MapDisplayWidth, MapDisplayWidth);

            var bg = CreateUi(content.transform, "Background", typeof(Image));
            StretchFull(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.14f, 1f);
            bg.GetComponent<Image>().raycastTarget = false;

            var optionsHost = CreateUi(content.transform, "OptionsHost", typeof(RectTransform));
            StretchFull(optionsHost.GetComponent<RectTransform>());
            optionsHost.GetComponent<RectTransform>().pivot = Vector2.zero;

            // Shared option card template lives under MapContent for map mode; View also uses StageRow's template.
            // Prefer StageScroll template; keep a hidden duplicate under Map OptionsHost only if Stage missing.
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollGo.SetActive(false);
        }

        private static GameObject CreateOptionCard(Transform parent)
        {
            var optionCard = CreateUi(parent, "OptionCardTemplate", typeof(Image), typeof(Button), typeof(LayoutElement));
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
            return optionCard;
        }

        private static void WireView(LevelRouteSelectView view, GameObject rootGo)
        {
            var panelTf = rootGo.transform.Find("Panel");
            if (panelTf == null)
            {
                EnsureLegacyMigratedToPanel(rootGo);
                panelTf = rootGo.transform.Find("Panel");
            }

            var panel = panelTf != null ? panelTf.gameObject : rootGo;
            var boxTf = rootGo.transform.Find(panelTf != null ? "Panel/Box" : "Box");
            EnsureLevelChrome(boxTf);

            var title = boxTf != null ? boxTf.Find("Title")?.GetComponent<Text>() : null;
            var stageScroll = boxTf != null ? boxTf.Find("StageScroll") : null;
            var content = stageScroll != null ? stageScroll.Find("Viewport/Content") : null;
            var stageRow = content != null ? content.Find("StageRowTemplate")?.gameObject : null;
            var optionCard = stageRow != null ? stageRow.transform.Find("OptionsHost/OptionCardTemplate")?.gameObject : null;
            var close = boxTf != null ? boxTf.Find("CloseButton")?.GetComponent<Button>() : null;
            var tabBar = boxTf != null ? boxTf.Find("LevelTabBar") : null;
            var tabTemplate = tabBar != null ? tabBar.Find("LevelTabTemplate")?.gameObject : null;

            var mapScroll = boxTf != null ? boxTf.Find("MapScroll") : null;
            var mapContent = mapScroll != null ? mapScroll.Find("Viewport/MapContent") as RectTransform : null;
            var mapBg = mapContent != null ? mapContent.Find("Background")?.GetComponent<Image>() : null;
            var mapHost = mapContent != null ? mapContent.Find("OptionsHost") as RectTransform : null;
            var mapScrollRect = mapScroll != null ? mapScroll.GetComponent<ScrollRect>() : null;
            var edge = mapContent != null
                ? mapContent.Find("EdgeLayer")?.GetComponent<RectTransform>()
                : null;
            if (edge == null && boxTf != null)
            {
                edge = boxTf.Find("EdgeLayer")?.GetComponent<RectTransform>();
            }

            var tipsTf = boxTf != null ? boxTf.Find("OptionHoverTips") : null;
            var tipsRoot = tipsTf != null ? tipsTf.gameObject : null;
            var tipsType = tipsTf != null ? tipsTf.Find("Type")?.GetComponent<Text>() : null;
            var tipsTitle = tipsTf != null ? tipsTf.Find("Title")?.GetComponent<Text>() : null;
            var tipsDesc = tipsTf != null ? tipsTf.Find("Description")?.GetComponent<Text>() : null;
            var tipsReward = tipsTf != null ? tipsTf.Find("Reward")?.GetComponent<Text>() : null;

            view.BindRuntime(
                panel,
                title,
                content,
                stageRow,
                optionCard,
                edge,
                close,
                tabBar,
                tabTemplate,
                stageScroll != null ? stageScroll.gameObject : null,
                mapScroll != null ? mapScroll.gameObject : null,
                mapContent,
                mapBg,
                mapHost,
                mapScrollRect,
                tipsRoot,
                tipsType,
                tipsTitle,
                tipsDesc,
                tipsReward);

            if (panel != null && panel != rootGo)
            {
                panel.SetActive(false);
            }

            EditorUtility.SetDirty(view);
        }

        private static void EnsureLevelChrome(Transform box)
        {
            if (box == null)
            {
                return;
            }

            var boxRt = box as RectTransform;
            if (boxRt != null)
            {
                boxRt.sizeDelta = new Vector2(BoxWidth, BoxHeight);
            }

            EnsureLevelTabBar(box);
            if (box.Find("StageScroll") == null)
            {
                BuildStageScroll(box);
            }
            else
            {
                var scroll = box.Find("StageScroll") as RectTransform;
                if (scroll != null)
                {
                    scroll.sizeDelta = new Vector2(1460f, 680f);
                    scroll.anchoredPosition = new Vector2(0f, -40f);
                }
            }

            if (box.Find("MapScroll") == null)
            {
                BuildMapScroll(box);
            }

            if (box.Find("OptionHoverTips") == null)
            {
                var tips = LevelRouteSelectView.BuildOptionHoverTips(box);
                tips.SetActive(false);
            }
            else
            {
                box.Find("OptionHoverTips").gameObject.SetActive(false);
            }

            var mapContent = box.Find("MapScroll/Viewport/MapContent");
            if (mapContent != null)
            {
                var edgeUnderBox = box.Find("EdgeLayer");
                if (edgeUnderBox != null)
                {
                    edgeUnderBox.SetParent(mapContent, false);
                }

                if (mapContent.Find("EdgeLayer") == null)
                {
                    BuildEdgeLayer(mapContent);
                }
                else
                {
                    var edge = mapContent.Find("EdgeLayer") as RectTransform;
                    if (edge != null)
                    {
                        StretchFull(edge);
                        edge.offsetMin = Vector2.zero;
                        edge.offsetMax = Vector2.zero;
                        edge.SetAsLastSibling();
                    }
                }
            }

            var close = box.Find("CloseButton");
            if (close != null)
            {
                close.SetAsLastSibling();
            }
        }

        private static void EnsureLevelTabBar(Transform box)
        {
            if (box == null)
            {
                return;
            }

            var tabBar = box.Find("LevelTabBar");
            if (tabBar == null)
            {
                BuildTabBar(box);
                return;
            }

            var tabRt = tabBar as RectTransform;
            if (tabRt != null)
            {
                tabRt.sizeDelta = new Vector2(1400f, 48f);
            }

            if (tabBar.Find("LevelTabTemplate") == null)
            {
                var tabTemplate = new GameObject(
                    "LevelTabTemplate",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));
                tabTemplate.transform.SetParent(tabBar, false);
                tabTemplate.GetComponent<LayoutElement>().preferredWidth = 140f;
                tabTemplate.GetComponent<LayoutElement>().preferredHeight = 40f;
                tabTemplate.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.30f, 1f);
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelGo.transform.SetParent(tabTemplate.transform, false);
                StretchFull(labelGo.GetComponent<RectTransform>());
                var text = labelGo.GetComponent<Text>();
                text.text = "Level";
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 18;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.raycastTarget = false;
                tabTemplate.SetActive(false);
            }
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
