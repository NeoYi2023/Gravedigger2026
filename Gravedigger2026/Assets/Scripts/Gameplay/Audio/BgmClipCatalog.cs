using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Audio
{
    /// <summary>
    /// SerializeField bindings for BGM clips under Art/Audio/BGM (SPEC_04 §9.29 / §13).
    /// </summary>
    [CreateAssetMenu(fileName = "BgmClipCatalog", menuName = "Gravedigger2026/Audio/BGM Clip Catalog")]
    public sealed class BgmClipCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class ClipEntry
        {
            public string ClipId;
            public AudioClip Clip;
        }

        [SerializeField] private List<ClipEntry> _clips = new List<ClipEntry>();

        public bool TryGetClip(string clipId, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(clipId) || _clips == null)
            {
                return false;
            }

            for (var i = 0; i < _clips.Count; i++)
            {
                var e = _clips[i];
                if (e != null
                    && string.Equals(e.ClipId, clipId, StringComparison.Ordinal)
                    && e.Clip != null)
                {
                    clip = e.Clip;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        public void EditorSet(List<ClipEntry> clips)
        {
            _clips = clips ?? new List<ClipEntry>();
        }
#endif
    }
}
