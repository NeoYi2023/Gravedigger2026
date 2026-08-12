using Gravedigger2026.Core;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Prefab binding for shared FormationEditorRoot (SPEC_04 §13 / D-032).
    /// Mode2 fork hides ControlPower HUD (D-053 / AM-07 Approach C).
    /// </summary>
    [CreateAssetMenu(fileName = "FormationPrefabCatalog", menuName = "Gravedigger2026/Formation/Prefab Catalog")]
    public sealed class FormationPrefabCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _formationEditorRootPrefab;
        [SerializeField] private GameObject _formationEditorRootPrefabMode2;

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

#if UNITY_EDITOR
        public void EditorSet(GameObject formationEditorRoot)
        {
            _formationEditorRootPrefab = formationEditorRoot;
        }

        public void EditorSetMode2(GameObject formationEditorRootMode2)
        {
            _formationEditorRootPrefabMode2 = formationEditorRootMode2;
        }
#endif
    }
}
