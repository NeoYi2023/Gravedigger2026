using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// Ground-plane fog cells (SPEC_03 §3.21, Approach A).
    /// Cell size is 0.25. Slice 04b stamps Revealed / Explored in place.
    /// Slice 05 stamps Permanent; reveal does not overwrite it.
    /// Approach B stores FogGroupId per cell so the overlay fade does not blend groups.
    /// </summary>
    public sealed class CocFogGrid
    {
        public const float CellSize = 0.25f;
        public const byte Outside = 0;
        public const byte Unseen = 1;
        public const byte Revealed = 2;
        public const byte Explored = 3;
        public const byte Permanent = 4;
        public const int NoGroup = -1;
        public const int MaxGroupId = 254;

        private readonly byte[] _cells;
        private readonly int[] _groups;
        private readonly bool[] _covered;
        private readonly List<int> _revealed;

        public int Width { get; }
        public int Height { get; }
        public float OriginX { get; }
        public float OriginZ { get; }
        public int UnseenCount { get; }
        public bool HasCoverage => UnseenCount > 0;

        public readonly struct RevealStamp
        {
            public readonly Vector2 Center;
            public readonly float Radius;

            public RevealStamp(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }
        }

        public readonly struct AuthoredPolygon
        {
            public readonly Vector2[] Points;
            public readonly int GroupId;

            public AuthoredPolygon(Vector2[] points, int groupId)
            {
                Points = points;
                GroupId = groupId;
            }
        }

        private CocFogGrid(
            int width,
            int height,
            float originX,
            float originZ,
            byte[] cells,
            int[] groups,
            int unseenCount)
        {
            Width = width;
            Height = height;
            OriginX = originX;
            OriginZ = originZ;
            _cells = cells;
            _groups = groups ?? System.Array.Empty<int>();
            UnseenCount = unseenCount;
            _covered = cells != null && cells.Length > 0 ? new bool[cells.Length] : System.Array.Empty<bool>();
            _revealed = new List<int>(64);
        }

        public static CocFogGrid Empty()
        {
            return new CocFogGrid(0, 0, 0f, 0f, System.Array.Empty<byte>(), System.Array.Empty<int>(), 0);
        }

        public static int NormalizeGroupId(int groupId)
        {
            if (groupId < 0)
            {
                return 0;
            }

            return groupId > MaxGroupId ? MaxGroupId : groupId;
        }

        public static CocFogGrid Build(IReadOnlyList<Vector2[]> polygons)
        {
            if (polygons == null || polygons.Count == 0)
            {
                return Empty();
            }

            var authored = new AuthoredPolygon[polygons.Count];
            for (var i = 0; i < polygons.Count; i++)
            {
                authored[i] = new AuthoredPolygon(polygons[i], 0);
            }

            return Build(authored);
        }

        public static CocFogGrid Build(IReadOnlyList<AuthoredPolygon> polygons)
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
                var poly = polygons[p].Points;
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
            var groups = new int[width * height];
            var unseen = 0;
            for (var iz = 0; iz < height; iz++)
            {
                var z = originZ + (iz + 0.5f) * CellSize;
                for (var ix = 0; ix < width; ix++)
                {
                    var x = originX + (ix + 0.5f) * CellSize;
                    var group = ResolveGroup(polygons, x, z);
                    var index = iz * width + ix;
                    if (group == NoGroup)
                    {
                        groups[index] = NoGroup;
                        continue;
                    }

                    cells[index] = Unseen;
                    groups[index] = group;
                    unseen++;
                }
            }

            return new CocFogGrid(width, height, originX, originZ, cells, groups, unseen);
        }

        public byte GetCell(int ix, int iz)
        {
            if (_cells == null || ix < 0 || iz < 0 || ix >= Width || iz >= Height)
            {
                return Outside;
            }

            return _cells[iz * Width + ix];
        }

        public int GetGroup(int ix, int iz)
        {
            if (_groups == null || _groups.Length == 0 || ix < 0 || iz < 0 || ix >= Width || iz >= Height)
            {
                return NoGroup;
            }

            return _groups[iz * Width + ix];
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
        /// Each stamp may use a different radius (D-100 ObserveRange).
        /// Returns true when any cell changed.
        /// </summary>
        public bool ApplyReveal(IReadOnlyList<RevealStamp> stamps)
        {
            if (!HasCoverage || _cells == null || _cells.Length == 0)
            {
                return false;
            }

            var changed = false;
            System.Array.Clear(_covered, 0, _covered.Length);
            if (stamps != null)
            {
                for (var c = 0; c < stamps.Count; c++)
                {
                    var radius = stamps[c].Radius;
                    if (radius <= 0f)
                    {
                        continue;
                    }

                    var r2 = radius * radius;
                    var cx = stamps[c].Center.x;
                    var cz = stamps[c].Center.y;
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

        private static int ResolveGroup(IReadOnlyList<AuthoredPolygon> polygons, float x, float z)
        {
            var group = NoGroup;
            for (var p = 0; p < polygons.Count; p++)
            {
                var poly = polygons[p].Points;
                if (poly == null || poly.Length < 3 || !Contains(poly, x, z))
                {
                    continue;
                }

                var candidate = NormalizeGroupId(polygons[p].GroupId);
                if (group == NoGroup || candidate < group)
                {
                    group = candidate;
                }
            }

            return group;
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
