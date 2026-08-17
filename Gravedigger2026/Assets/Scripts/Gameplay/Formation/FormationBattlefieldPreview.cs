using System;
using System.Collections.Generic;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Idle stand-in previews for deployed warriors on the formation map.
    /// </summary>
    public sealed class FormationBattlefieldPreview : MonoBehaviour
    {
        private const string IdleStateName = "IdleBT";
        private const string DirIndexParam = "DirIndex";
        private const int FixedDirIndex = 2;
        /// <summary>SPEC_04 §15.2 — above GroundTilemap (order 0).</summary>
        private const int SpriteSortingOrder = 200;

        private static readonly string[] LocomotionBools =
        {
            "IsRun", "IsWalk", "IsStrafeLeft", "IsStrafeRight", "IsRunBackwards",
            "IsCrouching", "IsMounted", "UseIdle2", "UseIdle3", "UseIdle4"
        };

        private readonly Dictionary<string, GameObject> _previews = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, Collider> _colliders = new Dictionary<string, Collider>(StringComparer.Ordinal);

        private DefendPrefabCatalog _catalog;
        private Transform _parent;
        private Vector3 _mapCenter;

        public void Configure(DefendPrefabCatalog catalog, Transform parent, Vector3 mapCenter)
        {
            _catalog = catalog;
            _parent = parent;
            _mapCenter = mapCenter;
        }

        public void Sync(BattleFormationService formation, WarriorPoolService pool)
        {
            if (formation == null)
            {
                Clear();
                return;
            }

            var keep = new HashSet<string>(StringComparer.Ordinal);
            var entries = formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                keep.Add(e.WarriorId);
                if (!_previews.TryGetValue(e.WarriorId, out var go) || go == null)
                {
                    go = SpawnPreview(e.WarriorId, pool);
                    if (go == null)
                    {
                        continue;
                    }

                    _previews[e.WarriorId] = go;
                }

                go.SetActive(true);
                go.transform.position = new Vector3(
                    _mapCenter.x + e.PositionX,
                    _mapCenter.y,
                    _mapCenter.z + e.PositionZ);
                PreparePreviewVisual(go);
            }

            var remove = new List<string>();
            foreach (var kv in _previews)
            {
                if (!keep.Contains(kv.Key))
                {
                    remove.Add(kv.Key);
                }
            }

            for (var i = 0; i < remove.Count; i++)
            {
                DestroyPreview(remove[i]);
            }
        }

        public bool TryPickWarrior(Ray ray, out string warriorId)
        {
            warriorId = null;
            if (!Physics.Raycast(ray, out var hit, 500f))
            {
                return false;
            }

            foreach (var kv in _colliders)
            {
                if (kv.Value != null && kv.Value == hit.collider)
                {
                    warriorId = kv.Key;
                    return true;
                }
            }

            var marker = hit.collider != null ? hit.collider.GetComponentInParent<FormationPreviewMarker>() : null;
            if (marker != null && !string.IsNullOrEmpty(marker.WarriorId))
            {
                warriorId = marker.WarriorId;
                return true;
            }

            return false;
        }

        public void SetPreviewVisible(string warriorId, bool visible)
        {
            if (_previews.TryGetValue(warriorId, out var go) && go != null)
            {
                go.SetActive(visible);
            }
        }

        public void Clear()
        {
            foreach (var kv in _previews)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }

            _previews.Clear();
            _colliders.Clear();
        }

        /// <summary>
        /// Force Idle facing for formation / drag previews (sprites are Animator-driven; prefab has no default sprite).
        /// </summary>
        public static void PreparePreviewVisual(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            DisableCombatComponents(go);

            var animators = go.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null)
                {
                    continue;
                }

                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                ClearLocomotionBools(animator);
                ApplyFixedFacing(animator);
                animator.Play(IdleStateName, 0, 0f);
                animator.Update(0f);
            }

            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null)
                {
                    continue;
                }

                sr.enabled = true;
                sr.gameObject.SetActive(true);
                sr.sortingOrder = SpriteSortingOrder;
                if (sr.color.a < 0.01f)
                {
                    var c = sr.color;
                    c.a = 1f;
                    sr.color = c;
                }
            }

            // Keep Visual child facing top-down camera (SPEC_04 §15.2).
            var visual = go.transform.Find("Visual");
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.gameObject.SetActive(true);
            }
        }

        /// <summary>Sample Idle sprite for soldier-bar thumbnail (prefab SpriteRenderer starts empty).</summary>
        public static Sprite SampleIdleSprite(GameObject appearancePrefab)
        {
            if (appearancePrefab == null)
            {
                return null;
            }

            var temp = Instantiate(appearancePrefab);
            temp.name = "FormationThumbSample";
            temp.hideFlags = HideFlags.HideAndDontSave;
            temp.transform.position = new Vector3(0f, -9999f, 0f);
            try
            {
                PreparePreviewVisual(temp);
                var srs = temp.GetComponentsInChildren<SpriteRenderer>(true);
                for (var i = 0; i < srs.Length; i++)
                {
                    if (srs[i] != null && srs[i].sprite != null)
                    {
                        return srs[i].sprite;
                    }
                }
            }
            finally
            {
                if (Application.isPlaying)
                {
                    Destroy(temp);
                }
                else
                {
                    DestroyImmediate(temp);
                }
            }

            return null;
        }

        private GameObject SpawnPreview(string warriorId, WarriorPoolService pool)
        {
            if (_catalog == null || pool == null || _parent == null)
            {
                return null;
            }

            WarriorInstance warrior = null;
            var warriors = pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                if (string.Equals(warriors[i].Id, warriorId, StringComparison.Ordinal))
                {
                    warrior = warriors[i];
                    break;
                }
            }

            var appearanceId = warrior != null ? warrior.AppearanceId : null;
            if (string.IsNullOrEmpty(appearanceId) || !_catalog.TryGetWarriorAppearance(appearanceId, out var prefab))
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = $"FormationPreview_{warriorId}";
                fallback.transform.SetParent(_parent, false);
                fallback.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                AttachMarker(fallback, warriorId);
                return fallback;
            }

            var go = Instantiate(prefab, _parent);
            go.name = $"FormationPreview_{warriorId}";
            WarriorAllIn1StyleView.ApplyTo(go, _catalog != null ? _catalog.VisualStyleCatalog : null, warrior);
            PreparePreviewVisual(go);
            AttachMarker(go, warriorId);
            return go;
        }

        private void AttachMarker(GameObject go, string warriorId)
        {
            var marker = go.GetComponent<FormationPreviewMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<FormationPreviewMarker>();
            }

            marker.WarriorId = warriorId;

            var col = go.GetComponentInChildren<Collider>();
            if (col == null)
            {
                col = go.AddComponent<CapsuleCollider>();
                var capsule = col as CapsuleCollider;
                if (capsule != null)
                {
                    capsule.height = 1.6f;
                    capsule.radius = 0.45f;
                    capsule.center = new Vector3(0f, 0.1f, 0f);
                }
            }

            col.enabled = true;
            _colliders[warriorId] = col;
        }

        private static void DisableCombatComponents(GameObject go)
        {
            var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null || b is FormationPreviewMarker)
                {
                    continue;
                }

                var typeName = b.GetType().Name;
                if (typeName.IndexOf("Agent", StringComparison.Ordinal) >= 0
                    || typeName.IndexOf("NavMesh", StringComparison.Ordinal) >= 0
                    || typeName.IndexOf("WarriorAgent", StringComparison.Ordinal) >= 0)
                {
                    b.enabled = false;
                }
            }

            var agents = go.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
            for (var i = 0; i < agents.Length; i++)
            {
                agents[i].enabled = false;
            }
        }

        private static void ApplyFixedFacing(Animator animator, bool facingYawFlip = false)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            var dirIndexHash = Animator.StringToHash(DirIndexParam);
            var directionHash = Animator.StringToHash("Direction");
            var written = WarriorAnimView.ApplyFacingYawFlip(FixedDirIndex, facingYawFlip);

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == dirIndexHash && p.type == AnimatorControllerParameterType.Int)
                {
                    animator.SetInteger(dirIndexHash, written);
                }
                else if (p.nameHash == directionHash && p.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(directionHash, written);
                }
            }
        }

        private static void ClearLocomotionBools(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            for (var i = 0; i < LocomotionBools.Length; i++)
            {
                var name = LocomotionBools[i];
                foreach (var p in animator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
                    {
                        animator.SetBool(name, false);
                        break;
                    }
                }
            }
        }

        private void DestroyPreview(string warriorId)
        {
            if (_previews.TryGetValue(warriorId, out var go) && go != null)
            {
                Destroy(go);
            }

            _previews.Remove(warriorId);
            _colliders.Remove(warriorId);
        }
    }

    /// <summary>Marker on formation preview roots for ray picking.</summary>
    public sealed class FormationPreviewMarker : MonoBehaviour
    {
        public string WarriorId;
    }
}
