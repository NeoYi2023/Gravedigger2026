using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Save-scoped deployable soldier pool (SPEC_03 §3.11). Demo: in-memory; consumed by formation (04c).
    /// </summary>
    public sealed class WarriorPoolService
    {
        private readonly List<WarriorInstance> _warriors = new List<WarriorInstance>();
        private int _nextSerial = 1;

        public IReadOnlyList<WarriorInstance> Warriors => _warriors;

        public event Action Changed;

        public void Clear()
        {
            _warriors.Clear();
            _nextSerial = 1;
            Changed?.Invoke();
        }

        public string ReserveNextId()
        {
            return "W_" + _nextSerial.ToString("D3");
        }

        public void Add(WarriorInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            _warriors.Add(instance);
            _nextSerial++;
            Changed?.Invoke();
        }

        public bool TryRemove(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < _warriors.Count; i++)
            {
                if (string.Equals(_warriors[i].Id, warriorId, StringComparison.Ordinal))
                {
                    _warriors.RemoveAt(i);
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool TryGet(string warriorId, out WarriorInstance instance)
        {
            instance = null;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < _warriors.Count; i++)
            {
                if (string.Equals(_warriors[i].Id, warriorId, StringComparison.Ordinal))
                {
                    instance = _warriors[i];
                    return true;
                }
            }

            return false;
        }
    }
}
