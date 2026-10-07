using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Painted air-wall layer under the map Grid (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// Visible while editing; hidden in play so combat does not draw it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public sealed class AirWallTilemap : MonoBehaviour
    {
        public const string LayerName = "AirWallTilemap";

        private Tilemap _tilemap;
        private TilemapRenderer _renderer;

        public Tilemap Tilemap
        {
            get
            {
                if (_tilemap == null)
                {
                    _tilemap = GetComponent<Tilemap>();
                }

                return _tilemap;
            }
        }

        private void Awake()
        {
            HideInPlay();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                HideInPlay();
            }
        }

        /// <summary>
        /// True when world XZ lands on a painted cell.
        /// <paramref name="ignoreSupportsSpecialMove"/> skips <see cref="AirWallPaintTile.SupportsSpecialMove"/> cells.
        /// </summary>
        public bool Blocks(float worldX, float worldZ, bool ignoreSupportsSpecialMove)
        {
            var tilemap = Tilemap;
            if (tilemap == null)
            {
                return false;
            }

            var origin = tilemap.transform.position;
            var cell = tilemap.WorldToCell(new Vector3(worldX, origin.y, worldZ));
            if (!tilemap.HasTile(cell))
            {
                return false;
            }

            if (!ignoreSupportsSpecialMove)
            {
                return true;
            }

            var paint = tilemap.GetTile(cell) as AirWallPaintTile;
            return paint == null || !paint.SupportsSpecialMove;
        }

        private void HideInPlay()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<TilemapRenderer>();
            }

            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }
    }
}
