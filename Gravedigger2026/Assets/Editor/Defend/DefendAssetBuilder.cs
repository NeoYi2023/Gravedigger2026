#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Editor.Art;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Maps;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using Gravedigger2026.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.Defend
{
    /// <summary>
    /// Builds Defend StageRoot / BattleProtagonist / Projectile / Catalog, ensures EngageZone + SpawnPoints on Maps,
    /// Monster Prefabs (Art Visual when ready, else temp cubes), and wires MetaShellRoot (D-040–D-042).
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
        private const string BattleModeSelectRootPath = PrefabDefendDir + "/BattleModeSelectRoot.prefab";
        private const string BattleProtagonistPath = PrefabDefendDir + "/BattleProtagonist.prefab";
        private const string ProjectilePath = PrefabDefendDir + "/Projectile.prefab";
        private const string ArcherProjectileArtPath = "Assets/Art/Defend/Projectile/JianShi_1.png";
        private const string MageProjectileArtPath = "Assets/Art/Defend/Projectile/MoFa_1.png";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string AppearanceCsv = "Manufacture_BodyAppearanceConfig.csv";
        private const string MonsterCsv = "Defend_MonsterConfig.csv";
        private const string ClassConfigCsv = "Manufacture_ClassConfig.csv";
        private const string RegenPrefsKey = "Gravedigger2026.DefendAssets.Regen.v0774";

        /// <summary>Sample FormationClassZone IsoDiamond half-extents (SPEC_04 §13 / D-057).</summary>
        private static readonly Vector2 SampleClassZoneHalfExtents = new Vector2(3.85f, 2f);

        private static readonly string[] MapIds =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05"
        };

        /// <summary>
        /// Default world-XZ offsets (relative to DigMapBounds center) for Mode2 ClassIds that need a new zone.
        /// Existing zones keep their world positions; unknown future ClassIds fall back to a simple grid.
        /// </summary>
        private static readonly Dictionary<string, Vector2> NewClassZoneRelXZ =
            new Dictionary<string, Vector2>(StringComparer.Ordinal)
            {
                { "Class_Warrior", new Vector2(-2.0f, -1.2f) },
                { "Class_Knight", new Vector2(-1.0f, -1.2f) },
                { "Class_Paladin", new Vector2(0.0f, -1.2f) },
                { "Class_Berserker", new Vector2(1.0f, -1.2f) },
                { "Class_Rogue", new Vector2(2.0f, -1.2f) },
                { "Class_Servants", new Vector2(0.0f, -0.2f) },
                { "Class_Archer", new Vector2(-2.0f, 1.0f) },
                { "Class_Ranger", new Vector2(-1.0f, 1.0f) },
                { "Class_Mage", new Vector2(0.0f, 1.0f) },
                { "Class_Warlock", new Vector2(1.0f, 1.0f) },
                { "Class_Priest", new Vector2(2.0f, 1.0f) },
                { "Class_Guardian", new Vector2(-2.0f, -1.9f) },
                { "Class_Brawler", new Vector2(0.0f, -1.9f) },
                { "Class_Shadowblade", new Vector2(2.0f, -1.9f) },
                { "Class_Longbowman", new Vector2(-2.0f, 1.7f) },
                { "Class_BombMaster", new Vector2(-1.0f, 1.7f) },
                { "Class_IceMage", new Vector2(0.0f, 1.7f) },
                { "Class_FireMage", new Vector2(1.0f, 1.7f) },
                { "Class_DarkMage", new Vector2(2.0f, 1.7f) },
                { "Class_Warrior_0", new Vector2(-1.0f, -1.9f) },
                { "Class_Rogue_0", new Vector2(1.0f, -1.9f) },
                { "Class_Archer_0", new Vector2(-2.0f, 2.4f) },
                { "Class_Mage_0", new Vector2(0.0f, 2.4f) },
            };

        /// <summary>
        /// Bound into Catalog.Maps only (no Dig EngageZone/SpawnPoint ensure —
        /// PushMap / SearchExtract maps have their own markers).
        /// </summary>
        private static readonly string[] CatalogExtraMapIds =
        {
            "PushMap_Demo_01",
            "PushMap_Demo_02",
            "PushMap_Demo_03",
            "SearchExtract_Demo_01",
            "SearchExtract_Lv1_01"
        };

        /// <summary>PushMap sample maps that need FormationClassZone for Prepare one-click (D-057/D-074).</summary>
        private static readonly string[] PushMapSampleMapIds =
        {
            "PushMap_Demo_01",
            "PushMap_Demo_02",
            "PushMap_Demo_03"
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

            // SPEC_04 §15.2: assemble 2D Visual from Art; never regenerate Capsule Body.
            if (!ProtagonistPrefabAssembler.AssembleBattleProtagonist())
            {
                Debug.LogError("[DefendAssetBuilder] Failed to assemble BattleProtagonist Prefab from Art.");
            }

            var battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleProtagonistPath);

            var projectileGo = BuildProjectile();
            PrefabUtility.SaveAsPrefabAsset(projectileGo, ProjectilePath);
            UnityEngine.Object.DestroyImmediate(projectileGo);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);

            var stageGo = BuildStageRoot();
            PrefabUtility.SaveAsPrefabAsset(stageGo, StageRootPath);
            UnityEngine.Object.DestroyImmediate(stageGo);
            var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath);

            var modeSelectGo = BuildBattleModeSelectRoot();
            PrefabUtility.SaveAsPrefabAsset(modeSelectGo, BattleModeSelectRootPath);
            UnityEngine.Object.DestroyImmediate(modeSelectGo);
            var modeSelectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleModeSelectRootPath);

            var mapEntries = new List<DefendPrefabCatalog.MapEntry>();
            AppendMapEntries(mapEntries, MapIds, warnIfMissing: true);
            AppendMapEntries(mapEntries, CatalogExtraMapIds, warnIfMissing: true);

            var warriorEntries = BuildWarriorAppearanceEntries();
            var monsterEntries = BuildMonsterModelEntries();
            var archerProjectileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArcherProjectileArtPath);
            var mageProjectileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MageProjectileArtPath);

            var catalog = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DefendPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(
                stagePrefab,
                modeSelectPrefab,
                battlePrefab,
                projectilePrefab,
                mapEntries,
                warriorEntries,
                monsterEntries,
                archerProjectileSprite,
                mageProjectileSprite);
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

        private static void AppendMapEntries(
            List<DefendPrefabCatalog.MapEntry> mapEntries,
            string[] mapIds,
            bool warnIfMissing)
        {
            for (var i = 0; i < mapIds.Length; i++)
            {
                var id = mapIds[i];
                var path = $"{PrefabMapsDir}/{id}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    if (warnIfMissing)
                    {
                        Debug.LogWarning($"[DefendAssetBuilder] Map missing: {path}");
                    }

                    continue;
                }

                mapEntries.Add(new DefendPrefabCatalog.MapEntry { MapId = id, Prefab = prefab });
            }
        }

        /// <summary>
        /// Writes FormationClassZone markers onto Ground_01…05 only (SPEC_03 §3.8 D-057).
        /// Does not call GenerateAll; does not rewrite EngageZone / spawn points.
        /// SearchExtract sample map zones: <see cref="EnsureFormationClassZonesOnSearchExtractSample"/>.
        /// </summary>
        [MenuItem("Gravedigger2026/Defend/Ensure Formation Class Zones on Maps")]
        public static void EnsureFormationClassZonesOnMaps()
        {
            for (var i = 0; i < MapIds.Length; i++)
            {
                var path = $"{PrefabMapsDir}/{MapIds[i]}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    continue;
                }

                var contents = PrefabUtility.LoadPrefabContents(path);
                var bounds = contents.GetComponent<DigMapBounds>();
                var center = bounds != null ? bounds.Center : contents.transform.position;
                EnsureFormationClassZones(contents.transform, center);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DefendAssetBuilder] EnsureFormationClassZonesOnMaps done (no GenerateAll).");
        }

        /// <summary>
        /// SearchExtract_Demo_01 FormationClassZone sync (SPEC_03 §3.19 / D-074 Prepare one-click).
        /// Does not rewrite PushMap_Demo_01.
        /// </summary>
        [MenuItem("Gravedigger2026/SearchExtract/Ensure Formation Class Zones on Sample Map")]
        public static void EnsureFormationClassZonesOnSearchExtractSample()
        {
            var path = $"{PrefabMapsDir}/SearchExtract_Demo_01.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"[DefendAssetBuilder] Missing sample map: {path}");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var bounds = contents.GetComponent<DigMapBounds>();
                var center = bounds != null ? bounds.Center : contents.transform.position;
                EnsureFormationClassZones(contents.transform, center);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DefendAssetBuilder] EnsureFormationClassZonesOnSearchExtractSample done.");
        }

        /// <summary>
        /// PushMap_Demo_01/02/03 FormationClassZone sync (SPEC_03 D-057/D-074 Approach B).
        /// Anchor = CameraFollowPath/WP_Start world XZ (else DigMapBounds.Center).
        /// </summary>
        [MenuItem("Gravedigger2026/PushMap/Ensure Formation Class Zones on Sample Maps")]
        public static void EnsureFormationClassZonesOnPushMapSamples()
        {
            var ensured = 0;
            for (var i = 0; i < PushMapSampleMapIds.Length; i++)
            {
                var mapId = PushMapSampleMapIds[i];
                var path = $"{PrefabMapsDir}/{mapId}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[DefendAssetBuilder] Missing PushMap sample map: {path}");
                    continue;
                }

                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var anchor = ResolvePushMapClassZoneAnchor(contents);
                    EnsureFormationClassZones(contents.transform, anchor);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    ensured++;
                    Debug.Log(
                        $"[DefendAssetBuilder] FormationClassZones ensured on {mapId} " +
                        $"anchor=({anchor.x:F2},{anchor.y:F2},{anchor.z:F2})");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[DefendAssetBuilder] EnsureFormationClassZonesOnPushMapSamples done " +
                $"ensured={ensured}/{PushMapSampleMapIds.Length}");
        }

        /// <summary>
        /// Batchmode: -executeMethod Gravedigger2026.Editor.Defend.DefendAssetBuilder.EnsureFormationClassZonesOnPushMapSamplesBatch
        /// </summary>
        public static void EnsureFormationClassZonesOnPushMapSamplesBatch()
        {
            EnsureFormationClassZonesOnPushMapSamples();
            EditorApplication.Exit(0);
        }

        /// <summary>Public entry for SearchExtractSampleMapBuilder / PushMapSampleMapBuilder.</summary>
        public static void EnsureFormationClassZonesOnMapRoot(Transform mapRoot, Vector3 mapCenter)
        {
            EnsureFormationClassZones(mapRoot, mapCenter);
        }

        /// <summary>
        /// PushMap Prepare one-click zone anchor: WP_Start world position, else DigMapBounds.Center.
        /// </summary>
        public static Vector3 ResolvePushMapClassZoneAnchor(GameObject mapRoot)
        {
            if (mapRoot == null)
            {
                return Vector3.zero;
            }

            var wpStart = FindDeepChildByName(mapRoot.transform, "WP_Start");
            if (wpStart != null)
            {
                return wpStart.position;
            }

            var bounds = mapRoot.GetComponentInChildren<DigMapBounds>(true);
            if (bounds != null)
            {
                Debug.LogWarning(
                    $"[DefendAssetBuilder] {mapRoot.name}: WP_Start missing — " +
                    "FormationClassZone anchor falls back to DigMapBounds.Center.");
                return bounds.Center;
            }

            Debug.LogWarning(
                $"[DefendAssetBuilder] {mapRoot.name}: WP_Start and DigMapBounds missing — " +
                "FormationClassZone anchor falls back to map root position.");
            return mapRoot.transform.position;
        }

        private static Transform FindDeepChildByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t != null && t.name == name)
                {
                    return t;
                }
            }

            return null;
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
                EnsureFormationClassZones(contents.transform, center);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// Demo sample class zones on Ground_* (SPEC_03 §3.15 / D-057 / FZ-02).
        /// Authoritative ClassId list = Mode2 Manufacture_ClassConfig. Existing zones keep world XZ;
        /// HalfExtents forced to SampleClassZoneHalfExtents; orphans removed. Parent/children identity.
        /// </summary>
        private static void EnsureFormationClassZones(Transform mapRoot, Vector3 mapCenter)
        {
            var root = ResolveFormationClassZonesRoot(mapRoot, mapCenter);
            ApplyIdentityAuthoringFrame(root, mapCenter);
            var classIds = LoadMode2ClassIds();
            if (classIds.Count == 0)
            {
                Debug.LogWarning(
                    "[DefendAssetBuilder] Mode2 Manufacture_ClassConfig empty or missing — FormationClassZones unchanged.");
                return;
            }

            var half = SampleClassZoneHalfExtents;
            var fallbackIndex = 0;
            for (var i = 0; i < classIds.Count; i++)
            {
                var classId = classIds[i];
                ResolveNewClassZoneRelXZ(classId, ref fallbackIndex, out var relX, out var relZ);
                EnsureOneClassZone(root, mapCenter, classId, relX, relZ, half);
            }

            RemoveOrphanClassZones(root, classIds);
            FinalizeClassZoneMeshes(root);
        }

        private static List<string> LoadMode2ClassIds()
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var csvPath = CsvPathResolver.ResolveExistingFile(ClassConfigCsv, CampaignMode.Mode2);
            if (csvPath == null)
            {
                Debug.LogWarning(
                    $"[DefendAssetBuilder] {ClassConfigCsv} (Mode2) not found — cannot sync FormationClassZones.");
                return result;
            }

            var rows = SimpleCsv.ReadRows(csvPath);
            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].TryGetValue("ClassId", out var classId) || string.IsNullOrWhiteSpace(classId))
                {
                    continue;
                }

                classId = classId.Trim();
                if (!seen.Add(classId))
                {
                    continue;
                }

                result.Add(classId);
            }

            return result;
        }

        private static void ResolveNewClassZoneRelXZ(
            string classId,
            ref int fallbackIndex,
            out float relX,
            out float relZ)
        {
            if (NewClassZoneRelXZ.TryGetValue(classId, out var known))
            {
                relX = known.x;
                relZ = known.y;
                return;
            }

            var col = fallbackIndex % 5;
            var row = fallbackIndex / 5;
            fallbackIndex++;
            relX = -2.0f + col * 1.0f;
            relZ = 3.1f + row * 0.7f;
        }

        private static void RemoveOrphanClassZones(Transform zonesRoot, List<string> keepClassIds)
        {
            var keep = new HashSet<string>(keepClassIds, StringComparer.Ordinal);
            var toDestroy = new List<GameObject>();
            var childCount = zonesRoot.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = zonesRoot.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                var zone = child.GetComponent<FormationClassZone>();
                if (zone == null)
                {
                    continue;
                }

                var id = zone.ClassId;
                if (string.IsNullOrEmpty(id))
                {
                    id = child.name;
                }

                if (!keep.Contains(id) && !keep.Contains(child.name))
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            for (var i = 0; i < toDestroy.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(toDestroy[i]);
            }
        }

        private static Transform ResolveFormationClassZonesRoot(Transform mapRoot, Vector3 mapCenter)
        {
            Transform preferred = null;
            var extras = new List<Transform>();
            var all = mapRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                var tf = all[i];
                if (tf == null || tf.name != "FormationClassZones")
                {
                    continue;
                }

                if (tf.parent == mapRoot && preferred == null)
                {
                    preferred = tf;
                }
                else
                {
                    extras.Add(tf);
                }
            }

            if (preferred == null)
            {
                if (extras.Count > 0)
                {
                    extras.Sort((a, b) => b.childCount.CompareTo(a.childCount));
                    preferred = extras[0];
                    extras.RemoveAt(0);
                    preferred.SetParent(mapRoot, true);
                    preferred.position = mapCenter;
                }
                else
                {
                    var go = new GameObject("FormationClassZones");
                    go.transform.SetParent(mapRoot, false);
                    go.transform.position = mapCenter;
                    preferred = go.transform;
                }
            }

            for (var e = 0; e < extras.Count; e++)
            {
                var extra = extras[e];
                if (extra == null)
                {
                    continue;
                }

                var toMove = new List<Transform>(extra.childCount);
                for (var c = 0; c < extra.childCount; c++)
                {
                    toMove.Add(extra.GetChild(c));
                }

                for (var c = 0; c < toMove.Count; c++)
                {
                    var child = toMove[c];
                    if (child == null)
                    {
                        continue;
                    }

                    if (preferred.Find(child.name) == null)
                    {
                        child.SetParent(preferred, true);
                    }
                }

                UnityEngine.Object.DestroyImmediate(extra.gameObject);
            }

            if (preferred.GetComponent<FormationClassZonesRoot>() == null)
            {
                preferred.gameObject.AddComponent<FormationClassZonesRoot>();
            }

            return preferred;
        }

        private static void ApplyIdentityAuthoringFrame(Transform zonesRoot, Vector3 mapCenter)
        {
            var root = zonesRoot.GetComponent<FormationClassZonesRoot>();
            if (root != null)
            {
                root.ApplyIdentityAuthoringFrame();
            }

            var childCount = zonesRoot.childCount;
            var worldPos = new Vector3[childCount];
            for (var i = 0; i < childCount; i++)
            {
                worldPos[i] = zonesRoot.GetChild(i).position;
            }

            zonesRoot.position = mapCenter;
            zonesRoot.localRotation = Quaternion.identity;
            zonesRoot.localScale = Vector3.one;

            for (var i = 0; i < childCount; i++)
            {
                var child = zonesRoot.GetChild(i);
                child.position = worldPos[i];
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
            }
        }

        private static void FinalizeClassZoneMeshes(Transform zonesRoot)
        {
            var zones = zonesRoot.GetComponentsInChildren<FormationClassZone>(true);
            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null)
                {
                    continue;
                }

                zone.EnsureMeshComponents();
                zone.RebuildMesh();
                EditorUtility.SetDirty(zone);
            }

            EditorUtility.SetDirty(zonesRoot.gameObject);
        }

        private static void EnsureOneClassZone(
            Transform zonesRoot,
            Vector3 mapCenter,
            string classId,
            float relX,
            float relZ,
            Vector2 halfExtents)
        {
            var child = zonesRoot.Find(classId);
            if (child != null)
            {
                var existing = child.GetComponent<FormationClassZone>();
                if (existing == null)
                {
                    existing = child.gameObject.AddComponent<FormationClassZone>();
                }

                // Keep world position; force ClassId + sample HalfExtents (D-057).
                existing.EditorSet(classId, halfExtents);
                existing.EnsureMeshComponents();
                existing.RebuildMesh();
                EditorUtility.SetDirty(existing);
                return;
            }

            var go = new GameObject(classId);
            go.transform.SetParent(zonesRoot, false);
            child = go.transform;
            child.position = new Vector3(mapCenter.x + relX, mapCenter.y + 0.05f, mapCenter.z + relZ);
            child.localRotation = Quaternion.identity;
            var zone = go.AddComponent<FormationClassZone>();
            zone.EditorSet(classId, halfExtents);
            EditorUtility.SetDirty(zone);
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
            // Union Mode1 + Mode2 ModelIds so Mode2-only skins (e.g. MonsterModel_03) stay bound.
            var modelIds = CollectMonsterModelIdsFromCsvs();
            if (modelIds.Count == 0)
            {
                Debug.LogWarning($"[DefendAssetBuilder] {MonsterCsv} not found — monster bindings empty.");
                return entries;
            }

            var colorIndex = 0;
            for (var i = 0; i < modelIds.Count; i++)
            {
                var modelId = modelIds[i];
                var path = $"{PrefabMonstersDir}/{modelId}.prefab";
                // SPEC_04 §15.2: Art-ready ModelIds assemble Visual; never overwrite with temp cube.
                if (MonsterModelPrefabAssembler.HasArtReady(modelId))
                {
                    if (!MonsterModelPrefabAssembler.TryAssemble(modelId))
                    {
                        Debug.LogWarning(
                            $"[DefendAssetBuilder] Art-ready monster assemble failed: {modelId}");
                    }
                }

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

        /// <summary>
        /// Distinct ModelId values from Mode1 and Mode2 Defend_MonsterConfig (stable insert order).
        /// </summary>
        private static List<string> CollectMonsterModelIdsFromCsvs()
        {
            var seen = new HashSet<string>();
            var ordered = new List<string>();
            AppendMonsterModelIds(CsvPathResolver.ResolveExistingFile(MonsterCsv, CampaignMode.Mode1), seen, ordered);
            AppendMonsterModelIds(CsvPathResolver.ResolveExistingFile(MonsterCsv, CampaignMode.Mode2), seen, ordered);
            return ordered;
        }

        private static void AppendMonsterModelIds(string csvPath, HashSet<string> seen, List<string> ordered)
        {
            if (csvPath == null || seen == null || ordered == null)
            {
                return;
            }

            var rows = SimpleCsv.ReadRows(csvPath);
            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].TryGetValue("ModelId", out var modelIdRaw) || string.IsNullOrEmpty(modelIdRaw))
                {
                    continue;
                }

                foreach (var modelId in MonsterModelIdFieldParser.EnumerateModelIds(modelIdRaw))
                {
                    if (seen.Add(modelId))
                    {
                        ordered.Add(modelId);
                    }
                }
            }
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
            agent.radius = 0.03f;
            agent.height = 1.2f;
            return root;
        }

        private static GameObject BuildProjectile()
        {
            var root = new GameObject("Projectile");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 210;
            root.AddComponent<ProjectileView>();
            return root;
        }

        private static GameObject BuildBattleModeSelectRoot()
        {
            var root = new GameObject("BattleModeSelectRoot");
            var view = root.AddComponent<BattleModeSelectView>();

            var canvasGo = new GameObject(
                "BattleModeSelectCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panel = CreateUiPanel(canvasGo.transform, "Panel", new Color(0.06f, 0.07f, 0.1f, 0.94f));
            Stretch(panel.GetComponent<RectTransform>());

            var title = CreateUiText(panel.transform, "Title", "选择战斗模式与关卡", 32, TextAnchor.UpperCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(900f, 48f));

            var defendMode = CreateUiButton(panel.transform, "DefendModeButton", "模式1 保卫战",
                new Color(0.45f, 0.32f, 0.18f, 1f));
            Place(defendMode.GetComponent<RectTransform>(), new Vector2(0.32f, 0.82f), new Vector2(0.32f, 0.82f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 56f));

            var pushMapMode = CreateUiButton(panel.transform, "PushMapModeButton", "模式2 推图战",
                new Color(0.22f, 0.24f, 0.28f, 1f));
            Place(pushMapMode.GetComponent<RectTransform>(), new Vector2(0.68f, 0.82f), new Vector2(0.68f, 0.82f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 56f));

            var listHost = CreateUiPanel(panel.transform, "LevelListHost", new Color(0.1f, 0.12f, 0.15f, 0.95f));
            StretchFill(listHost.GetComponent<RectTransform>(), new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.74f), 0f);

            var content = CreateListColumn(listHost.transform, "LevelContent",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
            var rowTemplateButton = CreateRowTemplate(content, "LevelRowTemplate");
            var rowLe = rowTemplateButton.GetComponent<LayoutElement>();
            if (rowLe != null)
            {
                rowLe.preferredHeight = 48f;
                rowLe.minHeight = 48f;
            }

            var confirm = CreateUiButton(panel.transform, "ConfirmButton", "进入保卫战",
                new Color(0.55f, 0.32f, 0.22f, 1f));
            Place(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 60f));

            var status = CreateUiText(panel.transform, "Status", string.Empty, 18, TextAnchor.LowerCenter);
            Place(status.GetComponent<RectTransform>(), new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.04f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 36f));

            view.EditorBind(
                panel,
                defendMode.GetComponent<Button>(),
                pushMapMode.GetComponent<Button>(),
                content,
                rowTemplateButton.gameObject,
                confirm.GetComponent<Button>(),
                status,
                title);

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
            // DefendRoot is a layout host only — do not draw/raycast a fullscreen Image overlay.
            var defendRootImage = panelRoot.GetComponent<Image>();
            if (defendRootImage != null)
            {
                defendRootImage.enabled = false;
                defendRootImage.raycastTarget = false;
            }

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
            var combatBondHud = EnsureCombatBondHud(panelRoot.transform);

            var hso = new SerializedObject(hud);
            hso.FindProperty("_root").objectReferenceValue = panelRoot;
            hso.FindProperty("_preparePanel").objectReferenceValue = preparePanel;
            hso.FindProperty("_combatPanel").objectReferenceValue = combatPanel;
            hso.FindProperty("_phaseText").objectReferenceValue = phase;
            hso.FindProperty("_combatStatusText").objectReferenceValue = combatStatus;
            hso.FindProperty("_startBattleButton").objectReferenceValue = startBattle.GetComponent<Button>();
            hso.FindProperty("_hintText").objectReferenceValue = hint;
            hso.FindProperty("_combatBondHud").objectReferenceValue = combatBondHud;
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

        private static FormationBondHudView EnsureCombatBondHud(Transform panelRoot)
        {
            var existing = panelRoot.Find("CombatBondHudRoot");
            if (existing != null)
            {
                var view = existing.GetComponent<FormationBondHudView>();
                if (view != null)
                {
                    return view;
                }
            }

            var hudGo = new GameObject("CombatBondHudRoot", typeof(RectTransform), typeof(FormationBondHudView));
            hudGo.transform.SetParent(panelRoot, false);
            var hudRt = hudGo.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0f, 1f);
            hudRt.anchorMax = new Vector2(0f, 1f);
            hudRt.pivot = new Vector2(0f, 1f);
            hudRt.anchoredPosition = new Vector2(16f, -16f);
            hudRt.sizeDelta = new Vector2(160f, 400f);

            var canvas = panelRoot.GetComponentInParent<Canvas>();
            Transform detailParent = canvas != null ? canvas.transform : panelRoot;

            var existingDetail = detailParent.Find("BondDetailModal");
            GameObject detailGo;
            FormationBondDetailView detailView;
            if (existingDetail != null)
            {
                detailGo = existingDetail.gameObject;
                detailView = detailGo.GetComponent<FormationBondDetailView>();
                if (detailView == null)
                {
                    detailView = detailGo.AddComponent<FormationBondDetailView>();
                }
            }
            else
            {
                detailGo = CreateUiPanel(detailParent, "BondDetailModal", new Color(0.08f, 0.1f, 0.14f, 0.96f));
                Place(
                    detailGo.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(720f, 520f));
                detailView = detailGo.AddComponent<FormationBondDetailView>();
                var title = CreateUiText(detailGo.transform, "Title", "阵容羁绊", 24, TextAnchor.UpperCenter);
                Place(title.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -12f), new Vector2(0f, 40f));
                var body = CreateUiText(detailGo.transform, "Body", string.Empty, 16, TextAnchor.UpperLeft);
                Place(body.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(16f, 56f), new Vector2(-32f, -72f));
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                var closeBtn = CreateUiButton(detailGo.transform, "CloseButton", "关闭",
                    new Color(0.35f, 0.4f, 0.5f, 1f));
                Place(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(140f, 40f));
                detailView.Configure(detailGo, title, body, closeBtn.GetComponent<Button>());
                detailGo.SetActive(false);
            }

            var viewBtn = CreateUiButton(hudGo.transform, "ViewBondsButton", "查看阵容羁绊",
                new Color(0.28f, 0.36f, 0.48f, 1f));
            Place(viewBtn.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(160f, 36f));

            var iconRowGo = new GameObject(
                "ActiveBondIconsRow",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            iconRowGo.transform.SetParent(hudGo.transform, false);
            Place(iconRowGo.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -42f), new Vector2(40f, 0f));
            var iconVlg = iconRowGo.GetComponent<VerticalLayoutGroup>();
            iconVlg.spacing = 6f;
            iconVlg.childAlignment = TextAnchor.UpperLeft;
            iconVlg.childControlWidth = false;
            iconVlg.childControlHeight = false;
            iconVlg.childForceExpandWidth = false;
            iconVlg.childForceExpandHeight = false;
            var iconFitter = iconRowGo.GetComponent<ContentSizeFitter>();
            iconFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            iconFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hudView = hudGo.GetComponent<FormationBondHudView>();
            hudView.Configure(viewBtn.GetComponent<Button>(), iconRowGo.GetComponent<RectTransform>(), detailView);
            hudGo.SetActive(false);
            return hudView;
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
