using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>UI DigReward flyer: screen-space lerp to HUD portrait, then credit callback.</summary>
    public sealed class DigRewardFlyerView : MonoBehaviour
    {
        [SerializeField] private float _flySeconds = 0.45f;
        [SerializeField] private float _iconSize = 36f;

        private RectTransform _rect;
        private Text _labelText;

        public void PlayScreen(
            Canvas canvas,
            RectTransform parentLayer,
            Vector2 startScreen,
            Vector2 endScreen,
            string label,
            Action onArrived)
        {
            if (canvas == null || parentLayer == null)
            {
                onArrived?.Invoke();
                Destroy(gameObject);
                return;
            }

            EnsureUi(parentLayer);
            if (_labelText != null)
            {
                _labelText.text = ShortLabel(label);
            }

            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentLayer, startScreen, uiCamera, out var startLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentLayer, endScreen, uiCamera, out var endLocal))
            {
                onArrived?.Invoke();
                Destroy(gameObject);
                return;
            }

            _rect.anchoredPosition = startLocal;
            transform.SetAsLastSibling();
            StartCoroutine(Fly(startLocal, endLocal, onArrived));
        }

        private void EnsureUi(RectTransform parentLayer)
        {
            _rect = GetComponent<RectTransform>();
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            transform.SetParent(parentLayer, false);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(_iconSize, _iconSize);

            if (_labelText == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(transform, false);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                _labelText = labelGo.GetComponent<Text>();
                _labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _labelText.fontSize = 18;
                _labelText.alignment = TextAnchor.MiddleCenter;
                _labelText.color = new Color(1f, 0.92f, 0.35f, 1f);
                _labelText.raycastTarget = false;
            }

            var bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.14f, 0.18f, 0.88f);
                bg.raycastTarget = false;
            }
        }

        private static string ShortLabel(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return "★";
            }

            var first = raw.Split('|')[0];
            var parts = first.Split('_');
            return parts.Length > 0 ? parts[0] : first;
        }

        private IEnumerator Fly(Vector2 from, Vector2 to, Action onArrived)
        {
            var t = 0f;
            var dur = Mathf.Max(0.05f, _flySeconds);
            while (t < dur)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / dur);
                var ease = u * u * (3f - 2f * u);
                _rect.anchoredPosition = Vector2.Lerp(from, to, ease);
                yield return null;
            }

            onArrived?.Invoke();
            Destroy(gameObject);
        }
    }
}
