namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Cross-stage flag: after AutoManufacture presentation, UM auto-opens Formation once (UI-016 Step3).
    /// </summary>
    public sealed class AutoManufacturePresentationFlags
    {
        public bool AutoOpenFormationOnce { get; private set; }

        public void ArmAutoOpenFormation()
        {
            AutoOpenFormationOnce = true;
        }

        public bool ConsumeAutoOpenFormation()
        {
            if (!AutoOpenFormationOnce)
            {
                return false;
            }

            AutoOpenFormationOnce = false;
            return true;
        }

        public void Clear()
        {
            AutoOpenFormationOnce = false;
        }
    }
}
