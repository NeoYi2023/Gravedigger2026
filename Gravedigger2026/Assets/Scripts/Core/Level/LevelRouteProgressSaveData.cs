using System;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// PlayerPrefs JSON DTO for level route cleared options (SPEC_04 §6 / D-088).
    /// </summary>
    [Serializable]
    public sealed class LevelRouteProgressSaveData
    {
        /// <summary>
        /// Cleared GameplayOptionIds (flat; OptionIds must not reuse across LevelIds).
        /// </summary>
        public string[] ClearedOptionIds = Array.Empty<string>();
    }
}
