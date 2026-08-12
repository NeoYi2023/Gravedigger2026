using System.Collections.Generic;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// AutoManufacture batch buffer (SPEC_03 §3.15 TempWarriorWarehouse). Not WarriorPool.
    /// </summary>
    public sealed class TempWarriorWarehouse
    {
        private readonly List<AutoCraftDraft> _drafts = new List<AutoCraftDraft>();

        public IReadOnlyList<AutoCraftDraft> Drafts => _drafts;

        public int Count => _drafts.Count;

        public void Clear()
        {
            _drafts.Clear();
        }

        public void Add(AutoCraftDraft draft)
        {
            if (draft != null)
            {
                _drafts.Add(draft);
            }
        }
    }
}
