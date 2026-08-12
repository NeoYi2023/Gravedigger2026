using System.IO;
using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Resolves ConfigTables CSV roots per CampaignMode (SPEC_04 §14.5).
    /// </summary>
    public static class CsvPathResolver
    {
        public const string RelativeCsvFolderMode1 = "ConfigTables/Csv";
        public const string RelativeCsvFolderMode2 = "ConfigTables/Mode2/Csv";

        /// <summary>Backward-compat alias for Mode1.</summary>
        public const string RelativeCsvFolder = RelativeCsvFolderMode1;

        public static string RelativeCsvFolderFor(CampaignMode mode)
        {
            return mode == CampaignMode.Mode2 ? RelativeCsvFolderMode2 : RelativeCsvFolderMode1;
        }

        public static string ResolveExistingFile(string csvFileName, CampaignMode mode)
        {
            foreach (var root in EnumerateCandidateRoots(mode))
            {
                var path = Path.Combine(root, csvFileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>Mode1 default for callers that have not yet bound a CampaignMode.</summary>
        public static string ResolveExistingFile(string csvFileName)
        {
            return ResolveExistingFile(csvFileName, CampaignMode.Mode1);
        }

        public static string[] EnumerateCandidateRoots(CampaignMode mode)
        {
            var relative = RelativeCsvFolderFor(mode);
            return new[]
            {
                Path.Combine(Application.dataPath, relative),
                Path.Combine(Application.streamingAssetsPath, relative)
            };
        }

        public static string[] EnumerateCandidateRoots()
        {
            return EnumerateCandidateRoots(CampaignMode.Mode1);
        }
    }
}
