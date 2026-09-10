using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-034 / D-090: off-screen spawn edge hints — intro red blink then solid hold.
    /// </summary>
    public sealed class OffScreenSpawnHintView : MonoBehaviour
    {
        private const string IconAssetId = "EnemyAttack_1";
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;

        private static readonly Color ColorNormal = Color.white;
        private static readonly Color ColorBlinkRed = Color.red;

        private GameObject _canvasRoot;
        private RectTransform _layer;
        private Sprite _iconSprite;
        private ConfigCsvRepository _configs;
        private Camera _combatCamera;
        private bool _configured;
        private bool _sessionActive;

        private float _introBlinkSeconds = CombatConstantKeys.Safety.OffScreenSpawnHintIntroBlinkSeconds;
        private int _introBlinkCount = Mathf.RoundToInt(CombatConstantKeys.Safety.OffScreenSpawnHintIntroBlinkCount);
        private float _holdSeconds = CombatConstantKeys.Safety.OffScreenSpawnHintHoldSeconds;
        private float _displayScale = CombatConstantKeys.Safety.OffScreenSpawnHintDisplayScale;
        private float _edgeMarginPx = CombatConstantKeys.Safety.OffScreenSpawnHintEdgeMarginPx;
        private float _iconSizePx = CombatConstantKeys.Safety.OffScreenSpawnHintIconSizePx;

        private readonly List<HintInstance> _active = new List<HintInstance>(8);
        private readonly Stack<HintInstance> _pool = new Stack<HintInstance>(8);

        public void Configure(GameObject canvasRoot, RectTransform layer, Sprite iconSprite)
        {
            _canvasRoot = canvasRoot;
            _layer = layer;
            _iconSprite = iconSprite;
            _configured = true;
        }

        public void Bind(ConfigCsvRepository configs, Camera combatCamera)
        {
            _configs = configs;
            _combatCamera = combatCamera;
            ReloadConstants();
        }

        public void SetCombatCamera(Camera combatCamera)
        {
            _combatCamera = combatCamera;
        }

        public void ShowSession()
        {
            _sessionActive = true;
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(true);
            }
        }

        public void HideSession()
        {
            _sessionActive = false;
            ClearAll();
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(false);
            }
        }

        public void ClearAll()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                Recycle(_active[i]);
            }

            _active.Clear();
        }

        /// <summary>
        /// Shows an edge hint when <paramref name="worldBasePos"/> is outside the combat camera viewport.
        /// </summary>
        public bool TryShow(Vector3 worldBasePos)
        {
            if (!_configured || !_sessionActive || _layer == null || _combatCamera == null)
            {
                return false;
            }

            if (!TryComputeEdgeAnchored(worldBasePos, out var anchored))
            {
                return false;
            }

            var hint = Rent();
            hint.Rect.anchoredPosition = anchored;
            hint.Rect.localScale = Vector3.one * _displayScale;
            hint.Elapsed = 0f;
            hint.Root.SetActive(true);
            ApplyTint(hint, ColorNormal);
            _active.Add(hint);
            return true;
        }

        private void Update()
        {
            if (!_sessionActive || _active.Count == 0)
            {
                return;
            }

            var dt = Time.deltaTime;
            var intro = Mathf.Max(0.05f, _introBlinkSeconds);
            var blinkCount = Mathf.Max(1, _introBlinkCount);
            var hold = Mathf.Max(0f, _holdSeconds);
            var totalLife = intro + hold;
            var period = intro / blinkCount;

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var hint = _active[i];
                hint.Elapsed += dt;
                if (hint.Elapsed >= totalLife)
                {
                    _active.RemoveAt(i);
                    Recycle(hint);
                    continue;
                }

                if (hint.Elapsed < intro)
                {
                    // Half period white, half red — two red pulses in 0.4s when count=2.
                    var phase = (hint.Elapsed % period) / period;
                    ApplyTint(hint, phase < 0.5f ? ColorNormal : ColorBlinkRed);
                }
                else
                {
                    ApplyTint(hint, ColorNormal);
                }
            }
        }

        private void ReloadConstants()
        {
            if (_configs == null)
            {
                return;
            }

            _introBlinkSeconds = Mathf.Max(
                0.05f,
                _configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.OffScreenSpawnHintIntroBlinkSeconds,
                    CombatConstantKeys.Safety.OffScreenSpawnHintIntroBlinkSeconds));
            _introBlinkCount = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    _configs.GetCombatConstantOrFallback(
                        CombatConstantKeys.OffScreenSpawnHintIntroBlinkCount,
                        CombatConstantKeys.Safety.OffScreenSpawnHintIntroBlinkCount)));
            _holdSeconds = Mathf.Max(
                0f,
                _configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.OffScreenSpawnHintHoldSeconds,
                    CombatConstantKeys.Safety.OffScreenSpawnHintHoldSeconds));
            _displayScale = Mathf.Max(
                0.1f,
                _configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.OffScreenSpawnHintDisplayScale,
                    CombatConstantKeys.Safety.OffScreenSpawnHintDisplayScale));
            _edgeMarginPx = Mathf.Max(
                0f,
                _configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.OffScreenSpawnHintEdgeMarginPx,
                    CombatConstantKeys.Safety.OffScreenSpawnHintEdgeMarginPx));
            _iconSizePx = Mathf.Max(
                8f,
                _configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.OffScreenSpawnHintIconSizePx,
                    CombatConstantKeys.Safety.OffScreenSpawnHintIconSizePx));
        }

        private bool TryComputeEdgeAnchored(Vector3 worldBasePos, out Vector2 anchored)
        {
            anchored = default;
            var vp = _combatCamera.WorldToViewportPoint(worldBasePos);
            if (vp.z < 0f)
            {
                vp.x = 1f - vp.x;
                vp.y = 1f - vp.y;
            }

            var inside = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            if (inside)
            {
                return false;
            }

            var fromCenter = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (fromCenter.sqrMagnitude < 1e-8f)
            {
                fromCenter = Vector2.right;
            }

            var sx = Mathf.Abs(fromCenter.x) > 1e-6f ? 0.5f / Mathf.Abs(fromCenter.x) : float.PositiveInfinity;
            var sy = Mathf.Abs(fromCenter.y) > 1e-6f ? 0.5f / Mathf.Abs(fromCenter.y) : float.PositiveInfinity;
            var scale = Mathf.Min(sx, sy);
            var edgeVp = new Vector2(0.5f, 0.5f) + fromCenter * scale;

            var marginX = _edgeMarginPx / RefWidth;
            var marginY = _edgeMarginPx / RefHeight;
            edgeVp.x = Mathf.Clamp(edgeVp.x, marginX, 1f - marginX);
            edgeVp.y = Mathf.Clamp(edgeVp.y, marginY, 1f - marginY);

            anchored = new Vector2((edgeVp.x - 0.5f) * RefWidth, (edgeVp.y - 0.5f) * RefHeight);
            return true;
        }

        private HintInstance Rent()
        {
            if (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                pooled.Rect.sizeDelta = new Vector2(_iconSizePx, _iconSizePx);
                return pooled;
            }

            var go = new GameObject("SpawnHintIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(_layer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_iconSizePx, _iconSizePx);

            var image = go.GetComponent<Image>();
            image.sprite = _iconSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = ColorNormal;

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            return new HintInstance
            {
                Root = go,
                Rect = rt,
                Group = group,
                Image = image,
            };
        }

        private void Recycle(HintInstance hint)
        {
            if (hint == null || hint.Root == null)
            {
                return;
            }

            hint.Root.SetActive(false);
            hint.Elapsed = 0f;
            hint.Rect.localScale = Vector3.one;
            ApplyTint(hint, ColorNormal);
            if (hint.Group != null)
            {
                hint.Group.alpha = 1f;
            }

            _pool.Push(hint);
        }

        private static void ApplyTint(HintInstance hint, Color color)
        {
            if (hint.Image == null)
            {
                return;
            }

            color.a = 1f;
            hint.Image.color = color;
        }

        private sealed class HintInstance
        {
            public GameObject Root;
            public RectTransform Rect;
            public CanvasGroup Group;
            public Image Image;
            public float Elapsed;
        }
    }
}
