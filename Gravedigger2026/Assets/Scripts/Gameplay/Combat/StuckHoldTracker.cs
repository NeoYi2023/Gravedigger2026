using UnityEngine;

namespace Gravedigger2026.Gameplay.Combat
{
    /// <summary>
    /// Presentation-only stuck hold (SPEC_04 §15.5 v0.75.30).
    /// wantsMove for DetectWindowSeconds with XZ displacement &lt; DisplacementEpsilon
    /// → IsHolding for HoldSeconds (force Idle); pathing/rules unchanged.
    /// </summary>
    public sealed class StuckHoldTracker
    {
        public const float DetectWindowSeconds = 0.5f;
        public const float DisplacementEpsilon = 0.2f;
        public const float HoldSeconds = 1f;

        private Vector3 _windowStartPos;
        private float _windowTimer;
        private float _holdTimer;
        private bool _holding;
        private bool _hasWindowStart;

        public bool IsHolding => _holding;

        public void Reset()
        {
            _holding = false;
            _windowTimer = 0f;
            _holdTimer = 0f;
            _hasWindowStart = false;
        }

        /// <summary>
        /// Advance detection / hold timers. Pass wantsMove as the locomotion intent
        /// that would drive SetMoving(true) if StuckHold were ignored.
        /// </summary>
        public void Tick(bool wantsMove, Vector3 worldPos, float dt)
        {
            if (dt < 0f)
            {
                dt = 0f;
            }

            if (_holding)
            {
                _holdTimer += dt;
                if (_holdTimer >= HoldSeconds)
                {
                    _holding = false;
                    _holdTimer = 0f;
                    _windowTimer = 0f;
                    _windowStartPos = worldPos;
                    _hasWindowStart = true;
                }

                return;
            }

            if (!wantsMove)
            {
                _windowTimer = 0f;
                _windowStartPos = worldPos;
                _hasWindowStart = true;
                return;
            }

            if (!_hasWindowStart)
            {
                _windowStartPos = worldPos;
                _hasWindowStart = true;
                _windowTimer = 0f;
            }

            _windowTimer += dt;
            if (_windowTimer < DetectWindowSeconds)
            {
                return;
            }

            var dx = worldPos.x - _windowStartPos.x;
            var dz = worldPos.z - _windowStartPos.z;
            var displacementSqr = dx * dx + dz * dz;
            var epsilon = DisplacementEpsilon;
            if (displacementSqr < epsilon * epsilon)
            {
                _holding = true;
                _holdTimer = 0f;
            }

            _windowTimer = 0f;
            _windowStartPos = worldPos;
        }
    }
}
