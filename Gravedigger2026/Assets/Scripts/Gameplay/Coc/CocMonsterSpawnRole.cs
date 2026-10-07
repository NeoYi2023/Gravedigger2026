using Gravedigger2026.Core.Coc;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Spawn role for fog visibility (SPEC_03 §3.21). Final Boss and Mini Boss stay above the fog sheet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocMonsterSpawnRole : MonoBehaviour
    {
        public const int BossSortingOrder = 280;

        private string _role = CocSpawnConfigRow.RoleNormal;
        private Renderer[] _renderers;
        private bool _hidden;
        private bool _applied;
        private bool _discovered;

        public bool StaysVisible =>
            _role == CocSpawnConfigRow.RoleMiniBoss ||
            _role == CocSpawnConfigRow.RoleFinalBoss;

        public void Bind(string role)
        {
            _role = string.IsNullOrEmpty(role) ? CocSpawnConfigRow.RoleNormal : role;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void ApplyFog(byte cellState)
        {
            if (StaysVisible)
            {
                RaiseAboveFog();
                return;
            }

            var hideBody = true;
            if (cellState == CocFogGrid.Outside)
            {
                hideBody = false;
            }
            else if (cellState == CocFogGrid.Revealed || cellState == CocFogGrid.Permanent)
            {
                _discovered = true;
                hideBody = false;
            }
            else if (cellState == CocFogGrid.Explored && _discovered)
            {
                hideBody = false;
            }

            if (_renderers == null || (_applied && _hidden == hideBody))
            {
                return;
            }

            _applied = true;
            _hidden = hideBody;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer != null)
                {
                    renderer.enabled = !hideBody;
                }
            }
        }

        private void RaiseAboveFog()
        {
            if (_renderers == null)
            {
                return;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer != null && renderer.sortingOrder < BossSortingOrder)
                {
                    renderer.sortingOrder = BossSortingOrder;
                }
            }
        }
    }
}
