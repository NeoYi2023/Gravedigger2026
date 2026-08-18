using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Off-screen Camera+RT bays for UI-016 soldier-card reveal (SPEC_03 §3.15 / Approach B).
    /// Step2 pulse peak may early-show / refresh VisualStyle before final Taunt.
    /// </summary>
    public sealed class AutoMfgSoldierPreviewFarm : MonoBehaviour
    {
        private const float BaySpacing = 8f;
        private const float BaseOrthoSize = 1.35f;
        private const int RtSize = 256;
        private const float TauntTimeoutSeconds = 2.8f;
        private const int SpriteSortingOrder = 200;

        private readonly List<Bay> _bays = new List<Bay>();
        private readonly Dictionary<string, Bay> _bayByWarriorId =
            new Dictionary<string, Bay>(StringComparer.Ordinal);

        public static AutoMfgSoldierPreviewFarm Ensure(Transform worldParent)
        {
            var go = new GameObject("AmSoldierPreviewWorld");
            if (worldParent != null)
            {
                go.transform.SetParent(worldParent, false);
            }

            go.transform.position = new Vector3(520f, 8f, 0f);
            return go.AddComponent<AutoMfgSoldierPreviewFarm>();
        }

        public static bool HasBakedVisual(WarriorInstance warrior)
        {
            if (warrior == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(warrior.VisualStyleId)
                || Mathf.Abs(WarriorVisualModelScale.Resolve(warrior) - 1f) > 0.0001f;
        }

        public bool HasActivePreview(string warriorId)
        {
            return !string.IsNullOrEmpty(warriorId)
                && _bayByWarriorId.TryGetValue(warriorId, out var bay)
                && bay != null
                && bay.Instance != null;
        }

        public bool TryReveal(
            AutoMfgSoldierCardView card,
            GameObject appearancePrefab,
            WarriorInstance warrior,
            WarriorVisualStyleCatalog styleCatalog)
        {
            if (card == null || appearancePrefab == null || warrior == null)
            {
                return false;
            }

            if (!TryEnsurePreview(card, appearancePrefab, warrior, styleCatalog))
            {
                return false;
            }

            return TryPlayTaunt(warrior.Id);
        }

        /// <summary>
        /// Create or reuse a bay for <paramref name="card"/>; show RT with Idle (no Taunt).
        /// </summary>
        public bool TryEnsurePreview(
            AutoMfgSoldierCardView card,
            GameObject appearancePrefab,
            WarriorInstance warrior,
            WarriorVisualStyleCatalog styleCatalog)
        {
            if (card == null || appearancePrefab == null || warrior == null)
            {
                return false;
            }

            var warriorId = warrior.Id ?? string.Empty;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            Bay bay;
            if (_bayByWarriorId.TryGetValue(warriorId, out bay) && bay != null && bay.Instance != null)
            {
                ApplyVisualToInstance(bay, warrior, styleCatalog);
                card.ShowLivePreview(bay.Rt);
                return true;
            }

            bay = CreateBay(_bays.Count);
            bay.WarriorId = warriorId;
            bay.AppearanceId = warrior.AppearanceId ?? string.Empty;

            var instance = SpawnPreviewInstance(appearancePrefab, bay, warrior);
            if (instance == null)
            {
                DestroyBay(bay);
                return false;
            }

            bay.Instance = instance;
            ApplyVisualToInstance(bay, warrior, styleCatalog);
            card.ShowLivePreview(bay.Rt);
            _bays.Add(bay);
            _bayByWarriorId[warriorId] = bay;
            return true;
        }

        /// <summary>
        /// Refresh AllIn1 / scale on an existing bay, or create one when VisualStyle is baked.
        /// </summary>
        public bool RefreshVisual(
            AutoMfgSoldierCardView card,
            WarriorInstance warrior,
            DefendPrefabCatalog defendCatalog,
            WarriorVisualStyleCatalog styleCatalog)
        {
            if (card == null || warrior == null || !HasBakedVisual(warrior))
            {
                return false;
            }

            if (defendCatalog == null
                || !defendCatalog.TryGetWarriorAppearance(warrior.AppearanceId, out var prefab)
                || prefab == null)
            {
                return false;
            }

            var warriorId = warrior.Id ?? string.Empty;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            if (_bayByWarriorId.TryGetValue(warriorId, out var bay)
                && bay != null
                && bay.Instance != null)
            {
                var appearanceId = warrior.AppearanceId ?? string.Empty;
                if (!string.Equals(bay.AppearanceId, appearanceId, StringComparison.Ordinal))
                {
                    DestroyInstance(bay);
                    bay.AppearanceId = appearanceId;
                    bay.Instance = SpawnPreviewInstance(prefab, bay, warrior);
                    if (bay.Instance == null)
                    {
                        RemoveBay(bay);
                        return false;
                    }
                }

                ApplyVisualToInstance(bay, warrior, styleCatalog);
                card.ShowLivePreview(bay.Rt);
                return true;
            }

            return TryEnsurePreview(card, prefab, warrior, styleCatalog);
        }

        public bool TryPlayTaunt(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId)
                || !_bayByWarriorId.TryGetValue(warriorId, out var bay)
                || bay == null
                || bay.Instance == null)
            {
                return false;
            }

            if (bay.TauntPlayed)
            {
                return true;
            }

            var anim = bay.Instance.GetComponent<WarriorAnimView>();
            if (anim == null)
            {
                anim = bay.Instance.GetComponentInChildren<WarriorAnimView>();
            }

            if (anim == null)
            {
                return false;
            }

            bay.TauntPlayed = true;
            if (bay.Routine != null)
            {
                StopCoroutine(bay.Routine);
            }

            bay.Routine = StartCoroutine(CoTauntThenIdle(anim));
            return true;
        }

        public void ClearAll()
        {
            for (var i = 0; i < _bays.Count; i++)
            {
                DestroyBay(_bays[i]);
            }

            _bays.Clear();
            _bayByWarriorId.Clear();
        }

        private void OnDestroy()
        {
            ClearAll();
        }

        private GameObject SpawnPreviewInstance(GameObject appearancePrefab, Bay bay, WarriorInstance warrior)
        {
            var instance = Instantiate(appearancePrefab, bay.Anchor);
            instance.name = "CardPreview_" + (warrior != null ? warrior.Id : appearancePrefab.name);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            DisableWorldGameplay(instance);
            PrepareVisual(instance);

            var anim = instance.GetComponent<WarriorAnimView>();
            if (anim == null)
            {
                anim = instance.GetComponentInChildren<WarriorAnimView>();
            }

            if (anim == null)
            {
                anim = instance.AddComponent<WarriorAnimView>();
            }

            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Destroy(instance);
                return null;
            }

            anim.ResetToIdle();
            return instance;
        }

        private static void ApplyVisualToInstance(
            Bay bay,
            WarriorInstance warrior,
            WarriorVisualStyleCatalog styleCatalog)
        {
            if (bay == null || bay.Instance == null || warrior == null)
            {
                return;
            }

            WarriorAllIn1StyleView.ApplyTo(bay.Instance, styleCatalog, warrior);
            var scale = WarriorVisualModelScale.Resolve(warrior);
            if (bay.Camera != null)
            {
                bay.Camera.orthographicSize = BaseOrthoSize * (scale > 0.01f ? scale : 1f);
            }
        }

        private Bay CreateBay(int index)
        {
            var bayGo = new GameObject("Bay_" + index);
            bayGo.transform.SetParent(transform, false);
            bayGo.transform.localPosition = new Vector3(index * BaySpacing, 0f, 0f);

            var anchor = new GameObject("Anchor").transform;
            anchor.SetParent(bayGo.transform, false);
            anchor.localPosition = Vector3.zero;

            var camGo = new GameObject("Camera", typeof(Camera));
            camGo.transform.SetParent(bayGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            camGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = BaseOrthoSize;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 12f;
            cam.depth = -40;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            var rt = new RenderTexture(RtSize, RtSize, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                hideFlags = HideFlags.DontSave
            };
            rt.Create();
            cam.targetTexture = rt;

            return new Bay
            {
                Root = bayGo,
                Anchor = anchor,
                Camera = cam,
                Rt = rt
            };
        }

        private void RemoveBay(Bay bay)
        {
            if (bay == null)
            {
                return;
            }

            _bays.Remove(bay);
            if (!string.IsNullOrEmpty(bay.WarriorId))
            {
                _bayByWarriorId.Remove(bay.WarriorId);
            }

            DestroyBay(bay);
        }

        private void DestroyBay(Bay bay)
        {
            if (bay == null)
            {
                return;
            }

            if (bay.Routine != null)
            {
                StopCoroutine(bay.Routine);
                bay.Routine = null;
            }

            DestroyInstance(bay);

            if (bay.Camera != null)
            {
                bay.Camera.targetTexture = null;
            }

            if (bay.Rt != null)
            {
                bay.Rt.Release();
                Destroy(bay.Rt);
                bay.Rt = null;
            }

            if (bay.Root != null)
            {
                Destroy(bay.Root);
            }
        }

        private static void DestroyInstance(Bay bay)
        {
            if (bay == null || bay.Instance == null)
            {
                return;
            }

            Destroy(bay.Instance);
            bay.Instance = null;
        }

        private IEnumerator CoTauntThenIdle(WarriorAnimView anim)
        {
            if (anim == null)
            {
                yield break;
            }

            anim.ResetToIdle();
            yield return null;
            if (anim.HasTauntTrigger)
            {
                anim.PlayTaunt();
                var elapsed = 0f;
                var sawTaunt = false;
                while (elapsed < TauntTimeoutSeconds)
                {
                    if (anim.IsPlayingTaunt())
                    {
                        sawTaunt = true;
                    }
                    else if (sawTaunt)
                    {
                        break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            anim.ResetToIdle();
        }

        private static void PrepareVisual(GameObject go)
        {
            var visual = go.transform.Find("Visual");
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.gameObject.SetActive(true);
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
        }

        private static void DisableWorldGameplay(GameObject go)
        {
            var agents = go.GetComponentsInChildren<NavMeshAgent>(true);
            for (var i = 0; i < agents.Length; i++)
            {
                if (agents[i] != null)
                {
                    agents[i].enabled = false;
                }
            }

            var colliders = go.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        private sealed class Bay
        {
            public string WarriorId;
            public string AppearanceId;
            public GameObject Root;
            public Transform Anchor;
            public Camera Camera;
            public RenderTexture Rt;
            public GameObject Instance;
            public Coroutine Routine;
            public bool TauntPlayed;
        }
    }
}
