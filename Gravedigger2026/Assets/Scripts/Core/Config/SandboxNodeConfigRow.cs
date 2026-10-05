namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Level_SandboxNodeConfig row — one Sandbox cell (SPEC_04 §9.1c / SPEC_03 §3.20).
    /// </summary>
    public sealed class SandboxNodeConfigRow
    {
        public const string TypeDig = "Dig";
        public const string TypeCocCombat = "CocCombat";
        public const string TypeShop = "Shop";
        public const string TypeAutoManufacture = "AutoManufacture";

        public string NodeId;
        public string DifficultyId;
        public int SortOrder;
        public string DisplayName;
        public string GameplayType;
        public int RepeatEnterCount;
        public string GameplayConfigId;

        public static bool IsKnownGameplayType(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            switch (text.Trim())
            {
                case TypeDig:
                case TypeCocCombat:
                case TypeShop:
                case TypeAutoManufacture:
                    return true;
                default:
                    return false;
            }
        }
    }
}
