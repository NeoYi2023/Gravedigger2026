using System;
using System.Collections.Generic;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Legacy PM-04 living-monster-in-zone probe (pre-v0.74.8 Capture timer).
    /// Capture is now arrive-based; Stage no longer wires this. Kept for optional debug injection.
    /// </summary>
    public sealed class PushMapMonsterPresenceProbe
    {
        private Func<IReadOnlyList<MonsterAgentView>> _monstersProvider;

        public bool ForceHasMonster;

        public void BindMonstersProvider(Func<IReadOnlyList<MonsterAgentView>> monstersProvider)
        {
            _monstersProvider = monstersProvider;
        }

        public bool HasLivingMonster(CaptureZone zone)
        {
            if (ForceHasMonster)
            {
                return true;
            }

            if (zone == null || _monstersProvider == null)
            {
                return false;
            }

            var list = _monstersProvider();
            if (list == null)
            {
                return false;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m != null && m.IsAlive && zone.ContainsXZ(m.transform.position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
