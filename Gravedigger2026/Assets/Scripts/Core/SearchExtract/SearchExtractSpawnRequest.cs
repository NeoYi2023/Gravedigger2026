namespace Gravedigger2026.Core.SearchExtract
{
    /// <summary>
    /// One SearchExtractWaveSpawnConfig activation payload for View (SE-06; SPEC_04 §9.33).
    /// World position resolved by View via SpawnPointId — rules do not own Transforms.
    /// </summary>
    public sealed class SearchExtractSpawnRequest
    {
        public int GatherPointOrder;
        public int WaveIndex;
        public string SpawnPointId;
        public string MonsterId;
        public int SpawnCount;
    }
}
