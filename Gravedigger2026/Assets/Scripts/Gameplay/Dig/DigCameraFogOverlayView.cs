using System.Collections;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig-stage camera fog overlay pulse: linear center-out scale 1.0 → 1.05 over 5s, then back over 5s, looped.
    /// Started/stopped by DigStageController while the dig session is active.
    /// </summary>
    public sealed class DigCameraFogOverlayView : MonoBehaviour
    {
        private const float DefaultExpandSeconds = 5f;
        private const float DefaultShrinkSeconds = 5f;
        private const float DefaultScaleDelta = 0.05f;

        [SerializeField] private float _expandSeconds = DefaultExpandSeconds;
        [SerializeField] private float _shrinkSeconds = DefaultShrinkSeconds;
        [SerializeField] private float _scaleDelta = DefaultScaleDelta;

        private RectTransform _rect;
        private Coroutine _pulseRoutine;
        private static readonly Vector3 BaseScale = Vector3.one;

        private void Awake()
        {
            _rect = transform as RectTransform;
            if (_rect != null)
            {
                _rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        public void Play()
        {
            if (_pulseRoutine != null)
            {
                return;
            }

            ResetScale();
            _pulseRoutine = StartCoroutine(PulseLoop());
        }

        public void Stop()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            ResetScale();
        }

        private void ResetScale()
        {
            if (_rect != null)
            {
                _rect.localScale = BaseScale;
            }
        }

        private IEnumerator PulseLoop()
        {
            var expandedScale = BaseScale * (1f + _scaleDelta);
            while (true)
            {
                yield return AnimateScale(BaseScale, expandedScale, _expandSeconds);
                yield return AnimateScale(expandedScale, BaseScale, _shrinkSeconds);
            }
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            var dur = Mathf.Max(0.01f, duration);
            var elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / dur);
                if (_rect != null)
                {
                    _rect.localScale = Vector3.Lerp(from, to, t);
                }

                yield return null;
            }

            if (_rect != null)
            {
                _rect.localScale = to;
            }
        }
    }
}
