using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Overlay canvas stack while Dig is active: world (camera) &lt; fog &lt; Meta shell &lt; Dig HUD &lt; modals.
    /// Fog canvas itself is owned by <see cref="Gravedigger2026.UI.CameraFogService"/> (not DigStageRoot).
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
            SuppressStageLocalFogCanvas(digStageRoot);

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

        /// <summary>Keep DigStageRoot prefab fog inactive; runtime fog is CameraFogService.</summary>
        public static void SuppressStageLocalFogCanvas(Transform digStageRoot)
        {
            if (digStageRoot == null)
            {
                return;
            }

            var fogCanvasTr = digStageRoot.Find(DigFogCanvasName);
            if (fogCanvasTr != null && fogCanvasTr.gameObject.activeSelf)
            {
                fogCanvasTr.gameObject.SetActive(false);
            }
        }

        private static Canvas FindMetaCanvas()
        {
            var metaCanvasGo = GameObject.Find(MetaCanvasName);
            return metaCanvasGo != null ? metaCanvasGo.GetComponent<Canvas>() : null;
        }
    }
}
