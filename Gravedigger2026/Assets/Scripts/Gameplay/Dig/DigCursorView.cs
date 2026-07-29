using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig circle cursor: screen-space Prefab ring (UiDigCursorRing) + optional world ring.
    /// Hit radius is circular (DigSessionService); UI diameter tracks DigCursorRadius projection.
    /// </summary>
    public sealed class DigCursorView : MonoBehaviour
    {
        [SerializeField] private Transform _ring;
        [SerializeField] private DigCursorRingView _uiRingPrefab;
        [SerializeField] private DigCursorRingView _uiRing;
        [SerializeField] private float _planeY;
        [SerializeField] private bool _showWorldRing;

        private Camera _digCamera;
        private float _radius = 1.2f;
        private Canvas _uiCanvas;
        private DigCursorRingView _spawnedUiRing;

        public Vector3 WorldPosition { get; private set; }
        public bool IsValid { get; private set; }

        public void SetUiRingPrefab(DigCursorRingView prefab)
        {
            if (prefab != null)
            {
                _uiRingPrefab = prefab;
            }
        }

        public void Configure(Camera digCamera, float radius, float planeY, Canvas hudCanvas = null)
        {
            _digCamera = digCamera;
            _radius = Mathf.Max(0.1f, radius);
            _planeY = planeY;
            if (hudCanvas != null)
            {
                _uiCanvas = hudCanvas;
            }

            EnsureUiRing();
            ApplyRingScale();
            SampleFromScreen(Input.mousePosition);
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

            if (_showWorldRing && _ring != null)
            {
                _ring.position = world + Vector3.up * 0.05f;
                ApplyRingScale();
            }

            var activeUi = ActiveUiRing;
            if (activeUi != null)
            {
                activeUi.Root.position = screenPosition;
                activeUi.ApplyDiameter(ScreenRadiusToUiSize());
            }
        }

        public void DestroySpawnedUiRing()
        {
            if (_spawnedUiRing != null)
            {
                Destroy(_spawnedUiRing.gameObject);
                _spawnedUiRing = null;
            }
        }

        private DigCursorRingView ActiveUiRing => _spawnedUiRing != null ? _spawnedUiRing : _uiRing;

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
            if (ActiveUiRing != null)
            {
                if (_uiCanvas == null)
                {
                    _uiCanvas = ActiveUiRing.GetComponentInParent<Canvas>();
                }

                return;
            }

            if (_uiRingPrefab == null)
            {
                return;
            }

            var canvas = _uiCanvas;
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                canvas = transform.root.GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                return;
            }

            _uiCanvas = canvas;
            _spawnedUiRing = Instantiate(_uiRingPrefab, canvas.transform, false);
            _spawnedUiRing.name = "UiDigCursorRing";
            var root = _spawnedUiRing.Root;
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.SetAsLastSibling();
            _spawnedUiRing.ApplyDiameter(96f);
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
            if (!_showWorldRing || _ring == null)
            {
                return;
            }

            _ring.localScale = new Vector3(_radius * 2f, 0.05f, _radius * 2f);
        }

        private void SetRingVisible(bool visible)
        {
            if (_showWorldRing && _ring != null)
            {
                _ring.gameObject.SetActive(visible);
            }
            else if (_ring != null)
            {
                _ring.gameObject.SetActive(false);
            }

            var activeUi = ActiveUiRing;
            if (activeUi != null)
            {
                activeUi.gameObject.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            IsValid = false;
            SetRingVisible(false);
        }
    }
}
