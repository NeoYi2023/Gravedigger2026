using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.TacticalFormation;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Mode2 AutoManufacture auto-deploy: PlacementOrder + FormationClassZone spiral
    /// (SPEC_03 §3.15 / D-052 AM-06 Approach A). Does not redeploy prior pool soldiers.
    /// </summary>
    public sealed class AutoFormationDeployService
    {
        private readonly ConfigCsvRepository _configs;
        private readonly WarriorPoolService _warriorPool;
        private readonly BattleFormationService _formation;
        private readonly ITacticalFormationPatternLookup _patterns;
        private readonly TacticalFormationLayoutService _layout = new TacticalFormationLayoutService();

        public AutoFormationDeployService(
            ConfigCsvRepository configs,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            ITacticalFormationPatternLookup patterns = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _patterns = patterns;
        }

        /// <summary>
        /// Deploy batch warrior Ids into matching class zones. Returns how many were deployed.
        /// Missing zone / no free slot → leave undeployed in pool.
        /// </summary>
        public int DeployBatch(
            IReadOnlyList<string> batchWarriorIds,
            IReadOnlyList<FormationClassZoneSnapshot> zones)
        {
            if (batchWarriorIds == null || batchWarriorIds.Count == 0)
            {
                Debug.Log("[AutoFormationDeploy] Batch empty — nothing to deploy.");
                return 0;
            }

            var zoneByClass = BuildZoneMap(zones);
            var ordered = OrderByPlacement(batchWarriorIds);
            var occupied = new List<FormationZoneSpiralSearch.Footprint>(ordered.Count);
            var deployed = 0;

            for (var i = 0; i < ordered.Count; i++)
            {
                var warriorId = ordered[i];
                if (!_warriorPool.TryGet(warriorId, out var warrior) || warrior == null)
                {
                    Debug.LogWarning($"[AutoFormationDeploy] Skip missing pool warrior Id={warriorId}");
                    continue;
                }

                if (_formation.IsDeployed(warriorId))
                {
                    continue;
                }

                var classId = warrior.ClassId ?? string.Empty;
                if (!zoneByClass.TryGetValue(classId, out var zone))
                {
                    Debug.LogWarning(
                        $"[AutoFormationDeploy] No FormationClassZone for ClassId={classId} " +
                        $"warrior={warriorId} — leave in pool.");
                    continue;
                }

                var bodyRadius = ResolveBodyRadius(warrior);
                if (!FormationZoneSpiralSearch.TryFindSlot(zone, bodyRadius, occupied, out var relX, out var relZ))
                {
                    Debug.LogWarning(
                        $"[AutoFormationDeploy] No free slot in zone ClassId={classId} " +
                        $"warrior={warriorId} r={bodyRadius:0.###} — leave in pool.");
                    continue;
                }

                if (!_formation.TryDeployAt(warriorId, relX, relZ, out var error))
                {
                    Debug.LogWarning(
                        $"[AutoFormationDeploy] TryDeployAt failed warrior={warriorId}: {error}");
                    continue;
                }

                occupied.Add(new FormationZoneSpiralSearch.Footprint(relX, relZ, bodyRadius));
                deployed++;
                Debug.Log(
                    $"[AutoFormationDeploy] Deployed {warriorId} Class={classId} " +
                    $"pos=({relX:0.###},{relZ:0.###}) PlacementOrder={ResolvePlacementOrder(classId)}");
            }

            Debug.Log(
                $"[AutoFormationDeploy] Done deployed={deployed}/{batchWarriorIds.Count} " +
                $"zones={zoneByClass.Count}");

            if (_formation.Entries.Count > 0)
            {
                _layout.EvaluateAndApply(
                    _formation,
                    _warriorPool,
                    _configs,
                    _patterns,
                    TacticalFormationLayoutContext.DefaultPlusZ(zones));
            }

            return deployed;
        }

        private float ResolveBodyRadius(WarriorInstance warrior)
        {
            var scale = WarriorVisualModelScale.Resolve(warrior);
            var appearanceId = warrior != null ? warrior.AppearanceId : null;
            if (!string.IsNullOrEmpty(appearanceId)
                && _configs.TryGetAppearance(appearanceId, out var row)
                && row != null)
            {
                return Mathf.Max(FormationZoneSpiralSearch.MinRadius, row.BodyRadius * scale);
            }

            return BodyAppearanceConfigRow.DefaultBodyRadius * scale;
        }

        private List<string> OrderByPlacement(IReadOnlyList<string> batchWarriorIds)
        {
            var list = new List<(string id, int order)>(batchWarriorIds.Count);
            for (var i = 0; i < batchWarriorIds.Count; i++)
            {
                var id = batchWarriorIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var classId = string.Empty;
                if (_warriorPool.TryGet(id, out var warrior) && warrior != null)
                {
                    classId = warrior.ClassId;
                }

                list.Add((id, ResolvePlacementOrder(classId)));
            }

            list.Sort((a, b) =>
            {
                var byOrder = a.order.CompareTo(b.order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.id, b.id);
            });

            var result = new List<string>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                result.Add(list[i].id);
            }

            return result;
        }

        private int ResolvePlacementOrder(string classId)
        {
            if (!string.IsNullOrEmpty(classId)
                && _configs.TryGetClass(classId, out var row)
                && row != null)
            {
                return row.PlacementOrder > 0
                    ? row.PlacementOrder
                    : ClassConfigRow.DefaultPlacementOrderMissing;
            }

            return ClassConfigRow.DefaultPlacementOrderMissing;
        }

        private static Dictionary<string, FormationClassZoneSnapshot> BuildZoneMap(
            IReadOnlyList<FormationClassZoneSnapshot> zones)
        {
            var map = new Dictionary<string, FormationClassZoneSnapshot>(StringComparer.Ordinal);
            if (zones == null)
            {
                return map;
            }

            for (var i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (string.IsNullOrEmpty(z.ClassId))
                {
                    continue;
                }

                if (map.ContainsKey(z.ClassId))
                {
                    Debug.LogWarning(
                        $"[AutoFormationDeploy] Duplicate FormationClassZone ClassId={z.ClassId} — keep first.");
                    continue;
                }

                map[z.ClassId] = z;
            }

            return map;
        }
    }
}
