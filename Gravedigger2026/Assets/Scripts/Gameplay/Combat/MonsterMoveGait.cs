namespace Gravedigger2026.Gameplay.Combat
{
    /// <summary>
    /// Presentation-side walk/run gait timer for monsters (SPEC_04 §9.19 / §15.5).
    /// Rules layer supplies locomotion intent; this tracks walk elapsed and run latch.
    /// </summary>
    public sealed class MonsterMoveGait
    {
        private float _walkElapsed;
        private bool _isRun;
        private bool _wasLocomoting;

        public bool IsRun => _isRun;

        public void Reset()
        {
            _walkElapsed = 0f;
            _isRun = false;
            _wasLocomoting = false;
        }

        /// <summary>
        /// Advance gait. When locomotion stops, full reset (next bout starts walk).
        /// </summary>
        public void Tick(bool isLocomoting, float walkToRunSeconds, float dt)
        {
            if (dt < 0f)
            {
                dt = 0f;
            }

            if (!isLocomoting)
            {
                Reset();
                return;
            }

            if (!_wasLocomoting)
            {
                _walkElapsed = 0f;
                _isRun = walkToRunSeconds <= 0f;
            }
            else if (!_isRun)
            {
                _walkElapsed += dt;
                if (_walkElapsed >= walkToRunSeconds)
                {
                    _isRun = true;
                }
            }

            _wasLocomoting = true;
        }
    }
}
