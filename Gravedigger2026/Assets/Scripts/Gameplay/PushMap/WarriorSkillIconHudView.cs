using System.Collections.Generic;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// CombatSkillIcon (UI-025 / D-071 Approach A): overhead 35×35 hold 0.6s then +Z rise
    /// 0.3s fade; persist 20×20 at feet. Screen-pixel scale via ortho camera.
    /// Child of the soldier root; world pose is applied in LateUpdate so facing yaw
    /// does not rotate the icons. SPEC_03 §3.12 SkillCast / SPEC_04 §9.22.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WarriorSkillIconHudView : MonoBehaviour
    {
        public const string IconObjectPrefix = "SkillIcon";
        public const string OverlayNamePrefix = IconObjectPrefix;
        public const int OverheadPixelSize = 35;
        public const int PersistPixelSize = 20;
        public const float HoldSeconds = 0.6f;
        public const float RiseSeconds = 0.3f;
        public const float RiseZ = 0.5f;
        public const float GapPixels = 4f;
        public const float HeadOffsetY = 1.2f;
        public const float FootOffsetY = -0.04f;
        public const float FootOffsetZ = -0.38f;
        public const int SortingOrder = 220;

        private const int EmptyFrameSize = 32;

        private static Sprite s_emptyFrame;

        private readonly List<PopupSlot> _popups = new List<PopupSlot>(4);
        private readonly List<PersistSlot> _persists = new List<PersistSlot>(4);

        private Camera _camera;
        private GameObject _iconPrefab;

        public void Bind(Camera combatCamera, GameObject iconPrefab)
        {
            _camera = combatCamera;
            _iconPrefab = iconPrefab;
        }

        public void PlayPopup(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            var slot = SpawnSlot(skillId, OverheadPixelSize, "Popup");
            if (slot == null)
            {
                return;
            }

            _popups.Add(new PopupSlot
            {
                SkillId = skillId,
                Root = slot.transform,
                Renderer = slot.GetComponent<SpriteRenderer>(),
                Elapsed = 0f
            });
        }

        public void SetPersist(string skillId, bool on)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            var existing = IndexOfPersist(skillId);
            if (on)
            {
                if (existing >= 0)
                {
                    return;
                }

                var slot = SpawnSlot(skillId, PersistPixelSize, "Persist");
                if (slot == null)
                {
                    return;
                }

                _persists.Add(new PersistSlot
                {
                    SkillId = skillId,
                    Root = slot.transform,
                    Renderer = slot.GetComponent<SpriteRenderer>()
                });
                return;
            }

            if (existing < 0)
            {
                return;
            }

            DestroySlot(_persists[existing].Root);
            _persists.RemoveAt(existing);
        }

        public void ClearAll()
        {
            for (var i = 0; i < _popups.Count; i++)
            {
                DestroySlot(_popups[i].Root);
            }

            _popups.Clear();
            for (var i = 0; i < _persists.Count; i++)
            {
                DestroySlot(_persists[i].Root);
            }

            _persists.Clear();
        }

        private void Update()
        {
            if (_popups.Count == 0)
            {
                return;
            }

            var dt = Time.deltaTime;
            var lifetime = HoldSeconds + RiseSeconds;
            for (var i = _popups.Count - 1; i >= 0; i--)
            {
                var item = _popups[i];
                item.Elapsed += dt;
                if (item.Renderer != null)
                {
                    var color = item.Renderer.color;
                    if (item.Elapsed <= HoldSeconds)
                    {
                        color.a = 1f;
                    }
                    else
                    {
                        var t = Mathf.Clamp01((item.Elapsed - HoldSeconds) / RiseSeconds);
                        color.a = 1f - t;
                    }

                    item.Renderer.color = color;
                }

                if (item.Elapsed >= lifetime)
                {
                    DestroySlot(item.Root);
                    _popups.RemoveAt(i);
                }
                else
                {
                    _popups[i] = item;
                }
            }
        }

        private void LateUpdate()
        {
            if (_popups.Count == 0 && _persists.Count == 0)
            {
                return;
            }

            var origin = transform.position;
            var right = ResolveScreenRight();
            var face = Quaternion.Euler(90f, 0f, 0f);
            LayoutPopups(origin, right, face);
            LayoutPersists(origin, right, face);
        }

        private void OnDestroy()
        {
            ClearAll();
        }

        private void LayoutPopups(Vector3 origin, Vector3 right, Quaternion face)
        {
            var step = PixelsToWorld(OverheadPixelSize + GapPixels);
            var size = PixelsToWorld(OverheadPixelSize);
            for (var i = 0; i < _popups.Count; i++)
            {
                var item = _popups[i];
                if (item.Root == null)
                {
                    continue;
                }

                var rise = 0f;
                if (item.Elapsed > HoldSeconds)
                {
                    rise = RiseZ * Mathf.Clamp01((item.Elapsed - HoldSeconds) / RiseSeconds);
                }

                item.Root.SetPositionAndRotation(
                    origin + Vector3.up * HeadOffsetY + right * (i * step) + Vector3.forward * rise,
                    face);
                ApplyPixelScale(item.Root, item.Renderer, size);
            }
        }

        private void LayoutPersists(Vector3 origin, Vector3 right, Quaternion face)
        {
            var step = PixelsToWorld(PersistPixelSize + GapPixels);
            var size = PixelsToWorld(PersistPixelSize);
            var foot = origin + Vector3.up * FootOffsetY + Vector3.forward * FootOffsetZ;
            for (var i = 0; i < _persists.Count; i++)
            {
                var item = _persists[i];
                if (item.Root == null)
                {
                    continue;
                }

                item.Root.SetPositionAndRotation(foot + right * (i * step), face);
                ApplyPixelScale(item.Root, item.Renderer, size);
            }
        }

        private GameObject SpawnSlot(string skillId, int pixelSize, string kind)
        {
            GameObject go;
            if (_iconPrefab != null)
            {
                go = Instantiate(_iconPrefab, transform);
            }
            else
            {
                go = new GameObject();
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
            }

            go.name = IconObjectPrefix + "_" + kind + "_" + skillId;
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<SpriteRenderer>();
            }

            var sprite = FormationSoldierHoverTooltipView.LoadSkillIcon(skillId);
            renderer.sprite = sprite != null ? sprite : GetOrCreateEmptyFrame();
            renderer.color = Color.white;
            renderer.sortingOrder = SortingOrder;
            ApplyPixelScale(go.transform, renderer, PixelsToWorld(pixelSize));
            return go;
        }

        private int IndexOfPersist(string skillId)
        {
            for (var i = 0; i < _persists.Count; i++)
            {
                if (string.Equals(_persists[i].SkillId, skillId, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void DestroySlot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Destroy(root.gameObject);
        }

        private Vector3 ResolveScreenRight()
        {
            if (_camera == null)
            {
                return Vector3.right;
            }

            var right = _camera.transform.right;
            right.y = 0f;
            if (right.sqrMagnitude < 0.0001f)
            {
                return Vector3.right;
            }

            return right.normalized;
        }

        private float PixelsToWorld(float pixels)
        {
            if (_camera == null || !_camera.orthographic || Screen.height <= 0)
            {
                return pixels * 0.01f;
            }

            return pixels * (2f * _camera.orthographicSize) / Screen.height;
        }

        private static void ApplyPixelScale(Transform root, SpriteRenderer renderer, float worldSize)
        {
            if (root == null || renderer == null || renderer.sprite == null)
            {
                return;
            }

            var native = renderer.sprite.rect.width / renderer.sprite.pixelsPerUnit;
            if (native < 0.0001f)
            {
                native = 1f;
            }

            var s = worldSize / native;
            root.localScale = new Vector3(s, s, s);
        }

        private static Sprite GetOrCreateEmptyFrame()
        {
            if (s_emptyFrame != null)
            {
                return s_emptyFrame;
            }

            var tex = new Texture2D(EmptyFrameSize, EmptyFrameSize, TextureFormat.RGBA32, false)
            {
                name = "SkillIconEmptyFrame",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var fill = new Color32(40, 40, 40, 220);
            var border = new Color32(230, 230, 230, 255);
            var pixels = new Color32[EmptyFrameSize * EmptyFrameSize];
            for (var y = 0; y < EmptyFrameSize; y++)
            {
                for (var x = 0; x < EmptyFrameSize; x++)
                {
                    var edge = x == 0 || y == 0 || x == EmptyFrameSize - 1 || y == EmptyFrameSize - 1;
                    pixels[y * EmptyFrameSize + x] = edge ? border : fill;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            s_emptyFrame = Sprite.Create(
                tex,
                new Rect(0f, 0f, EmptyFrameSize, EmptyFrameSize),
                new Vector2(0.5f, 0.5f),
                EmptyFrameSize);
            s_emptyFrame.name = "SkillIconEmptyFrameSprite";
            s_emptyFrame.hideFlags = HideFlags.HideAndDontSave;
            return s_emptyFrame;
        }

        private struct PopupSlot
        {
            public string SkillId;
            public Transform Root;
            public SpriteRenderer Renderer;
            public float Elapsed;
        }

        private struct PersistSlot
        {
            public string SkillId;
            public Transform Root;
            public SpriteRenderer Renderer;
        }
    }
}
