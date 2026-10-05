namespace Gravedigger2026.Core.Config
{
    /// <summary>One COC spawn definition (SPEC_04 §9.36). Same SpawnPointId may repeat.</summary>
    public sealed class CocSpawnConfigRow
    {
        public const string RoleNormal = "Normal";
        public const string RoleMiniBoss = "MiniBoss";
        public const string RoleFinalBoss = "FinalBoss";

        public string GameplayConfigId;
        public string SpawnPointId;
        public string MonsterId;
        public int SpawnCount;
        public string SpawnRole;
        public int SpawnOrder;

        public static bool IsKnownRole(string role)
        {
            return role == RoleNormal || role == RoleMiniBoss || role == RoleFinalBoss;
        }
    }
}
