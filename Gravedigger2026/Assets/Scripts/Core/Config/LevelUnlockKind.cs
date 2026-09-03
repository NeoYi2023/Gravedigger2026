namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Stage1 <c>UnlockLevelId</c> resolve result (SPEC_03 §3.9 / SPEC_04 §9.1).
    /// </summary>
    public enum LevelUnlockKind
    {
        /// <summary>Empty UnlockLevelId → LevelId default unlocked.</summary>
        AlwaysUnlocked = 0,

        /// <summary>Value is a known SubLevel GameplayOptionId; unlock when cleared.</summary>
        PrerequisiteOption = 1,

        /// <summary>Value not found in SubLevelConfig → never unlockable via formal entry.</summary>
        NeverUnlockable = 2
    }
}
