using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Briefly Instantiates BattleMap Prefab, snapshots FormationClassZone markers
    /// into map-center-relative IsoDiamond data, then destroys (SPEC_03 §3.15 AM-06 / FZ-01).
    /// </summary>
    public static class FormationClassZoneCollector
    {
        public static List<FormationClassZoneSnapshot> CollectFromCatalog(
            DefendPrefabCatalog catalog,
            string mapId)
        {
            var list = new List<FormationClassZoneSnapshot>();
            if (catalog == null || string.IsNullOrEmpty(mapId))
            {
                Debug.LogWarning("[FormationClassZoneCollector] Catalog/mapId missing.");
                return list;
            }

            if (!catalog.TryGetMap(mapId, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"[FormationClassZoneCollector] Map prefab missing for '{mapId}'.");
                return list;
            }

            var instance = Object.Instantiate(prefab);
            instance.name = $"{mapId}__FormationZoneProbe";
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                CollectFromMapInstance(instance, list);
            }
            finally
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(instance);
                }
                else
                {
                    Object.DestroyImmediate(instance);
                }
            }

            Debug.Log($"[FormationClassZoneCollector] Map={mapId} zones={list.Count}");
            return list;
        }

        public static void CollectFromMapInstance(GameObject mapInstance, List<FormationClassZoneSnapshot> into)
        {
            if (into == null || mapInstance == null)
            {
                return;
            }

            var bounds = mapInstance.GetComponent<DigMapBounds>();
            var mapCenter = bounds != null ? bounds.Center : mapInstance.transform.position;
            var zones = mapInstance.GetComponentsInChildren<FormationClassZone>(true);
            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null || string.IsNullOrEmpty(zone.ClassId))
                {
                    continue;
                }

                var half = zone.HalfExtents;
                var world = zone.Center;
                into.Add(new FormationClassZoneSnapshot(
                    zone.ClassId,
                    world.x - mapCenter.x,
                    world.z - mapCenter.z,
                    half.x,
                    half.y));
            }
        }
    }
}
