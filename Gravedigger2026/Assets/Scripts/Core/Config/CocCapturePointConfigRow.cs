namespace Gravedigger2026.Core.Config
{
    /// <summary>COC capture point row (SPEC_04 §9.37). Radius is not stored on the map marker.</summary>
    public sealed class CocCapturePointConfigRow
    {
        public string CapturePointId;
        public string GameplayConfigId;
        public float Radius;
    }
}
