using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>DamagePopup style (SPEC_03 §3.14): monster red / soldier white; shared font size 12.</summary>
    public enum DamagePopupStyle
    {
        Monster = 0,
        Soldier = 1
    }

    /// <summary>
    /// DamagePopup (PM-12/13, SPEC_04 §9.22): world-space TextMesh `-N` above the hit target;
    /// over 0.5s world position.z rises +0→+0.5 then despawn. Prefab carries this view; the label
    /// child is ensured at runtime (same dynamic-font approach as WarriorTaskDebugLabelView).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamagePopupView : MonoBehaviour
    {
        public const int FontSize = 12;

        private static readonly Color MonsterColor = new Color(1f, 0.25f, 0.2f);
        private static readonly Color SoldierColor = Color.white;

        private const float HeadOffsetY = 1.2f;
        private const float LifetimeSeconds = 0.5f;
        private const float RiseZ = 0.5f;
        private const float CharacterSize = 0.12f;
        private const int SortingOrder = 210;

        private float _startZ;
        private float _elapsed;
        private bool _playing;

        public static DamagePopupView Spawn(
            GameObject prefab,
            Transform parent,
            Vector3 worldPos,
            float damage,
            DamagePopupStyle style)
        {
            if (prefab == null)
            {
                return null;
            }

            var go = Instantiate(prefab, parent);
            go.name = $"DamagePopup_{style}";
            var view = go.GetComponent<DamagePopupView>();
            if (view == null)
            {
                view = go.AddComponent<DamagePopupView>();
            }

            view.Play(worldPos, damage, style);
            return view;
        }

        public void Play(Vector3 worldPos, float damage, DamagePopupStyle style)
        {
            transform.position = worldPos + Vector3.up * HeadOffsetY;
            _startZ = transform.position.z;
            _elapsed = 0f;
            var text = $"-{Mathf.Max(1, Mathf.RoundToInt(damage))}";
            var label = EnsureLabel();
            label.text = text;
            label.color = style == DamagePopupStyle.Soldier ? SoldierColor : MonsterColor;
            label.fontSize = FontSize;
            _playing = true;
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / LifetimeSeconds);
            var p = transform.position;
            p.z = _startZ + RiseZ * t;
            transform.position = p;
            if (t >= 1f)
            {
                _playing = false;
                Destroy(gameObject);
            }
        }

        private TextMesh EnsureLabel()
        {
            var label = GetComponentInChildren<TextMesh>(true);
            if (label != null)
            {
                return label;
            }

            var go = new GameObject("Label");
            var t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = Vector3.zero;
            // Top-down Combat camera looks down -Y; face text upward.
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);

            label = go.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = CharacterSize;
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS", "Arial" },
                32);
            if (font != null)
            {
                label.font = font;
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (font.material != null)
                    {
                        renderer.sharedMaterial = font.material;
                    }

                    renderer.sortingOrder = SortingOrder;
                }
            }

            return label;
        }
    }
}
