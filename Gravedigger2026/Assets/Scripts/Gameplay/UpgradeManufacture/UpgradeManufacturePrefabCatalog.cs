using System;
using UnityEngine;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Prefab binding for UM stage root and warrior appearance Prefabs (SPEC_04 §13). Built by UmAssetBuilder.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UpgradeManufacturePrefabCatalog",
        menuName = "Gravedigger2026/UpgradeManufacture/Prefab Catalog")]
    public sealed class UpgradeManufacturePrefabCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class WarriorAppearanceEntry
        {
            public string AppearanceId;
            public GameObject Prefab;
        }

        [SerializeField] private GameObject _stageRootPrefab;
        [SerializeField] private WarriorAppearanceEntry[] _warriorAppearances = Array.Empty<WarriorAppearanceEntry>();

        public GameObject StageRootPrefab => _stageRootPrefab;

        /// <summary>
        /// Resolves Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab bound at build time.
        /// </summary>
        public bool TryGetWarriorAppearance(string appearanceId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(appearanceId) || _warriorAppearances == null)
            {
                return false;
            }

            for (var i = 0; i < _warriorAppearances.Length; i++)
            {
                var entry = _warriorAppearances[i];
                if (entry != null && string.Equals(entry.AppearanceId, appearanceId, StringComparison.Ordinal))
                {
                    prefab = entry.Prefab;
                    return prefab != null;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        public void EditorSet(GameObject stageRoot)
        {
            _stageRootPrefab = stageRoot;
        }

        public void EditorSetWarriorAppearances(WarriorAppearanceEntry[] entries)
        {
            _warriorAppearances = entries ?? Array.Empty<WarriorAppearanceEntry>();
        }
#endif
    }
}
