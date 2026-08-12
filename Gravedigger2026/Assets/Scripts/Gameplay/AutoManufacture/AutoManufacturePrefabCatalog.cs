using System;
using UnityEngine;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Catalog for AutoManufacture presentation Prefab (SPEC_04 §13 / UI-016).
    /// </summary>
    [CreateAssetMenu(
        fileName = "AutoManufacturePrefabCatalog",
        menuName = "Gravedigger2026/AutoManufacture/Prefab Catalog")]
    public sealed class AutoManufacturePrefabCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _presentationRoot;

        public GameObject PresentationRoot => _presentationRoot;

#if UNITY_EDITOR
        public void EditorSet(GameObject presentationRoot)
        {
            _presentationRoot = presentationRoot;
        }
#endif
    }
}
