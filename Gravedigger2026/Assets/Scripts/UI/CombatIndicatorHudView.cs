using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// CombatIndicator View (UI-033 / D-089): 0.2s snapshot poll, death linger, single-row truncate.
    /// Binds <see cref="ICombatIndicatorSessionReads"/> (PushMap / SearchExtract shared).
    /// </summary>
    public sealed class CombatIndicatorHudView : MonoBehaviour
    {
        private const float PollIntervalSeconds = 0.2f;
        private const float DeathLingerSeconds = 0.5f;
        private const float SlotSpacing = 4f;
        private const float CenterGap = 12f;

        private static readonly Color TintGreen = new Color(0.25f, 0.9f, 0.35f, 1f);
        private static readonly Color TintOrange = new Color(1f, 0.6f, 0.15f, 1f);
        private static readonly Color TintRed = new Color(0.95f, 0.25f, 0.22f, 1f);
        private static readonly Color TintGray = new Color(0.45f, 0.45f, 0.45f, 1f);

        private GameObject _canvasRoot;
        private Text _allyAliveLabel;
        private Text _enemyAliveLabel;
        private RectTransform _allyHost;
        private RectTransform _enemyHost;
        private RectTransform _allyTemplate;
        private RectTransform _enemyTemplate;
        private Sprite _deathMarkSprite;
        private Vector2 _slotSize;
        private float _centerWidth;
        private float _centerHeight;

        private ICombatIndicatorSessionReads _session;
        private WarriorPoolService _pool;
        private ConfigCsvRepository _configs;
        private readonly CombatIndicatorSnapshotBuilder _builder = new CombatIndicatorSnapshotBuilder();

        private readonly List<SlotView> _allySlots = new List<SlotView>(32);
        private readonly List<SlotView> _enemySlots = new List<SlotView>(64);
        private readonly Stack<SlotView> _allyPool = new Stack<SlotView>(32);
        private readonly Stack<SlotView> _enemyPool = new Stack<SlotView>(64);
        private readonly HashSet<string> _expiredUnitIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _deathExpireAt =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> _silhouetteCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private float _pollAccum;
        private int _lastMembershipFingerprint = int.MinValue;
        private int _lastAppearanceFingerprint = int.MinValue;
        private bool _visible;
        private bool _configured;
        /// <summary>True after EnemyAliveCount has been &gt;0 at least once this battle.</summary>
        private bool _enemyAliveCountRevealed;

        public void Configure(
            GameObject canvasRoot,
            Text allyAliveLabel,
            Text enemyAliveLabel,
            RectTransform allyHost,
            RectTransform enemyHost,
            RectTransform allyTemplate,
            RectTransform enemyTemplate,
            Sprite deathMarkSprite,
            Vector2 slotSize,
            float centerWidth,
            float centerHeight)
        {
            _canvasRoot = canvasRoot;
            _allyAliveLabel = allyAliveLabel;
            _enemyAliveLabel = enemyAliveLabel;
            _allyHost = allyHost;
            _enemyHost = enemyHost;
            _allyTemplate = allyTemplate;
            _enemyTemplate = enemyTemplate;
            _deathMarkSprite = deathMarkSprite;
            _slotSize = slotSize;
            _centerWidth = centerWidth;
            _centerHeight = centerHeight;
            _configured = true;
        }

        public void Bind(
            ICombatIndicatorSessionReads session,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            _session = session;
            _pool = pool;
            _configs = configs;
        }

        public void Show()
        {
            _visible = true;
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(true);
            }

            _pollAccum = PollIntervalSeconds;
            ForceRefresh();
        }

        public void Hide()
        {
            _visible = false;
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(false);
            }
        }

        public void ResetBattleState()
        {
            _expiredUnitIds.Clear();
            _deathExpireAt.Clear();
            _lastMembershipFingerprint = int.MinValue;
            _lastAppearanceFingerprint = int.MinValue;
            _enemyAliveCountRevealed = false;
            RecycleAll(_allySlots, _allyPool);
            RecycleAll(_enemySlots, _enemyPool);
            if (_allyAliveLabel != null)
            {
                _allyAliveLabel.text = "0";
            }

            if (_enemyAliveLabel != null)
            {
                _enemyAliveLabel.text = "?";
            }
        }

        private void Update()
        {
            if (!_visible || !_configured || _session == null)
            {
                return;
            }

            // Expire timers may elapse between polls — keep layout responsive.
            if (_deathExpireAt.Count > 0 && CullExpiredIfNeeded())
            {
                ForceRefresh();
                return;
            }

            _pollAccum += Time.unscaledDeltaTime;
            if (_pollAccum < PollIntervalSeconds)
            {
                return;
            }

            _pollAccum = 0f;
            RefreshFromSession();
        }

        private void ForceRefresh()
        {
            _lastMembershipFingerprint = int.MinValue;
            _lastAppearanceFingerprint = int.MinValue;
            RefreshFromSession();
        }

        private void RefreshFromSession()
        {
            if (_session == null || !_configured)
            {
                return;
            }

            var snap = _builder.Build(
                _session.CopyWarriorIndicatorReads(),
                _session.CopyMonsterIndicatorReads(),
                _pool,
                _configs);

            NoteNewDeaths(snap.Allies);
            NoteNewDeaths(snap.Enemies);
            CullExpiredIfNeeded();

            ApplyAliveCountLabels(snap.AllyAliveCount, snap.EnemyAliveCount);

            var membership = ComputeDisplayedMembershipFingerprint(snap);
            var appearance = snap.AppearanceFingerprint;
            var needRebuild = membership != _lastMembershipFingerprint;
            if (needRebuild)
            {
                RebuildSide(_allySlots, _allyPool, _allyHost, _allyTemplate, snap.Allies, growLeft: true);
                RebuildSide(_enemySlots, _enemyPool, _enemyHost, _enemyTemplate, snap.Enemies, growLeft: false);
                _lastMembershipFingerprint = membership;
            }
            else if (appearance != _lastAppearanceFingerprint)
            {
                ApplyAppearance(_allySlots, snap.Allies);
                ApplyAppearance(_enemySlots, snap.Enemies);
            }

            _lastAppearanceFingerprint = appearance;
        }

        private void ApplyAliveCountLabels(int allyAlive, int enemyAlive)
        {
            if (_allyAliveLabel != null)
            {
                _allyAliveLabel.text = allyAlive.ToString(CultureInfo.InvariantCulture);
            }

            if (_enemyAliveLabel == null)
            {
                return;
            }

            if (enemyAlive > 0)
            {
                _enemyAliveCountRevealed = true;
            }

            _enemyAliveLabel.text = (!_enemyAliveCountRevealed && enemyAlive == 0)
                ? "?"
                : enemyAlive.ToString(CultureInfo.InvariantCulture);
        }

        private void NoteNewDeaths(List<CombatIndicatorSlotData> slots)
        {
            var now = Time.unscaledTime;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.ShowDeathOverlay || string.IsNullOrEmpty(slot.UnitId))
                {
                    continue;
                }

                if (_expiredUnitIds.Contains(slot.UnitId))
                {
                    continue;
                }

                if (!_deathExpireAt.ContainsKey(slot.UnitId))
                {
                    _deathExpireAt[slot.UnitId] = now + DeathLingerSeconds;
                }
            }
        }

        private bool CullExpiredIfNeeded()
        {
            if (_deathExpireAt.Count == 0)
            {
                return false;
            }

            var now = Time.unscaledTime;
            var removed = false;
            // Copy keys to avoid modify-during-enumerate without alloc: iterate list of active slots.
            _expireKeyScratch.Clear();
            foreach (var pair in _deathExpireAt)
            {
                if (pair.Value <= now)
                {
                    _expireKeyScratch.Add(pair.Key);
                }
            }

            for (var i = 0; i < _expireKeyScratch.Count; i++)
            {
                var id = _expireKeyScratch[i];
                _deathExpireAt.Remove(id);
                _expiredUnitIds.Add(id);
                removed = true;
            }

            return removed;
        }

        private readonly List<string> _expireKeyScratch = new List<string>(16);
        private readonly List<CombatIndicatorSlotData> _displayScratch = new List<CombatIndicatorSlotData>(64);

        private int ComputeDisplayedMembershipFingerprint(CombatIndicatorSnapshot snap)
        {
            unchecked
            {
                var hash = 17;
                hash = AccumulateDisplayed(snap.Allies, hash);
                hash = AccumulateDisplayed(snap.Enemies, hash);
                return hash;
            }
        }

        private int AccumulateDisplayed(List<CombatIndicatorSlotData> slots, int hash)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (IsHiddenByExpire(s.UnitId))
                {
                    continue;
                }

                hash = hash * 31 + (s.UnitId != null ? s.UnitId.GetHashCode() : 0);
                hash = hash * 31 + (s.ShowDeathOverlay ? 1 : 0);
            }

            return hash;
        }

        private bool IsHiddenByExpire(string unitId)
        {
            return !string.IsNullOrEmpty(unitId) && _expiredUnitIds.Contains(unitId);
        }

        private void RebuildSide(
            List<SlotView> active,
            Stack<SlotView> pool,
            RectTransform host,
            RectTransform template,
            List<CombatIndicatorSlotData> source,
            bool growLeft)
        {
            RecycleAll(active, pool);
            CollectDisplayed(source, _displayScratch);
            // Single-row only: drop overflow beyond perRow (rows 2…N never shown).
            var perRow = ComputePerRow();
            var count = Math.Min(_displayScratch.Count, perRow);
            for (var i = 0; i < count; i++)
            {
                var data = _displayScratch[i];
                var slot = Rent(pool, host, template, growLeft);
                ApplySlotVisual(slot, data);
                active.Add(slot);
            }

            LayoutSide(active, growLeft);
        }

        private void CollectDisplayed(List<CombatIndicatorSlotData> source, List<CombatIndicatorSlotData> dest)
        {
            dest.Clear();
            for (var i = 0; i < source.Count; i++)
            {
                var s = source[i];
                if (IsHiddenByExpire(s.UnitId))
                {
                    continue;
                }

                dest.Add(s);
            }
        }

        private void ApplyAppearance(List<SlotView> active, List<CombatIndicatorSlotData> source)
        {
            CollectDisplayed(source, _displayScratch);
            var perRow = ComputePerRow();
            var n = Math.Min(active.Count, Math.Min(_displayScratch.Count, perRow));
            for (var i = 0; i < n; i++)
            {
                ApplySlotVisual(active[i], _displayScratch[i]);
            }
        }

        private int ComputePerRow()
        {
            var available = Mathf.Max(80f, (1920f - _centerWidth) * 0.5f - CenterGap);
            var cell = _slotSize.x + SlotSpacing;
            return Mathf.Max(1, Mathf.FloorToInt((available + SlotSpacing) / cell));
        }

        private void LayoutSide(List<SlotView> slots, bool growLeft)
        {
            var cell = _slotSize.x + SlotSpacing;
            var originX = growLeft
                ? -(_centerWidth * 0.5f + CenterGap)
                : (_centerWidth * 0.5f + CenterGap);
            // Align single row with vertical center of HPPK_UI_1 (pivot top).
            var originY = -(_centerHeight * 0.5f - _slotSize.y * 0.5f);

            for (var i = 0; i < slots.Count; i++)
            {
                var x = growLeft
                    ? originX - i * cell - _slotSize.x * 0.5f
                    : originX + i * cell + _slotSize.x * 0.5f;
                var rt = slots[i].Root;
                rt.anchoredPosition = new Vector2(x, originY);
            }
        }

        private SlotView Rent(
            Stack<SlotView> pool,
            RectTransform host,
            RectTransform template,
            bool growLeft)
        {
            SlotView slot;
            if (pool.Count > 0)
            {
                slot = pool.Pop();
                slot.Root.SetParent(host, false);
                slot.Root.gameObject.SetActive(true);
            }
            else
            {
                var go = Instantiate(template.gameObject, host, false);
                go.name = growLeft ? "AllySlot" : "EnemySlot";
                go.SetActive(true);
                var root = go.GetComponent<RectTransform>();
                root.anchorMin = new Vector2(0.5f, 1f);
                root.anchorMax = new Vector2(0.5f, 1f);
                root.pivot = new Vector2(0.5f, 1f);
                root.sizeDelta = _slotSize;
                var silhouette = root.Find("Silhouette")?.GetComponent<Image>();
                var death = root.Find("DeathMark")?.GetComponent<Image>();
                if (death != null)
                {
                    death.sprite = _deathMarkSprite;
                }

                slot = new SlotView(root, silhouette, death);
            }

            return slot;
        }

        private static void RecycleAll(List<SlotView> active, Stack<SlotView> pool)
        {
            for (var i = 0; i < active.Count; i++)
            {
                var slot = active[i];
                slot.Root.gameObject.SetActive(false);
                pool.Push(slot);
            }

            active.Clear();
        }

        private void ApplySlotVisual(SlotView slot, CombatIndicatorSlotData data)
        {
            slot.UnitId = data.UnitId;
            if (slot.Silhouette != null)
            {
                slot.Silhouette.sprite = LoadSilhouette(data.SilhouetteIconAssetId);
                slot.Silhouette.enabled = slot.Silhouette.sprite != null;
                slot.Silhouette.color = ResolveTint(data.HpRatio, data.ShowDeathOverlay);
            }

            if (slot.DeathMark != null)
            {
                var show = data.ShowDeathOverlay;
                slot.DeathMark.gameObject.SetActive(show);
                slot.DeathMark.enabled = show;
                if (show && slot.DeathMark.sprite == null)
                {
                    slot.DeathMark.sprite = _deathMarkSprite;
                }
            }
        }

        private static Color ResolveTint(float hpRatio, bool showDeathOverlay)
        {
            switch (CombatIndicatorSnapshotBuilder.HpTintBucket(hpRatio, showDeathOverlay))
            {
                case 3:
                    return TintGreen;
                case 2:
                    return TintOrange;
                case 1:
                    return TintRed;
                default:
                    return TintGray;
            }
        }

        private Sprite LoadSilhouette(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return null;
            }

            if (_silhouetteCache.TryGetValue(assetId, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>($"UI/Icons/{assetId}");
            _silhouetteCache[assetId] = sprite;
            return sprite;
        }

        private void OnDestroy()
        {
            if (_canvasRoot != null && _canvasRoot != gameObject)
            {
                Destroy(_canvasRoot);
            }
        }

        private sealed class SlotView
        {
            public readonly RectTransform Root;
            public readonly Image Silhouette;
            public readonly Image DeathMark;
            public string UnitId;

            public SlotView(RectTransform root, Image silhouette, Image deathMark)
            {
                Root = root;
                Silhouette = silhouette;
                DeathMark = deathMark;
            }
        }
    }
}
