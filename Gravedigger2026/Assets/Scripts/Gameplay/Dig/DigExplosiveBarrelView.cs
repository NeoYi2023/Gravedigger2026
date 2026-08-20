using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Parabolic barrel throw on XZ with Y arc (D-077). Sprite tip convention matches Defend Visual RotX 90.
    /// </summary>
    public sealed class DigExplosiveBarrelView : MonoBehaviour
    {
        public const float ArcHeight = 1.75f;
        public const int SortingOrder = 210;

        private Vector3 _origin;
        private Vector3 _target;
        private float _duration;
        private float _elapsed;
        private bool _flying = true;
        private SpriteRenderer _visualRenderer;

        public void Launch(Vector3 origin, Vector3 target, float flightSeconds, Sprite sprite)
        {
            _origin = origin;
            _target = target;
            _duration = Mathf.Max(0.01f, flightSeconds);
            _elapsed = 0f;
            _flying = true;
            EnsureVisual(sprite);
            transform.position = origin;
            FaceTravel();
        }

        private void Update()
        {
            if (!_flying)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            var pos = Vector3.Lerp(_origin, _target, t);
            pos.y = Mathf.Lerp(_origin.y, _target.y, t) + Mathf.Sin(t * Mathf.PI) * ArcHeight;
            transform.position = pos;
            FaceTravel();
            if (t >= 1f)
            {
                _flying = false;
                transform.position = _target;
            }
        }

        private void FaceTravel()
        {
            var dir = _target - _origin;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        private void EnsureVisual(Sprite sprite)
        {
            var visual = transform.Find("Visual");
            if (visual == null)
            {
                var go = new GameObject("Visual");
                visual = go.transform;
                visual.SetParent(transform, false);
                visual.localEulerAngles = new Vector3(90f, 0f, 0f);
                _visualRenderer = go.AddComponent<SpriteRenderer>();
                _visualRenderer.sortingOrder = SortingOrder;
            }
            else
            {
                _visualRenderer = visual.GetComponent<SpriteRenderer>();
                if (_visualRenderer == null)
                {
                    _visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
                    _visualRenderer.sortingOrder = SortingOrder;
                }
            }

            if (sprite != null)
            {
                _visualRenderer.sprite = sprite;
            }
        }
    }
}
