using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig circle cursor: screen-space ring (always visible) + world hit on Dig plane.
    /// </summary>
    public sealed class DigCursorView : MonoBehaviour
    {
        [SerializeField] private Transform _ring;
        [SerializeField] private RectTransform _uiRing;
        [SerializeField] private float _planeY;

        private Camera _digCamera;
        private float _radius = 1.2f;
        private Canvas _uiCanvas;

        public Vector3 WorldPosition { get; private set; }
        public bool IsValid { get; private set; }

        public void Configure(Camera digCamera, float radius, float planeY)
        {
            _digCamera = digCamera;
            _radius = Mathf.Max(0.1f, radius);
            _planeY = planeY;
            EnsureUiRing();
            ApplyRingScale();
            SampleFromScreen(Input.mousePosition);
        }

        public void SetUiRing(RectTransform uiRing)
        {
            _uiRing = uiRing;
            if (_uiRing != null)
            {
                _uiCanvas = _uiRing.GetComponentInParent<Canvas>();
            }
        }

        /// <summary>Called by DigStageController before feeding cursor into DigSessionService.</summary>
        public void SampleFromScreen(Vector3 screenPosition)
        {
            if (_digCamera == null || !_digCamera.isActiveAndEnabled)
            {
                IsValid = false;
                SetRingVisible(false);
                return;
            }

            if (!TryGetWorldOnPlane(screenPosition, out var world))
            {
                IsValid = false;
                SetRingVisible(false);
                return;
            }

            WorldPosition = world;
            IsValid = true;
            SetRingVisible(true);

            if (_ring != null)
            {
                _ring.position = world + Vector3.up * 0.05f;
                ApplyRingScale();
            }

            if (_uiRing != null)
            {
                _uiRing.position = screenPosition;
                var size = ScreenRadiusToUiSize();
                _uiRing.sizeDelta = new Vector2(size, size);
            }
        }

        private void LateUpdate()
        {
            // Keep ring following even if controller is paused briefly.
            SampleFromScreen(Input.mousePosition);
        }

        private static bool TryGetWorldOnPlane(Camera cam, float planeY, Vector3 screenPosition, out Vector3 world)
        {
            world = default;
            var ray = cam.ScreenPointToRay(screenPosition);
            if (Mathf.Abs(ray.direction.y) < 1e-5f)
            {
                return false;
            }

            var t = (planeY - ray.origin.y) / ray.direction.y;
            if (t < 0f)
            {
                return false;
            }

            world = ray.origin + ray.direction * t;
            world.y = planeY;
            return true;
        }

        private bool TryGetWorldOnPlane(Vector3 screenPosition, out Vector3 world)
        {
            if (TryGetWorldOnPlane(_digCamera, _planeY, screenPosition, out world))
            {
                return true;
            }

            // Ortho top-down fallback via ScreenToWorldPoint distance.
            var camPos = _digCamera.transform.position;
            var distance = Mathf.Abs(camPos.y - _planeY);
            var sp = screenPosition;
            sp.z = distance;
            world = _digCamera.ScreenToWorldPoint(sp);
            world.y = _planeY;
            return true;
        }

        private void EnsureUiRing()
        {
            if (_uiRing != null)
            {
                if (_uiCanvas == null)
                {
                    _uiCanvas = _uiRing.GetComponentInParent<Canvas>();
                }

                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = transform.root.GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                return;
            }

            _uiCanvas = canvas;
            var go = new GameObject("UiDigCursorRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 0.92f, 0.2f, 0.9f);
            image.raycastTarget = false;
            _uiRing = go.GetComponent<RectTransform>();
            _uiRing.anchorMin = new Vector2(0.5f, 0.5f);
            _uiRing.anchorMax = new Vector2(0.5f, 0.5f);
            _uiRing.pivot = new Vector2(0.5f, 0.5f);
            _uiRing.sizeDelta = new Vector2(96f, 96f);
            _uiRing.SetAsLastSibling();
        }

        private float ScreenRadiusToUiSize()
        {
            if (_digCamera == null)
            {
                return 96f;
            }

            var worldEdge = WorldPosition + _digCamera.transform.right * _radius;
            var a = _digCamera.WorldToScreenPoint(WorldPosition);
            var b = _digCamera.WorldToScreenPoint(worldEdge);
            var px = Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
            return Mathf.Max(64f, px * 2f);
        }

        private void ApplyRingScale()
        {
            if (_ring == null)
            {
                return;
            }

            _ring.localScale = new Vector3(_radius * 2f, 0.05f, _radius * 2f);
        }

        private void SetRingVisible(bool visible)
        {
            if (_ring != null)
            {
                _ring.gameObject.SetActive(visible);
            }

            if (_uiRing != null)
            {
                _uiRing.gameObject.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            IsValid = false;
            SetRingVisible(false);
        }
    }
}
