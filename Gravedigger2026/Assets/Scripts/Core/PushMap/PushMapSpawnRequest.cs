namespace Gravedigger2026.Core.PushMap
{
    /// <summary>
    /// One PushMapSpawnConfig activation payload for View (PM-05; SPEC_04 §9.23).
    /// Position is resolved by the View via SpawnPointId / BossPoint — rules do not own Transforms.
    /// </summary>
    public enum PushMapSpawnTrigger
    {
        StartBattle = 0,
        Trap = 1,
        /// <summary>Prepare Idle visuals for StartBattle-eligible non-trap rows; not registered in Session.</summary>
        PreparePreview = 2
    }

    public sealed class PushMapSpawnRequest
    {
        public string SpawnPointId;
        public string MonsterId;
        public int SpawnCount;
        public int LinkedObjectiveOrder;
        public bool IsBoss;
        public int SpawnOrder;
        public PushMapSpawnTrigger Trigger;
    }
}
