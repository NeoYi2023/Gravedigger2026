#if UNITY_EDITOR
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.SearchExtract;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.EditorTools.SearchExtract
{
        /// <summary>SE-CAM-01/03 scene-free HoldFraming solver + table-key + sample-range checks.</summary>
        public static class SearchExtractHoldFramingChecksMenu
        {
            private const string MenuPath = "Gravedigger2026/SearchExtract/Run HoldFraming Solver Checks (SE-CAM-01)";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var solverError = SearchExtractHoldFramingCorrectnessChecks.RunAll();
            var configs = new ConfigCsvRepository();
            string tableError;
            if (!configs.TryLoadAll(CampaignMode.Mode2))
            {
                tableError = "TableKeys: TryLoadAll(Mode2) failed.";
            }
            else
            {
                tableError = SearchExtractHoldFramingCorrectnessChecks.RunTableKeys(configs);
            }

            if (solverError == null && tableError == null)
            {
                Debug.Log("[SearchExtractHoldFramingChecks] All checks passed (SE-CAM-01/03).");
                return;
            }

            if (solverError != null)
            {
                Debug.LogError($"[SearchExtractHoldFramingChecks] Solver FAILED:\n{solverError}");
            }

            if (tableError != null)
            {
                Debug.LogError($"[SearchExtractHoldFramingChecks] Table FAILED:\n{tableError}");
            }
        }
    }
}
#endif
