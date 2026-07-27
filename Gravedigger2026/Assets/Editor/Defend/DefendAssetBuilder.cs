#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Maps;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using Gravedigger2026.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.Defend
{
    /// <summary>
    /// Builds Defend StageRoot / BattleProtagonist / Projectile / Catalog, ensures EngageZone + SpawnPoints on Maps,
    /// temp Monster Prefabs, and wires MetaShellRoot (Approach A / D-040–D-042).
    /// </summary>
    public static class DefendAssetBuilder
    {
        private const string PrefabDefendDir = "Assets/Prefabs/Defend";
        private const string PrefabMapsDir = "Assets/Prefabs/Maps";
        private const string PrefabWarriorsDir = "Assets/Prefabs/Defend/Warriors";
        private const string PrefabMonstersDir = "Assets/Prefabs/Defend/Monsters";
        private const string SettingsDefendDir = "Assets/Settings/Defend";
        private const string CatalogPath = SettingsDefendDir + "/DefendPrefabCatalog.asset";
        private const string StageRootPath = PrefabDefendDir + "/DefendStageRoot.prefab";
        private const string BattleProtagonistPath = PrefabDefendDir + "/BattleProtagonist.prefab";
        private const string ProjectilePath = PrefabDefendDir + "/Projectile.prefab";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string AppearanceCsv = "Manufacture_BodyAppearanceConfig.csv";
        private const string MonsterCsv = "Defend_MonsterConfig.csv";
        private const string RegenPrefsKey = "Gravedigger2026.DefendAssets.Regen.v0460";

        private static readonly string[] MapIds =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05"
        };

        private static readonly Color[] MonsterColors =
        {
            new Color(0.75f, 0.25f, 0.25f),
            new Color(0.85f, 0.45f, 0.2f),
            new Color(0.55f, 0.2f, 0.55f),
            new Color(0.3f, 0.55f, 0.35f),
            new Color(0.35f, 0.35f, 0.7f),
            new Color(0.7f, 0.55f, 0.2f),
            new Color(0.5f, 0.15f, 0.15f),
            new Color(0.2f, 0.5f, 0.55f),
            new Color(0.6f, 0.25f, 0.45f),
            new Color(0.4f, 0.4f, 0.25f)
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

                var missing = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(CatalogPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(BattleProtagonistPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath) == null;
                var needsRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (missing || needsRegen)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Defend/Generate Defend Prefabs + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();
            EnsureEngageZonesAndSpawnPointsOnMaps();

            var battleGo = BuildBattleProtagonist();
            PrefabUtility.SaveAsPrefabAsset(battleGo, BattleProtagonistPath);
            UnityEngine.Object.DestroyImmediate(battleGo);
            var battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleProtagonistPath);

            var projectileGo = BuildProjectile();
            PrefabUtility.SaveAsPrefabAsset(projectileGo, ProjectilePath);
            UnityEngine.Object.DestroyImmediate(projectileGo);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);

            var stageGo = BuildStageRoot();
            PrefabUtility.SaveAsPrefabAsset(stageGo, StageRootPath);
            UnityEngine.Object.DestroyImmediate(stageGo);
            var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath);

            var mapEntries = new List<DefendPrefabCatalog.MapEntry>();
            for (var i = 0; i < MapIds.Length; i++)
            {
                var id = MapIds[i];
                var path = $"{PrefabMapsDir}/{id}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[DefendAssetBuilder] Map missing: {path} — run Dig builder first.");
                    continue;
                }

                mapEntries.Add(new DefendPrefabCatalog.MapEntry { MapId = id, Prefab = prefab });
            }

            var warriorEntries = BuildWarriorAppearanceEntries();
            var monsterEntries = BuildMonsterModelEntries();

            var catalog = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DefendPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(stagePrefab, battlePrefab, projectilePrefab, mapEntries, warriorEntries, monsterEntries);
            EditorUtility.SetDirty(catalog);

            var stageContents = PrefabUtility.LoadPrefabContents(StageRootPath);
            var controller = stageContents.GetComponent<DefendStageController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(stageContents, StageRootPath);
            PrefabUtility.UnloadPrefabContents(stageContents);

            WireMetaShell(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[DefendAssetBuilder] Generated Defend Prefabs + Catalog (maps={mapEntries.Count}, warriors={warriorEntries.Count}, monsters={monsterEntries.Count}) and wired MetaShellRoot.");
        }

        public static void EnsureEngageZonesAndSpawnPointsOnMaps()
        {
            for (var i = 0; i < MapIds.Length; i++)
            {
                var path = $"{PrefabMapsDir}/{MapIds[i]}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                var contents = PrefabUtility.LoadPrefabContents(path);
                var bounds = contents.GetComponent<DigMapBounds>();
                var half = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
                var center = bounds != null ? bounds.Center : contents.transform.position;

                var existing = contents.GetComponentInChildren<EngageZone>(true);
                if (existing == null)
                {
                    var zoneGo = new GameObject("EngageZone");
                    zoneGo.transform.SetParent(contents.transform, false);
                    zoneGo.transform.position = center;
                    existing = zoneGo.AddComponent<EngageZone>();
                }
                else
                {
                    existing.transform.position = center;
                }

                var zso = new SerializedObject(existing);
                zso.FindProperty("_halfExtents").vector2Value = half * 0.85f;
                zso.ApplyModifiedPropertiesWithoutUndo();

                EnsureSpawnPointSet(contents.transform, center, half);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureSpawnPointSet(Transform mapRoot, Vector3 center, Vector2 halfExtents)
        {
            var existing = mapRoot.GetComponentInChildren<DefendSpawnPointSet>(true);
            var root = existing != null ? existing.transform : null;
            if (root == null)
            {
                var go = new GameObject("DefendSpawnPoints");
                go.transform.SetParent(mapRoot, false);
                go.transform.position = center;
                root = go.transform;
                existing = go.AddComponent<DefendSpawnPointSet>();
            }

            var clock = new Transform[13];
            var random = new List<Transform>(12);
            for (var hour = 1; hour <= 12; hour++)
            {
                var name = $"SpawnClock_{hour:00}";
                var child = root.Find(name);
                if (child == null)
                {
                    var pointGo = new GameObject(name);
                    pointGo.transform.SetParent(root, false);
                    child = pointGo.transform;
                }

                var rim = MapFootprintMath.PointOnClockHour(center, halfExtents, hour, 0.9f);
                child.position = new Vector3(rim.x, center.y + 0.05f, rim.z);
                clock[hour] = child;
                random.Add(child);
            }

            existing.EditorSetPoints(clock, random.ToArray());
            EditorUtility.SetDirty(existing);
        }

        private static List<DefendPrefabCatalog.WarriorAppearanceEntry> BuildWarriorAppearanceEntries()
        {
            var entries = new List<DefendPrefabCatalog.WarriorAppearanceEntry>();
            var csvPath = CsvPathResolver.ResolveExistingFile(AppearanceCsv);
            if (csvPath == null)
            {
                Debug.LogWarning($"[DefendAssetBuilder] {AppearanceCsv} not found — warrior bindings empty.");
                return entries;
            }

            var rows = SimpleCsv.ReadRows(csvPath);
            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].TryGetValue("AppearanceId", out var appearanceId) || string.IsNullOrEmpty(appearanceId))
                {
                    continue;
                }

                var path = $"{PrefabWarriorsDir}/{appearanceId}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    temp.name = appearanceId;
                    temp.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                    PrefabUtility.SaveAsPrefabAsset(temp, path);
                    UnityEngine.Object.DestroyImmediate(temp);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }

                entries.Add(new DefendPrefabCatalog.WarriorAppearanceEntry
                {
                    AppearanceId = appearanceId,
                    Prefab = prefab
                });
            }

            return entries;
        }

        private static List<DefendPrefabCatalog.MonsterModelEntry> BuildMonsterModelEntries()
        {
            var entries = new List<DefendPrefabCatalog.MonsterModelEntry>();
            var csvPath = CsvPathResolver.ResolveExistingFile(MonsterCsv);
            if (csvPath == null)
            {
                Debug.LogWarning($"[DefendAssetBuilder] {MonsterCsv} not found — monster bindings empty.");
                return entries;
            }

            var seen = new HashSet<string>();
            var rows = SimpleCsv.ReadRows(csvPath);
            var colorIndex = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].TryGetValue("ModelId", out var modelId) || string.IsNullOrEmpty(modelId))
                {
                    continue;
                }

                if (!seen.Add(modelId))
                {
                    continue;
                }

                var path = $"{PrefabMonstersDir}/{modelId}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    var color = MonsterColors[colorIndex % MonsterColors.Length];
                    colorIndex++;
                    var temp = BuildTempMonster(modelId, color);
                    PrefabUtility.SaveAsPrefabAsset(temp, path);
                    UnityEngine.Object.DestroyImmediate(temp);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }

                entries.Add(new DefendPrefabCatalog.MonsterModelEntry
                {
                    ModelId = modelId,
                    Prefab = prefab
                });
            }

            return entries;
        }

        private static GameObject BuildTempMonster(string modelId, Color color)
        {
            var root = new GameObject(modelId);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.sharedMaterial = mat;
            }

            root.AddComponent<MonsterAgentView>();
            var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.2f;
            return root;
        }

        private static GameObject BuildBattleProtagonist()
        {
            var root = new GameObject("BattleProtagonist");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.85f, 1.0f, 0.85f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            return root;
        }

        private static GameObject BuildProjectile()
        {
            var root = new GameObject("Projectile");
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.95f, 0.85f, 0.25f);
                renderer.sharedMaterial = mat;
            }

            root.AddComponent<ProjectileView>();
            return root;
        }

        private static GameObject BuildStageRoot()
        {
            var root = new GameObject("DefendStageRoot");
            var controller = root.AddComponent<DefendStageController>();

            var world = new GameObject("WorldRoot");
            world.transform.SetParent(root.transform, false);

            var camGo = new GameObject("DefendCamera", typeof(Camera));
            camGo.transform.SetParent(root.transform, false);
            camGo.SetActive(false);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            cam.depth = 5;

            var canvasGo = new GameObject("DefendCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panelRoot = CreateUiPanel(canvasGo.transform, "DefendRoot", new Color(0.07f, 0.09f, 0.12f, 0.88f));
            Stretch(panelRoot.GetComponent<RectTransform>());

            var phase = CreateUiText(panelRoot.transform, "PhaseText", "DefendPhase：Prepare", 26, TextAnchor.UpperCenter);
            Place(phase.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(900f, 40f));

            var preparePanel = CreateUiPanel(panelRoot.transform, "PreparePanel", new Color(0.1f, 0.12f, 0.16f, 0.92f));
            StretchFill(preparePanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f), 0f);

            var formationZone = CreateUiPanel(preparePanel.transform, "FormationZone", new Color(0.16f, 0.18f, 0.22f, 0.95f));
            StretchFill(formationZone.GetComponent<RectTransform>(), new Vector2(0.01f, 0.18f), new Vector2(0.72f, 0.98f), 4f);
            var formationView = BuildFormationZone(formationZone.transform);

            var startBattle = CreateUiButton(preparePanel.transform, "StartBattleButton", "开战（StartBattle）",
                new Color(0.55f, 0.32f, 0.22f, 1f));
            Place(startBattle.GetComponent<RectTransform>(), new Vector2(0.86f, 0.08f), new Vector2(0.86f, 0.08f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 56f));

            var hint = CreateUiText(preparePanel.transform, "HintText", "须 ≥1 上阵才可开战", 16, TextAnchor.LowerLeft);
            StretchFill(hint.GetComponent<RectTransform>(), new Vector2(0.02f, 0.02f), new Vector2(0.7f, 0.16f), 0f);

            var combatPanel = CreateUiPanel(panelRoot.transform, "CombatPanel", new Color(0.12f, 0.1f, 0.1f, 0.75f));
            StretchFill(combatPanel.GetComponent<RectTransform>(), new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.96f), 0f);
            combatPanel.SetActive(false);

            var combatStatus = CreateUiText(combatPanel.transform, "CombatStatus", "护盾 / 倒计时", 22, TextAnchor.MiddleCenter);
            Stretch(combatStatus.GetComponent<RectTransform>());

            var hud = panelRoot.AddComponent<DefendHudView>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("_root").objectReferenceValue = panelRoot;
            hso.FindProperty("_preparePanel").objectReferenceValue = preparePanel;
            hso.FindProperty("_combatPanel").objectReferenceValue = combatPanel;
            hso.FindProperty("_phaseText").objectReferenceValue = phase;
            hso.FindProperty("_combatStatusText").objectReferenceValue = combatStatus;
            hso.FindProperty("_startBattleButton").objectReferenceValue = startBattle.GetComponent<Button>();
            hso.FindProperty("_hintText").objectReferenceValue = hint;
            hso.ApplyModifiedPropertiesWithoutUndo();

            var cso = new SerializedObject(controller);
            cso.FindProperty("_worldRoot").objectReferenceValue = world.transform;
            cso.FindProperty("_defendCamera").objectReferenceValue = cam;
            cso.FindProperty("_hudView").objectReferenceValue = hud;
            cso.FindProperty("_formationPanel").objectReferenceValue = formationView;
            cso.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);
            return root;
        }

        private static FormationPanelView BuildFormationZone(Transform zone)
        {
            var header = CreateUiText(zone, "Header", "Prepare 布阵（与 UM 共用 · 点左上阵 / 点右选中）", 15,
                TextAnchor.UpperCenter);
            StretchFill(header.GetComponent<RectTransform>(), new Vector2(0.02f, 0.94f), new Vector2(0.98f, 1f), 0f);

            var poolContent = CreateListColumn(zone, "PoolColumn",
                new Vector2(0.02f, 0.42f), new Vector2(0.49f, 0.93f));
            var poolTemplate = CreateRowTemplate(poolContent, "PoolRowTemplate");

            var formationContent = CreateListColumn(zone, "FormationColumn",
                new Vector2(0.51f, 0.42f), new Vector2(0.98f, 0.93f));
            var formationTemplate = CreateRowTemplate(formationContent, "FormationRowTemplate");

            var statusPanel = CreateUiPanel(zone, "StatusPanel", new Color(0.13f, 0.14f, 0.18f, 0.95f));
            StretchFill(statusPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.41f), 0f);
            var statusText = CreateUiText(statusPanel.transform, "StatusText", "布阵区", 13, TextAnchor.UpperLeft);
            StretchFill(statusText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 4f);

            var undeploy = CreateUiButton(zone, "UndeployButton", "下阵", new Color(0.5f, 0.3f, 0.3f, 1f));
            StretchFill(undeploy.GetComponent<RectTransform>(), new Vector2(0.02f, 0.01f), new Vector2(0.22f, 0.16f), 2f);
            SetButtonFontSize(undeploy, 14);

            var negX = CreateUiButton(zone, "NudgeNegX", "−X", new Color(0.3f, 0.4f, 0.55f, 1f));
            StretchFill(negX.GetComponent<RectTransform>(), new Vector2(0.23f, 0.01f), new Vector2(0.40f, 0.16f), 2f);
            SetButtonFontSize(negX, 14);

            var posX = CreateUiButton(zone, "NudgePosX", "+X", new Color(0.3f, 0.4f, 0.55f, 1f));
            StretchFill(posX.GetComponent<RectTransform>(), new Vector2(0.41f, 0.01f), new Vector2(0.58f, 0.16f), 2f);
            SetButtonFontSize(posX, 14);

            var negZ = CreateUiButton(zone, "NudgeNegZ", "−Z", new Color(0.3f, 0.45f, 0.4f, 1f));
            StretchFill(negZ.GetComponent<RectTransform>(), new Vector2(0.59f, 0.01f), new Vector2(0.76f, 0.16f), 2f);
            SetButtonFontSize(negZ, 14);

            var posZ = CreateUiButton(zone, "NudgePosZ", "+Z", new Color(0.3f, 0.45f, 0.4f, 1f));
            StretchFill(posZ.GetComponent<RectTransform>(), new Vector2(0.77f, 0.01f), new Vector2(0.98f, 0.16f), 2f);
            SetButtonFontSize(posZ, 14);

            var view = zone.gameObject.AddComponent<FormationPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_poolContent").objectReferenceValue = poolContent;
            so.FindProperty("_poolRowTemplate").objectReferenceValue = poolTemplate;
            so.FindProperty("_formationContent").objectReferenceValue = formationContent;
            so.FindProperty("_formationRowTemplate").objectReferenceValue = formationTemplate;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_undeployButton").objectReferenceValue = undeploy.GetComponent<Button>();
            so.FindProperty("_nudgeNegXButton").objectReferenceValue = negX.GetComponent<Button>();
            so.FindProperty("_nudgePosXButton").objectReferenceValue = posX.GetComponent<Button>();
            so.FindProperty("_nudgeNegZButton").objectReferenceValue = negZ.GetComponent<Button>();
            so.FindProperty("_nudgePosZButton").objectReferenceValue = posZ.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void WireMetaShell(DefendPrefabCatalog catalog)
        {
            var meta = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (meta == null)
            {
                Debug.LogWarning("[DefendAssetBuilder] MetaShellRoot missing — run Meta shell builder first.");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(MetaRootPath);
            var controller = contents.GetComponent<MetaShellController>();
            var defendParent = contents.transform.Find("DefendWorldParent");
            if (defendParent == null)
            {
                var defendParentGo = new GameObject("DefendWorldParent");
                defendParentGo.transform.SetParent(contents.transform, false);
                defendParent = defendParentGo.transform;
            }

            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_defendPrefabCatalog").objectReferenceValue = catalog;
                so.FindProperty("_defendWorldParent").objectReferenceValue = defendParent;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, MetaRootPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static RectTransform CreateListColumn(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = CreateUiPanel(parent, name, new Color(0.12f, 0.13f, 0.17f, 0.95f));
            StretchFill(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, 0f);
            panel.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(panel.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            StretchFill(content, Vector2.zero, Vector2.one, 2f);

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 1f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return content;
        }

        private static Button CreateRowTemplate(Transform content, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<Image>().color = new Color(0.24f, 0.27f, 0.34f, 0.95f);
            go.GetComponent<LayoutElement>().preferredHeight = 18f;

            var text = CreateUiText(go.transform, "Label", "row", 11, TextAnchor.MiddleLeft);
            var rect = text.GetComponent<RectTransform>();
            Stretch(rect);
            rect.offsetMin = new Vector2(4f, 0f);
            rect.offsetMax = new Vector2(-4f, 0f);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        private static void SetButtonFontSize(GameObject button, int fontSize)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontSize = fontSize;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabDefendDir);
            EnsureFolder(PrefabWarriorsDir);
            EnsureFolder(PrefabMonstersDir);
            EnsureFolder(PrefabMapsDir);
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsDefendDir);
            EnsureFolder("Assets/Editor");
            EnsureFolder("Assets/Editor/Defend");
            EnsureFolder("Assets/Scripts/Gameplay/Defend");
            EnsureFolder("Assets/Scripts/Core/Defend");
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
            var text = CreateUiText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
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

        private static void StretchFill(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float padding)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            if (size.sqrMagnitude > 0.01f)
            {
                rt.sizeDelta = size;
            }
        }
    }
}
#endif
