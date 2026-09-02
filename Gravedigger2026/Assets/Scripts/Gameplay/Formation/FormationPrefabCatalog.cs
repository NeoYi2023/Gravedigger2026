using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.TacticalFormation;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Prefab binding for shared FormationEditorRoot (SPEC_04 §13 / D-032)
    /// and tactical Pattern Prefabs (SPEC_04 §9.30 / D-084 TF-02).
    /// Mode2 fork hides ControlPower HUD (D-053 / AM-07 Approach C).
    /// </summary>
    [CreateAssetMenu(fileName = "FormationPrefabCatalog", menuName = "Gravedigger2026/Formation/Prefab Catalog")]
    public sealed class FormationPrefabCatalog : ScriptableObject, ITacticalFormationPatternLookup
    {
        [Serializable]
        public sealed class PatternEntry
        {
            public string PrefabId;
            public GameObject Prefab;
        }

        [SerializeField] private GameObject _formationEditorRootPrefab;
        [SerializeField] private GameObject _formationEditorRootPrefabMode2;
        [SerializeField] private List<PatternEntry> _patterns = new List<PatternEntry>();

        public GameObject FormationEditorRootPrefab => _formationEditorRootPrefab;

        /// <summary>
        /// Mode1 → FormationEditorRoot; Mode2 → FormationEditorRoot_Mode2 (ControlPower HUD off).
        /// Falls back to Mode1 with warning if Mode2 asset missing.
        /// </summary>
        public GameObject ResolveEditorRoot(CampaignMode mode)
        {
            if (mode == CampaignMode.Mode2)
            {
                if (_formationEditorRootPrefabMode2 != null)
                {
                    return _formationEditorRootPrefabMode2;
                }

                Debug.LogWarning(
                    "[Formation Catalog] Mode2 EditorRoot missing — falling back to Mode1 Prefab (ControlPower HUD may show).");
            }

            return _formationEditorRootPrefab;
        }

        /// <summary>
        /// Resolves table <c>PrefabId</c> → Pattern Prefab authoring component (SPEC_04 §9.30).
        /// Missing bind / missing component → false (caller may Warning).
        /// </summary>
        public bool TryGetPattern(string prefabId, out TacticalFormationPattern pattern)
        {
            pattern = null;
            if (string.IsNullOrWhiteSpace(prefabId) || _patterns == null)
            {
                return false;
            }

            var key = prefabId.Trim();
            for (var i = 0; i < _patterns.Count; i++)
            {
                var e = _patterns[i];
                if (e == null
                    || !string.Equals(e.PrefabId, key, StringComparison.Ordinal)
                    || e.Prefab == null)
                {
                    continue;
                }

                pattern = e.Prefab.GetComponent<TacticalFormationPattern>();
                return pattern != null;
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryGetSlotLocalXZ(string prefabId, out Vector3[] slotLocalXZ)
        {
            slotLocalXZ = Array.Empty<Vector3>();
            if (!TryGetPattern(prefabId, out var pattern) || pattern == null)
            {
                return false;
            }

            var slots = pattern.Slots;
            var count = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    count++;
                }
            }

            if (count <= 0)
            {
                return false;
            }

            var result = new Vector3[count];
            var w = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                result[w++] = pattern.GetSlotLocalXZ(i);
            }

            slotLocalXZ = result;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetMoveParams(string prefabId, out TacticalFormationMoveParams moveParams)
        {
            moveParams = TacticalFormationMoveParams.CreateDefault();
            if (!TryGetPattern(prefabId, out var pattern) || pattern == null)
            {
                return false;
            }

            moveParams = pattern.ReadMoveParams();
            return true;
        }

#if UNITY_EDITOR
        public void EditorSet(GameObject formationEditorRoot)
        {
            _formationEditorRootPrefab = formationEditorRoot;
        }

        public void EditorSetMode2(GameObject formationEditorRootMode2)
        {
            _formationEditorRootPrefabMode2 = formationEditorRootMode2;
        }

        public void EditorUpsertPattern(string prefabId, GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(prefabId) || prefab == null)
            {
                return;
            }

            if (_patterns == null)
            {
                _patterns = new List<PatternEntry>();
            }

            var key = prefabId.Trim();
            for (var i = 0; i < _patterns.Count; i++)
            {
                var e = _patterns[i];
                if (e != null && string.Equals(e.PrefabId, key, StringComparison.Ordinal))
                {
                    e.Prefab = prefab;
                    return;
                }
            }

            _patterns.Add(new PatternEntry { PrefabId = key, Prefab = prefab });
        }
#endif
    }
}
