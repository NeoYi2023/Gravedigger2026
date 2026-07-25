using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Minimal RFC4180-ish CSV reader (comma, quoted fields, header row required).
    /// </summary>
    public static class SimpleCsv
    {
        public static List<Dictionary<string, string>> ReadRows(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV not found.", filePath ?? "(null)");
            }

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0)
            {
                throw new InvalidOperationException($"CSV empty: {filePath}");
            }

            var headers = ParseLine(lines[0]);
            if (headers.Count == 0)
            {
                throw new InvalidOperationException($"CSV has no header: {filePath}");
            }

            var rows = new List<Dictionary<string, string>>();
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var cells = ParseLine(line);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < headers.Count; c++)
                {
                    row[headers[c]] = c < cells.Count ? cells[c] : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        public static string Require(Dictionary<string, string> row, string column, string table, int rowIndex)
        {
            if (!row.TryGetValue(column, out var value))
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: missing column '{column}'.");
            }

            return value ?? string.Empty;
        }

        private static List<string> ParseLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    if (ch == '"')
                    {
                        inQuotes = true;
                    }
                    else if (ch == ',')
                    {
                        result.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
            }

            result.Add(sb.ToString());
            return result;
        }
    }
}
