using System;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Dig countdown lightning interval (D-078). Pure C#; first fire after a full wait.
    /// </summary>
    public sealed class DigLightningScheduler
    {
        private float _remaining;
        private bool _active;

        public void Reset(float intervalSeconds)
        {
            _active = intervalSeconds > 0f;
            _remaining = Mathf.Max(0.01f, intervalSeconds);
        }

        public void Clear()
        {
            _active = false;
            _remaining = 0f;
        }

        public void Tick(float deltaTime, float intervalSeconds, Action onFire)
        {
            if (!_active || deltaTime <= 0f || intervalSeconds <= 0f)
            {
                return;
            }

            if (_remaining > intervalSeconds)
            {
                _remaining = intervalSeconds;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f)
            {
                return;
            }

            onFire?.Invoke();
            _remaining = Mathf.Max(0.01f, intervalSeconds);
        }
    }
}
