namespace Gravedigger2026.Core.Defend
{
    /// <summary>One WaveSpawnConfig row activation payload for View (SPEC_03 §3.12).</summary>
    public sealed class DefendWaveSpawnRequest
    {
        public string WaveConfigId;
        public int SpawnOrder;
        public int SpawnRemainingSeconds;
        public string MonsterId;
        public int SpawnCount;
        public string AppearLocation;
        public string SpawnMode;
        public int SpawnClockHour;
    }
}
