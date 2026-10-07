using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// COC soldier corner follower (SPEC_03 §3.21 Approach A).
    /// When the straight line on that soldier's NavMesh is blocked, a budgeted
    /// <see cref="NavMesh.CalculatePath"/> supplies the next corner for SetGoal.
    /// PushMap / Defend / SearchExtract do not call this.
    /// </summary>
    public sealed class CocSoldierNavPath
    {
        public const int MaxPathsPerFrame = 8;
        public const float RefreshIntervalSeconds = 1f;
        public const float StuckRecalcSeconds = 1f;
        private const float SampleRadius = 2f;

        private struct Cache
        {
            public string TargetId;
            public Vector2 Destination;
            public int AgentTypeId;
            public Vector2[] Corners;
            public int CornerIndex;
            public bool HasPath;
            public bool Pending;
            public float NextRefreshTime;
            public Vector2 StuckOrigin;
            public float StuckSeconds;
            public bool TrackingStuck;
        }

        private readonly Dictionary<int, Cache> _entries = new Dictionary<int, Cache>(16);
        private NavMeshPath _scratch;
        private int _pathsThisFrame;
        private int _frame = -1;

        public void BeginFrame()
        {
            _frame = Time.frameCount;
            _pathsThisFrame = 0;
        }

        public void Clear()
        {
            _entries.Clear();
            _pathsThisFrame = 0;
        }

        public void Release(int moveId)
        {
            _entries.Remove(moveId);
        }

        /// <summary>
        /// Steer point for a soldier who is not yet in attack range.
        /// A clear straight line returns <paramref name="destinationXZ"/>.
        /// </summary>
        public Vector2 ResolveSteer(
            int moveId,
            string targetId,
            Vector3 soldierWorld,
            Vector2 destinationXZ,
            int agentTypeId,
            float deltaTime)
        {
            EnsureFrame();
            var soldier = new Vector2(soldierWorld.x, soldierWorld.z);
            var target = targetId ?? string.Empty;
            if (!_entries.TryGetValue(moveId, out var cache))
            {
                cache = new Cache
                {
                    TargetId = target,
                    Destination = destinationXZ,
                    AgentTypeId = agentTypeId,
                    Pending = true,
                    StuckOrigin = soldier,
                    TrackingStuck = true
                };
            }
            else
            {
                TrackStuck(ref cache, soldier, deltaTime);
                var moveThresh = CombatRuntimeTuning.AttackSlotReclaimMoveThreshold;
                var destMoved = (cache.Destination - destinationXZ).sqrMagnitude >
                                moveThresh * moveThresh;
                var stuck = cache.StuckSeconds >= StuckRecalcSeconds;
                if (!string.Equals(cache.TargetId, target, StringComparison.Ordinal) ||
                    destMoved ||
                    cache.AgentTypeId != agentTypeId ||
                    stuck)
                {
                    cache.Pending = true;
                    cache.NextRefreshTime = 0f;
                    cache.TargetId = target;
                    cache.Destination = destinationXZ;
                    cache.AgentTypeId = agentTypeId;
                    if (stuck)
                    {
                        cache.StuckSeconds = 0f;
                        cache.StuckOrigin = soldier;
                    }
                }

                if (cache.HasPath && Time.time >= cache.NextRefreshTime)
                {
                    cache.Pending = true;
                }
            }

            var fromWorld = soldierWorld;
            var toWorld = new Vector3(destinationXZ.x, soldierWorld.y, destinationXZ.y);
            if (!TrySnap(fromWorld, agentTypeId, out var from) ||
                !TrySnap(toWorld, agentTypeId, out var to))
            {
                DropPath(ref cache);
                _entries[moveId] = cache;
                return destinationXZ;
            }

            if (!IsBlocked(from, to, agentTypeId))
            {
                DropPath(ref cache);
                cache.Pending = false;
                cache.StuckSeconds = 0f;
                cache.StuckOrigin = soldier;
                _entries[moveId] = cache;
                return destinationXZ;
            }

            if (cache.Pending &&
                Time.time >= cache.NextRefreshTime &&
                _pathsThisFrame < MaxPathsPerFrame)
            {
                _pathsThisFrame++;
                if (TryBuild(from, to, agentTypeId, out var corners))
                {
                    cache.Corners = corners;
                    cache.CornerIndex = 0;
                    cache.HasPath = true;
                    cache.Pending = false;
                    cache.NextRefreshTime = Time.time + RefreshIntervalSeconds;
                    cache.StuckSeconds = 0f;
                    cache.StuckOrigin = soldier;
                }
                else
                {
                    cache.Pending = false;
                    cache.NextRefreshTime = Time.time + RefreshIntervalSeconds;
                    if (!cache.HasPath)
                    {
                        DropPath(ref cache);
                    }
                }
            }

            if (cache.HasPath && cache.Corners != null)
            {
                var corner = NextCorner(soldier, cache.Corners, ref cache.CornerIndex, destinationXZ);
                _entries[moveId] = cache;
                return corner;
            }

            _entries[moveId] = cache;
            return destinationXZ;
        }

        private void EnsureFrame()
        {
            var frame = Time.frameCount;
            if (frame == _frame)
            {
                return;
            }

            _frame = frame;
            _pathsThisFrame = 0;
        }

        private static void TrackStuck(ref Cache cache, Vector2 soldier, float deltaTime)
        {
            if (!cache.TrackingStuck)
            {
                cache.StuckOrigin = soldier;
                cache.StuckSeconds = 0f;
                cache.TrackingStuck = true;
                return;
            }

            var delta = soldier - cache.StuckOrigin;
            var eps = CombatRuntimeTuning.StuckDisplacementEpsilon;
            if (delta.sqrMagnitude >= eps * eps)
            {
                cache.StuckOrigin = soldier;
                cache.StuckSeconds = 0f;
                return;
            }

            cache.StuckSeconds += Mathf.Max(0f, deltaTime);
        }

        private static void DropPath(ref Cache cache)
        {
            cache.HasPath = false;
            cache.Corners = null;
            cache.CornerIndex = 0;
        }

        private static Vector2 NextCorner(
            Vector2 soldier,
            Vector2[] corners,
            ref int index,
            Vector2 fallback)
        {
            var arrive = MassMoveScheduler.ArriveEpsilon;
            var arriveSqr = arrive * arrive;
            while (index < corners.Length)
            {
                var corner = corners[index];
                var delta = corner - soldier;
                if (delta.sqrMagnitude > arriveSqr)
                {
                    return corner;
                }

                index++;
            }

            return fallback;
        }

        private bool TryBuild(Vector3 from, Vector3 to, int agentTypeId, out Vector2[] corners)
        {
            corners = null;
            if (_scratch == null)
            {
                // NavMeshPath cannot be created from a MonoBehaviour field initializer.
                _scratch = new NavMeshPath();
            }

            var filter = MakeFilter(agentTypeId);
            if (!NavMesh.CalculatePath(from, to, filter, _scratch) ||
                _scratch.status == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }

            var src = _scratch.corners;
            if (src == null || src.Length < 2)
            {
                return false;
            }

            corners = new Vector2[src.Length];
            for (var i = 0; i < src.Length; i++)
            {
                corners[i] = new Vector2(src[i].x, src[i].z);
            }

            return true;
        }

        private static bool IsBlocked(Vector3 from, Vector3 to, int agentTypeId)
        {
            var flat = to - from;
            flat.y = 0f;
            if (flat.sqrMagnitude <= 1e-6f)
            {
                return false;
            }

            var filter = MakeFilter(agentTypeId);
            if (!NavMesh.Raycast(from, to, out var hit, filter))
            {
                return false;
            }

            var remain = to - hit.position;
            remain.y = 0f;
            var arrive = MassMoveScheduler.ArriveEpsilon;
            return remain.sqrMagnitude > arrive * arrive;
        }

        private static bool TrySnap(Vector3 source, int agentTypeId, out Vector3 snapped)
        {
            var filter = MakeFilter(agentTypeId);
            if (NavMesh.SamplePosition(source, out var hit, SampleRadius, filter))
            {
                snapped = hit.position;
                return true;
            }

            snapped = source;
            return false;
        }

        private static NavMeshQueryFilter MakeFilter(int agentTypeId)
        {
            return new NavMeshQueryFilter
            {
                agentTypeID = agentTypeId,
                areaMask = NavMesh.AllAreas
            };
        }
    }
}
