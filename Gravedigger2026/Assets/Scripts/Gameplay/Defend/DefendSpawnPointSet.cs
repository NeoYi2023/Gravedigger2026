using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Demo-min fixed spawn markers on BattleMap Prefab (SPEC_03 §3.12 / D-041).
    /// ClockHour 1–12 maps to clockPoints on IsoDiamond rim; RegionRandom picks from randomPool.
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

                return MapFootprintMath.PointOnClockHour(mapCenter, halfExtents, hour, 0.9f);
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

            // InsideMap / OutsideMap both fall back to diamond rim this Demo slice.
            var edge = string.Equals(appearLocation, "InsideMap", System.StringComparison.OrdinalIgnoreCase)
                ? 0.55f
                : 0.95f;
            return MapFootprintMath.PointOnClockHour(mapCenter, halfExtents, 3, edge);
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
