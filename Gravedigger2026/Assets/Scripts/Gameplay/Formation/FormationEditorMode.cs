namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Host mode for shared FormationEditorRoot (SPEC_03 §3.11 / §3.12).
    /// </summary>
    public enum FormationEditorMode
    {
        UpgradeManufacture = 0,
        DefendPrepare = 1,
        PushMapPrepare = 2,
        SearchExtractPrepare = 3
    }

    public static class FormationEditorModeUtil
    {
        public static bool ShowsStartBattle(FormationEditorMode mode)
        {
            return mode == FormationEditorMode.DefendPrepare
                || mode == FormationEditorMode.PushMapPrepare
                || mode == FormationEditorMode.SearchExtractPrepare;
        }

        public static bool UsesPushMapPrepareFraming(FormationEditorMode mode)
        {
            return mode == FormationEditorMode.PushMapPrepare
                || mode == FormationEditorMode.SearchExtractPrepare;
        }
    }
}
