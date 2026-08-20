using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Prefab bindings for Defend Prepare / Combat (SPEC_04 §13 / D-040 / D-041).
    /// Built by DefendAssetBuilder.
    /// </summary>
    [CreateAssetMenu(fileName = "DefendPrefabCatalog", menuName = "Gravedigger2026/Defend/Prefab Catalog")]
    public sealed class DefendPrefabCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class MapEntry
        {
            public string MapId;
            public GameObject Prefab;
        }
        [Serializable]
        public sealed class WarriorAppearanceEntry
        {
            public string AppearanceId;
            public GameObject Prefab;
        }
        [Serializable]
        public sealed class MonsterModelEntry
        {
            public string ModelId;
            public GameObject Prefab;
        }
        [SerializeField] private GameObject _defendStageRootPrefab;
        [SerializeField] private GameObject _battleModeSelectRootPrefab;
        [SerializeField] private GameObject _battleProtagonistPrefab;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Sprite _archerProjectileSprite;
        [SerializeField] private Sprite _mageProjectileSprite;
        [SerializeField] private GameObject _damagePopupPrefab;
        [SerializeField] private GameObject _skillIconHudPrefab;
        [SerializeField] private List<MapEntry> _maps = new List<MapEntry>();
        [SerializeField] private List<WarriorAppearanceEntry> _warriorAppearances = new List<WarriorAppearanceEntry>();
        [SerializeField] private List<MonsterModelEntry> _monsterModels = new List<MonsterModelEntry>();
        [SerializeField] private WarriorVisualStyleCatalog _visualStyleCatalog;
        public GameObject DefendStageRootPrefab => _defendStageRootPrefab;
        public GameObject BattleModeSelectRootPrefab => _battleModeSelectRootPrefab;
        public GameObject BattleProtagonistPrefab => _battleProtagonistPrefab;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public bool TryGetProjectileSprite(BaseClassKind baseClass, out Sprite sprite)
        {
            switch (baseClass)
            {
                case BaseClassKind.Archer:
                    sprite = _archerProjectileSprite;
                    return sprite != null;
                case BaseClassKind.Mage:
                    sprite = _mageProjectileSprite;
                    return sprite != null;
                default:
                    sprite = null;
                    return false;
            }
        }

        /// <summary>PushMap DamagePopup (PM-12/13); not reset by EditorSet — bound by catalog asset.</summary>
        public GameObject DamagePopupPrefab => _damagePopupPrefab;
        /// <summary>PushMap CombatSkillIcon slot (D-071); not reset by EditorSet — bound by catalog asset.</summary>
        public GameObject SkillIconHudPrefab => _skillIconHudPrefab;
        public WarriorVisualStyleCatalog VisualStyleCatalog => _visualStyleCatalog;
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
        public bool TryGetWarriorAppearance(string appearanceId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(appearanceId) || _warriorAppearances == null)
            {
                return false;
            }
            for (var i = 0; i < _warriorAppearances.Count; i++)
            {
                var e = _warriorAppearances[i];
                if (e != null && string.Equals(e.AppearanceId, appearanceId, StringComparison.Ordinal) && e.Prefab != null)
                {
                    prefab = e.Prefab;
                    return true;
                }
            }
            return false;
        }
        public bool TryGetMonsterModel(string modelId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(modelId) || _monsterModels == null)
            {
                return false;
            }
            for (var i = 0; i < _monsterModels.Count; i++)
            {
                var e = _monsterModels[i];
                if (e != null && string.Equals(e.ModelId, modelId, StringComparison.Ordinal) && e.Prefab != null)
                {
                    prefab = e.Prefab;
                    return true;
                }
            }
            return false;
        }
#if UNITY_EDITOR
        public void EditorSet(
            GameObject stageRoot,
            GameObject battleModeSelectRoot,
            GameObject battleProtagonist,
            GameObject projectile,
            List<MapEntry> maps,
            List<WarriorAppearanceEntry> warriorAppearances,
            List<MonsterModelEntry> monsterModels,
            Sprite archerProjectileSprite = null,
            Sprite mageProjectileSprite = null)
        {
            _defendStageRootPrefab = stageRoot;
            _battleModeSelectRootPrefab = battleModeSelectRoot;
            _battleProtagonistPrefab = battleProtagonist;
            _projectilePrefab = projectile;
            _archerProjectileSprite = archerProjectileSprite;
            _mageProjectileSprite = mageProjectileSprite;
            _maps = maps ?? new List<MapEntry>();
            _warriorAppearances = warriorAppearances ?? new List<WarriorAppearanceEntry>();
            _monsterModels = monsterModels ?? new List<MonsterModelEntry>();
        }
#endif
    }
}
