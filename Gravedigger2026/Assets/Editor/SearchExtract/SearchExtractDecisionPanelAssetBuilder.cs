using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.EditorTools.SearchExtract
{
    /// <summary>Generates SearchExtractDecisionPanel Prefab (UI-032 / SE-07).</summary>
    public static class SearchExtractDecisionPanelAssetBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/SearchExtract/SearchExtractDecisionPanel.prefab";
        private const string ResourcesPrefabPath =
            "Assets/Resources/UI/SearchExtract/SearchExtractDecisionPanel.prefab";
        private const string MenuPath = "Gravedigger2026/SearchExtract/Ensure DecisionPanel Prefab (UI-032)";

        [MenuItem(MenuPath)]
        public static void EnsurePrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/SearchExtract");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            EnsureFolder("Assets/Resources/UI/SearchExtract");

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject rootGo;
            if (existing != null)
            {
                rootGo = PrefabUtility.InstantiatePrefab(existing) as GameObject;
            }
            else
            {
                rootGo = BuildFresh();
            }

            WireView(rootGo);
            PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGo, ResourcesPrefabPath);
            Object.DestroyImmediate(rootGo);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SearchExtractDecisionPanel] Ensured Prefab at {PrefabPath} + Resources copy");
        }

        private static GameObject BuildFresh()
        {
            var canvasGo = new GameObject(
                "SearchExtractDecisionPanel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(SearchExtractDecisionPanelView));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = CreateUi(canvasGo.transform, "Root", typeof(Image));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 48f);
            rootRt.sizeDelta = new Vector2(560f, 160f);
            root.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

            var title = CreateText(root.transform, "Title", "搜集点完成", 24, TextAnchor.MiddleCenter);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.55f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, 0f);
            titleRt.offsetMax = new Vector2(-16f, -8f);

            var continueGo = CreateUi(root.transform, "ContinueButton", typeof(Image), typeof(Button));
            Place(continueGo.GetComponent<RectTransform>(), new Vector2(0.28f, 0.22f), new Vector2(0.28f, 0.22f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 56f));
            continueGo.GetComponent<Image>().color = new Color(0.22f, 0.45f, 0.28f, 1f);
            var continueLabel = CreateText(continueGo.transform, "Label", "继续搜集", 22, TextAnchor.MiddleCenter);
            StretchFull(continueLabel.GetComponent<RectTransform>());

            var leaveGo = CreateUi(root.transform, "LeaveButton", typeof(Image), typeof(Button));
            Place(leaveGo.GetComponent<RectTransform>(), new Vector2(0.72f, 0.22f), new Vector2(0.72f, 0.22f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 56f));
            leaveGo.GetComponent<Image>().color = new Color(0.4f, 0.28f, 0.18f, 1f);
            var leaveLabel = CreateText(leaveGo.transform, "Label", "离开", 22, TextAnchor.MiddleCenter);
            StretchFull(leaveLabel.GetComponent<RectTransform>());

            return canvasGo;
        }

        private static void WireView(GameObject rootGo)
        {
            var view = rootGo.GetComponent<SearchExtractDecisionPanelView>();
            if (view == null)
            {
                view = rootGo.AddComponent<SearchExtractDecisionPanelView>();
            }

            var root = rootGo.transform.Find("Root");
            var continueBtn = root != null ? root.Find("ContinueButton")?.GetComponent<Button>() : null;
            var leaveBtn = root != null ? root.Find("LeaveButton")?.GetComponent<Button>() : null;
            var title = root != null ? root.Find("Title")?.GetComponent<Text>() : null;

            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root != null ? root.gameObject : null;
            so.FindProperty("_continueButton").objectReferenceValue = continueBtn;
            so.FindProperty("_leaveButton").objectReferenceValue = leaveBtn;
            so.FindProperty("_titleLabel").objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUi(Transform parent, string name, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            for (var i = 0; i < components.Length; i++)
            {
                if (go.GetComponent(components[i]) == null)
                {
                    go.AddComponent(components[i]);
                }
            }

            return go;
        }

        private static GameObject CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor align)
        {
            var go = CreateUi(parent, name, typeof(Text));
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.alignment = align;
            label.color = Color.white;
            label.text = text;
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

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
