using System;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Binds VisualStyleId → AllIn1 material + intensity property names, or scale-channel (SPEC_04 §15.2).
    /// </summary>
    [CreateAssetMenu(
        fileName = "WarriorVisualStyleCatalog",
        menuName = "Gravedigger2026/Defend/Warrior Visual Style Catalog")]
    public sealed class WarriorVisualStyleCatalog : ScriptableObject
    {
        public enum StyleKind
        {
            Material = 0,
            ScaleModel = 1
        }

        [Serializable]
        public sealed class Entry
        {
            public string StyleId;
            public StyleKind Kind;
            public Material Material;
            public string[] IntensityFloatProperties;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public bool TryGet(string styleId, out Entry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(styleId) || _entries == null)
            {
                return false;
            }

            for (var i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e == null
                    || !string.Equals(e.StyleId, styleId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (e.Kind == StyleKind.ScaleModel)
                {
                    entry = e;
                    return true;
                }

                if (e.Material != null)
                {
                    entry = e;
                    return true;
                }
            }

            return false;
        }
    }
}
