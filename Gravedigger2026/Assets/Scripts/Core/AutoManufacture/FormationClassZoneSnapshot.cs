namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Map-center-relative FormationClassZone snapshot for rules deploy
    /// (SPEC_03 §3.15; IsoDiamond; no Transform ownership).
    /// </summary>
    public readonly struct FormationClassZoneSnapshot
    {
        public readonly string ClassId;
        public readonly float CenterRelX;
        public readonly float CenterRelZ;
        public readonly float HalfExtentX;
        public readonly float HalfExtentZ;

        public FormationClassZoneSnapshot(
            string classId,
            float centerRelX,
            float centerRelZ,
            float halfExtentX,
            float halfExtentZ)
        {
            ClassId = classId ?? string.Empty;
            CenterRelX = centerRelX;
            CenterRelZ = centerRelZ;
            HalfExtentX = halfExtentX < 0.05f ? 0.05f : halfExtentX;
            HalfExtentZ = halfExtentZ < 0.05f ? 0.05f : halfExtentZ;
        }
    }
}
