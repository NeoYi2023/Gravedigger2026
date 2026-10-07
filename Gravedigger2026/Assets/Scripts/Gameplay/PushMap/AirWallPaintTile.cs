using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Paint brush for <see cref="AirWallTilemap"/> (SPEC_03 §3.14 / SPEC_04 §9.22 / D-101).
    /// <see cref="SupportsSpecialMove"/> matches <see cref="AirWall.SupportsSpecialMove"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "AirWallTile", menuName = "Gravedigger2026/Air Wall Tile（空气墙砖）")]
    public sealed class AirWallPaintTile : Tile
    {
        [SerializeField]
        [Tooltip("支持特殊移动：勾选后，职业 SpecialMove=1 的士兵可将本砖视为可走（D-101）。缺省否。")]
        private bool _supportsSpecialMove;

        public bool SupportsSpecialMove => _supportsSpecialMove;

        public void SetSupportsSpecialMove(bool supportsSpecialMove)
        {
            _supportsSpecialMove = supportsSpecialMove;
        }
    }
}
