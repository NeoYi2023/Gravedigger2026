using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Soft collision footprint registry + frame-budgeted neighborhood repulsion
    /// (SPEC_03 §3.12 Approach B+ / SPEC_04 §9.7; slice SC-01, wired SC-03).
    /// Pure C#: no Transform/NavMesh. Chosen approach A — position impulse with
    /// round-robin retain: bodies outside this frame's budget keep their last
    /// <c>CorrectionXz</c>. MassMoveScheduler owns an instance and applies it after
    /// LocalDetour (SC-03); static blockers stay on NavMesh/AirWall — this never
    /// replaces them. Per-body strength: effective scale = global
    /// <see cref="RepulsionScale"/> × <see cref="SetRepulsionScale"/> factor.
    /// Hot path: pooled SpatialHash2D + reused buffers, no per-frame managed alloc.
    /// </summary>
    public sealed class SoftCollisionService
    {
        /// <summary>SPEC_04 §9.7: repulsionScale default 1.0 (engage may lower to 0.35–0.5).</summary>
        public const float DefaultRepulsionScale = 1.0f;

        /// <summary>Impulse speed cap (units/sec) so correction stays a soft push, not a teleport.</summary>
        public const float MaxCorrectionSpeed = 2.0f;

        /// <summary>SPEC_04 §9.7: budget aligned with MassMoveScheduler (≤50 round-robin).</summary>
        public const int DefaultMaxBodiesPerFrame = MassMoveScheduler.MaxRecalcPerFrame;

        private struct BodyState
        {
            public int Id;
            public float Radius;
            public Func<Vector2> GetPos;
            public Vector2 CachedPos;
            public Vector2 Correction;
            public float RepulsionScale;
        }

        private readonly List<BodyState> _bodies = new List<BodyState>(64);
        private readonly Dictionary<int, int> _indexById = new Dictionary<int, int>(64);
        private readonly SpatialHash2D _hash = new SpatialHash2D(); // cell 0.5 per SPEC_04 §9.7
        private readonly List<SpatialHashEntry> _neighborBuffer = new List<SpatialHashEntry>(32);
        private int _cursor;

        /// <summary>Debug switch (SPEC_04 §9.7): default true; off → corrections zero, overlap visible.</summary>
        public bool ResolveCollisions { get; set; } = true;

        public float RepulsionScale { get; set; } = DefaultRepulsionScale;

        public int Count => _bodies.Count;

        /// <summary>Bodies actually re-resolved by the most recent <see cref="Tick"/> (acceptance probe).</summary>
        public int LastFrameResolvedCount { get; private set; }

        /// <summary>Buckets examined by the most recent neighbor query (no-O(n²) acceptance probe).</summary>
        public int LastQueryBucketsVisited => _hash.LastQueryBucketsVisited;

        public void Register(int id, float radius, Func<Vector2> getPos)
        {
            if (getPos == null || _indexById.ContainsKey(id))
            {
                return;
            }

            _indexById[id] = _bodies.Count;
            _bodies.Add(new BodyState
            {
                Id = id,
                Radius = Mathf.Max(0.01f, radius),
                GetPos = getPos,
                CachedPos = Vector2.zero,
                Correction = Vector2.zero,
                RepulsionScale = 1f,
            });
        }

        public void Unregister(int id)
        {
            if (!_indexById.TryGetValue(id, out var index))
            {
                return;
            }

            var last = _bodies.Count - 1;
            if (index != last)
            {
                var moved = _bodies[last];
                _bodies[index] = moved;
                _indexById[moved.Id] = index;
            }

            _bodies.RemoveAt(last);
            _indexById.Remove(id);
            if (_cursor >= _bodies.Count)
            {
                _cursor = 0;
            }
        }

        public void Clear()
        {
            _bodies.Clear();
            _indexById.Clear();
            _hash.Clear();
            _neighborBuffer.Clear();
            _cursor = 0;
            LastFrameResolvedCount = 0;
        }

        /// <summary>
        /// Call once per frame (Stage wiring is SC-03): snapshot positions into the hash,
        /// then re-resolve ≤ <paramref name="maxBodiesPerFrame"/> bodies round-robin.
        /// Correction = Σ pushDir · penetration · 0.5 · <see cref="RepulsionScale"/>,
        /// magnitude capped at <see cref="MaxCorrectionSpeed"/> · dt.
        /// </summary>
        public void Tick(float dt, int maxBodiesPerFrame = DefaultMaxBodiesPerFrame)
        {
            LastFrameResolvedCount = 0;
            if (_bodies.Count == 0)
            {
                return;
            }

            if (!ResolveCollisions)
            {
                for (var i = 0; i < _bodies.Count; i++)
                {
                    var b = _bodies[i];
                    b.Correction = Vector2.zero;
                    _bodies[i] = b;
                }

                return;
            }

            _hash.Clear();
            for (var i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                b.CachedPos = b.GetPos();
                _bodies[i] = b;
                _hash.Insert(b.Id, b.CachedPos, b.Radius);
            }

            var budget = Mathf.Clamp(maxBodiesPerFrame, 1, _bodies.Count);
            var maxImpulse = MaxCorrectionSpeed * Mathf.Max(0f, dt);
            var maxImpulseSq = maxImpulse * maxImpulse;
            var globalScale = Mathf.Max(0f, RepulsionScale);

            for (var n = 0; n < budget; n++)
            {
                if (_cursor >= _bodies.Count)
                {
                    _cursor = 0;
                }

                var index = _cursor++;
                var self = _bodies[index];

                // SPEC_04 §9.7: query radius ≈ 2·radius + 0.2.
                var queryRadius = SpatialHash2D.RecommendedQueryRadius(self.Radius);
                _neighborBuffer.Clear();
                _hash.QueryNeighbors(self.CachedPos, queryRadius, _neighborBuffer);

                var push = Vector2.zero;
                for (var i = 0; i < _neighborBuffer.Count; i++)
                {
                    var other = _neighborBuffer[i];
                    if (other.Id == self.Id)
                    {
                        continue;
                    }

                    var minDist = self.Radius + other.Radius;
                    if (minDist < 1e-4f)
                    {
                        continue;
                    }

                    var dx = self.CachedPos.x - other.Position.x;
                    var dy = self.CachedPos.y - other.Position.y;
                    var distSq = dx * dx + dy * dy;

                    if (distSq < 1e-8f)
                    {
                        // Fully coincident: deterministic anti-parallel side push by stable RuntimeId.
                        var dir = CoincidentPushDir(self.Id, other.Id);
                        push.x += dir.x * minDist * 0.5f;
                        push.y += dir.y * minDist * 0.5f;
                        continue;
                    }

                    if (distSq >= minDist * minDist)
                    {
                        continue;
                    }

                    var dist = Mathf.Sqrt(distSq);
                    var halfPen = (minDist - dist) * (0.5f / dist);
                    push.x += dx * halfPen;
                    push.y += dy * halfPen;
                }

                push *= globalScale * self.RepulsionScale;
                var pushLenSq = push.sqrMagnitude;
                if (pushLenSq > maxImpulseSq && pushLenSq > 1e-8f)
                {
                    push *= maxImpulse / Mathf.Sqrt(pushLenSq);
                }

                self.Correction = push;
                _bodies[index] = self;
                LastFrameResolvedCount++;
            }
        }

        public bool TryGetCorrection(int id, out Vector2 correctionXz)
        {
            if (_indexById.TryGetValue(id, out var index))
            {
                correctionXz = _bodies[index].Correction;
                return true;
            }

            correctionXz = Vector2.zero;
            return false;
        }

        /// <summary>
        /// SC-03 per-body strength (SPEC_04 §9.7): effective scale = global
        /// <see cref="RepulsionScale"/> × this factor (default 1.0; engage bubble
        /// GoalKind=AttackSlot/ChaseAnchor lowers it to ~0.35). False when not registered.
        /// </summary>
        public bool SetRepulsionScale(int id, float scale)
        {
            if (!_indexById.TryGetValue(id, out var index))
            {
                return false;
            }

            var b = _bodies[index];
            b.RepulsionScale = Mathf.Max(0f, scale);
            _bodies[index] = b;
            return true;
        }

        /// <summary>Per-body factor probe (acceptance/debug). False when not registered.</summary>
        public bool TryGetRepulsionScale(int id, out float scale)
        {
            if (_indexById.TryGetValue(id, out var index))
            {
                scale = _bodies[index].RepulsionScale;
                return true;
            }

            scale = 0f;
            return false;
        }

        /// <summary>
        /// Order-independent base angle from the id pair; smaller Id takes the base angle,
        /// larger Id takes angle + π — anti-parallel pushes guarantee unstacking (SPEC_03 §3.12).
        /// </summary>
        internal static Vector2 CoincidentPushDir(int selfId, int otherId)
        {
            var lo = Mathf.Min(selfId, otherId);
            var hi = Mathf.Max(selfId, otherId);
            var h = unchecked(lo * 1103515245 + hi * 12345);
            var ang = (h & 0xFF) * (Mathf.PI * 2f / 256f);
            if (selfId > otherId)
            {
                ang += Mathf.PI;
            }

            return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }
    }
}
