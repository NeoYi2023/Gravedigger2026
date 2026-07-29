using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Prefab bindings for Dig vertical (SPEC_04 §13). Built by DigAssetBuilder.
    /// </summary>
    [CreateAssetMenu(fileName = "DigPrefabCatalog", menuName = "Gravedigger2026/Dig/Prefab Catalog")]
    public sealed class DigPrefabCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class MapEntry
        {
            public string MapId;
            public GameObject Prefab;
        }

        [Serializable]
        public sealed class GraveEntry
        {
            public string QualityId;
            public GameObject Prefab;
        }

        [SerializeField] private GameObject _digStageRootPrefab;
        [SerializeField] private GameObject _diggerPrefab;
        [SerializeField] private GameObject _rewardFlyerPrefab;
        [SerializeField] private DigCursorRingView _uiDigCursorRingPrefab;
        [SerializeField] private List<MapEntry> _maps = new List<MapEntry>();
        [SerializeField] private List<GraveEntry> _graves = new List<GraveEntry>();

        public GameObject DigStageRootPrefab => _digStageRootPrefab;
        public GameObject DiggerPrefab => _diggerPrefab;
        public GameObject RewardFlyerPrefab => _rewardFlyerPrefab;
        public DigCursorRingView UiDigCursorRingPrefab => _uiDigCursorRingPrefab;

        public bool TryGetMap(string mapId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(mapId) || _maps == null)
            {
                return false;
            }

            for (var i = 0; i < _maps.Count; i++)
            {
                var e = _maps[i];
                if (e != null && string.Equals(e.MapId, mapId, StringComparison.Ordinal) && e.Prefab != null)
                {
                    prefab = e.Prefab;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetGrave(string qualityId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(qualityId) || _graves == null)
            {
                return false;
            }

            for (var i = 0; i < _graves.Count; i++)
            {
                var e = _graves[i];
                if (e != null && string.Equals(e.QualityId, qualityId, StringComparison.Ordinal) && e.Prefab != null)
                {
                    prefab = e.Prefab;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        public void EditorSet(
            GameObject digStageRoot,
            GameObject digger,
            GameObject rewardFlyer,
            DigCursorRingView uiDigCursorRing,
            List<MapEntry> maps,
            List<GraveEntry> graves)
        {
            _digStageRootPrefab = digStageRoot;
            _diggerPrefab = digger;
            _rewardFlyerPrefab = rewardFlyer;
            _uiDigCursorRingPrefab = uiDigCursorRing;
            _maps = maps ?? new List<MapEntry>();
            _graves = graves ?? new List<GraveEntry>();
        }
#endif
    }
}
