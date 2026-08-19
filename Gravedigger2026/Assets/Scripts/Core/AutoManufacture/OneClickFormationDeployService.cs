using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Mode2 formation editor “one-click deploy” (SPEC_03 D-074):
    /// deploy only not-yet deployed pool warriors into their class zones with in-zone randomized candidates.
    /// </summary>
    public sealed class OneClickFormationDeployService
    {
        private readonly ConfigCsvRepository _configs;
        private readonly WarriorPoolService _pool;
        private readonly BattleFormationService _formation;

        public OneClickFormationDeployService(
            ConfigCsvRepository configs,
            WarriorPoolService pool,
            BattleFormationService formation)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
        }

        public int DeployNotYetDeployedRandom(IReadOnlyList<FormationClassZoneSnapshot> zones)
        {
            if (zones == null || zones.Count == 0)
            {
                return 0;
            }

            var zoneByClass = BuildZoneMap(zones);
            if (zoneByClass.Count == 0)
            {
                return 0;
            }

            var occupied = BuildOccupiedFootprints();

            var rng = CreateRng();
            var undeployedByClass = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var warriors = _pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var w = warriors[i];
                if (w == null || string.IsNullOrEmpty(w.Id))
                {
                    continue;
                }

                if (_formation.IsDeployed(w.Id))
                {
                    continue;
                }

                var classId = w.ClassId ?? string.Empty;
                if (!zoneByClass.ContainsKey(classId))
                {
                    // No matching profession zone: keep it in pool.
                    continue;
                }

                if (!undeployedByClass.TryGetValue(classId, out var list))
                {
                    list = new List<string>();
                    undeployedByClass[classId] = list;
                }

                list.Add(w.Id);
            }

            var deployed = 0;

            foreach (var kvp in undeployedByClass)
            {
                if (!zoneByClass.TryGetValue(kvp.Key, out var zone))
                {
                    continue;
                }

                // Randomize which one gets which slots.
                ShuffleInPlace(kvp.Value, rng);

                var ids = kvp.Value;
                for (var i = 0; i < ids.Count; i++)
                {
                    var warriorId = ids[i];
                    if (string.IsNullOrEmpty(warriorId))
                    {
                        continue;
                    }

                    if (!_pool.TryGet(warriorId, out var warrior) || warrior == null)
                    {
                        continue;
                    }

                    var bodyRadius = ResolveBodyRadius(warrior);
                    if (!FormationZoneSpiralSearch.TryFindRandomSlot(
                            zone,
                            bodyRadius,
                            occupied,
                            rng,
                            out var relX,
                            out var relZ))
                    {
                        continue;
                    }

                    if (!_formation.TryDeployAt(warriorId, relX, relZ, out var err))
                    {
                        Debug.LogWarning($"[OneClickFormationDeploy] TryDeployAt failed warriorId={warriorId} err={err}");
                        continue;
                    }

                    occupied.Add(new FormationZoneSpiralSearch.Footprint(relX, relZ, bodyRadius));
                    deployed++;
                }
            }

            return deployed;
        }

        private List<FormationZoneSpiralSearch.Footprint> BuildOccupiedFootprints()
        {
            var occupied = new List<FormationZoneSpiralSearch.Footprint>(_formation.Entries.Count);
            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.WarriorId))
                {
                    continue;
                }

                if (!_pool.TryGet(e.WarriorId, out var warrior) || warrior == null)
                {
                    continue;
                }

                var bodyRadius = ResolveBodyRadius(warrior);
                occupied.Add(new FormationZoneSpiralSearch.Footprint(e.PositionX, e.PositionZ, bodyRadius));
            }

            return occupied;
        }

        private float ResolveBodyRadius(WarriorInstance warrior)
        {
            if (warrior == null)
            {
                return BodyAppearanceConfigRow.DefaultBodyRadius;
            }

            var scale = WarriorVisualModelScale.Resolve(warrior);
            var appearanceId = warrior.AppearanceId;
            if (!string.IsNullOrEmpty(appearanceId) &&
                _configs.TryGetAppearance(appearanceId, out var row) &&
                row != null)
            {
                return Mathf.Max(FormationZoneSpiralSearch.MinRadius, row.BodyRadius * scale);
            }

            return BodyAppearanceConfigRow.DefaultBodyRadius * scale;
        }

        private static void ShuffleInPlace(List<string> list, System.Random rng)
        {
            if (list == null || list.Count <= 1)
            {
                return;
            }

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private System.Random CreateRng()
        {
            // Tick-based seed: different clicks produce different shuffle.
            // (No deterministic requirement for this demo feature.)
            var seed = unchecked(Environment.TickCount * 31
                                 + _formation.BoundSlotIndex * 7
                                 + (int)_formation.BoundCampaignMode);
            return new System.Random(seed);
        }

        private static Dictionary<string, FormationClassZoneSnapshot> BuildZoneMap(IReadOnlyList<FormationClassZoneSnapshot> zones)
        {
            var map = new Dictionary<string, FormationClassZoneSnapshot>(StringComparer.Ordinal);
            for (var i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z.ClassId == null || string.IsNullOrEmpty(z.ClassId))
                {
                    continue;
                }

                if (!map.ContainsKey(z.ClassId))
                {
                    map[z.ClassId] = z;
                }
            }

            return map;
        }
    }
}

