namespace Gravedigger2026.Core.Config
{
    /// <summary>COC monster vs destructible obstacle (SPEC_04 §9.19 / D-100). Unread by other modes.</summary>
    public enum CocObstaclePathMode
    {
        TreatAsObstacle = 0,
        TreatAsTarget = 1,
        IgnoreObstacle = 2
    }
}
