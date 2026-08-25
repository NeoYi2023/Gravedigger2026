using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Audio;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gravedigger2026.Core.Audio
{
    /// <summary>
    /// Weighted-random BGM player (SPEC_03 §3.4 / SPEC_04 §9.29). Same Context is idempotent.
    /// </summary>
    public sealed class BgmService
    {
        private ConfigCsvRepository _configs;
        private BgmClipCatalog _catalog;
        private AudioSource _source;
        private BgmContext _current = BgmContext.None;

        public BgmContext Current => _current;

        public void Bind(ConfigCsvRepository configs, BgmClipCatalog catalog, AudioSource source)
        {
            _configs = configs;
            _catalog = catalog;
            _source = source;
            if (_source != null)
            {
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }
        }

        public void Play(BgmContext context)
        {
            if (context == BgmContext.None)
            {
                Stop();
                return;
            }

            if (_current == context && _source != null && _source.isPlaying)
            {
                return;
            }

            if (_configs == null || !_configs.IsLoaded)
            {
                Debug.LogWarning("[BgmService] Configs not loaded — cannot play BGM.");
                return;
            }

            if (_catalog == null || _source == null)
            {
                Debug.LogWarning("[BgmService] Catalog or AudioSource missing — cannot play BGM.");
                return;
            }

            var rows = _configs.GetBgmRows(context);
            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning($"[BgmService] No BgmConfig rows for Context={context}.");
                Stop();
                return;
            }

            var picked = PickWeighted(rows);
            if (picked == null)
            {
                Debug.LogWarning($"[BgmService] All weights zero for Context={context}.");
                Stop();
                return;
            }

            if (!_catalog.TryGetClip(picked.ClipId, out var clip) || clip == null)
            {
                Debug.LogWarning($"[BgmService] ClipId '{picked.ClipId}' missing on BgmClipCatalog.");
                Stop();
                return;
            }

            _source.Stop();
            _source.clip = clip;
            _source.loop = picked.Loop;
            _source.volume = Mathf.Clamp01(picked.Volume);
            _source.Play();
            _current = context;
            Debug.Log(
                $"[BgmService] Play Context={context} BgmId={picked.BgmId} ClipId={picked.ClipId} Loop={picked.Loop}");
        }

        public void Stop()
        {
            if (_source != null)
            {
                _source.Stop();
                _source.clip = null;
            }

            _current = BgmContext.None;
        }

        private static BgmConfigRow PickWeighted(IReadOnlyList<BgmConfigRow> rows)
        {
            var total = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                var w = rows[i] != null ? rows[i].Weight : 0;
                if (w > 0)
                {
                    total += w;
                }
            }

            if (total <= 0)
            {
                return null;
            }

            var roll = Random.Range(0, total);
            var acc = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || row.Weight <= 0)
                {
                    continue;
                }

                acc += row.Weight;
                if (roll < acc)
                {
                    return row;
                }
            }

            return rows[rows.Count - 1];
        }
    }
}
