using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Overlay canvas stack while Dig is active: world (camera) &lt; fog &lt; Meta shell &lt; Dig HUD &lt; modals.
    /// </summary>
    public static class DigUiLayering
    {
        public const int FogCanvasOrder = 10;
        public const int MetaShellCanvasOrder = 20;
        public const int HudCanvasOrder = 30;

        private const string MetaCanvasName = "MetaCanvas";
        private const string DigFogCanvasName = "DigFogCanvas";
        private const string DigHudCanvasName = "DigHudCanvas";

        private static int _savedMetaCanvasOrder;
        private static bool _metaCanvasOrderSaved;

        public static void ApplyDigSessionStack(Transform digStageRoot)
        {
            EnsureFogCanvas(digStageRoot);

            var metaCanvas = FindMetaCanvas();
            if (metaCanvas != null)
            {
                if (!_metaCanvasOrderSaved)
                {
                    _savedMetaCanvasOrder = metaCanvas.sortingOrder;
                    _metaCanvasOrderSaved = true;
                }

                if (metaCanvas.sortingOrder < MetaShellCanvasOrder)
                {
                    metaCanvas.sortingOrder = MetaShellCanvasOrder;
                }
            }

            var hudCanvas = digStageRoot != null
                ? digStageRoot.Find(DigHudCanvasName)?.GetComponent<Canvas>()
                : null;
            if (hudCanvas != null)
            {
                hudCanvas.sortingOrder = HudCanvasOrder;
            }
        }

        public static void RestoreAfterDigSession()
        {
            var metaCanvas = FindMetaCanvas();
            if (metaCanvas != null && _metaCanvasOrderSaved)
            {
                metaCanvas.sortingOrder = _savedMetaCanvasOrder;
                _metaCanvasOrderSaved = false;
            }
        }

        public static Canvas EnsureFogCanvas(Transform digStageRoot)
        {
            if (digStageRoot == null)
            {
                return null;
            }

            var fogCanvasTr = digStageRoot.Find(DigFogCanvasName);
            if (fogCanvasTr == null)
            {
                var go = new GameObject(DigFogCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(digStageRoot, false);
                fogCanvasTr = go.transform;
            }

            var fogCanvas = fogCanvasTr.GetComponent<Canvas>();
            fogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fogCanvas.sortingOrder = FogCanvasOrder;

            var hudCanvas = digStageRoot.Find(DigHudCanvasName)?.GetComponent<Canvas>();
            var hudScaler = hudCanvas != null ? hudCanvas.GetComponent<CanvasScaler>() : null;
            var fogScaler = fogCanvasTr.GetComponent<CanvasScaler>();
            if (fogScaler == null)
            {
                fogScaler = fogCanvasTr.gameObject.AddComponent<CanvasScaler>();
            }

            fogScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            fogScaler.referenceResolution = hudScaler != null
                ? hudScaler.referenceResolution
                : new Vector2(1920f, 1080f);
            fogScaler.matchWidthOrHeight = hudScaler != null ? hudScaler.matchWidthOrHeight : 0f;

            if (fogCanvasTr.GetComponent<GraphicRaycaster>() == null)
            {
                fogCanvasTr.gameObject.AddComponent<GraphicRaycaster>();
            }

            MigrateFogOverlayToFogCanvas(digStageRoot, fogCanvasTr);

            var hudIndex = hudCanvas != null ? hudCanvas.transform.GetSiblingIndex() : -1;
            if (hudIndex >= 0)
            {
                fogCanvasTr.SetSiblingIndex(hudIndex);
            }

            return fogCanvas;
        }

        private static void MigrateFogOverlayToFogCanvas(Transform digStageRoot, Transform fogCanvasTr)
        {
            var fogOverlay = fogCanvasTr.Find("CameraFogOverlay");
            if (fogOverlay == null)
            {
                fogOverlay = digStageRoot.Find($"{DigHudCanvasName}/CameraFogOverlay");
            }

            if (fogOverlay == null)
            {
                fogOverlay = digStageRoot.GetComponentInChildren<DigCameraFogOverlayView>(true)?.transform;
            }

            if (fogOverlay == null)
            {
                return;
            }

            if (fogOverlay.parent != fogCanvasTr)
            {
                fogOverlay.SetParent(fogCanvasTr, false);
            }

            StretchFullScreen(fogOverlay as RectTransform);
            fogOverlay.SetAsFirstSibling();
        }

        private static Canvas FindMetaCanvas()
        {
            var metaCanvasGo = GameObject.Find(MetaCanvasName);
            return metaCanvasGo != null ? metaCanvasGo.GetComponent<Canvas>() : null;
        }

        private static void StretchFullScreen(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
