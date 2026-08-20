using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// One-shot lightning sequence (D-078). Each frame's Sprite CustomPivot is anchored
    /// to the strike point (grave center / fallback). Visual RotX 90 matches Dig floor sprites.
    /// </summary>
    public sealed class DigLightningBoltView : MonoBehaviour
    {
        public const int SortingOrder = 220;

        private Sprite[] _frames;
        private float _frameSeconds;
        private float _elapsed;
        private int _index;
        private SpriteRenderer _visualRenderer;
        private Transform _visualTransform;
        private Vector3 _anchorWorld;
        private bool _playing;

        public void Play(Vector3 worldPosition, Sprite[] frames, float frameSeconds)
        {
            _anchorWorld = worldPosition;
            transform.position = worldPosition;
            _frames = frames;
            _frameSeconds = Mathf.Max(0.01f, frameSeconds);
            _elapsed = 0f;
            _index = 0;
            _playing = _frames != null && _frames.Length > 0;
            EnsureVisual();
            ApplyFrame(0);
            if (!_playing)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            var next = Mathf.FloorToInt(_elapsed / _frameSeconds);
            if (next >= _frames.Length)
            {
                _playing = false;
                Destroy(gameObject);
                return;
            }

            if (next != _index)
            {
                _index = next;
                ApplyFrame(_index);
            }
        }

        private void ApplyFrame(int index)
        {
            if (_visualRenderer == null || _frames == null || index < 0 || index >= _frames.Length)
            {
                return;
            }

            var sprite = _frames[index];
            if (sprite == null)
            {
                return;
            }

            _visualRenderer.sprite = sprite;
            // SpriteRenderer places sprite.pivot (CustomPivot) at the Visual transform.
            // Keep Visual at local origin so every frame's CustomPivot sits on the strike point.
            if (_visualTransform != null)
            {
                _visualTransform.localPosition = Vector3.zero;
                _visualTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _visualTransform.localScale = Vector3.one;
            }

            transform.position = _anchorWorld;
        }

        private void EnsureVisual()
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
                _visualRenderer.drawMode = SpriteDrawMode.Simple;
            }
            else
            {
                _visualRenderer = visual.GetComponent<SpriteRenderer>();
                if (_visualRenderer == null)
                {
                    _visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
                    _visualRenderer.sortingOrder = SortingOrder;
                }

                _visualRenderer.drawMode = SpriteDrawMode.Simple;
            }

            _visualTransform = visual;
        }
    }
}
