using System;
using System.Collections.Generic;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PM-04: living-monster-in-zone probe (SPEC_04 §9.22 capture runtime contract).
    /// Default: scan injected MonsterAgentView list — IsAlive && CaptureZone.ContainsXZ.
    /// Rebels do not block capture (not part of the monster list). Pre-PM-05 the list is
    /// naturally empty; force flag may be used for reset acceptance.
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

            if (zone == null)
            {
                return false;
            }

            var monsters = _monstersProvider != null ? _monstersProvider() : null;
            if (monsters == null)
            {
                return false;
            }

            for (var i = 0; i < monsters.Count; i++)
            {
                var m = monsters[i];
                if (m != null && m.IsAlive && zone.ContainsXZ(m.transform.position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
