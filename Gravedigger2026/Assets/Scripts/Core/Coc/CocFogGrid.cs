using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// Ground-plane fog cells (SPEC_03 §3.21, Approach A).
    /// Cell size is 0.25. Slice 04b stamps Revealed / Explored in place.
    /// Slice 05 stamps Permanent; reveal does not overwrite it.
    /// </summary>
    public sealed class CocFogGrid
    {
        public const float CellSize = 0.25f;
        public const byte Outside = 0;
        public const byte Unseen = 1;
        public const byte Revealed = 2;
        public const byte Explored = 3;
        public const byte Permanent = 4;

        private readonly byte[] _cells;
        private readonly bool[] _covered;
        private readonly List<int> _revealed;

        public int Width { get; }
        public int Height { get; }
        public float OriginX { get; }
        public float OriginZ { get; }
        public int UnseenCount { get; }
        public bool HasCoverage => UnseenCount > 0;

        private CocFogGrid(int width, int height, float originX, float originZ, byte[] cells, int unseenCount)
        {
            Width = width;
            Height = height;
            OriginX = originX;
            OriginZ = originZ;
            _cells = cells;
            UnseenCount = unseenCount;
            _covered = cells != null && cells.Length > 0 ? new bool[cells.Length] : System.Array.Empty<bool>();
            _revealed = new List<int>(64);
        }

        public static CocFogGrid Empty()
        {
            return new CocFogGrid(0, 0, 0f, 0f, System.Array.Empty<byte>(), 0);
        }

        public static CocFogGrid Build(IReadOnlyList<Vector2[]> polygons)
        {
            if (polygons == null || polygons.Count == 0)
            {
                return Empty();
            }

            var minX = float.PositiveInfinity;
            var minZ = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxZ = float.NegativeInfinity;
            var usable = 0;
            for (var p = 0; p < polygons.Count; p++)
            {
                var poly = polygons[p];
                if (poly == null || poly.Length < 3)
                {
                    continue;
                }

                usable++;
                for (var i = 0; i < poly.Length; i++)
                {
                    var point = poly[i];
                    if (point.x < minX) minX = point.x;
                    if (point.y < minZ) minZ = point.y;
                    if (point.x > maxX) maxX = point.x;
                    if (point.y > maxZ) maxZ = point.y;
                }
            }

            if (usable == 0 || float.IsInfinity(minX))
            {
                return Empty();
            }

            var originX = Mathf.Floor(minX / CellSize) * CellSize;
            var originZ = Mathf.Floor(minZ / CellSize) * CellSize;
            var width = Mathf.CeilToInt((maxX - originX) / CellSize);
            var height = Mathf.CeilToInt((maxZ - originZ) / CellSize);
            if (width < 1 || height < 1 || width > 512 || height > 512)
            {
                Debug.LogWarning(
                    $"[CocFog] Polygon grid {width}x{height} is empty or too large. Fog skipped.");
                return Empty();
            }

            var cells = new byte[width * height];
            var unseen = 0;
            for (var iz = 0; iz < height; iz++)
            {
                var z = originZ + (iz + 0.5f) * CellSize;
                for (var ix = 0; ix < width; ix++)
                {
                    var x = originX + (ix + 0.5f) * CellSize;
                    if (!ContainsAny(polygons, x, z))
                    {
                        continue;
                    }

                    cells[iz * width + ix] = Unseen;
                    unseen++;
                }
            }

            return new CocFogGrid(width, height, originX, originZ, cells, unseen);
        }

        public byte GetCell(int ix, int iz)
        {
            if (_cells == null || ix < 0 || iz < 0 || ix >= Width || iz >= Height)
            {
                return Outside;
            }

            return _cells[iz * Width + ix];
        }

        public byte GetWorldCell(float worldX, float worldZ)
        {
            if (!HasCoverage)
            {
                return Outside;
            }

            var ix = Mathf.FloorToInt((worldX - OriginX) / CellSize);
            var iz = Mathf.FloorToInt((worldZ - OriginZ) / CellSize);
            return GetCell(ix, iz);
        }

        public bool IsUnseen(float worldX, float worldZ)
        {
            return GetWorldCell(worldX, worldZ) == Unseen;
        }

        public bool BlocksDeploy(float worldX, float worldZ)
        {
            var cell = GetWorldCell(worldX, worldZ);
            return cell != Outside && cell != Permanent;
        }

        /// <summary>
        /// Stamp an activated capture circle as Permanent. Outside cells stay clear.
        /// Returns true when any cell changed.
        /// </summary>
        public bool StampPermanent(float centerX, float centerZ, float radius)
        {
            if (!HasCoverage || _cells == null || _cells.Length == 0 || radius <= 0f)
            {
                return false;
            }

            var changed = false;
            var r2 = radius * radius;
            var minIx = Mathf.Max(0, Mathf.FloorToInt((centerX - radius - OriginX) / CellSize));
            var maxIx = Mathf.Min(Width - 1, Mathf.FloorToInt((centerX + radius - OriginX) / CellSize));
            var minIz = Mathf.Max(0, Mathf.FloorToInt((centerZ - radius - OriginZ) / CellSize));
            var maxIz = Mathf.Min(Height - 1, Mathf.FloorToInt((centerZ + radius - OriginZ) / CellSize));
            for (var iz = minIz; iz <= maxIz; iz++)
            {
                var z = OriginZ + (iz + 0.5f) * CellSize;
                var dz = z - centerZ;
                for (var ix = minIx; ix <= maxIx; ix++)
                {
                    var x = OriginX + (ix + 0.5f) * CellSize;
                    var dx = x - centerX;
                    if (dx * dx + dz * dz > r2)
                    {
                        continue;
                    }

                    var index = iz * Width + ix;
                    if (_cells[index] == Outside || _cells[index] == Permanent)
                    {
                        continue;
                    }

                    if (_cells[index] == Revealed)
                    {
                        RemoveRevealed(index);
                    }

                    _cells[index] = Permanent;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Stamp living-soldier circles as Revealed; uncovered Revealed cells become Explored.
        /// Returns true when any cell changed.
        /// </summary>
        public bool ApplyReveal(IReadOnlyList<Vector2> centers, float radius)
        {
            if (!HasCoverage || _cells == null || _cells.Length == 0)
            {
                return false;
            }

            var changed = false;
            System.Array.Clear(_covered, 0, _covered.Length);
            if (centers != null && radius > 0f)
            {
                var r2 = radius * radius;
                for (var c = 0; c < centers.Count; c++)
                {
                    var cx = centers[c].x;
                    var cz = centers[c].y;
                    var minIx = Mathf.Max(0, Mathf.FloorToInt((cx - radius - OriginX) / CellSize));
                    var maxIx = Mathf.Min(Width - 1, Mathf.FloorToInt((cx + radius - OriginX) / CellSize));
                    var minIz = Mathf.Max(0, Mathf.FloorToInt((cz - radius - OriginZ) / CellSize));
                    var maxIz = Mathf.Min(Height - 1, Mathf.FloorToInt((cz + radius - OriginZ) / CellSize));
                    for (var iz = minIz; iz <= maxIz; iz++)
                    {
                        var z = OriginZ + (iz + 0.5f) * CellSize;
                        var dz = z - cz;
                        for (var ix = minIx; ix <= maxIx; ix++)
                        {
                            var x = OriginX + (ix + 0.5f) * CellSize;
                            var dx = x - cx;
                            if (dx * dx + dz * dz > r2)
                            {
                                continue;
                            }

                            var index = iz * Width + ix;
                            if (_cells[index] == Outside || _cells[index] == Permanent)
                            {
                                continue;
                            }

                            _covered[index] = true;
                            if (_cells[index] == Revealed)
                            {
                                continue;
                            }

                            _cells[index] = Revealed;
                            _revealed.Add(index);
                            changed = true;
                        }
                    }
                }
            }

            for (var n = _revealed.Count - 1; n >= 0; n--)
            {
                var index = _revealed[n];
                if (_covered[index])
                {
                    continue;
                }

                _cells[index] = Explored;
                _revealed.RemoveAt(n);
                changed = true;
            }

            return changed;
        }

        private void RemoveRevealed(int index)
        {
            for (var n = _revealed.Count - 1; n >= 0; n--)
            {
                if (_revealed[n] == index)
                {
                    _revealed.RemoveAt(n);
                    return;
                }
            }
        }

        private static bool ContainsAny(IReadOnlyList<Vector2[]> polygons, float x, float z)
        {
            for (var p = 0; p < polygons.Count; p++)
            {
                var poly = polygons[p];
                if (poly != null && poly.Length >= 3 && Contains(poly, x, z))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(Vector2[] poly, float x, float z)
        {
            var inside = false;
            var j = poly.Length - 1;
            for (var i = 0; i < poly.Length; j = i++)
            {
                var yi = poly[i].y;
                var yj = poly[j].y;
                if ((yi > z) == (yj > z))
                {
                    continue;
                }

                var xi = poly[i].x;
                var xj = poly[j].x;
                var cross = (xj - xi) * (z - yi) / (yj - yi) + xi;
                if (x < cross)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
