using System;
using Gravedigger2026.Core.Pathing;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// FlowField mask: box AirWalls plus painted <see cref="AirWallTilemap"/> cells (SPEC_04 §9.22).
    /// </summary>
    public sealed class AirWallCombinedWalkMask : IFlowFieldWalkableMask
    {
        private static readonly AirWallTilemap[] EmptyLayers = Array.Empty<AirWallTilemap>();

        private readonly StaticBoxWalkableMask _boxes = new StaticBoxWalkableMask();
        private AirWallTilemap[] _layers = EmptyLayers;
        private bool _ignoreSupportsSpecialMove;

        public int BoxCount => _boxes.BoxCount;

        public void Clear()
        {
            _boxes.Clear();
            _layers = EmptyLayers;
            _ignoreSupportsSpecialMove = false;
        }

        public void AddBox(StaticBoxWalkableMask.BoxObstacle box)
        {
            _boxes.AddBox(box);
        }

        public void SetLayers(AirWallTilemap[] layers, bool ignoreSupportsSpecialMove)
        {
            _layers = layers ?? EmptyLayers;
            _ignoreSupportsSpecialMove = ignoreSupportsSpecialMove;
        }

        public bool IsWalkable(float worldX, float worldZ)
        {
            if (!_boxes.IsWalkable(worldX, worldZ))
            {
                return false;
            }

            for (var i = 0; i < _layers.Length; i++)
            {
                var layer = _layers[i];
                if (layer != null && layer.Blocks(worldX, worldZ, _ignoreSupportsSpecialMove))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
