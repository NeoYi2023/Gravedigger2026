using UnityEngine;

namespace Gravedigger2026.Gameplay.Shop
{
    /// <summary>
    /// Catalog for Shop StageRoot Prefab (SPEC_04 §10 / UI-026 / D-075).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ShopPrefabCatalog",
        menuName = "Gravedigger2026/Shop/Prefab Catalog")]
    public sealed class ShopPrefabCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _stageRoot;

        public GameObject StageRoot => _stageRoot;

#if UNITY_EDITOR
        public void EditorSet(GameObject stageRoot)
        {
            _stageRoot = stageRoot;
        }
#endif
    }
}
