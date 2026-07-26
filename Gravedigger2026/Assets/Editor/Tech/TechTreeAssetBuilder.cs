#if UNITY_EDITOR
using System.IO;
using Gravedigger2026.Meta;
using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.Tech
{
    /// <summary>
    /// Builds TechTreeCanvasRoot Prefab and wires MetaShellRoot Settings (Approach A / UI-012).
    /// </summary>
    public static class TechTreeAssetBuilder
    {
        private const string PrefabMetaDir = "Assets/Prefabs/Meta";
        private const string CanvasPath = PrefabMetaDir + "/TechTreeCanvasRoot.prefab";
        private const string MetaRootPath = PrefabMetaDir + "/MetaShellRoot.prefab";
        private const string RegenPrefsKey = "Gravedigger2026.TechTreeAssets.Regen.v0420c";

        private static readonly (string TechId, Vector2 Pos, Color Icon)[] Layout =
        {
            ("Tech_Root", new Vector2(0f, 0f), new Color(0.4f, 0.75f, 0.95f)),
            ("Tech_DigDamage", new Vector2(-220f, 140f), new Color(0.9f, 0.4f, 0.35f)),
            ("Tech_DigSpeed", new Vector2(0f, 160f), new Color(0.45f, 0.85f, 0.5f)),
            ("Tech_DigRadius", new Vector2(220f, 140f), new Color(0.55f, 0.55f, 0.95f)),
            ("Tech_Key_01", new Vector2(-220f, 300f), new Color(0.85f, 0.45f, 0.95f)),
            ("Tech_Normal_01", new Vector2(0f, 320f), new Color(0.6f, 0.75f, 0.4f)),
            ("Tech_Normal_02", new Vector2(220f, 300f), new Color(0.5f, 0.7f, 0.85f)),
            ("Tech_DigDuration", new Vector2(0f, 460f), new Color(0.75f, 0.65f, 0.35f)),
            ("Tech_Capstone", new Vector2(-220f, 460f), new Color(0.95f, 0.55f, 0.25f)),
            ("Tech_Leaf", new Vector2(0f, 600f), new Color(0.65f, 0.65f, 0.65f))
        };

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                var missing = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) == null;
                var needsRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (missing || needsRegen)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Tech/Generate TechTree Canvas Prefab")]
        public static void GenerateAll()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabMetaDir);
            EnsureFolder("Assets/Editor/Tech");

            // Drop stale canvas with missing scripts (PanLayer fileID:0 from earlier builds).
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) != null)
            {
                AssetDatabase.DeleteAsset(CanvasPath);
            }

            var canvasGo = BuildCanvasRoot();
            StripMissingScriptsRecursive(canvasGo);
            PrefabUtility.SaveAsPrefabAsset(canvasGo, CanvasPath);
            Object.DestroyImmediate(canvasGo);

            WireMetaShell();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TechTreeAssetBuilder] Generated TechTreeCanvasRoot and wired MetaShellRoot.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject BuildCanvasRoot()
        {
            var root = new GameObject("TechTreeCanvasRoot", typeof(RectTransform));
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            var backdrop = CreatePanel(root.transform, "Backdrop", new Color(0.05f, 0.06f, 0.08f, 0.92f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var header = CreatePanel(root.transform, "Header", new Color(0.14f, 0.16f, 0.2f, 0.98f));
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 110f);
            headerRt.anchoredPosition = Vector2.zero;

            var title = CreateText(header.transform, "Title", "设置 — 科技树", 28, TextAnchor.MiddleLeft);
            Place(title.GetComponent<RectTransform>(), new Vector2(24f, -16f), new Vector2(420f, 36f), new Vector2(0f, 1f));

            var techPoints = CreateText(header.transform, "TechPoints", "科技点: 0", 22, TextAnchor.MiddleLeft);
            Place(techPoints.GetComponent<RectTransform>(), new Vector2(24f, -56f), new Vector2(280f, 28f), new Vector2(0f, 1f));

            var caps = CreateText(header.transform, "Caps", "DigDamage=…", 18, TextAnchor.MiddleLeft);
            Place(caps.GetComponent<RectTransform>(), new Vector2(320f, -56f), new Vector2(900f, 28f), new Vector2(0f, 1f));

            var closeBtn = CreateButton(header.transform, "CloseButton", "关闭", new Vector2(-24f, -20f), new Vector2(120f, 44f));
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);

            var grantBtn = CreateButton(header.transform, "DebugGrantButton", "Debug+5点", new Vector2(-160f, -20f), new Vector2(140f, 44f));
            var grantRt = grantBtn.GetComponent<RectTransform>();
            grantRt.anchorMin = new Vector2(1f, 1f);
            grantRt.anchorMax = new Vector2(1f, 1f);
            grantRt.pivot = new Vector2(1f, 1f);

            var viewport = CreatePanel(root.transform, "Viewport", new Color(0.08f, 0.09f, 0.11f, 1f));
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = new Vector2(0f, 0f);
            viewportRt.anchorMax = new Vector2(1f, 1f);
            viewportRt.offsetMin = new Vector2(16f, 16f);
            viewportRt.offsetMax = new Vector2(-16f, -126f);
            viewport.AddComponent<RectMask2D>();

            var panGo = CreatePanel(viewport.transform, "PanLayer", new Color(0.08f, 0.09f, 0.11f, 1f));
            StretchFull(panGo.GetComponent<RectTransform>());
            // No TechTreePanLayer on Prefab — added at runtime by TechTreeCanvasView.

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(1400f, 1400f);
            contentRt.anchoredPosition = Vector2.zero;

            var edges = new GameObject("Edges", typeof(RectTransform));
            edges.transform.SetParent(content.transform, false);
            StretchFull(edges.GetComponent<RectTransform>());

            var nodesParent = new GameObject("Nodes", typeof(RectTransform));
            nodesParent.transform.SetParent(content.transform, false);
            StretchFull(nodesParent.GetComponent<RectTransform>());

            var nodeViews = new TechTreeNodeView[Layout.Length];
            for (var i = 0; i < Layout.Length; i++)
            {
                nodeViews[i] = BuildNode(nodesParent.transform, Layout[i].TechId, Layout[i].Pos, Layout[i].Icon);
            }

            var tooltip = CreatePanel(root.transform, "Tooltip", new Color(0.12f, 0.14f, 0.18f, 0.96f));
            var tipRt = tooltip.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0f, 0f);
            tipRt.anchorMax = new Vector2(0f, 0f);
            tipRt.pivot = new Vector2(0f, 0f);
            tipRt.anchoredPosition = new Vector2(32f, 32f);
            tipRt.sizeDelta = new Vector2(420f, 120f);
            var tipTitle = CreateText(tooltip.transform, "TipTitle", "Name", 22, TextAnchor.UpperLeft);
            Place(tipTitle.GetComponent<RectTransform>(), new Vector2(12f, -10f), new Vector2(396f, 28f), new Vector2(0f, 1f));
            var tipBody = CreateText(tooltip.transform, "TipBody", "Desc", 18, TextAnchor.UpperLeft);
            Place(tipBody.GetComponent<RectTransform>(), new Vector2(12f, -44f), new Vector2(396f, 66f), new Vector2(0f, 1f));
            tooltip.SetActive(false);

            var view = root.AddComponent<TechTreeCanvasView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_content").objectReferenceValue = contentRt;
            so.FindProperty("_edgesParent").objectReferenceValue = edges.GetComponent<RectTransform>();
            so.FindProperty("_techPointsLabel").objectReferenceValue = techPoints;
            so.FindProperty("_capsLabel").objectReferenceValue = caps;
            so.FindProperty("_tooltipTitle").objectReferenceValue = tipTitle;
            so.FindProperty("_tooltipBody").objectReferenceValue = tipBody;
            so.FindProperty("_tooltipRoot").objectReferenceValue = tooltip;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            so.FindProperty("_debugGrantTechPointsButton").objectReferenceValue = grantBtn.GetComponent<Button>();
            so.FindProperty("_panLayerRoot").objectReferenceValue = panGo;
            var nodesProp = so.FindProperty("_nodes");
            nodesProp.arraySize = nodeViews.Length;
            for (var i = 0; i < nodeViews.Length; i++)
            {
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodeViews[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return root;
        }

        private static TechTreeNodeView BuildNode(Transform parent, string techId, Vector2 pos, Color iconColor)
        {
            var go = CreatePanel(parent, techId, new Color(0.25f, 0.27f, 0.3f, 1f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 96f);
            rt.anchoredPosition = pos;

            var frame = go.GetComponent<Image>();
            var iconGo = CreatePanel(go.transform, "Icon", iconColor);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(56f, 56f);
            iconRt.anchoredPosition = new Vector2(0f, 8f);

            var label = CreateText(go.transform, "Label", techId.Replace("Tech_", string.Empty), 14, TextAnchor.MiddleCenter);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(0f, 22f);
            labelRt.anchoredPosition = new Vector2(0f, 4f);

            var node = go.AddComponent<TechTreeNodeView>();
            var so = new SerializedObject(node);
            so.FindProperty("_techId").stringValue = techId;
            so.FindProperty("_frameImage").objectReferenceValue = frame;
            so.FindProperty("_iconImage").objectReferenceValue = iconGo.GetComponent<Image>();
            so.FindProperty("_debugLabel").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return node;
        }

        private static void WireMetaShell()
        {
            var metaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (metaPrefab == null)
            {
                Debug.LogWarning("[TechTreeAssetBuilder] MetaShellRoot missing — skip wire.");
                return;
            }

            var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath);
            if (canvasPrefab == null)
            {
                return;
            }

            var metaRoot = PrefabUtility.LoadPrefabContents(MetaRootPath);
            try
            {
                var controller = metaRoot.GetComponent<MetaShellController>();
                if (controller == null)
                {
                    Debug.LogWarning("[TechTreeAssetBuilder] MetaShellController missing.");
                    return;
                }

                var canvasHost = metaRoot.transform.Find("MetaCanvas");
                if (canvasHost == null)
                {
                    Debug.LogWarning("[TechTreeAssetBuilder] MetaCanvas missing.");
                    return;
                }

                var existing = canvasHost.Find("TechTreeCanvasRoot");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                // Also clear any nested missing scripts left from prior failed wires.
                StripMissingScriptsRecursive(metaRoot);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab, canvasHost);
                instance.name = "TechTreeCanvasRoot";
                StripMissingScriptsRecursive(instance);
                var view = instance.GetComponent<TechTreeCanvasView>();

                var so = new SerializedObject(controller);
                var prop = so.FindProperty("_techTreeCanvasView");
                if (prop != null)
                {
                    prop.objectReferenceValue = view;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                StripMissingScriptsRecursive(metaRoot);
                PrefabUtility.SaveAsPrefabAsset(metaRoot, MetaRootPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(metaRoot);
            }
        }

        private static void StripMissingScriptsRecursive(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
        {
            var go = CreatePanel(parent, name, new Color(0.28f, 0.34f, 0.42f, 1f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.AddComponent<Button>();
            var text = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
            StretchFull(text.GetComponent<RectTransform>());
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchoredPos, Vector2 size, Vector2 anchor)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
#endif
