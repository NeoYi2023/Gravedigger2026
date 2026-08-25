using Gravedigger2026.Gameplay.Dig;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Global DigFogCanvas owner (SPEC_03 §3.10 / §3.14): show only Dig session + PushMap Combat;
    /// hide while Meta overlays block. Do not parent fog to DigStageRoot / transform.root.
    /// </summary>
    public sealed class CameraFogService : MonoBehaviour
    {
        private const string FogCanvasName = "DigFogCanvas";
        private const string FogOverlayName = "CameraFogOverlay";

        public static CameraFogService Instance { get; private set; }

        /// <summary>Prefer Instance; fall back to scene search when static was cleared unexpectedly.</summary>
        public static CameraFogService Resolve()
        {
            if (Instance != null)
            {
                return Instance;
            }

            return FindObjectOfType<CameraFogService>();
        }

        [SerializeField] private Sprite _fogSprite;
        [SerializeField] private DigPrefabCatalog _digCatalog;

        private Canvas _fogCanvas;
        private DigCameraFogOverlayView _overlayView;
        private bool _digSessionActive;
        private bool _pushMapCombatActive;
        private bool _metaOverlayBlocking;
        private bool _pulseDesired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureFogCanvas();
            ApplyVisibility();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(DigPrefabCatalog digCatalog, Sprite fogSprite = null)
        {
            if (digCatalog != null)
            {
                _digCatalog = digCatalog;
            }

            if (fogSprite != null)
            {
                _fogSprite = fogSprite;
            }

            EnsureFogCanvas();
            ApplyOverlaySprite();
            ApplyVisibility();
        }

        /// <summary>Dig stage session window (incl. DigStageSummary until End).</summary>
        public void SetDigSessionActive(bool active)
        {
            _digSessionActive = active;
            if (!active)
            {
                _pulseDesired = false;
            }

            ApplyVisibility();
        }

        /// <summary>PushMap Combat phase only; not Prepare/Ended.</summary>
        public void SetPushMapCombatActive(bool active)
        {
            _pushMapCombatActive = active;
            if (active)
            {
                _pulseDesired = true;
            }
            else if (!_digSessionActive)
            {
                _pulseDesired = false;
            }

            ApplyVisibility();
        }

        public void SetMetaOverlayBlocking(bool blocking)
        {
            _metaOverlayBlocking = blocking;
            ApplyVisibility();
        }

        public void SetPulseDesired(bool pulse)
        {
            _pulseDesired = pulse;
            ApplyPulse();
        }

        private bool IsEligibleGameplay => _digSessionActive || _pushMapCombatActive;

        private bool ShouldShow => IsEligibleGameplay && !_metaOverlayBlocking;

        private void ApplyVisibility()
        {
            EnsureFogCanvas();
            if (_fogCanvas == null)
            {
                return;
            }

            var show = ShouldShow;
            if (_fogCanvas.gameObject.activeSelf != show)
            {
                _fogCanvas.gameObject.SetActive(show);
            }

            ApplyPulse();
        }

        private void ApplyPulse()
        {
            if (_overlayView == null)
            {
                return;
            }

            if (ShouldShow && _pulseDesired)
            {
                _overlayView.Play();
            }
            else
            {
                _overlayView.Stop();
            }
        }

        private void EnsureFogCanvas()
        {
            if (_fogCanvas != null)
            {
                return;
            }

            DestroyOrphanFogCanvases();

            var existing = transform.Find(FogCanvasName);
            GameObject canvasGo;
            if (existing != null)
            {
                canvasGo = existing.gameObject;
            }
            else
            {
                canvasGo = new GameObject(FogCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
            }

            _fogCanvas = canvasGo.GetComponent<Canvas>();
            _fogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fogCanvas.sortingOrder = DigUiLayering.FogCanvasOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasGo.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            var rt = canvasGo.transform as RectTransform;
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            EnsureOverlay(canvasGo.transform);
            canvasGo.SetActive(false);
        }

        private void EnsureOverlay(Transform fogCanvasTr)
        {
            var overlayTr = fogCanvasTr.Find(FogOverlayName);
            if (overlayTr == null)
            {
                var go = new GameObject(FogOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(fogCanvasTr, false);
                overlayTr = go.transform;
            }

            var overlayRt = overlayTr as RectTransform;
            if (overlayRt != null)
            {
                overlayRt.anchorMin = Vector2.zero;
                overlayRt.anchorMax = Vector2.one;
                overlayRt.offsetMin = Vector2.zero;
                overlayRt.offsetMax = Vector2.zero;
                overlayRt.pivot = new Vector2(0.5f, 0.5f);
                overlayRt.localScale = Vector3.one;
            }

            var image = overlayTr.GetComponent<Image>();
            if (image == null)
            {
                image = overlayTr.gameObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.preserveAspect = false;
            image.color = Color.white;

            _overlayView = overlayTr.GetComponent<DigCameraFogOverlayView>();
            if (_overlayView == null)
            {
                _overlayView = overlayTr.gameObject.AddComponent<DigCameraFogOverlayView>();
            }

            ApplyOverlaySprite();
        }

        private void ApplyOverlaySprite()
        {
            if (_overlayView == null)
            {
                return;
            }

            var image = _overlayView.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var sprite = _fogSprite;
            if (sprite == null && _digCatalog != null)
            {
                sprite = _digCatalog.CameraFogSprite;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        private void DestroyOrphanFogCanvases()
        {
            var canvases = FindObjectsOfType<Canvas>(true);
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null || c.gameObject == null || c.gameObject.name != FogCanvasName)
                {
                    continue;
                }

                // Keep only the fog canvas parented under this Meta host.
                if (c.transform.parent == transform)
                {
                    continue;
                }

                // DigStageRoot / transform.root leftovers: destroy so Hierarchy only shows Meta fog.
                Destroy(c.gameObject);
            }
        }
    }
}
