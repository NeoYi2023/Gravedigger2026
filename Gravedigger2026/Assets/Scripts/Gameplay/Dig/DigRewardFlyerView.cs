using System;
using System.Collections;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>Simple DigReward flyer: lerp to HUD portrait world target then invoke credit callback.</summary>
    public sealed class DigRewardFlyerView : MonoBehaviour
    {
        [SerializeField] private float _flySeconds = 0.45f;

        public void Play(Vector3 from, Vector3 to, string label, Action onArrived)
        {
            gameObject.name = string.IsNullOrEmpty(label) ? "DigReward" : $"DigReward_{label}";
            transform.position = from;
            StartCoroutine(Fly(from, to, onArrived));
        }

        private IEnumerator Fly(Vector3 from, Vector3 to, Action onArrived)
        {
            var t = 0f;
            var dur = Mathf.Max(0.05f, _flySeconds);
            while (t < dur)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / dur);
                var ease = u * u * (3f - 2f * u);
                transform.position = Vector3.Lerp(from, to, ease);
                yield return null;
            }

            onArrived?.Invoke();
            Destroy(gameObject);
        }
    }
}
