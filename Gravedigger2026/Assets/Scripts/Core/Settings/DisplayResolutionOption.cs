namespace Gravedigger2026.Core.Settings
{
    /// <summary>
    /// Deduped width×height candidate for UI-028 Display tab.
    /// </summary>
    public readonly struct DisplayResolutionOption
    {
        public readonly int Width;
        public readonly int Height;

        public DisplayResolutionOption(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public string Label => $"{Width} × {Height}";
    }
}
