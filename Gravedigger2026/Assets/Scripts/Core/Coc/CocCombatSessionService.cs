using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// COC session (SPEC_03 §3.21). Stores this round's spawn rows.
    /// Slice 03b deploy consumption is the warrior pool itself. Win/loss, fog, and capture stay later.
    /// Return is not a round loss.
    /// </summary>
    public sealed class CocCombatSessionService
    {
        private readonly List<CocSpawnConfigRow> _spawns = new List<CocSpawnConfigRow>(8);

        public CocGameplayConfigRow Config { get; private set; }

        public bool IsActive { get; private set; }

        public IReadOnlyList<CocSpawnConfigRow> Spawns => _spawns;

        public void Begin(CocGameplayConfigRow config, IReadOnlyList<CocSpawnConfigRow> spawns)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _spawns.Clear();
            if (spawns != null)
            {
                for (var i = 0; i < spawns.Count; i++)
                {
                    if (spawns[i] != null)
                    {
                        _spawns.Add(spawns[i]);
                    }
                }
            }

            IsActive = true;
        }

        public void End()
        {
            IsActive = false;
            Config = null;
            _spawns.Clear();
        }
    }
}
