#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Config
{
    /// <summary>
    /// One-click Excel → CSV bake (SPEC_04 §14.4 Approach A).
    /// Menu: Gravedigger2026/Config/Bake Tables
    /// </summary>
    public static class ConfigTableBaker
    {
        private const string MenuPath = "Gravedigger2026/Config/Bake Tables";
        private const string LogPrefix = "[ConfigTableBaker]";

        [MenuItem(MenuPath)]
        public static void BakeAllTables()
        {
            var excelDir = Path.Combine(Application.dataPath, "ConfigTables", "Excel");
            var csvDir = Path.Combine(Application.dataPath, "ConfigTables", "Csv");

            if (!Directory.Exists(excelDir))
            {
                Debug.LogError($"{LogPrefix} Excel folder missing: {excelDir}");
                return;
            }

            Directory.CreateDirectory(csvDir);

            var xlsxFiles = Directory.GetFiles(excelDir, "*.xlsx", SearchOption.TopDirectoryOnly)
                .Where(p =>
                {
                    var name = Path.GetFileName(p);
                    return !name.StartsWith("~$", StringComparison.Ordinal);
                })
                .OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
                .ToArray();

            if (xlsxFiles.Length == 0)
            {
                Debug.LogWarning($"{LogPrefix} No .xlsx files under {excelDir}");
                return;
            }

            var pending = new List<(string ExcelFileName, string CsvPath, string CsvText)>();

            foreach (var xlsxPath in xlsxFiles)
            {
                var excelFileName = Path.GetFileName(xlsxPath);
                var baseName = Path.GetFileNameWithoutExtension(xlsxPath);

                if (!TryMapExcelBaseToCsvBase(baseName, out var csvBase, out var nameError))
                {
                    Debug.LogError(
                        $"{LogPrefix} Abort — {excelFileName}: {nameError}. No CSV files were written.");
                    return;
                }

                List<string[]> rows;
                try
                {
                    rows = XlsxSheetReader.ReadFirstSheet(xlsxPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"{LogPrefix} Abort — {excelFileName}: failed to read sheet — {ex.Message}. No CSV files were written.");
                    return;
                }

                if (rows == null || rows.Count == 0 || !HeaderHasContent(rows[0]))
                {
                    Debug.LogError(
                        $"{LogPrefix} Abort — {excelFileName}: first sheet has no header row. No CSV files were written.");
                    return;
                }

                var csvText = BuildCsv(rows);
                var csvPath = Path.Combine(csvDir, csvBase + ".csv");
                pending.Add((excelFileName, csvPath, csvText));
            }

            foreach (var item in pending)
            {
                File.WriteAllText(item.CsvPath, item.CsvText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            AssetDatabase.Refresh();

            var summary = new StringBuilder();
            summary.AppendLine($"{LogPrefix} Baked {pending.Count} table(s):");
            foreach (var item in pending)
            {
                summary.AppendLine($"  {item.ExcelFileName} → {Path.GetFileName(item.CsvPath)}");
            }

            Debug.Log(summary.ToString());
        }

        /// <summary>
        /// Excel `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV `{SystemEN}_{TableEN}`.
        /// </summary>
        public static bool TryMapExcelBaseToCsvBase(string excelBase, out string csvBase, out string error)
        {
            csvBase = null;
            error = null;
            if (string.IsNullOrEmpty(excelBase))
            {
                error = "empty basename";
                return false;
            }

            var parts = excelBase.Split('_');
            if (parts.Length != 4)
            {
                error =
                    $"filename must be exactly four underscore-separated parts " +
                    $"(SystemZH_TableZH_SystemEN_TableEN), got {parts.Length} part(s)";
                return false;
            }

            for (var i = 0; i < 4; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    error = "one or more name segments are empty";
                    return false;
                }
            }

            csvBase = parts[2] + "_" + parts[3];
            return true;
        }

        private static bool HeaderHasContent(string[] header)
        {
            if (header == null || header.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < header.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(header[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildCsv(List<string[]> rows)
        {
            var sb = new StringBuilder();
            var wroteAny = false;

            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                if (r > 0 && IsRowEmpty(row))
                {
                    continue;
                }

                if (wroteAny)
                {
                    sb.Append('\n');
                }

                for (var c = 0; c < row.Length; c++)
                {
                    if (c > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(EscapeCsvField(row[c] ?? string.Empty));
                }

                wroteAny = true;
            }

            if (wroteAny)
            {
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static bool IsRowEmpty(string[] row)
        {
            if (row == null || row.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < row.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string EscapeCsvField(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
#endif
