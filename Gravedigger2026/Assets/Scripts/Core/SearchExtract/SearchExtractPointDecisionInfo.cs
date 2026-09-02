namespace Gravedigger2026.Core.SearchExtract
{
    /// <summary>UI-032 payload after gather-point success (SPEC_03 §3.19).</summary>
    public sealed class SearchExtractPointDecisionInfo
    {
        public int GatherPointOrder;
        public int GatherPointCount;
        public bool IsLastPoint;
        public bool ShowContinue;
    }
}
