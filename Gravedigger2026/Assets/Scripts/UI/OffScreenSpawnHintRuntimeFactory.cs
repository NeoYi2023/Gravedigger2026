using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Runtime OffScreenSpawnHint HUD (UI-034 / D-090 Approach A).
    /// </summary>
    public static class OffScreenSpawnHintRuntimeFactory
    {
        public const int DefaultSortingOrder = 70;
        public const string IconAssetId = "EnemyAttack_1";

        public static OffScreenSpawnHintView Create(Transform parent, int sortingOrder = DefaultSortingOrder)
        {
            var canvasGo = new GameObject(
                "OffScreenSpawnHintCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var rootGo = new GameObject("OffScreenSpawnHintHud", typeof(RectTransform), typeof(OffScreenSpawnHintView));
            rootGo.transform.SetParent(canvasGo.transform, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var layerGo = new GameObject("HintLayer", typeof(RectTransform));
            layerGo.transform.SetParent(rootGo.transform, false);
            var layerRt = layerGo.GetComponent<RectTransform>();
            layerRt.anchorMin = Vector2.zero;
            layerRt.anchorMax = Vector2.one;
            layerRt.offsetMin = Vector2.zero;
            layerRt.offsetMax = Vector2.zero;

            var view = rootGo.GetComponent<OffScreenSpawnHintView>();
            view.Configure(canvasGo, layerRt, Resources.Load<Sprite>($"UI/Icons/{IconAssetId}"));
            canvasGo.SetActive(false);
            return view;
        }
    }
}
