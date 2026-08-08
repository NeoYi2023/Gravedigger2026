using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Uniform-grid spatial hash on XZ (SPEC_04 §9.7 LocalDetour). Pure C#: no Transform.
    /// Insert/query neighbors via cell buckets — never full-table O(n²) scans.
    /// </summary>
    public sealed class SpatialHash2D
    {
        public const float DefaultCellSize = 0.5f;

        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly Dictionary<long, List<SpatialHashEntry>> _buckets =
            new Dictionary<long, List<SpatialHashEntry>>(256);

        private readonly List<List<SpatialHashEntry>> _listPool = new List<List<SpatialHashEntry>>(64);
        private int _count;
        private int _lastQueryBucketsVisited;

        public SpatialHash2D(float cellSize = DefaultCellSize)
        {
            _cellSize = cellSize > 1e-4f ? cellSize : DefaultCellSize;
            _invCellSize = 1f / _cellSize;
        }

        public float CellSize => _cellSize;
        public int Count => _count;
        public int BucketCount => _buckets.Count;

        /// <summary>Buckets examined by the most recent <see cref="QueryNeighbors"/> (acceptance probe).</summary>
        public int LastQueryBucketsVisited => _lastQueryBucketsVisited;

        public void Clear()
        {
            foreach (var pair in _buckets)
            {
                pair.Value.Clear();
                _listPool.Add(pair.Value);
            }

            _buckets.Clear();
            _count = 0;
            _lastQueryBucketsVisited = 0;
        }

        public void Insert(int id, Vector2 position, float radius)
        {
            var key = CellKey(position.x, position.y);
            if (!_buckets.TryGetValue(key, out var list))
            {
                list = RentList();
                _buckets[key] = list;
            }

            list.Add(new SpatialHashEntry(id, position, radius));
            _count++;
        }

        /// <summary>
        /// Appends entries whose cells overlap the query disk into <paramref name="results"/>.
        /// Caller must Clear/reuse <paramref name="results"/>; this method does not allocate the list.
        /// </summary>
        public void QueryNeighbors(Vector2 center, float queryRadius, List<SpatialHashEntry> results)
        {
            if (results == null)
            {
                return;
            }

            _lastQueryBucketsVisited = 0;
            if (queryRadius < 0f || _count == 0)
            {
                return;
            }

            var minCx = FloorToCell(center.x - queryRadius);
            var maxCx = FloorToCell(center.x + queryRadius);
            var minCy = FloorToCell(center.y - queryRadius);
            var maxCy = FloorToCell(center.y + queryRadius);
            var radiusSq = queryRadius * queryRadius;

            for (var cy = minCy; cy <= maxCy; cy++)
            {
                for (var cx = minCx; cx <= maxCx; cx++)
                {
                    var key = PackKey(cx, cy);
                    if (!_buckets.TryGetValue(key, out var list))
                    {
                        continue;
                    }

                    _lastQueryBucketsVisited++;
                    for (var i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        var dx = e.Position.x - center.x;
                        var dy = e.Position.y - center.y;
                        if (dx * dx + dy * dy <= radiusSq)
                        {
                            results.Add(e);
                        }
                    }
                }
            }
        }

        /// <summary>Recommended neighbor query radius (SPEC_04 §9.7).</summary>
        public static float RecommendedQueryRadius(float agentRadius)
        {
            return 2f * Mathf.Max(0f, agentRadius) + 0.2f;
        }

        private int FloorToCell(float world)
        {
            return Mathf.FloorToInt(world * _invCellSize);
        }

        private long CellKey(float x, float y)
        {
            return PackKey(FloorToCell(x), FloorToCell(y));
        }

        private static long PackKey(int cx, int cy)
        {
            // Pack two 32-bit cell indices into one 64-bit key.
            return ((long)cx << 32) ^ (uint)cy;
        }

        private List<SpatialHashEntry> RentList()
        {
            var last = _listPool.Count - 1;
            if (last < 0)
            {
                return new List<SpatialHashEntry>(8);
            }

            var list = _listPool[last];
            _listPool.RemoveAt(last);
            return list;
        }
    }

    /// <summary>One agent sample stored in <see cref="SpatialHash2D"/> (XZ plane).</summary>
    public readonly struct SpatialHashEntry
    {
        public readonly int Id;
        public readonly Vector2 Position;
        public readonly float Radius;

        public SpatialHashEntry(int id, Vector2 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }
    }
}
