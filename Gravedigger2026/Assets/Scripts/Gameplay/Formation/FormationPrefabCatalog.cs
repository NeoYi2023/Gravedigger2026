using System;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Prefab binding for shared FormationEditorRoot (SPEC_04 §13 / D-032).
    /// </summary>
    [CreateAssetMenu(fileName = "FormationPrefabCatalog", menuName = "Gravedigger2026/Formation/Prefab Catalog")]
    public sealed class FormationPrefabCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _formationEditorRootPrefab;

        public GameObject FormationEditorRootPrefab => _formationEditorRootPrefab;

#if UNITY_EDITOR
        public void EditorSet(GameObject formationEditorRoot)
        {
            _formationEditorRootPrefab = formationEditorRoot;
        }
#endif
    }
}
