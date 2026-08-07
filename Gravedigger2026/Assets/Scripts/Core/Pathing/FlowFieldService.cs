using System;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Shared-goal FlowField (SPEC_03 §3.12 / SPEC_04 §9.7 Approach B).
    /// Pure C#: no Transform/Animator. Same-goal units sample one buffer — no per-unit Dijkstra.
    /// Friendlies are never written into the field; only <see cref="IFlowFieldWalkableMask"/>.
    /// </summary>
    public sealed class FlowFieldService
    {
        public const float MinCellSize = 0.25f;
        public const float MaxCellSize = 0.5f;
        public const float DefaultCellSize = 0.5f;

        private const float CardinalCost = 1f;
        private const float DiagonalCost = 1.41421356f;
        private const float Unreachable = float.PositiveInfinity;

        private static readonly int[] NeighborDx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NeighborDz = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private static readonly float[] NeighborCost =
        {
            CardinalCost, CardinalCost, CardinalCost, CardinalCost,
            DiagonalCost, DiagonalCost, DiagonalCost, DiagonalCost
        };

        private Vector3 _mapCenter;
        private Vector2 _halfExtents;
        private float _cellSize = DefaultCellSize;
        private bool _configured;

        private int _cols;
        private int _rows;
        private float _originX;
        private float _originZ;

        private bool[] _walkable;
        private float[] _integration;
        private Vector2[] _dirs;

        private Vector3 _goalWorld;
        private int _goalIndex = -1;
        private bool _hasField;
        private int _rebuildCount;

        /// <summary>Number of successful <see cref="Rebuild"/> calls (SampleDir never increments).</summary>
        public int RebuildCount => _rebuildCount;

        public bool HasField => _hasField;

        public Vector3 GoalWorld => _goalWorld;

        public float CellSize => _cellSize;

        public int Cols => _cols;

        public int Rows => _rows;

        /// <summary>
        /// Cover IsoDiamond / DigMapBounds half-extents. Cell size clamped to Demo 0.25–0.5.
        /// </summary>
        public void Configure(Vector3 mapCenter, Vector2 isoDiamondHalfExtents, float cellSize = DefaultCellSize)
        {
            _mapCenter = mapCenter;
            _halfExtents = SanitizeHalfExtents(isoDiamondHalfExtents);
            _cellSize = Mathf.Clamp(cellSize, MinCellSize, MaxCellSize);
            _configured = true;
            _hasField = false;

            _cols = Mathf.Max(1, Mathf.CeilToInt((_halfExtents.x * 2f) / _cellSize));
            _rows = Mathf.Max(1, Mathf.CeilToInt((_halfExtents.y * 2f) / _cellSize));
            _originX = _mapCenter.x - _halfExtents.x;
            _originZ = _mapCenter.z - _halfExtents.y;

            var count = _cols * _rows;
            EnsureBuffers(count);
        }

        /// <summary>
        /// Build one shared field toward <paramref name="goalWorld"/>.
        /// <paramref name="walkableMask"/> must be static only (incl. AirWall); null → stub full walkable.
        /// </summary>
        public void Rebuild(Vector3 goalWorld, IFlowFieldWalkableMask walkableMask)
        {
            if (!_configured)
            {
                throw new InvalidOperationException(
                    "FlowFieldService.Configure must be called before Rebuild.");
            }

            walkableMask ??= StubFullyWalkableMask.Instance;
            _goalWorld = goalWorld;
            var count = _cols * _rows;

            for (var i = 0; i < count; i++)
            {
                var (cx, cz) = CellCenterXZ(i);
                var inDiamond = ContainsIsoDiamond(cx, cz);
                _walkable[i] = inDiamond && walkableMask.IsWalkable(cx, cz);
                _integration[i] = Unreachable;
                _dirs[i] = Vector2.zero;
            }

            _goalIndex = WorldToIndexClamped(goalWorld.x, goalWorld.z);
            if (_goalIndex < 0 || !_walkable[_goalIndex])
            {
                _goalIndex = FindNearestWalkable(goalWorld.x, goalWorld.z);
            }

            if (_goalIndex >= 0)
            {
                IntegrateFromGoal();
                BuildDirections();
            }

            _hasField = _goalIndex >= 0;
            _rebuildCount++;
        }

        /// <summary>
        /// Sample shared buffer at world position → XZ direction (x,z). Does not rebuild.
        /// Unreachable / blocked / at-goal → zero.
        /// </summary>
        public Vector2 SampleDir(Vector3 worldPos)
        {
            if (!_hasField)
            {
                return Vector2.zero;
            }

            var index = WorldToIndexClamped(worldPos.x, worldPos.z);
            if (index < 0)
            {
                return Vector2.zero;
            }

            return _dirs[index];
        }

        /// <summary>True if cell at world XZ is marked walkable after last Rebuild.</summary>
        public bool IsCellWalkable(float worldX, float worldZ)
        {
            if (!_hasField)
            {
                return false;
            }

            var index = WorldToIndexClamped(worldX, worldZ);
            return index >= 0 && _walkable[index];
        }

        /// <summary>Integration cost at world XZ; infinity if unreachable.</summary>
        public float SampleIntegration(float worldX, float worldZ)
        {
            if (!_hasField)
            {
                return Unreachable;
            }

            var index = WorldToIndexClamped(worldX, worldZ);
            if (index < 0)
            {
                return Unreachable;
            }

            return _integration[index];
        }

        private void EnsureBuffers(int count)
        {
            if (_walkable != null && _walkable.Length == count)
            {
                return;
            }

            _walkable = new bool[count];
            _integration = new float[count];
            _dirs = new Vector2[count];
        }

        private void IntegrateFromGoal()
        {
            var count = _cols * _rows;
            _integration[_goalIndex] = 0f;

            // Open list: small Demo grids (cell 0.25–0.5 over DigMapBounds) — linear min is fine.
            var open = new int[count];
            var openCount = 0;
            var inOpen = new bool[count];

            open[openCount++] = _goalIndex;
            inOpen[_goalIndex] = true;

            while (openCount > 0)
            {
                var bestSlot = 0;
                var bestCost = _integration[open[0]];
                for (var s = 1; s < openCount; s++)
                {
                    var c = _integration[open[s]];
                    if (c < bestCost)
                    {
                        bestCost = c;
                        bestSlot = s;
                    }
                }

                var current = open[bestSlot];
                openCount--;
                open[bestSlot] = open[openCount];
                inOpen[current] = false;

                var (cx, cz) = IndexToCell(current);
                for (var n = 0; n < 8; n++)
                {
                    var nx = cx + NeighborDx[n];
                    var nz = cz + NeighborDz[n];
                    if (nx < 0 || nz < 0 || nx >= _cols || nz >= _rows)
                    {
                        continue;
                    }

                    // Diagonal: both cardinal sides must be walkable (no corner-cut through walls).
                    if (n >= 4)
                    {
                        var sideA = IndexOf(cx + NeighborDx[n], cz);
                        var sideB = IndexOf(cx, cz + NeighborDz[n]);
                        if (sideA < 0 || sideB < 0 || !_walkable[sideA] || !_walkable[sideB])
                        {
                            continue;
                        }
                    }

                    var ni = IndexOf(nx, nz);
                    if (!_walkable[ni])
                    {
                        continue;
                    }

                    var tentative = _integration[current] + NeighborCost[n];
                    if (tentative >= _integration[ni])
                    {
                        continue;
                    }

                    _integration[ni] = tentative;
                    if (!inOpen[ni])
                    {
                        open[openCount++] = ni;
                        inOpen[ni] = true;
                    }
                }
            }
        }

        private void BuildDirections()
        {
            var count = _cols * _rows;
            for (var i = 0; i < count; i++)
            {
                if (!_walkable[i] || float.IsInfinity(_integration[i]))
                {
                    _dirs[i] = Vector2.zero;
                    continue;
                }

                if (i == _goalIndex)
                {
                    _dirs[i] = Vector2.zero;
                    continue;
                }

                var (cx, cz) = IndexToCell(i);
                var bestCost = _integration[i];
                var bestDx = 0;
                var bestDz = 0;
                var found = false;

                for (var n = 0; n < 8; n++)
                {
                    var nx = cx + NeighborDx[n];
                    var nz = cz + NeighborDz[n];
                    if (nx < 0 || nz < 0 || nx >= _cols || nz >= _rows)
                    {
                        continue;
                    }

                    if (n >= 4)
                    {
                        var sideA = IndexOf(cx + NeighborDx[n], cz);
                        var sideB = IndexOf(cx, cz + NeighborDz[n]);
                        if (sideA < 0 || sideB < 0 || !_walkable[sideA] || !_walkable[sideB])
                        {
                            continue;
                        }
                    }

                    var ni = IndexOf(nx, nz);
                    if (!_walkable[ni] || float.IsInfinity(_integration[ni]))
                    {
                        continue;
                    }

                    if (_integration[ni] < bestCost)
                    {
                        bestCost = _integration[ni];
                        bestDx = NeighborDx[n];
                        bestDz = NeighborDz[n];
                        found = true;
                    }
                }

                if (!found)
                {
                    _dirs[i] = Vector2.zero;
                    continue;
                }

                var dir = new Vector2(bestDx, bestDz);
                if (dir.sqrMagnitude > 1e-8f)
                {
                    dir.Normalize();
                }

                _dirs[i] = dir;
            }
        }

        private int FindNearestWalkable(float worldX, float worldZ)
        {
            var best = -1;
            var bestDist = float.PositiveInfinity;
            var count = _cols * _rows;
            for (var i = 0; i < count; i++)
            {
                if (!_walkable[i])
                {
                    continue;
                }

                var (cx, cz) = CellCenterXZ(i);
                var dx = cx - worldX;
                var dz = cz - worldZ;
                var d = dx * dx + dz * dz;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        private (float x, float z) CellCenterXZ(int index)
        {
            var (cx, cz) = IndexToCell(index);
            return (
                _originX + (cx + 0.5f) * _cellSize,
                _originZ + (cz + 0.5f) * _cellSize);
        }

        private (int x, int z) IndexToCell(int index)
        {
            return (index % _cols, index / _cols);
        }

        private int IndexOf(int cellX, int cellZ)
        {
            if (cellX < 0 || cellZ < 0 || cellX >= _cols || cellZ >= _rows)
            {
                return -1;
            }

            return cellZ * _cols + cellX;
        }

        private int WorldToIndexClamped(float worldX, float worldZ)
        {
            if (_cols <= 0 || _rows <= 0)
            {
                return -1;
            }

            var cx = Mathf.FloorToInt((worldX - _originX) / _cellSize);
            var cz = Mathf.FloorToInt((worldZ - _originZ) / _cellSize);
            cx = Mathf.Clamp(cx, 0, _cols - 1);
            cz = Mathf.Clamp(cz, 0, _rows - 1);
            return IndexOf(cx, cz);
        }

        private bool ContainsIsoDiamond(float worldX, float worldZ)
        {
            var dx = Mathf.Abs(worldX - _mapCenter.x);
            var dz = Mathf.Abs(worldZ - _mapCenter.z);
            return dx / _halfExtents.x + dz / _halfExtents.y <= 1f;
        }

        private static Vector2 SanitizeHalfExtents(Vector2 halfExtents)
        {
            return new Vector2(
                Mathf.Max(0.5f, halfExtents.x),
                Mathf.Max(0.5f, halfExtents.y));
        }
    }
}
