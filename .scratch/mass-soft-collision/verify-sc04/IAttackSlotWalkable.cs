namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Walkability hook for AttackSlot candidates (SPEC_04 §9.7).
    /// Demo may use a stub; NavMesh.SamplePosition (or bake mask) plugs in later — not required for MP-02.
    /// </summary>
    public interface IAttackSlotWalkable
    {
        /// <summary>True if the candidate world point is standable for a slot claim.</summary>
        bool IsSlotWalkable(float worldX, float worldY, float worldZ);
    }

    /// <summary>
    /// Stub: every candidate is walkable. Replace with SamplePosition / mask when Stage wires MP-05.
    /// </summary>
    public sealed class StubAttackSlotFullyWalkable : IAttackSlotWalkable
    {
        public static readonly StubAttackSlotFullyWalkable Instance = new StubAttackSlotFullyWalkable();

        public bool IsSlotWalkable(float worldX, float worldY, float worldZ) => true;
    }
}
