using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Tech
{
    /// <summary>
    /// Save-scoped TechTree learn / DigProtagonistCapabilities recalc
    /// (SPEC_03 §3.13 / §3.16 — tech + Dig-domain protagonist gear, Approach A / PE-03).
    /// </summary>
    public sealed class TechTreeService
    {
        private readonly HashSet<string> _learned = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _unlockedFeatures = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _prerequisitesByTechId =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private ConfigCsvRepository _configs;
        private ProtagonistProgressService _progress;
        private ProtagonistEquipmentService _equipment;
        private DigProtagonistCapabilities _capabilities = new DigProtagonistCapabilities();

        public DigProtagonistCapabilities Capabilities => _capabilities;
        public IReadOnlyCollection<string> LearnedTechIds => _learned;
        public IReadOnlyCollection<string> UnlockedFeatureSystems => _unlockedFeatures;

        public event Action Changed;

        public void Bind(ConfigCsvRepository configs, ProtagonistProgressService progress)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        /// <summary>
        /// Optional Dig-domain gear source for caps merge (SPEC_03 §3.16). Subscribe to warehouse Changed.
        /// </summary>
        public void BindEquipment(ProtagonistEquipmentService equipment)
        {
            if (_equipment != null)
            {
                _equipment.Changed -= HandleEquipmentChanged;
            }

            _equipment = equipment;
            if (_equipment != null)
            {
                _equipment.Changed += HandleEquipmentChanged;
            }

            RecalcCapabilities();
            Changed?.Invoke();
        }

        public void ResetForNewSave()
        {
            _learned.Clear();
            _unlockedFeatures.Clear();
            RebuildPrerequisiteIndex();
            AutoLearnInitiallyUnlocked();
            RecalcCapabilities();
            Changed?.Invoke();
        }

        public bool IsLearned(string techId)
        {
            return !string.IsNullOrEmpty(techId) && _learned.Contains(techId);
        }

        public bool IsLearnable(string techId)
        {
            if (string.IsNullOrEmpty(techId) || _configs == null || _progress == null)
            {
                return false;
            }

            if (_learned.Contains(techId) || !_configs.TryGetTechTree(techId, out var row))
            {
                return false;
            }

            if (_progress.TechPoints < row.LearnCost)
            {
                return false;
            }

            return HasLearnedPrerequisite(techId);
        }

        public TechLearnResult TryLearn(string techId)
        {
            if (string.IsNullOrEmpty(techId) || _configs == null || _progress == null)
            {
                return TechLearnResult.Failed("invalid");
            }

            if (_learned.Contains(techId))
            {
                return TechLearnResult.Failed("already_learned");
            }

            if (!_configs.TryGetTechTree(techId, out var row))
            {
                return TechLearnResult.Failed("missing_config");
            }

            if (!HasLearnedPrerequisite(techId))
            {
                return TechLearnResult.Failed("prereq");
            }

            if (!_progress.TrySpendTechPoints(row.LearnCost))
            {
                return TechLearnResult.Failed("tech_points");
            }

            ApplyLearn(techId, row);
            RecalcCapabilities();
            Changed?.Invoke();
            Debug.Log(
                $"[TechTree] Learned {techId} cost={row.LearnCost} DigDamage={_capabilities.DigDamage} DigDurRed={_capabilities.DigDurationReductionSum} Cursor={_capabilities.DigCursorRadius} StageBonus={_capabilities.DigStageDurationBonus} TechPoints={_progress.TechPoints}");
            return TechLearnResult.Ok();
        }

        public string GetDefaultFocusTechId()
        {
            var rows = _configs?.GetAllTechTreeRows();
            if (rows == null || rows.Count == 0)
            {
                return null;
            }

            return rows[0].TechId;
        }

        private void RebuildPrerequisiteIndex()
        {
            _prerequisitesByTechId.Clear();
            if (_configs == null)
            {
                return;
            }

            var rows = _configs.GetAllTechTreeRows();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row?.UnlockNextTechIds == null)
                {
                    continue;
                }

                for (var n = 0; n < row.UnlockNextTechIds.Length; n++)
                {
                    var child = row.UnlockNextTechIds[n];
                    if (string.IsNullOrEmpty(child))
                    {
                        continue;
                    }

                    if (!_prerequisitesByTechId.TryGetValue(child, out var parents))
                    {
                        parents = new List<string>();
                        _prerequisitesByTechId[child] = parents;
                    }

                    parents.Add(row.TechId);
                }
            }
        }

        private void AutoLearnInitiallyUnlocked()
        {
            if (_configs == null)
            {
                return;
            }

            var rows = _configs.GetAllTechTreeRows();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || !row.InitiallyUnlocked || string.IsNullOrEmpty(row.TechId))
                {
                    continue;
                }

                if (_learned.Contains(row.TechId))
                {
                    continue;
                }

                ApplyLearn(row.TechId, row);
            }
        }

        private void ApplyLearn(string techId, TechTreeConfigRow row)
        {
            _learned.Add(techId);
            if (_configs != null && _configs.TryGetTechEffect(techId, out var effect)
                && !string.IsNullOrEmpty(effect.UnlockedFeatureSystemName))
            {
                _unlockedFeatures.Add(effect.UnlockedFeatureSystemName);
                Debug.Log($"[TechTree] Unlocked feature system '{effect.UnlockedFeatureSystemName}' via {techId}.");
            }
        }

        private bool HasLearnedPrerequisite(string techId)
        {
            if (!_prerequisitesByTechId.TryGetValue(techId, out var parents) || parents.Count == 0)
            {
                // Leaf with no inbound edge: only InitiallyUnlocked may be learned without prereq.
                return false;
            }

            for (var i = 0; i < parents.Count; i++)
            {
                if (_learned.Contains(parents[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleEquipmentChanged()
        {
            RecalcCapabilities();
            Changed?.Invoke();
        }

        private void RecalcCapabilities()
        {
            var sums = new Dictionary<string, float>(StringComparer.Ordinal);
            if (_configs != null)
            {
                foreach (var techId in _learned)
                {
                    if (!_configs.TryGetTechEffect(techId, out var effect)
                        || string.IsNullOrEmpty(effect.AttributeModifiers))
                    {
                        continue;
                    }

                    AccumulateModifiers(effect.AttributeModifiers, sums);
                }

                AccumulateOwnedDigEquipEffects(sums);
            }

            var next = DigProtagonistCapabilities.FromAttributeSums(
                sums,
                _configs != null ? _configs.GetAllGraveQualityIds() : null);
            _configs?.ApplyDigTimingConstants(next);
            // Mutate in place so DigSession holding Capabilities reference stays live (PE-03).
            CopyCapabilities(next, _capabilities);
        }

        private void AccumulateOwnedDigEquipEffects(Dictionary<string, float> sums)
        {
            if (_equipment == null || _configs == null)
            {
                return;
            }

            var owned = _equipment.OwnedEquips;
            for (var i = 0; i < owned.Count; i++)
            {
                var piece = owned[i];
                if (piece == null || string.IsNullOrEmpty(piece.EquipId))
                {
                    continue;
                }

                if (!_configs.TryGetProtagonistEquipment(piece.EquipId, piece.Level, out var row)
                    || row == null)
                {
                    continue;
                }

                if (!EffectDomainIncludesDig(row.EffectDomain))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(row.EquipEffect))
                {
                    continue;
                }

                AccumulateModifiers(row.EquipEffect, sums);
            }
        }

        private static bool EffectDomainIncludesDig(string effectDomain)
        {
            if (string.IsNullOrEmpty(effectDomain))
            {
                return false;
            }

            var segments = effectDomain.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length; i++)
            {
                if (string.Equals(segments[i].Trim(), "Dig", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CopyCapabilities(DigProtagonistCapabilities source, DigProtagonistCapabilities dest)
        {
            if (source == null || dest == null)
            {
                return;
            }

            dest.DigDamage = source.DigDamage;
            dest.DigDurationReductionSum = source.DigDurationReductionSum;
            dest.DigCursorRadius = source.DigCursorRadius;
            dest.DigStageDurationBonus = source.DigStageDurationBonus;
            dest.BaseDigDuration = source.BaseDigDuration;
            dest.DigActionDurationFloor = source.DigActionDurationFloor;
            dest.DiggableQualityIds.Clear();
            foreach (var id in source.DiggableQualityIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    dest.DiggableQualityIds.Add(id);
                }
            }

            if (dest.GraveSpawnWeightBonus == null)
            {
                dest.GraveSpawnWeightBonus = new Dictionary<string, float>(StringComparer.Ordinal);
            }
            else
            {
                dest.GraveSpawnWeightBonus.Clear();
            }

            if (source.GraveSpawnWeightBonus != null)
            {
                foreach (var kv in source.GraveSpawnWeightBonus)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        dest.GraveSpawnWeightBonus[kv.Key] = kv.Value;
                    }
                }
            }
        }

        private static void AccumulateModifiers(string encoded, Dictionary<string, float> sums)
        {
            var segments = encoded.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();
                if (segment.Length == 0)
                {
                    continue;
                }

                var underscore = segment.LastIndexOf('_');
                if (underscore <= 0 || underscore >= segment.Length - 1)
                {
                    Debug.LogWarning($"[TechTree] Bad AttributeModifiers segment '{segment}'.");
                    continue;
                }

                var key = segment.Substring(0, underscore);
                var valueText = segment.Substring(underscore + 1);
                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    Debug.LogWarning($"[TechTree] Bad AttributeModifiers value in '{segment}'.");
                    continue;
                }

                if (sums.TryGetValue(key, out var existing))
                {
                    sums[key] = existing + value;
                }
                else
                {
                    sums[key] = value;
                }
            }
        }
    }

    public readonly struct TechLearnResult
    {
        public bool Success { get; }
        public string FailReason { get; }

        private TechLearnResult(bool success, string failReason)
        {
            Success = success;
            FailReason = failReason;
        }

        public static TechLearnResult Ok() => new TechLearnResult(true, null);
        public static TechLearnResult Failed(string reason) => new TechLearnResult(false, reason);
    }
}
