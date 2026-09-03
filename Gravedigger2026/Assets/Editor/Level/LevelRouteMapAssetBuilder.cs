using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.EditorTools.Level
{
    /// <summary>
    /// Ensures per-level LevelRouteMap_{LevelId} Prefabs (UI-031 / D-086 / LRM-05).
    /// </summary>
    public static class LevelRouteMapAssetBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/Level";
        private const string ResourcesPrefabDir = "Assets/Resources/Prefabs/Level";
        private const string EnsureMenuPath = "Gravedigger2026/Level/Ensure LevelRouteMap Prefabs (UI-031)";
        private const string SyncMenuPath = "Gravedigger2026/Level/Sync LevelRouteMap Pins (UI-031)";
        private const string ResourcesMapDir = "Assets/Resources/UI/SubLevelMaps";
        private const string LogPrefix = "[LevelRouteMap]";
        private const string BackgroundName = "Background";
        private const float PinSize = 56f;
        private static readonly Color PinColor = new Color(0.18f, 0.72f, 0.82f, 0.88f);

        [MenuItem(EnsureMenuPath)]
        public static void EnsurePrefabs()
        {
            LevelRouteSelectAssetBuilder.EnsureRouteMapResources();
            EnsureFolder(PrefabDir);

            if (!TryLoadMode2Maps(out var maps))
            {
                return;
            }

            var ensured = 0;
            for (var i = 0; i < maps.Count; i++)
            {
                if (EnsureOne(maps[i]))
                {
                    ensured++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} Ensure complete. Maps={ensured}/{maps.Count} under {PrefabDir}");
        }

        [MenuItem(SyncMenuPath)]
        public static void SyncPins()
        {
            if (!TryLoadMode2Maps(out var maps))
            {
                return;
            }

            var synced = 0;
            for (var i = 0; i < maps.Count; i++)
            {
                var spec = maps[i];
                var path = PrefabPathFor(spec.LevelId);
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing == null)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} Sync skipped '{spec.LevelId}': missing Prefab {path}. Run Ensure first.");
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(existing) as GameObject;
                if (instance == null)
                {
                    Debug.LogWarning($"{LogPrefix} Sync failed to instantiate {path}");
                    continue;
                }

                SyncPinsOn(instance.transform, spec);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                UnityEngine.Object.DestroyImmediate(instance);
                SyncRuntimeCopy(path, spec.LevelId);
                synced++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} Sync complete. Prefabs={synced}/{maps.Count}");
        }

        private static bool EnsureOne(LevelMapSpec spec)
        {
            var path = PrefabPathFor(spec.LevelId);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject rootGo;
            if (existing != null)
            {
                rootGo = PrefabUtility.InstantiatePrefab(existing) as GameObject;
                if (rootGo == null)
                {
                    Debug.LogWarning($"{LogPrefix} Failed to instantiate {path}");
                    return false;
                }
            }
            else
            {
                rootGo = new GameObject("LevelRouteMap_" + spec.LevelId, typeof(RectTransform));
            }

            rootGo.name = "LevelRouteMap_" + spec.LevelId;
            ApplyRootLayout(rootGo.GetComponent<RectTransform>(), spec);
            ApplyBackground(rootGo.transform, spec);
            SyncPinsOn(rootGo.transform, spec);

            PrefabUtility.SaveAsPrefabAsset(rootGo, path);
            UnityEngine.Object.DestroyImmediate(rootGo);
            SyncRuntimeCopy(path, spec.LevelId);
            Debug.Log($"{LogPrefix} Ensured {path} (pins={spec.Pins.Count}, map={spec.RouteMapAssetId})");
            return true;
        }

        private static void ApplyRootLayout(RectTransform rootRt, LevelMapSpec spec)
        {
            var height = ResolveMapHeight(spec.Sprite);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.zero;
            rootRt.pivot = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = new Vector2(LevelRouteSelectView.MapDisplayWidth, height);
            rootRt.localScale = Vector3.one;
            rootRt.localRotation = Quaternion.identity;
        }

        private static void ApplyBackground(Transform root, LevelMapSpec spec)
        {
            var bgTf = root.Find(BackgroundName);
            GameObject bgGo;
            if (bgTf == null)
            {
                bgGo = new GameObject(BackgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGo.transform.SetParent(root, false);
                bgGo.transform.SetAsFirstSibling();
            }
            else
            {
                bgGo = bgTf.gameObject;
                if (bgGo.GetComponent<Image>() == null)
                {
                    bgGo.AddComponent<Image>();
                }

                bgGo.transform.SetAsFirstSibling();
            }

            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgRt.localScale = Vector3.one;

            var image = bgGo.GetComponent<Image>();
            image.sprite = spec.Sprite;
            image.color = spec.Sprite != null ? Color.white : new Color(0.1f, 0.12f, 0.14f, 1f);
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
        }

        private static void SyncPinsOn(Transform root, LevelMapSpec spec)
        {
            var expected = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < spec.Pins.Count; i++)
            {
                expected.Add(spec.Pins[i].OptionId);
            }

            for (var i = 0; i < spec.Pins.Count; i++)
            {
                var pin = spec.Pins[i];
                var child = root.Find(pin.OptionId);
                if (child == null)
                {
                    CreatePin(root, pin);
                    Debug.Log($"{LogPrefix} {spec.LevelId}: added pin '{pin.OptionId}' at {pin.MapPos}");
                    continue;
                }

                ApplyPinLayout(child.GetComponent<RectTransform>(), overwritePosition: false, pin.MapPos);
            }

            for (var c = 0; c < root.childCount; c++)
            {
                var child = root.GetChild(c);
                var name = child.name;
                if (string.Equals(name, BackgroundName, StringComparison.Ordinal) || expected.Contains(name))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"{LogPrefix} {spec.LevelId}: extra pin '{name}' is not in Operation/SubLevel tables.");
            }
        }

        private static void CreatePin(Transform root, PinSpec pin)
        {
            var go = new GameObject(pin.OptionId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(root, false);
            var image = go.GetComponent<Image>();
            image.color = PinColor;
            image.raycastTarget = true;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(2f, 2f);
            labelRt.offsetMax = new Vector2(-2f, -2f);
            var text = labelGo.GetComponent<Text>();
            text.text = pin.OptionId;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 11;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 7;
            text.resizeTextMaxSize = 11;

            ApplyPinLayout(go.GetComponent<RectTransform>(), overwritePosition: true, pin.MapPos);
        }

        private static void ApplyPinLayout(RectTransform rt, bool overwritePosition, Vector2 mapPos)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PinSize, PinSize);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            if (overwritePosition)
            {
                rt.anchoredPosition = mapPos;
            }
        }

        private static float ResolveMapHeight(Sprite sprite)
        {
            var width = LevelRouteSelectView.MapDisplayWidth;
            if (sprite != null && sprite.rect.width > 0.01f)
            {
                return width * (sprite.rect.height / sprite.rect.width);
            }

            return Mathf.Max(width, 2200f);
        }

        private static bool TryLoadMode2Maps(out List<LevelMapSpec> maps)
        {
            maps = new List<LevelMapSpec>();
            var opPath = CsvPathResolver.ResolveExistingFile(
                "Level_LevelOperationConfig.csv", CampaignMode.Mode2);
            var subPath = CsvPathResolver.ResolveExistingFile(
                "Level_SubLevelConfig.csv", CampaignMode.Mode2);
            if (string.IsNullOrEmpty(opPath) || string.IsNullOrEmpty(subPath))
            {
                Debug.LogError(
                    $"{LogPrefix} Mode2 CSV missing. Operation='{opPath}' SubLevel='{subPath}'.");
                return false;
            }

            var posByOption = LoadOptionalMapPositions(subPath);
            var opRows = SimpleCsv.ReadRows(opPath);
            var byLevel = new Dictionary<string, LevelMapSpec>(StringComparer.Ordinal);
            for (var i = 0; i < opRows.Count; i++)
            {
                var raw = opRows[i];
                var levelId = GetCell(raw, "LevelId");
                if (string.IsNullOrEmpty(levelId))
                {
                    continue;
                }

                if (!byLevel.TryGetValue(levelId, out var spec))
                {
                    spec = new LevelMapSpec { LevelId = levelId };
                    byLevel[levelId] = spec;
                    maps.Add(spec);
                }

                var mapId = GetCell(raw, "RouteMapAssetId");
                if (!string.IsNullOrEmpty(mapId))
                {
                    if (string.IsNullOrEmpty(spec.RouteMapAssetId))
                    {
                        spec.RouteMapAssetId = mapId;
                    }
                    else if (!string.Equals(spec.RouteMapAssetId, mapId, StringComparison.Ordinal))
                    {
                        Debug.LogWarning(
                            $"{LogPrefix} Level '{levelId}' has conflicting RouteMapAssetId '{spec.RouteMapAssetId}' vs '{mapId}'; using first.");
                    }
                }

                for (var slot = 1; slot <= 5; slot++)
                {
                    var optionId = GetCell(raw, "GameplayOptionId" + slot);
                    if (string.IsNullOrEmpty(optionId) || spec.HasPin(optionId))
                    {
                        continue;
                    }

                    var pos = Vector2.zero;
                    if (posByOption.TryGetValue(optionId, out var mapped))
                    {
                        pos = mapped;
                    }

                    spec.Pins.Add(new PinSpec { OptionId = optionId, MapPos = pos });
                }
            }

            for (var i = maps.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(maps[i].RouteMapAssetId))
                {
                    maps[i].Sprite = LoadMapSprite(maps[i].RouteMapAssetId);
                    continue;
                }

                Debug.Log(
                    $"{LogPrefix} Skip '{maps[i].LevelId}': empty RouteMapAssetId (legacy Stage-row layout).");
                maps.RemoveAt(i);
            }

            if (maps.Count == 0)
            {
                Debug.LogWarning($"{LogPrefix} No LevelId with RouteMapAssetId in Mode2 Operation CSV.");
                return false;
            }

            return true;
        }

        private static Dictionary<string, Vector2> LoadOptionalMapPositions(string subPath)
        {
            var result = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            var rows = SimpleCsv.ReadRows(subPath);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var id = GetCell(raw, "GameplayOptionId");
                if (string.IsNullOrEmpty(id) || result.ContainsKey(id))
                {
                    continue;
                }

                if (!raw.ContainsKey("MapPosX") && !raw.ContainsKey("MapPosY"))
                {
                    continue;
                }

                result[id] = new Vector2(ParseFloat(raw, "MapPosX"), ParseFloat(raw, "MapPosY"));
            }

            return result;
        }

        private static void SyncRuntimeCopy(string sourcePrefabPath, string levelId)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder(ResourcesPrefabDir);
            var dest = ResourcesPrefabDir + "/LevelRouteMap_" + levelId + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dest) != null)
            {
                AssetDatabase.DeleteAsset(dest);
            }

            if (!AssetDatabase.CopyAsset(sourcePrefabPath, dest))
            {
                Debug.LogWarning($"{LogPrefix} Failed to copy runtime Resources prefab '{dest}'.");
            }
        }

        private static Sprite LoadMapSprite(string routeMapAssetId)
        {
            if (string.IsNullOrEmpty(routeMapAssetId))
            {
                return null;
            }

            var assetPath = ResourcesMapDir + "/" + routeMapAssetId + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing sprite '{assetPath}'.");
            }

            return sprite;
        }

        private static string PrefabPathFor(string levelId)
        {
            return PrefabDir + "/LevelRouteMap_" + levelId + ".prefab";
        }

        private static string GetCell(Dictionary<string, string> row, string column)
        {
            if (row == null || !row.TryGetValue(column, out var value))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static float ParseFloat(Dictionary<string, string> row, string column)
        {
            var text = GetCell(row, column);
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0f;
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

        private sealed class LevelMapSpec
        {
            public string LevelId;
            public string RouteMapAssetId;
            public Sprite Sprite;
            public readonly List<PinSpec> Pins = new List<PinSpec>();

            public bool HasPin(string optionId)
            {
                for (var i = 0; i < Pins.Count; i++)
                {
                    if (string.Equals(Pins[i].OptionId, optionId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class PinSpec
        {
            public string OptionId;
            public Vector2 MapPos;
        }
    }
}
