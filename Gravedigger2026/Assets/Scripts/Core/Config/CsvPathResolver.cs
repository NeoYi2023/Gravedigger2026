using System.IO;
using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Resolves ConfigTables/Csv roots per SPEC_04 §14.5 (Editor dataPath, then StreamingAssets).
    /// </summary>
    public static class CsvPathResolver
    {
        public const string RelativeCsvFolder = "ConfigTables/Csv";

        public static string ResolveExistingFile(string csvFileName)
        {
            foreach (var root in EnumerateCandidateRoots())
            {
                var path = Path.Combine(root, csvFileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        public static string[] EnumerateCandidateRoots()
        {
            return new[]
            {
                Path.Combine(Application.dataPath, RelativeCsvFolder),
                Path.Combine(Application.streamingAssetsPath, RelativeCsvFolder)
            };
        }
    }
}
