namespace Gravedigger2026.Core.Config
{
    /// <summary>PushMap spawn config row (SPEC_04 §9.23).</summary>
    public sealed class PushMapSpawnConfigRow
    {
        public string GameplayConfigId;
        public string SpawnPointId;
        public string MonsterId;
        public int SpawnCount;
        public int LinkedObjectiveOrder;
        public string TrapZoneId;
        public bool IsBoss;
        public int SpawnOrder;
    }
}
