using System;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap Combat camera rail (SPEC_03 §3.14 / SPEC_04 §9.22 Approach B).
    /// Author child <see cref="CameraPathWaypoint"/> start/turns/end; bake world-XZ
    /// straight samples into local XZ polyline. Runtime look-at = Evaluate(max soldier projection).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapCameraPath : MonoBehaviour
    {
        [SerializeField] private Vector3[] _bakedPoints = Array.Empty<Vector3>();

        private float[] _prefix = Array.Empty<float>();
        private float _totalLength;

        public bool HasBakedPath => _bakedPoints != null && _bakedPoints.Length >= 2;

        public int BakedPointCount => _bakedPoints != null ? _bakedPoints.Length : 0;

        public void SetBakedPoints(Vector3[] localPoints)
        {
            _bakedPoints = localPoints ?? Array.Empty<Vector3>();
            RebuildLengthCache();
        }

        public CameraPathWaypoint[] CollectWaypoints()
        {
            var found = GetComponentsInChildren<CameraPathWaypoint>(true);
            if (found == null || found.Length == 0)
            {
                return Array.Empty<CameraPathWaypoint>();
            }

            Array.Sort(found, CompareWaypoints);
            return found;
        }

        public bool TryBake()
        {
            return PushMapCameraPathBaker.TryBake(this, out _);
        }

        public bool TryBake(out string error)
        {
            return PushMapCameraPathBaker.TryBake(this, out error);
        }

        public bool SnapWaypointsToGrid()
        {
            var grid = GetComponentInParent<Grid>();
            if (grid == null)
            {
                return false;
            }

            var waypoints = CollectWaypoints();
            var any = false;
            for (var i = 0; i < waypoints.Length; i++)
            {
                var wp = waypoints[i];
                if (wp == null)
                {
                    continue;
                }

                var cell = grid.WorldToCell(wp.transform.position);
                var center = grid.GetCellCenterWorld(cell);
                wp.transform.position = new Vector3(center.x, wp.transform.position.y, center.z);
                any = true;
            }

            return any;
        }

        public bool TryEvaluate(float s, out Vector3 worldXz)
        {
            worldXz = default;
            if (!HasBakedPath)
            {
                return false;
            }

            EnsureLengthCache();
            s = Mathf.Clamp01(s);
            if (_totalLength < 1e-6f)
            {
                worldXz = transform.TransformPoint(_bakedPoints[0]);
                worldXz.y = transform.position.y;
                return true;
            }

            var target = s * _totalLength;
            for (var i = 0; i < _bakedPoints.Length - 1; i++)
            {
                var a = transform.TransformPoint(_bakedPoints[i]);
                var b = transform.TransformPoint(_bakedPoints[i + 1]);
                var seg = _prefix[i + 1] - _prefix[i];
                if (target > _prefix[i + 1] && i < _bakedPoints.Length - 2)
                {
                    continue;
                }

                var t = seg > 1e-6f ? Mathf.Clamp01((target - _prefix[i]) / seg) : 0f;
                var p = Vector3.Lerp(a, b, t);
                worldXz = new Vector3(p.x, transform.position.y, p.z);
                return true;
            }

            var last = transform.TransformPoint(_bakedPoints[_bakedPoints.Length - 1]);
            worldXz = new Vector3(last.x, transform.position.y, last.z);
            return true;
        }

        public bool TryProjectProgress(Vector3 worldPos, out float s)
        {
            s = 0f;
            if (!HasBakedPath)
            {
                return false;
            }

            EnsureLengthCache();
            if (_totalLength < 1e-6f)
            {
                return true;
            }

            var bestDistSq = float.MaxValue;
            var bestArc = 0f;
            var px = worldPos.x;
            var pz = worldPos.z;
            for (var i = 0; i < _bakedPoints.Length - 1; i++)
            {
                var a = transform.TransformPoint(_bakedPoints[i]);
                var b = transform.TransformPoint(_bakedPoints[i + 1]);
                ProjectPointOntoSegmentXz(px, pz, a.x, a.z, b.x, b.z, out var t, out var distSq);
                if (distSq >= bestDistSq)
                {
                    continue;
                }

                bestDistSq = distSq;
                var seg = _prefix[i + 1] - _prefix[i];
                bestArc = _prefix[i] + t * seg;
            }

            s = Mathf.Clamp01(bestArc / _totalLength);
            return true;
        }

        private void OnEnable()
        {
            RebuildLengthCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLengthCache();
        }
#endif

        private void EnsureLengthCache()
        {
            if (_prefix != null && _prefix.Length == (_bakedPoints != null ? _bakedPoints.Length : 0))
            {
                return;
            }

            RebuildLengthCache();
        }

        private void RebuildLengthCache()
        {
            if (_bakedPoints == null || _bakedPoints.Length == 0)
            {
                _prefix = Array.Empty<float>();
                _totalLength = 0f;
                return;
            }

            var n = _bakedPoints.Length;
            if (_prefix == null || _prefix.Length != n)
            {
                _prefix = new float[n];
            }

            _totalLength = 0f;
            _prefix[0] = 0f;
            for (var i = 1; i < n; i++)
            {
                var a = _bakedPoints[i - 1];
                var b = _bakedPoints[i];
                var dx = b.x - a.x;
                var dz = b.z - a.z;
                _totalLength += Mathf.Sqrt(dx * dx + dz * dz);
                _prefix[i] = _totalLength;
            }
        }

        private static void ProjectPointOntoSegmentXz(
            float px,
            float pz,
            float ax,
            float az,
            float bx,
            float bz,
            out float t,
            out float distSq)
        {
            var abx = bx - ax;
            var abz = bz - az;
            var abLenSq = abx * abx + abz * abz;
            if (abLenSq < 1e-10f)
            {
                t = 0f;
                var dx0 = px - ax;
                var dz0 = pz - az;
                distSq = dx0 * dx0 + dz0 * dz0;
                return;
            }

            t = Mathf.Clamp01(((px - ax) * abx + (pz - az) * abz) / abLenSq);
            var qx = ax + abx * t;
            var qz = az + abz * t;
            var dx = px - qx;
            var dz = pz - qz;
            distSq = dx * dx + dz * dz;
        }

        private static int CompareWaypoints(CameraPathWaypoint a, CameraPathWaypoint b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            var order = a.Order.CompareTo(b.Order);
            if (order != 0)
            {
                return order;
            }

            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        }

        private void OnDrawGizmos()
        {
            var waypoints = CollectWaypoints();
            Gizmos.color = new Color(0.2f, 0.55f, 0.85f, 0.55f);
            for (var i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                {
                    continue;
                }

                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
            }

            if (!HasBakedPath)
            {
                return;
            }

            Gizmos.color = new Color(0.15f, 0.95f, 0.85f, 0.95f);
            for (var i = 0; i < _bakedPoints.Length - 1; i++)
            {
                var a = transform.TransformPoint(_bakedPoints[i]);
                var b = transform.TransformPoint(_bakedPoints[i + 1]);
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.08f);
            }

            var last = transform.TransformPoint(_bakedPoints[_bakedPoints.Length - 1]);
            Gizmos.DrawSphere(last, 0.08f);
        }
    }
}
