namespace Gravedigger2026.Core
{
    /// <summary>
    /// Session-scoped active <see cref="CampaignMode"/> while inside InSaveShell (SPEC_04 §6 D-045).
    /// </summary>
    public sealed class CampaignModeService
    {
        private CampaignMode? _current;

        public bool HasMode => _current.HasValue;

        public CampaignMode Current
        {
            get
            {
                if (!_current.HasValue)
                {
                    throw new System.InvalidOperationException(
                        "CampaignModeService: no mode bound (not inside InSaveShell).");
                }

                return _current.Value;
            }
        }

        public void Set(CampaignMode mode)
        {
            _current = mode;
        }

        public void Clear()
        {
            _current = null;
        }
    }
}
