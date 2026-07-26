using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Demo-min fixed spawn markers on BattleMap Prefab (SPEC_03 §3.12 / D-041).
    /// ClockHour 1–12 maps to clockPoints; RegionRandom picks from randomPool.
    /// </summary>
    public sealed class DefendSpawnPointSet : MonoBehaviour
    {
        [SerializeField] private Transform[] _clockPoints = new Transform[13];
        [SerializeField] private Transform[] _randomPool;

        public Vector3 ResolveSpawnPosition(
            string appearLocation,
            string spawnMode,
            int spawnClockHour,
            Vector3 mapCenter,
            Vector2 halfExtents)
        {
            if (string.Equals(spawnMode, "ClockDirection", System.StringComparison.OrdinalIgnoreCase))
            {
                var hour = Mathf.Clamp(spawnClockHour, 1, 12);
                if (_clockPoints != null && hour < _clockPoints.Length && _clockPoints[hour] != null)
                {
                    return _clockPoints[hour].position;
                }

                return ClockFallback(mapCenter, halfExtents, hour);
            }

            if (_randomPool != null && _randomPool.Length > 0)
            {
                var pick = _randomPool[Random.Range(0, _randomPool.Length)];
                if (pick != null)
                {
                    return pick.position;
                }
            }

            if (_clockPoints != null)
            {
                for (var i = 1; i < _clockPoints.Length; i++)
                {
                    if (_clockPoints[i] != null)
                    {
                        return _clockPoints[i].position;
                    }
                }
            }

            // InsideMap / OutsideMap both fall back to edge offset this Demo slice.
            var edge = string.Equals(appearLocation, "InsideMap", System.StringComparison.OrdinalIgnoreCase)
                ? 0.55f
                : 0.95f;
            return mapCenter + new Vector3(halfExtents.x * edge, 0f, 0f);
        }

        private static Vector3 ClockFallback(Vector3 mapCenter, Vector2 halfExtents, int hour)
        {
            // 12 = +Z, 3 = +X (clock on XZ plane).
            var angleDeg = (12 - hour) * 30f;
            var rad = angleDeg * Mathf.Deg2Rad;
            var radius = Mathf.Max(halfExtents.x, halfExtents.y) * 0.9f;
            return mapCenter + new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
        }

#if UNITY_EDITOR
        public void EditorSetPoints(Transform[] clockPoints, Transform[] randomPool)
        {
            _clockPoints = clockPoints ?? new Transform[13];
            _randomPool = randomPool;
        }
#endif
    }
}
