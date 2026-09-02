using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.SearchExtract
{
    /// <summary>
    /// StartBattle snapshot of deploy offsets; after gather activation, goals =
    /// current Objective center + offset (SPEC_03 §3.19 / SE-05 Approach A).
    /// </summary>
    public sealed class SearchExtractFormationRelocateService
    {
        private readonly Dictionary<string, Vector2> _offsetByWarriorId =
            new Dictionary<string, Vector2>(StringComparer.Ordinal);

        private Vector2 _deployAnchorCenter;
        private bool _snapshotReady;
        private bool _relocateActive;

        public bool HasSnapshot => _snapshotReady;
        public bool IsRelocateActive => _relocateActive;

        public void Clear()
        {
            _offsetByWarriorId.Clear();
            _deployAnchorCenter = default;
            _snapshotReady = false;
            _relocateActive = false;
        }

        /// <summary>
        /// Snapshot per-warrior XZ offset from deploy centroid (StartBattle).
        /// </summary>
        public void SnapshotFromDeployPositions(IReadOnlyList<DeployPosition> deployPositions)
        {
            Clear();
            if (deployPositions == null || deployPositions.Count == 0)
            {
                return;
            }

            var sum = Vector2.zero;
            var count = 0;
            for (var i = 0; i < deployPositions.Count; i++)
            {
                var entry = deployPositions[i];
                if (string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                sum += entry.WorldXZ;
                count++;
            }

            if (count <= 0)
            {
                return;
            }

            _deployAnchorCenter = sum / count;
            for (var i = 0; i < deployPositions.Count; i++)
            {
                var entry = deployPositions[i];
                if (string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                _offsetByWarriorId[entry.WarriorId] = entry.WorldXZ - _deployAnchorCenter;
            }

            _snapshotReady = true;
            Debug.Log(
                $"[SearchExtractRelocate] Snapshot soldiers={_offsetByWarriorId.Count} " +
                $"anchor={_deployAnchorCenter}");
        }

        /// <summary>Parallel with gather countdown; idempotent.</summary>
        public void ActivateRelocate(Vector2 objectiveCenterXZ)
        {
            if (!_snapshotReady || _relocateActive)
            {
                return;
            }

            _relocateActive = true;
            Debug.Log(
                $"[SearchExtractRelocate] Activated objectiveCenter={objectiveCenterXZ} " +
                $"soldiers={_offsetByWarriorId.Count}");
        }

        public bool TryGetRelocateGoal(string warriorId, Vector2 objectiveCenterXZ, out Vector2 goalXZ)
        {
            goalXZ = default;
            if (!_relocateActive
                || string.IsNullOrEmpty(warriorId)
                || !_offsetByWarriorId.TryGetValue(warriorId, out var offset))
            {
                return false;
            }

            goalXZ = objectiveCenterXZ + offset;
            return true;
        }

        public readonly struct DeployPosition
        {
            public readonly string WarriorId;
            public readonly Vector2 WorldXZ;

            public DeployPosition(string warriorId, Vector2 worldXZ)
            {
                WarriorId = warriorId;
                WorldXZ = worldXZ;
            }
        }
    }
}
