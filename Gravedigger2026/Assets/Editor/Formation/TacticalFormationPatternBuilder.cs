#if UNITY_EDITOR
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Formation;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Formation
{
    /// <summary>
    /// Builds sample tactical Pattern Prefabs and binds FormationPrefabCatalog
    /// (SPEC_04 §9.30 / §13; D-084 TF-02). Does not regenerate FormationEditorRoot.
    /// </summary>
    public static class TacticalFormationPatternBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/Formation/Patterns";
        private const string CatalogPath = "Assets/Settings/Formation/FormationPrefabCatalog.asset";
        public const string WedgePrefabId = "FormationPattern_Wedge_01";
        public const string ParallelPrefabId = "FormationPattern_Wedge_02";
        public const string ParallelCompactPrefabId = "FormationPattern_Wedge_03";
        public const string TightParallelPrefabId = "FormationPattern_Wedge_04";
        private const string WedgePath = PrefabDir + "/" + WedgePrefabId + ".prefab";
        private const string ParallelPath = PrefabDir + "/" + ParallelPrefabId + ".prefab";
        private const string ParallelCompactPath = PrefabDir + "/" + ParallelCompactPrefabId + ".prefab";
        private const string TightParallelPath = PrefabDir + "/" + TightParallelPrefabId + ".prefab";

        private static readonly Vector3[] WedgeSlotLocalXz =
        {
            new Vector3(0f, 0f, 1.2f),
            new Vector3(-0.85f, 0f, 0.2f),
            new Vector3(0.85f, 0f, 0.2f),
            new Vector3(-1.7f, 0f, -0.8f),
            new Vector3(1.7f, 0f, -0.8f)
        };

        /// <summary>
        /// Two ranks of five (parallel lines); matches authored FormationPattern_Wedge_02.
        /// </summary>
        private static readonly Vector3[] ParallelSlotLocalXz =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(-0.85f, 0f, 0f),
            new Vector3(0.85f, 0f, 0f),
            new Vector3(-1.7f, 0f, 0f),
            new Vector3(1.7f, 0f, 0f),
            new Vector3(0f, 0f, -0.6f),
            new Vector3(-0.85f, 0f, -0.6f),
            new Vector3(0.85f, 0f, -0.6f),
            new Vector3(-1.7f, 0f, -0.6f),
            new Vector3(1.7f, 0f, -0.6f)
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

                var wedgeMissing = AssetDatabase.LoadAssetAtPath<GameObject>(WedgePath) == null;
                var parallelMissing = AssetDatabase.LoadAssetAtPath<GameObject>(ParallelPath) == null;
                if (!wedgeMissing && !parallelMissing)
                {
                    return;
                }

                EnsureFolders();
                if (wedgeMissing)
                {
                    EnsurePatternPrefab(WedgePath, WedgePrefabId, WedgeSlotLocalXz, overwrite: false);
                }

                if (parallelMissing)
                {
                    EnsurePatternPrefab(ParallelPath, ParallelPrefabId, ParallelSlotLocalXz, overwrite: false);
                }

                BindCatalog(
                    AssetDatabase.LoadAssetAtPath<GameObject>(WedgePath),
                    WedgePrefabId);
                BindCatalog(
                    AssetDatabase.LoadAssetAtPath<GameObject>(ParallelPath),
                    ParallelPrefabId);
                BindCatalog(
                    AssetDatabase.LoadAssetAtPath<GameObject>(ParallelCompactPath),
                    ParallelCompactPrefabId);
                BindCatalog(
                    AssetDatabase.LoadAssetAtPath<GameObject>(TightParallelPath),
                    TightParallelPrefabId);
                AssetDatabase.SaveAssets();
            };
        }

        [MenuItem("Gravedigger2026/Formation（阵型）/Generate Tactical Formation Pattern Prefabs（生成战术阵型图案预制体）")]
        public static void GenerateSamplePatterns()
        {
            EnsureFolders();

            // Menu: regenerate wedge sample (historical TF-02 behavior).
            EnsurePatternPrefab(WedgePath, WedgePrefabId, WedgeSlotLocalXz, overwrite: true);
            // Parallel: create only when missing — never overwrite authored slots.
            EnsurePatternPrefab(ParallelPath, ParallelPrefabId, ParallelSlotLocalXz, overwrite: false);

            BindCatalog(
                AssetDatabase.LoadAssetAtPath<GameObject>(WedgePath),
                WedgePrefabId);
            BindCatalog(
                AssetDatabase.LoadAssetAtPath<GameObject>(ParallelPath),
                ParallelPrefabId);
            BindCatalog(
                AssetDatabase.LoadAssetAtPath<GameObject>(ParallelCompactPath),
                ParallelCompactPrefabId);
            BindCatalog(
                AssetDatabase.LoadAssetAtPath<GameObject>(TightParallelPath),
                TightParallelPrefabId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[TacticalFormationPatternBuilder] Ensured patterns "
                + WedgePrefabId + " / " + ParallelPrefabId + " / " + ParallelCompactPrefabId
                + " / " + TightParallelPrefabId
                + " and bound FormationPrefabCatalog.");
        }

        private static void EnsurePatternPrefab(
            string path,
            string prefabId,
            Vector3[] slotLocalXz,
            bool overwrite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !overwrite)
            {
                return;
            }

            var go = BuildPattern(prefabId, slotLocalXz);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BindCatalog(GameObject prefab, string prefabId)
        {
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[TacticalFormationPatternBuilder] Prefab missing for "
                    + prefabId + "; skip catalog bind.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<FormationPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning("[TacticalFormationPatternBuilder] Missing " + CatalogPath);
                return;
            }

            catalog.EditorUpsertPattern(prefabId, prefab);
            EditorUtility.SetDirty(catalog);
        }

        private static GameObject BuildPattern(string prefabId, Vector3[] slotLocalXz)
        {
            var root = new GameObject(prefabId);
            var pattern = root.AddComponent<TacticalFormationPattern>();
            pattern.EditorSetMoveParams(
                TacticalFormationPattern.DefaultLeashRadius,
                TacticalFormationPattern.DefaultSlotArriveEpsilon,
                TacticalFormationPattern.DefaultCenterMoveSpeedMul,
                TacticalFormationPattern.DefaultFacingTurnRate,
                TacticalFormationPattern.DefaultKeepFormationWhileEngage);

            for (var i = 0; i < slotLocalXz.Length; i++)
            {
                var slot = new GameObject("Slot_" + i.ToString("00"));
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = slotLocalXz[i];
                slot.transform.localRotation = Quaternion.identity;
                slot.transform.localScale = Vector3.one;
                slot.AddComponent<TacticalFormationSlot>();
            }

            pattern.RefreshSlotsFromChildren();
            return root;
        }

        [MenuItem("Gravedigger2026/Formation（阵型）/Ensure Slot PreferredClass Components（确保槽位优选职业组件）")]
        public static void EnsurePreferredClassOnAllPatternSlots()
        {
            EnsureFolders();
            var guids = AssetDatabase.FindAssets(
                "t:Prefab FormationPattern_Wedge",
                new[] { PrefabDir });
            var touched = 0;
            for (var g = 0; g < guids.Length; g++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (string.IsNullOrEmpty(path) || !path.Contains("/Patterns/"))
                {
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var pattern = root.GetComponent<TacticalFormationPattern>();
                    if (pattern == null)
                    {
                        continue;
                    }

                    pattern.RefreshSlotsFromChildren();
                    var slots = pattern.Slots;
                    var dirty = false;
                    var isWedge02 = path.IndexOf("FormationPattern_Wedge_02", System.StringComparison.Ordinal) >= 0;
                    for (var i = 0; i < slots.Length; i++)
                    {
                        var slotTf = slots[i];
                        if (slotTf == null)
                        {
                            continue;
                        }

                        var authored = slotTf.GetComponent<TacticalFormationSlot>();
                        if (authored == null)
                        {
                            authored = slotTf.gameObject.AddComponent<TacticalFormationSlot>();
                            dirty = true;
                        }

                        if (isWedge02)
                        {
                            var local = pattern.GetSlotLocalXZ(i);
                            var want = local.z < -0.01f
                                ? BaseClassKind.Archer
                                : BaseClassKind.Unspecified;
                            if (authored.PreferredClass != want)
                            {
                                authored.EditorSetPreferredClass(want);
                                dirty = true;
                            }
                        }
                    }

                    if (dirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        touched++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[TacticalFormationPatternBuilder] Ensured PreferredClass on pattern slots; saved="
                + touched);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs", "Formation");
            EnsureFolder("Assets/Prefabs/Formation", "Patterns");
            EnsureFolder("Assets/Settings", "Formation");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
