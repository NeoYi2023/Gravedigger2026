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
    }
}
