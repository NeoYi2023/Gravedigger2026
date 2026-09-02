namespace Gravedigger2026.Core.Config
{
    /// <summary>SearchExtract wave spawn row — one wave (SPEC_04 §9.33 / D-087).</summary>
    public sealed class SearchExtractWaveSpawnConfigRow
    {
        public string GameplayConfigId;
        public int GatherPointOrder;
        public int WaveIndex;
        public float FirstWaveDelaySeconds;
        public float WaveIntervalSeconds;
        public string SpawnPointId;
        public string MonsterId;
        public int SpawnCount;
    }
}
