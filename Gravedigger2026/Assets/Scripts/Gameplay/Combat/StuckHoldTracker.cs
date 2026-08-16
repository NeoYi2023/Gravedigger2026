using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Combat
{
    /// <summary>
    /// Presentation-only stuck hold (SPEC_04 §15.5).
    /// wantsMove for StuckDetectWindowSeconds with XZ displacement &lt; StuckDisplacementEpsilon
    /// → IsHolding for StuckHoldSeconds (force Idle); pathing/rules unchanged.
    /// </summary>
    public sealed class StuckHoldTracker
    {
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
                if (_holdTimer >= CombatRuntimeTuning.StuckHoldSeconds)
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
            if (_windowTimer < CombatRuntimeTuning.StuckDetectWindowSeconds)
            {
                return;
            }

            var dx = worldPos.x - _windowStartPos.x;
            var dz = worldPos.z - _windowStartPos.z;
            var displacementSqr = dx * dx + dz * dz;
            var epsilon = CombatRuntimeTuning.StuckDisplacementEpsilon;
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
