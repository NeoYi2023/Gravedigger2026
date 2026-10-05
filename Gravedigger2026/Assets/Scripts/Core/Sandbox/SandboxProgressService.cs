using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Sandbox
{
    /// <summary>
    /// Saved Sandbox remaining counts per slot and campaign mode (SPEC_03 §3.20 / SPEC_04 §6).
    /// A node is recorded only the first time this save sees it.
    /// </summary>
    public sealed class SandboxProgressService
    {
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;
        private readonly Dictionary<string, NodeState> _nodes = new Dictionary<string, NodeState>(StringComparer.Ordinal);
        private readonly List<string> _captures = new List<string>();
        private bool _newNodesPending;

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _campaignMode = campaignMode;
            _nodes.Clear();
            _captures.Clear();
            _newNodesPending = false;

            var key = ProgressKey(slotIndex, campaignMode);
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            try
            {
                ApplyLoaded(JsonUtility.FromJson<SandboxProgressSaveData>(raw));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SandboxProgress] Failed to parse JSON key='{key}': {ex.Message}. Reset to empty.");
                _nodes.Clear();
                _captures.Clear();
            }
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            _nodes.Clear();
            _captures.Clear();
            _newNodesPending = false;
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(ProgressKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(ProgressKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Record <paramref name="nodeId"/> with the table's initial count when this save has not seen it.
        /// Does not rewrite an existing node. Call <see cref="FlushNewNodes"/> once after a batch.
        /// </summary>
        public void EnsureSeen(string nodeId, int repeatEnterCount)
        {
            if (_slotIndex < 0 || string.IsNullOrEmpty(nodeId) || _nodes.ContainsKey(nodeId))
            {
                return;
            }

            _nodes.Add(nodeId, new NodeState(Math.Max(0, repeatEnterCount), false));
            _newNodesPending = true;
        }

        public void FlushNewNodes()
        {
            if (!_newNodesPending)
            {
                return;
            }

            Persist();
        }

        public bool TryGetNode(string nodeId, out int remainingEnterCount, out bool cleared)
        {
            remainingEnterCount = 0;
            cleared = false;
            if (string.IsNullOrEmpty(nodeId) || !_nodes.TryGetValue(nodeId, out var state))
            {
                return false;
            }

            remainingEnterCount = state.RemainingEnterCount;
            cleared = state.Cleared;
            return true;
        }

        /// <summary>Remaining above 0 and not cleared. Does not decrement.</summary>
        public bool CanEnter(string nodeId)
        {
            return TryGetNode(nodeId, out var remaining, out var cleared) && !cleared && remaining > 0;
        }

        /// <summary>
        /// Dig / Shop / AutoManufacture: decrement immediately when remaining is above 0 and the node is not cleared.
        /// </summary>
        public bool TryConsumeEnter(string nodeId)
        {
            if (_slotIndex < 0 || string.IsNullOrEmpty(nodeId) || !_nodes.TryGetValue(nodeId, out var state))
            {
                return false;
            }

            if (state.Cleared || state.RemainingEnterCount <= 0)
            {
                return false;
            }

            state.RemainingEnterCount -= 1;
            Persist();
            return true;
        }

        /// <summary>
        /// COC victory: mark the node cleared without decrementing remaining count (SPEC_03 §3.21).
        /// </summary>
        public bool TryMarkCleared(string nodeId)
        {
            if (_slotIndex < 0 || string.IsNullOrEmpty(nodeId) || !_nodes.TryGetValue(nodeId, out var state))
            {
                return false;
            }

            if (state.Cleared)
            {
                return true;
            }

            state.Cleared = true;
            Persist();
            return true;
        }

        /// <summary>
        /// COC round loss: decrement by 1 when remaining is above 0 and the node is not cleared.
        /// Returns the remaining count after the decrement (0 when blocked or already exhausted).
        /// </summary>
        public bool TryConsumeRoundLoss(string nodeId, out int remainingEnterCount)
        {
            remainingEnterCount = 0;
            if (_slotIndex < 0 || string.IsNullOrEmpty(nodeId) || !_nodes.TryGetValue(nodeId, out var state))
            {
                return false;
            }

            if (state.Cleared || state.RemainingEnterCount <= 0)
            {
                remainingEnterCount = state.RemainingEnterCount;
                return false;
            }

            state.RemainingEnterCount -= 1;
            remainingEnterCount = state.RemainingEnterCount;
            Persist();
            return true;
        }

        public bool IsCaptureActivated(string capturePointId)
        {
            return !string.IsNullOrEmpty(capturePointId) && _captures.Contains(capturePointId);
        }

        /// <summary>
        /// Record <paramref name="capturePointId"/> once (SPEC_03 §3.21 slice 05).
        /// Returns true only the first time, and writes the save before rewards are granted.
        /// </summary>
        public bool TryActivateCapture(string capturePointId)
        {
            if (_slotIndex < 0 || string.IsNullOrEmpty(capturePointId) || _captures.Contains(capturePointId))
            {
                return false;
            }

            _captures.Add(capturePointId);
            Persist();
            return true;
        }

        /// <summary>
        /// Add remaining enter count. A cleared node gains nothing. An unseen node is skipped.
        /// </summary>
        public bool TryAddRemainingEnterCount(string nodeId, int addCount)
        {
            if (_slotIndex < 0 || addCount <= 0 || string.IsNullOrEmpty(nodeId) ||
                !_nodes.TryGetValue(nodeId, out var state) || state.Cleared)
            {
                return false;
            }

            state.RemainingEnterCount += addCount;
            Persist();
            return true;
        }

        private void ApplyLoaded(SandboxProgressSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.Nodes != null)
            {
                for (var i = 0; i < data.Nodes.Length; i++)
                {
                    var node = data.Nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.NodeId) || _nodes.ContainsKey(node.NodeId))
                    {
                        continue;
                    }

                    _nodes.Add(node.NodeId, new NodeState(Math.Max(0, node.RemainingEnterCount), node.Cleared));
                }
            }

            if (data.Captures == null)
            {
                return;
            }

            for (var i = 0; i < data.Captures.Length; i++)
            {
                var id = data.Captures[i];
                if (string.IsNullOrEmpty(id) || _captures.Contains(id))
                {
                    continue;
                }

                _captures.Add(id);
            }
        }

        private void Persist()
        {
            _newNodesPending = false;
            if (_slotIndex < 0)
            {
                return;
            }

            var ids = new List<string>(_nodes.Count);
            foreach (var pair in _nodes)
            {
                ids.Add(pair.Key);
            }

            ids.Sort(StringComparer.Ordinal);
            var nodes = new SandboxNodeProgressSaveData[ids.Count];
            for (var i = 0; i < ids.Count; i++)
            {
                var state = _nodes[ids[i]];
                nodes[i] = new SandboxNodeProgressSaveData
                {
                    NodeId = ids[i],
                    RemainingEnterCount = state.RemainingEnterCount,
                    Cleared = state.Cleared
                };
            }

            var data = new SandboxProgressSaveData
            {
                Nodes = nodes,
                Captures = _captures.ToArray()
            };

            PlayerPrefs.SetString(ProgressKey(_slotIndex, _campaignMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private static string ProgressKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.SandboxProgressSuffix);
        }

        private sealed class NodeState
        {
            public int RemainingEnterCount;
            public bool Cleared;

            public NodeState(int remainingEnterCount, bool cleared)
            {
                RemainingEnterCount = remainingEnterCount;
                Cleared = cleared;
            }
        }
    }
}
