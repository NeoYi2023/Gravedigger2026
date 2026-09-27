using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Composite-key index for Combat_TacticalFormationConfig (SPEC_04 §9.30).
    /// Identity is the lowest <see cref="TacticalFormationConfigRow.FormationLevel"/> row.
    /// </summary>
    public sealed class TacticalFormationConfigIndex
    {
        private readonly Dictionary<string, TacticalFormationConfigRow> _byKey =
            new Dictionary<string, TacticalFormationConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<TacticalFormationConfigRow>> _rowsById =
            new Dictionary<string, List<TacticalFormationConfigRow>>(StringComparer.Ordinal);
        private readonly Dictionary<string, TacticalFormationConfigRow> _identityById =
            new Dictionary<string, TacticalFormationConfigRow>(StringComparer.Ordinal);
        private readonly List<TacticalFormationConfigRow> _rows = new List<TacticalFormationConfigRow>();
        private readonly List<string> _formationIds = new List<string>();
        private bool _sealed;

        public IReadOnlyList<TacticalFormationConfigRow> Rows => _rows;

        public IReadOnlyList<string> FormationIds => _formationIds;

        public void Clear()
        {
            _byKey.Clear();
            _rowsById.Clear();
            _identityById.Clear();
            _rows.Clear();
            _formationIds.Clear();
            _sealed = false;
        }

        /// <summary>Throws on a repeated (FormationId, FormationLevel).</summary>
        public void Add(TacticalFormationConfigRow row)
        {
            if (_sealed)
            {
                throw new InvalidOperationException("TacticalFormationConfigIndex is sealed.");
            }

            if (row == null || string.IsNullOrEmpty(row.FormationId) || row.FormationLevel < 1)
            {
                throw new InvalidOperationException(
                    "illegal TacticalFormationConfig row (need FormationId and FormationLevel ≥ 1).");
            }

            var key = MakeKey(row.FormationId, row.FormationLevel);
            if (_byKey.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"duplicate composite PK ({row.FormationId}, {row.FormationLevel}).");
            }

            _byKey[key] = row;
            _rows.Add(row);
            if (!_rowsById.TryGetValue(row.FormationId, out var byId))
            {
                byId = new List<TacticalFormationConfigRow>();
                _rowsById[row.FormationId] = byId;
                _formationIds.Add(row.FormationId);
            }

            byId.Add(row);
        }

        /// <summary>
        /// Sorts each FormationId by level and warns when display/geometry fields
        /// disagree with the lowest-level row.
        /// </summary>
        public void Seal()
        {
            if (_sealed)
            {
                return;
            }

            foreach (var pair in _rowsById)
            {
                var levels = pair.Value;
                levels.Sort((a, b) => a.FormationLevel.CompareTo(b.FormationLevel));
                var identity = levels[0];
                _identityById[pair.Key] = identity;
                for (var i = 1; i < levels.Count; i++)
                {
                    WarnIfIdentityMismatch(identity, levels[i]);
                }
            }

            _sealed = true;
        }

        public bool TryGetIdentity(string formationId, out TacticalFormationConfigRow row)
        {
            Seal();
            row = null;
            if (string.IsNullOrEmpty(formationId))
            {
                return false;
            }

            return _identityById.TryGetValue(formationId, out row);
        }

        /// <summary>Greatest FormationLevel ≤ <paramref name="computedLevel"/>; false if none.</summary>
        public bool TryGetForComputedLevel(
            string formationId,
            int computedLevel,
            out TacticalFormationConfigRow row)
        {
            Seal();
            row = null;
            if (string.IsNullOrEmpty(formationId)
                || !_rowsById.TryGetValue(formationId, out var levels)
                || levels == null
                || levels.Count == 0)
            {
                return false;
            }

            TacticalFormationConfigRow best = null;
            for (var i = 0; i < levels.Count; i++)
            {
                var candidate = levels[i];
                if (candidate == null || candidate.FormationLevel > computedLevel)
                {
                    break;
                }

                best = candidate;
            }

            if (best == null)
            {
                return false;
            }

            row = best;
            return true;
        }

        private static void WarnIfIdentityMismatch(
            TacticalFormationConfigRow identity,
            TacticalFormationConfigRow other)
        {
            if (identity == null || other == null)
            {
                return;
            }

            var diffs = new List<string>(8);
            NoteText(diffs, "DisplayName", identity.DisplayName, other.DisplayName);
            NoteText(diffs, "IconAssetId", identity.IconAssetId, other.IconAssetId);
            NoteText(diffs, "Description", identity.Description, other.Description);
            NoteText(diffs, "FormationSkillId", identity.FormationSkillId, other.FormationSkillId);
            NoteText(diffs, "PrefabId", identity.PrefabId, other.PrefabId);
            if (identity.MinMemberCount != other.MinMemberCount)
            {
                diffs.Add("MinMemberCount");
            }

            if (identity.MaxMemberCount != other.MaxMemberCount)
            {
                diffs.Add("MaxMemberCount");
            }

            if (diffs.Count == 0)
            {
                return;
            }

            Debug.LogWarning(
                $"[Config] Combat_TacticalFormationConfig '{identity.FormationId}' " +
                $"Lv{other.FormationLevel.ToString(CultureInfo.InvariantCulture)} differs from lowest " +
                $"FormationLevel {identity.FormationLevel.ToString(CultureInfo.InvariantCulture)} " +
                $"({string.Join(", ", diffs)}). Geometry and display use the lowest row.");
        }

        private static void NoteText(List<string> diffs, string field, string identity, string other)
        {
            if (!string.Equals(identity ?? string.Empty, other ?? string.Empty, StringComparison.Ordinal))
            {
                diffs.Add(field);
            }
        }

        private static string MakeKey(string formationId, int formationLevel)
        {
            return formationId + "\u001f" + formationLevel.ToString(CultureInfo.InvariantCulture);
        }
    }
}
