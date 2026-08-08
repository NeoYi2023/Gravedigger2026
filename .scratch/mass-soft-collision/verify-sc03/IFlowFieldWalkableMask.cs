namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Static walkability for FlowField cells (SPEC_04 §9.7).
    /// Includes map bounds / AirWall / bake non-walkable. Must NOT encode friendlies
    /// (no dynamic Carve into the field). PushMap wires <see cref="StaticBoxWalkableMask"/>
    /// from AirWall OBBs at StartBattle (MP-04).
    /// </summary>
    public interface IFlowFieldWalkableMask
    {
        /// <summary>True if world XZ is statically walkable (AirWall / bake blockers excluded).</summary>
        bool IsWalkable(float worldX, float worldZ);
    }

    /// <summary>
    /// Stub mask: every queried point is walkable. Diamond clipping still applied by
    /// <see cref="FlowFieldService"/>. Prefer <see cref="StaticBoxWalkableMask"/> in PushMap.
    /// </summary>
    public sealed class StubFullyWalkableMask : IFlowFieldWalkableMask
    {
        public static readonly StubFullyWalkableMask Instance = new StubFullyWalkableMask();

        public bool IsWalkable(float worldX, float worldZ) => true;
    }
}
