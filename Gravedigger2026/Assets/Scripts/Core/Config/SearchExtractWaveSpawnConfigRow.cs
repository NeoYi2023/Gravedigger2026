namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// SearchExtract independent spawn recipe row (SPEC_04 §9.33 / D-087).
    /// FirstWaveDelay from point activation; then Interval × RepeatSpawnCount re-spawns.
    /// </summary>
    public sealed class SearchExtractWaveSpawnConfigRow
    {
        public string GameplayConfigId;
        public int GatherPointOrder;
        public int WaveIndex;
        public float FirstWaveDelaySeconds;
        public float WaveIntervalSeconds;
        public int RepeatSpawnCount;
        public string SpawnPointId;
        public string MonsterId;
        public int SpawnCount;
    }
}
