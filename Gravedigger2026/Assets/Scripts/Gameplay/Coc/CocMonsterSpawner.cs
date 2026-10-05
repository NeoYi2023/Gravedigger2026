using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.PushMap;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Spawns CocSpawnConfig rows on map SpawnPoints (SPEC_03 §3.21).
    /// Slice 03a holds them idle. Slice 03b passes a warrior list and scheduler;
    /// combat stays off until a living friendly exists. HP is not registered on PushMapSessionService.
    /// </summary>
    public sealed class CocMonsterSpawnBindings
    {
        public MassMoveScheduler Scheduler;
        public AttackSlotService AttackSlots;
        public Func<IReadOnlyList<PushMapAdvanceView>> Warriors;
        public Func<string, string, float, bool> OnHitWarrior;
        public Func<int> AllocateMoveId;
        public Action<PushMapMonsterAgentView, MonsterConfigRow> OnSpawned;
    }

    public static class CocMonsterSpawner
    {
        private static readonly List<PushMapAdvanceView> NoWarriors = new List<PushMapAdvanceView>(0);

        public static int Spawn(
            GameObject mapInstance,
            DefendPrefabCatalog catalog,
            ConfigCsvRepository configs,
            IReadOnlyList<CocSpawnConfigRow> rows,
            CocMonsterSpawnBindings bindings,
            List<PushMapMonsterAgentView> spawnedOut)
        {
            if (mapInstance == null || configs == null)
            {
                return 0;
            }

            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning("[CocCombatStage] No CocSpawnConfig rows. Nothing spawned.");
                return 0;
            }

            var points = CollectSpawnPoints(mapInstance);
            var ordered = new List<CocSpawnConfigRow>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    ordered.Add(rows[i]);
                }
            }

            ordered.Sort(CompareRows);

            var occupied = new List<PushMapSpawnSpread.Footprint>(8);
            var spread = new List<Vector3>(4);
            var spawned = 0;
            for (var i = 0; i < ordered.Count; i++)
            {
                spawned += SpawnRow(
                    mapInstance.transform,
                    catalog,
                    configs,
                    points,
                    ordered[i],
                    occupied,
                    spread,
                    bindings,
                    spawnedOut);
            }

            Debug.Log($"[CocCombatStage] Spawned {spawned} monsters. Held idle (no friendlies).");
            return spawned;
        }

        private static int CompareRows(CocSpawnConfigRow a, CocSpawnConfigRow b)
        {
            var order = a.SpawnOrder.CompareTo(b.SpawnOrder);
            if (order != 0)
            {
                return order;
            }

            return string.CompareOrdinal(a.SpawnPointId, b.SpawnPointId);
        }

        private static int SpawnRow(
            Transform parent,
            DefendPrefabCatalog catalog,
            ConfigCsvRepository configs,
            Dictionary<string, SpawnPoint> points,
            CocSpawnConfigRow row,
            List<PushMapSpawnSpread.Footprint> occupied,
            List<Vector3> spread,
            CocMonsterSpawnBindings bindings,
            List<PushMapMonsterAgentView> spawnedOut)
        {
            if (string.IsNullOrEmpty(row.SpawnPointId) ||
                !points.TryGetValue(row.SpawnPointId, out var point) ||
                point == null)
            {
                Debug.LogWarning(
                    $"[CocCombatStage] SpawnPoint '{row.SpawnPointId}' missing on map. Skip {row.SpawnRole} {row.MonsterId}.");
                return 0;
            }

            if (!configs.TryGetMonster(row.MonsterId, out var monsterRow) || monsterRow == null)
            {
                Debug.LogWarning($"[CocCombatStage] MonsterConfig missing: {row.MonsterId}. Skip.");
                return 0;
            }

            var basePos = point.transform.position;
            var bodyRadius = Mathf.Max(0.05f, monsterRow.BodyRadius);
            PushMapSpawnSpread.ComputePositions(basePos, row.SpawnCount, bodyRadius, occupied, spread);

            var count = 0;
            for (var i = 0; i < row.SpawnCount; i++)
            {
                var pos = i < spread.Count ? spread[i] : basePos;
                var go = CreateVisual(catalog, monsterRow, parent);
                if (go == null)
                {
                    continue;
                }

                var warriors = bindings != null && bindings.Warriors != null
                    ? bindings.Warriors
                    : (Func<IReadOnlyList<PushMapAdvanceView>>)ProvideNoWarriors;
                var moveId = bindings != null && bindings.AllocateMoveId != null
                    ? bindings.AllocateMoveId()
                    : 0;
                go.name = $"Monster_{row.SpawnRole}_{monsterRow.MonsterId}_{moveId}_{count}";
                go.transform.position = pos;

                var view = go.GetComponent<PushMapMonsterAgentView>();
                if (view == null)
                {
                    view = go.AddComponent<PushMapMonsterAgentView>();
                }
                view.Bind(
                    monsterRow,
                    null,
                    warriors,
                    null,
                    1f,
                    bindings != null ? bindings.AttackSlots : null,
                    bindings != null ? bindings.Scheduler : null,
                    moveId,
                    bindings != null ? bindings.OnHitWarrior : null);
                view.SetCombatGameplayEnabled(false);
                if (row.SpawnRole == CocSpawnConfigRow.RoleFinalBoss)
                {
                    view.MarkAsBoss(true);
                }

                var role = go.GetComponent<CocMonsterSpawnRole>();
                if (role == null)
                {
                    role = go.AddComponent<CocMonsterSpawnRole>();
                }

                role.Bind(row.SpawnRole);

                bindings?.OnSpawned?.Invoke(view, monsterRow);
                if (spawnedOut != null)
                {
                    spawnedOut.Add(view);
                }

                HoldAgent(go);
                occupied.Add(new PushMapSpawnSpread.Footprint(go.transform.position, bodyRadius));
                count++;
            }

            return count;
        }

        private static GameObject CreateVisual(
            DefendPrefabCatalog catalog,
            MonsterConfigRow monsterRow,
            Transform parent)
        {
            var modelId = monsterRow.PickSpawnModelId();
            GameObject prefab = null;
            if (catalog != null && !string.IsNullOrEmpty(modelId))
            {
                catalog.TryGetMonsterModel(modelId, out prefab);
            }

            GameObject go;
            if (prefab != null)
            {
                go = UnityEngine.Object.Instantiate(prefab, parent);
            }
            else
            {
                Debug.LogWarning(
                    $"[CocCombatStage] Monster prefab missing for {monsterRow.MonsterId} model '{modelId}'. Using a temp cube.");
                go = CreateTempVisual(modelId);
                go.transform.SetParent(parent, false);
            }

            return go;
        }

        private static GameObject CreateTempVisual(string modelId)
        {
            var root = new GameObject(string.IsNullOrEmpty(modelId) ? "MonsterTemp" : modelId);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.75f, 0.25f, 0.25f);
            }

            return root;
        }

        private static void HoldAgent(GameObject go)
        {
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        private static IReadOnlyList<PushMapAdvanceView> ProvideNoWarriors()
        {
            return NoWarriors;
        }

        private static Dictionary<string, SpawnPoint> CollectSpawnPoints(GameObject mapInstance)
        {
            var found = mapInstance.GetComponentsInChildren<SpawnPoint>(true);
            var byId = new Dictionary<string, SpawnPoint>(found.Length, StringComparer.Ordinal);
            for (var i = 0; i < found.Length; i++)
            {
                var point = found[i];
                if (point == null)
                {
                    continue;
                }

                var id = point.SpawnPointId;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (byId.ContainsKey(id))
                {
                    Debug.LogWarning($"[CocCombatStage] Duplicate SpawnPointId '{id}'. Using the first.");
                    continue;
                }

                byId.Add(id, point);
            }

            return byId;
        }
    }
}
