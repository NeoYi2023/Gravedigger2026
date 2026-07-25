using System;

namespace Gravedigger2026.Core
{
    /// <summary>
    /// Rules-layer owner of current <see cref="GameplayState"/>. Views subscribe only.
    /// </summary>
    public sealed class GameplayStateService
    {
        public event Action<GameplayState> StateChanged;

        public GameplayState Current { get; private set; } = GameplayState.Dig;

        public void ResetToDefaultDig()
        {
            SetState(GameplayState.Dig);
        }

        public void SetState(GameplayState state)
        {
            if (Current == state)
            {
                return;
            }

            Current = state;
            StateChanged?.Invoke(Current);
        }

        /// <summary>
        /// Demo-only helper for hand-checking D-004. Not a formal shell switch (SPEC TBD).
        /// </summary>
        public void CycleNextForDemoDebug()
        {
            var next = (GameplayState)(((int)Current + 1) % 3);
            SetState(next);
        }
    }
}
