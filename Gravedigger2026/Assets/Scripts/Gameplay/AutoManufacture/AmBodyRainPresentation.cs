using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.UI;
using UnityEngine;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// UI-016 StepA/B rain + magic circle + revive, composited as Overlay Images
    /// on a 1920×1080 layer (same layout as Background) so ScreenSpaceOverlay can see it.
    /// </summary>
    public sealed class AmBodyRainPresentation : MonoBehaviour
    {
        public struct BodyDropEntry
        {
            public Sprite Sprite;
            public BodySlot Slot;
        }

        private const float RefHalfHeightPx = 540f;
        private const float OffscreenPadPx = 24f;
        private const float GravityPxPerUnit = 981f;

        private readonly List<AmBodyPartPiece> _activeBodies = new List<AmBodyPartPiece>();
        private readonly List<AmReviveSoldierPiece> _revivedSoldiers = new List<AmReviveSoldierPiece>();

        private RectTransform _layer;
        private AmBodyPartPool _bodyPool;
        private AmMagicCircleView _magicCircle;
        private AutoMfgPresentationConstants _constants;
        private float _gravityPx;
        private bool _simulate;

        public static AmBodyRainPresentation Ensure(Transform parent, AutoMfgPresentationConstants constants)
        {
            var existing = parent != null ? parent.GetComponentInChildren<AmBodyRainPresentation>(true) : null;
            if (existing != null)
            {
                existing.Initialize(parent, constants);
                return existing;
            }

            var go = new GameObject("AmBodyRainLayer", typeof(RectTransform), typeof(AmBodyRainPresentation));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var presentation = go.GetComponent<AmBodyRainPresentation>();
            presentation.SetupLayer(parent);
            presentation.Initialize(parent, constants);
            return presentation;
        }

        public void Initialize(Transform parent, AutoMfgPresentationConstants constants)
        {
            _constants = constants;
            _gravityPx = GravityPxPerUnit * Mathf.Max(0.01f, constants.GravityScale);
            if (_layer == null)
            {
                SetupLayer(parent);
            }

            ApplyLayerLayout();

            if (_bodyPool == null)
            {
                _bodyPool = new AmBodyPartPool(_layer);
            }

            EnsureMagicCircle();
            _simulate = true;
            gameObject.SetActive(true);
        }

        public static List<BodyDropEntry> CollectBatchBodyParts(
            IReadOnlyList<string> batchWarriorIds,
            WarriorPoolService warriorPool,
            ConfigCsvRepository configs)
        {
            var result = new List<BodyDropEntry>();
            if (batchWarriorIds == null || warriorPool == null || configs == null)
            {
                return result;
            }

            for (var i = 0; i < batchWarriorIds.Count; i++)
            {
                var warriorId = batchWarriorIds[i];
                if (string.IsNullOrEmpty(warriorId)
                    || !warriorPool.TryGet(warriorId, out var warrior)
                    || warrior?.SourceItemIds == null)
                {
                    continue;
                }

                for (var s = 0; s < warrior.SourceItemIds.Count; s++)
                {
                    var itemId = warrior.SourceItemIds[s];
                    if (string.IsNullOrEmpty(itemId)
                        || !configs.TryGetBodyPart(itemId, out var row)
                        || row == null)
                    {
                        continue;
                    }

                    var sprite = DigBodyArtLoader.LoadBodyPart(row.ArtAssetId, row.BodySlot);
                    result.Add(new BodyDropEntry
                    {
                        Sprite = sprite,
                        Slot = row.BodySlot
                    });
                }
            }

            return result;
        }

        public IEnumerator CoDropBodies(IReadOnlyList<BodyDropEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning(
                    "[AmBodyRain] No body parts to drop — SourceItemIds empty or BodyPartConfig miss.");
                yield return new WaitForSeconds(1f);
                yield break;
            }

            Debug.Log($"[AmBodyRain] Dropping {entries.Count} body parts on Overlay layer.");
            var remaining = new List<BodyDropEntry>(entries);
            Shuffle(remaining);

            var index = 0;
            while (index < remaining.Count)
            {
                var batchCount = Random.Range(_constants.DropCountMin, _constants.DropCountMax + 1);
                for (var i = 0; i < batchCount && index < remaining.Count; i++, index++)
                {
                    SpawnBodyPiece(remaining[index]);
                }

                if (index < remaining.Count)
                {
                    yield return new WaitForSeconds(_constants.BodyDropIntervalSeconds);
                }
            }

            yield return new WaitForSeconds(0.45f);
        }

        public void SetMagicCircleActive(bool active)
        {
            if (_magicCircle != null)
            {
                _magicCircle.SetActiveFlash(active);
            }
        }

        public IEnumerator CoReviveSoldier(
            string warriorId,
            WarriorPoolService warriorPool,
            DefendPrefabCatalog defendCatalog,
            float gravityScale)
        {
            if (string.IsNullOrEmpty(warriorId)
                || warriorPool == null
                || !warriorPool.TryGet(warriorId, out var warrior)
                || warrior == null)
            {
                yield break;
            }

            Sprite idleSprite = null;
            if (defendCatalog != null
                && defendCatalog.TryGetWarriorAppearance(warrior.AppearanceId, out var appearancePrefab)
                && appearancePrefab != null)
            {
                idleSprite = FormationBattlefieldPreview.SampleIdleSprite(appearancePrefab);
            }

            var piece = RentRevivePiece();
            var index = _revivedSoldiers.Count - 1;
            var pitch = _constants.SoldierMaxEdgePx * _constants.SoldierVisualScale;
            var slot = (index + 1) / 2;
            var sign = (index % 2 == 1) ? -1f : 1f;
            var spawnX = index == 0 ? 0f : sign * slot * pitch;
            var spawn = new Vector2(
                spawnX,
                _constants.PileFloorYPx + _constants.ReviveSpawnOffsetYPx);
            piece.SpawnMystery(
                spawn,
                _constants.SoldierLandYPx,
                _constants.SoldierMaxEdgePx,
                _constants.SoldierVisualScale,
                _constants.SoldierShadowWidthPx,
                _constants.SoldierShadowHeightPx,
                _constants.SoldierShadowAlpha,
                _constants.SoldierShadowOffsetYPx);
            piece.BeginMorph(idleSprite, GravityPxPerUnit * Mathf.Max(0.01f, gravityScale));

            while (!piece.IsLanded)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.12f);
        }

        public void Cleanup()
        {
            _simulate = false;
            SetMagicCircleActive(false);

            for (var i = 0; i < _activeBodies.Count; i++)
            {
                if (_activeBodies[i] != null)
                {
                    Destroy(_activeBodies[i].gameObject);
                }
            }

            _activeBodies.Clear();
            _bodyPool?.Clear();

            for (var i = 0; i < _revivedSoldiers.Count; i++)
            {
                if (_revivedSoldiers[i] != null)
                {
                    _revivedSoldiers[i].Cleanup();
                    Destroy(_revivedSoldiers[i].gameObject);
                }
            }

            _revivedSoldiers.Clear();

            if (_magicCircle != null)
            {
                Destroy(_magicCircle.gameObject);
                _magicCircle = null;
            }
        }

        private void Update()
        {
            if (!_simulate)
            {
                return;
            }

            var dt = Time.deltaTime;
            for (var i = 0; i < _activeBodies.Count; i++)
            {
                var piece = _activeBodies[i];
                if (piece == null || !piece.gameObject.activeSelf)
                {
                    continue;
                }

                piece.TickPhysics(
                    dt,
                    _gravityPx,
                    _constants.Bounciness,
                    _constants.Friction,
                    _constants.PileFloorYPx,
                    _constants.ColliderInset,
                    _constants.BodyAngleMaxDeg,
                    _activeBodies);
            }

            for (var i = 0; i < _revivedSoldiers.Count; i++)
            {
                var soldier = _revivedSoldiers[i];
                if (soldier != null && soldier.gameObject.activeSelf)
                {
                    soldier.TickFall(dt);
                }
            }
        }

        private void SetupLayer(Transform parent)
        {
            _layer = transform as RectTransform;
            if (_layer == null)
            {
                _layer = gameObject.AddComponent<RectTransform>();
            }

            _layer.anchorMin = new Vector2(0.5f, 0.5f);
            _layer.anchorMax = new Vector2(0.5f, 0.5f);
            _layer.pivot = new Vector2(0.5f, 0.5f);
            _layer.anchoredPosition = Vector2.zero;
            _layer.sizeDelta = new Vector2(1920f, 1080f);
            _layer.localScale = Vector3.one;
            _layer.localRotation = Quaternion.identity;

            PlaceAboveDim(parent);
        }

        private void ApplyLayerLayout()
        {
            if (_layer == null)
            {
                return;
            }

            _layer.anchoredPosition = new Vector2(0f, _constants.BodyRainLayerYPx);
        }

        private void PlaceAboveDim(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var dim = parent.Find("Dim");
            if (dim != null)
            {
                transform.SetSiblingIndex(dim.GetSiblingIndex() + 1);
                return;
            }

            var book = parent.Find("BookRow");
            if (book != null)
            {
                transform.SetSiblingIndex(book.GetSiblingIndex());
            }
        }

        private void EnsureMagicCircle()
        {
            if (_layer == null)
            {
                return;
            }

            var pos = new Vector2(0f, _constants.MagicCircleYPx);
            if (_magicCircle != null)
            {
                _magicCircle.Configure(
                    DigBodyArtLoader.LoadMagicCircle(),
                    pos,
                    _constants.MagicCircleFlashHz);
                _magicCircle.SetActiveFlash(false);
                KeepMagicCircleBehindBodies();
                return;
            }

            _magicCircle = AmMagicCircleView.Create(
                _layer,
                DigBodyArtLoader.LoadMagicCircle(),
                pos,
                _constants.MagicCircleFlashHz);
            _magicCircle.SetActiveFlash(false);
            KeepMagicCircleBehindBodies();
        }

        private void KeepMagicCircleBehindBodies()
        {
            if (_magicCircle != null)
            {
                _magicCircle.transform.SetAsFirstSibling();
            }
        }

        private void SpawnBodyPiece(BodyDropEntry entry)
        {
            var piece = _bodyPool.Rent();
            var size = AmBodyPartPiece.FitSize(entry.Sprite, _constants.BodyMaxEdgePx);
            var x = Random.Range(-_constants.SpawnJitterXPx, _constants.SpawnJitterXPx);
            var y = RefHalfHeightPx + size.y * 0.5f + OffscreenPadPx;
            piece.Activate(
                entry.Sprite,
                new Vector2(x, y),
                AmBodyPartPiece.FallbackColor(entry.Slot),
                _constants.SpawnAngleMaxDeg,
                _constants.BodyMaxEdgePx);
            _activeBodies.Add(piece);
            KeepMagicCircleBehindBodies();
        }

        private AmReviveSoldierPiece RentRevivePiece()
        {
            var prototype = AmReviveSoldierPiece.EnsurePrefab();
            var piece = Instantiate(prototype, _layer);
            piece.ConfigureRuntime();
            _revivedSoldiers.Add(piece);
            if (prototype != null && prototype.gameObject.scene.IsValid()
                && prototype.transform.parent == null)
            {
                Destroy(prototype.gameObject);
            }

            return piece;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
