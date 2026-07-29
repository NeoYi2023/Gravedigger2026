#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Gravedigger2026.Editor.Art;
using Gravedigger2026.Editor.Maps;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Meta;
using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.Dig
{
    /// <summary>
    /// Builds Dig Prefabs / Maps / DigPrefabCatalog and wires MetaShellRoot (Approach A / D-020).
    /// </summary>
    public static class DigAssetBuilder
    {
        private const string PrefabDigDir = "Assets/Prefabs/Dig";
        private const string PrefabMapsDir = "Assets/Prefabs/Maps";
        private const string SettingsDigDir = "Assets/Settings/Dig";
        private const string ArtUiDigDir = "Assets/Art/UI/Dig";
        private const string CatalogPath = SettingsDigDir + "/DigPrefabCatalog.asset";
        private const string StageRootPath = PrefabDigDir + "/DigStageRoot.prefab";
        private const string DiggerPath = PrefabDigDir + "/Digger.prefab";
        private const string RewardPath = PrefabDigDir + "/DigRewardFlyer.prefab";
        private const string UiCursorRingPath = PrefabDigDir + "/UiDigCursorRing.prefab";
        private const string CircleSpritePath = ArtUiDigDir + "/Ui_DigCursor_Circle.png";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string RegenPrefsKey = "Gravedigger2026.DigAssets.Regen.v0464";

        private static readonly string[] MapIds =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05"
        };

        private static readonly string[] QualityIds =
        {
            "Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7", "Q8", "Q9", "Q10"
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

                var missing = AssetDatabase.LoadAssetAtPath<DigPrefabCatalog>(CatalogPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath) == null;
                var needsRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (missing || needsRegen)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Dig/Generate Dig Prefabs + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();

            // Isometric Tilemap maps (preserve hand-painted tiles unless force via Maps menu).
            MapTilemapAssetBuilder.EnsureMapsForDigBuilder(forceRepaint: false);

            var mapPrefabs = new List<DigPrefabCatalog.MapEntry>();
            for (var i = 0; i < MapIds.Length; i++)
            {
                var id = MapIds[i];
                var path = $"{PrefabMapsDir}/{id}.prefab";
                mapPrefabs.Add(new DigPrefabCatalog.MapEntry
                {
                    MapId = id,
                    Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path)
                });
            }

            // SPEC_04 §15.2: assemble 2D Visual from Art; never regenerate Capsule Body.
            if (!ProtagonistPrefabAssembler.AssembleDigger())
            {
                Debug.LogError("[DigAssetBuilder] Failed to assemble Digger Prefab from Art.");
            }

            var diggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiggerPath);

            var graveEntries = new List<DigPrefabCatalog.GraveEntry>();
            for (var i = 0; i < QualityIds.Length; i++)
            {
                var q = QualityIds[i];
                var path = $"{PrefabDigDir}/Grave_{q}.prefab";
                Sprite keepSprite = null;
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null)
                {
                    var existingSr = existing.GetComponentInChildren<SpriteRenderer>(true);
                    if (existingSr != null)
                    {
                        keepSprite = existingSr.sprite;
                    }
                }

                var go = BuildGrave(q, i, keepSprite);
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                graveEntries.Add(new DigPrefabCatalog.GraveEntry
                {
                    QualityId = q,
                    Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path)
                });
            }

            var rewardGo = BuildRewardFlyer();
            PrefabUtility.SaveAsPrefabAsset(rewardGo, RewardPath);
            Object.DestroyImmediate(rewardGo);
            var rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPath);

            var circleSprite = EnsureCircleSprite();
            var cursorRingGo = BuildUiDigCursorRing(circleSprite);
            PrefabUtility.SaveAsPrefabAsset(cursorRingGo, UiCursorRingPath);
            Object.DestroyImmediate(cursorRingGo);
            var cursorRingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiCursorRingPath);
            var cursorRingView = cursorRingPrefab != null
                ? cursorRingPrefab.GetComponent<DigCursorRingView>()
                : null;

            var stageGo = BuildDigStageRoot(cursorRingView);
            PrefabUtility.SaveAsPrefabAsset(stageGo, StageRootPath);
            Object.DestroyImmediate(stageGo);
            var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath);

            var catalog = AssetDatabase.LoadAssetAtPath<DigPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DigPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(stagePrefab, diggerPrefab, rewardPrefab, cursorRingView, mapPrefabs, graveEntries);
            EditorUtility.SetDirty(catalog);

            // Re-bind catalog + cursor prefab on DigStageRoot controller / DigCursorView
            var stageContents = PrefabUtility.LoadPrefabContents(StageRootPath);
            var controller = stageContents.GetComponent<DigStageController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var cursorView = stageContents.GetComponentInChildren<DigCursorView>(true);
            if (cursorView != null && cursorRingView != null)
            {
                var cso = new SerializedObject(cursorView);
                cso.FindProperty("_uiRingPrefab").objectReferenceValue = cursorRingView;
                cso.FindProperty("_showWorldRing").boolValue = false;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(stageContents, StageRootPath);
            PrefabUtility.UnloadPrefabContents(stageContents);

            WireMetaShell(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DigAssetBuilder] Generated Dig Prefabs, Maps, Catalog, and wired MetaShellRoot.");
        }

        private static void WireMetaShell(DigPrefabCatalog catalog)
        {
            var meta = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (meta == null)
            {
                Debug.LogWarning("[DigAssetBuilder] MetaShellRoot missing — run Meta shell builder first.");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(MetaRootPath);
            var controller = contents.GetComponent<MetaShellController>();
            var digParent = contents.transform.Find("DigWorldParent");
            if (digParent == null)
            {
                var digParentGo = new GameObject("DigWorldParent");
                digParentGo.transform.SetParent(contents.transform, false);
                digParent = digParentGo.transform;
            }

            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_digPrefabCatalog").objectReferenceValue = catalog;
                so.FindProperty("_digWorldParent").objectReferenceValue = digParent;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var inSave = contents.GetComponentInChildren<InSaveShellView>(true);
            if (inSave != null)
            {
                var img = inSave.GetComponent<Image>();
                var so = new SerializedObject(inSave);
                var backdropProp = so.FindProperty("_backdropImage");
                if (backdropProp != null && backdropProp.objectReferenceValue == null && img != null)
                {
                    backdropProp.objectReferenceValue = img;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, MetaRootPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static GameObject BuildGrave(string qualityId, int index, Sprite sprite = null)
        {
            var root = new GameObject($"Grave_{qualityId}");

            // Flat on XZ, face +Y; Z=180 keeps art upright under Dig top-down camera.
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(root.transform, false);
            spriteGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 180f);
            spriteGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            spriteGo.transform.localScale = Vector3.one;
            var spriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 200;
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            var obstacle = root.AddComponent<DigObstacleRadius>();
            var oso = new SerializedObject(obstacle);
            oso.FindProperty("_radius").floatValue = 0.55f;
            oso.ApplyModifiedPropertiesWithoutUndo();

            var view = root.AddComponent<DigGraveView>();
            var vso = new SerializedObject(view);
            vso.FindProperty("_bodyRenderer").objectReferenceValue = spriteRenderer;
            vso.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject BuildRewardFlyer()
        {
            var root = new GameObject("DigRewardFlyer");
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Icon";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * 0.35f;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            root.AddComponent<DigRewardFlyerView>();
            return root;
        }

        private static Sprite EnsureCircleSprite()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/UI");
            EnsureFolder(ArtUiDigDir);

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(CircleSpritePath) == null)
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var center = (size - 1) * 0.5f;
                var radius = center - 0.5f;
                var radiusSq = radius * radius;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x - center;
                        var dy = y - center;
                        var inside = dx * dx + dy * dy <= radiusSq;
                        tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                }

                tex.Apply(false, false);
                var png = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                File.WriteAllBytes(
                    Path.Combine(Directory.GetCurrentDirectory(), CircleSpritePath.Replace('/', Path.DirectorySeparatorChar)),
                    png);
                AssetDatabase.ImportAsset(CircleSpritePath);
            }

            var importer = AssetImporter.GetAtPath(CircleSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        }

        private static GameObject BuildUiDigCursorRing(Sprite circleSprite)
        {
            const float strokeWidthPx = 3f;
            var root = new GameObject("UiDigCursorRing", typeof(RectTransform));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(96f, 96f);

            var strokeGo = new GameObject("Stroke", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            strokeGo.transform.SetParent(root.transform, false);
            var strokeRt = strokeGo.GetComponent<RectTransform>();
            strokeRt.anchorMin = new Vector2(0.5f, 0.5f);
            strokeRt.anchorMax = new Vector2(0.5f, 0.5f);
            strokeRt.pivot = new Vector2(0.5f, 0.5f);
            strokeRt.sizeDelta = new Vector2(96f, 96f);
            var strokeImg = strokeGo.GetComponent<Image>();
            strokeImg.sprite = circleSprite;
            strokeImg.type = Image.Type.Simple;
            strokeImg.preserveAspect = true;
            strokeImg.color = new Color(1f, 0.92f, 0.2f, 50f / 255f);
            strokeImg.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(root.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.5f, 0.5f);
            fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            var fillSize = 96f - strokeWidthPx * 2f;
            fillRt.sizeDelta = new Vector2(fillSize, fillSize);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = circleSprite;
            fillImg.type = Image.Type.Simple;
            fillImg.preserveAspect = true;
            fillImg.color = new Color(1f, 1f, 1f, 0.3f);
            fillImg.raycastTarget = false;

            var view = root.AddComponent<DigCursorRingView>();
            view.EditorBind(rootRt, strokeRt, fillRt, strokeWidthPx);
            return root;
        }

        private static GameObject BuildDigStageRoot(DigCursorRingView uiCursorRingPrefab)
        {
            var root = new GameObject("DigStageRoot");
            var controller = root.AddComponent<DigStageController>();

            var world = new GameObject("DigWorld");
            world.transform.SetParent(root.transform, false);

            var camGo = new GameObject("DigCamera", typeof(Camera));
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.position = new Vector3(0f, 18f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            cam.depth = 10;
            camGo.SetActive(false);

            var cursorGo = new GameObject("DigCursor");
            cursorGo.transform.SetParent(root.transform, false);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(cursorGo.transform, false);
            ring.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            ring.SetActive(false);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            var cursor = cursorGo.AddComponent<DigCursorView>();
            var cso = new SerializedObject(cursor);
            cso.FindProperty("_ring").objectReferenceValue = ring.transform;
            cso.FindProperty("_uiRingPrefab").objectReferenceValue = uiCursorRingPrefab;
            cso.FindProperty("_showWorldRing").boolValue = false;
            cso.ApplyModifiedPropertiesWithoutUndo();
            cursorGo.SetActive(false);

            var canvasGo = new GameObject("DigHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var hudRoot = CreateUiPanel(canvasGo.transform, "HudRoot", new Color(0f, 0f, 0f, 0f));
            Stretch(hudRoot.GetComponent<RectTransform>());

            var timer = CreateUiText(hudRoot.transform, "Timer", "Dig 剩余 --", 28, TextAnchor.UpperCenter);
            Place(timer.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(640f, 40f));

            var warehouse = CreateUiText(hudRoot.transform, "Warehouse", "精魂 0", 22, TextAnchor.UpperLeft);
            Place(warehouse.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -70f), new Vector2(900f, 36f));

            // Transparent HUD panel must not eat mouse for Dig cursor / Meta buttons.
            var hudImage = hudRoot.GetComponent<Image>();
            if (hudImage != null)
            {
                hudImage.raycastTarget = false;
            }

            var hud = hudRoot.AddComponent<DigHudView>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("_root").objectReferenceValue = hudRoot;
            hso.FindProperty("_timerText").objectReferenceValue = timer;
            hso.FindProperty("_warehouseText").objectReferenceValue = warehouse;
            hso.ApplyModifiedPropertiesWithoutUndo();
            hudRoot.SetActive(false);

            var summaryRoot = CreateUiPanel(canvasGo.transform, "SummaryRoot", new Color(0.08f, 0.09f, 0.12f, 0.92f));
            Place(summaryRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 420f));

            var summaryTitle = CreateUiText(summaryRoot.transform, "Title", "挖坟阶段汇总", 32, TextAnchor.UpperCenter);
            Place(summaryTitle.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(640f, 44f));

            var summaryBody = CreateUiText(summaryRoot.transform, "Body", "", 22, TextAnchor.UpperLeft);
            Place(summaryBody.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 220f));

            var confirmBtn = CreateUiButton(summaryRoot.transform, "ConfirmButton", "确认", new Color(0.28f, 0.55f, 0.35f, 1f));
            Place(confirmBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(200f, 48f));

            var summary = summaryRoot.AddComponent<DigStageSummaryView>();
            var sso = new SerializedObject(summary);
            sso.FindProperty("_root").objectReferenceValue = summaryRoot;
            sso.FindProperty("_bodyText").objectReferenceValue = summaryBody;
            sso.FindProperty("_confirmButton").objectReferenceValue = confirmBtn.GetComponent<Button>();
            sso.ApplyModifiedPropertiesWithoutUndo();
            summaryRoot.SetActive(false);

            var ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("_worldRoot").objectReferenceValue = world.transform;
            ctrlSo.FindProperty("_digCamera").objectReferenceValue = cam;
            ctrlSo.FindProperty("_cursorView").objectReferenceValue = cursor;
            ctrlSo.FindProperty("_hudView").objectReferenceValue = hud;
            ctrlSo.FindProperty("_summaryView").objectReferenceValue = summary;
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabDigDir);
            EnsureFolder(PrefabMapsDir);
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsDigDir);
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/UI");
            EnsureFolder(ArtUiDigDir);
            EnsureFolder("Assets/Editor");
            EnsureFolder("Assets/Editor/Dig");
            EnsureFolder("Assets/Scripts/Gameplay");
            EnsureFolder("Assets/Scripts/Gameplay/Dig");
            EnsureFolder("Assets/Scripts/Core/Dig");
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

        private static GameObject CreateUiPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateUiText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        private static GameObject CreateUiButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var text = CreateUiText(go.transform, "Label", label, 24, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
#endif
