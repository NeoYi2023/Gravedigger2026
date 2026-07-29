#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Gravedigger2026.Editor.Config
{
    /// <summary>
    /// Minimal Open XML (.xlsx) reader: first worksheet → rectangular string grid.
    /// Zero third-party packages (SPEC_04 §14.4 Approach A).
    /// </summary>
    public static class XlsxSheetReader
    {
        private static readonly XNamespace SsMl =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private static readonly XNamespace PkgRel =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        private static readonly XNamespace OdRel =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        /// <summary>
        /// Reads the first worksheet as rows of cell strings (empty cells = "").
        /// </summary>
        public static List<string[]> ReadFirstSheet(string xlsxPath)
        {
            if (string.IsNullOrEmpty(xlsxPath) || !File.Exists(xlsxPath))
            {
                throw new FileNotFoundException("xlsx not found.", xlsxPath ?? "(null)");
            }

            using (var zip = ZipFile.OpenRead(xlsxPath))
            {
                var sharedStrings = ReadSharedStrings(zip);
                var sheetEntry = ResolveFirstWorksheetEntry(zip);
                using (var stream = sheetEntry.Open())
                {
                    var doc = XDocument.Load(stream);
                    return ParseSheetData(doc, sharedStrings);
                }
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return new List<string>();
            }

            using (var stream = entry.Open())
            {
                var doc = XDocument.Load(stream);
                var list = new List<string>();
                foreach (var si in doc.Root.Elements(SsMl + "si"))
                {
                    list.Add(ReadSharedStringItem(si));
                }

                return list;
            }
        }

        private static string ReadSharedStringItem(XElement si)
        {
            var direct = si.Element(SsMl + "t");
            if (direct != null)
            {
                return direct.Value ?? string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var t in si.Descendants(SsMl + "t"))
            {
                sb.Append(t.Value);
            }

            return sb.ToString();
        }

        private static ZipArchiveEntry ResolveFirstWorksheetEntry(ZipArchive zip)
        {
            var workbookEntry = zip.GetEntry("xl/workbook.xml")
                ?? throw new InvalidOperationException("xlsx missing xl/workbook.xml");

            string firstRid;
            using (var wbStream = workbookEntry.Open())
            {
                var wb = XDocument.Load(wbStream);
                var firstSheet = wb.Root?
                    .Element(SsMl + "sheets")?
                    .Elements(SsMl + "sheet")
                    .FirstOrDefault();
                if (firstSheet == null)
                {
                    throw new InvalidOperationException("xlsx workbook has no sheets");
                }

                firstRid = (string)firstSheet.Attribute(OdRel + "id");
                if (string.IsNullOrEmpty(firstRid))
                {
                    throw new InvalidOperationException("xlsx first sheet missing r:id");
                }
            }

            var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels")
                ?? throw new InvalidOperationException("xlsx missing workbook.xml.rels");

            string target;
            using (var relStream = relsEntry.Open())
            {
                var rels = XDocument.Load(relStream);
                var rel = rels.Root?
                    .Elements(PkgRel + "Relationship")
                    .FirstOrDefault(r => (string)r.Attribute("Id") == firstRid);
                target = (string)rel?.Attribute("Target");
                if (string.IsNullOrEmpty(target))
                {
                    throw new InvalidOperationException(
                        $"xlsx worksheet relationship '{firstRid}' not found");
                }
            }

            // Target is typically "worksheets/sheet1.xml" relative to xl/
            var normalized = target.Replace('\\', '/').TrimStart('/');
            if (normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(3);
            }

            var sheetPath = "xl/" + normalized;
            var sheetEntry = zip.GetEntry(sheetPath)
                ?? throw new InvalidOperationException($"xlsx missing sheet entry: {sheetPath}");
            return sheetEntry;
        }

        private static List<string[]> ParseSheetData(XDocument sheetDoc, List<string> sharedStrings)
        {
            var sheetData = sheetDoc.Root?.Element(SsMl + "sheetData");
            if (sheetData == null)
            {
                throw new InvalidOperationException("xlsx worksheet missing sheetData");
            }

            var sparse = new Dictionary<(int Row, int Col), string>();
            var maxRow = -1;
            var maxCol = -1;

            foreach (var rowEl in sheetData.Elements(SsMl + "row"))
            {
                foreach (var cellEl in rowEl.Elements(SsMl + "c"))
                {
                    var refer = (string)cellEl.Attribute("r");
                    if (string.IsNullOrEmpty(refer) || !TryParseCellRef(refer, out var col, out var row))
                    {
                        continue;
                    }

                    var value = ReadCellValue(cellEl, sharedStrings);
                    sparse[(row, col)] = value;
                    if (row > maxRow)
                    {
                        maxRow = row;
                    }

                    if (col > maxCol)
                    {
                        maxCol = col;
                    }
                }
            }

            if (maxRow < 0 || maxCol < 0)
            {
                return new List<string[]>();
            }

            var result = new List<string[]>(maxRow + 1);
            for (var r = 0; r <= maxRow; r++)
            {
                var line = new string[maxCol + 1];
                for (var c = 0; c <= maxCol; c++)
                {
                    line[c] = sparse.TryGetValue((r, c), out var v) ? v : string.Empty;
                }

                result.Add(line);
            }

            return result;
        }

        private static string ReadCellValue(XElement cellEl, List<string> sharedStrings)
        {
            var type = (string)cellEl.Attribute("t");
            if (type == "s")
            {
                var idxText = cellEl.Element(SsMl + "v")?.Value;
                if (string.IsNullOrEmpty(idxText)
                    || !int.TryParse(idxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
                    || idx < 0
                    || idx >= sharedStrings.Count)
                {
                    return string.Empty;
                }

                return sharedStrings[idx] ?? string.Empty;
            }

            if (type == "inlineStr")
            {
                var isEl = cellEl.Element(SsMl + "is");
                if (isEl == null)
                {
                    return string.Empty;
                }

                var t = isEl.Element(SsMl + "t");
                if (t != null)
                {
                    return t.Value ?? string.Empty;
                }

                var sb = new StringBuilder();
                foreach (var te in isEl.Descendants(SsMl + "t"))
                {
                    sb.Append(te.Value);
                }

                return sb.ToString();
            }

            // t="str" / t="b" / numeric / default: use <v>
            var vEl = cellEl.Element(SsMl + "v");
            if (vEl == null)
            {
                return string.Empty;
            }

            var raw = vEl.Value ?? string.Empty;
            if (type == "b")
            {
                return raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                    ? "TRUE"
                    : "FALSE";
            }

            // Normalize integer-valued doubles so CSV matches designer integers (e.g. 80 not 80.0).
            if (string.IsNullOrEmpty(type)
                && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                && !double.IsNaN(number)
                && !double.IsInfinity(number)
                && Math.Abs(number - Math.Round(number)) < 1e-9
                && Math.Abs(number) < 1e15)
            {
                return ((long)Math.Round(number)).ToString(CultureInfo.InvariantCulture);
            }

            return raw;
        }

        /// <summary>
        /// Parses A1-style refs to 0-based column/row. Supports AA1 etc.
        /// </summary>
        public static bool TryParseCellRef(string cellRef, out int col, out int row)
        {
            col = 0;
            row = 0;
            if (string.IsNullOrEmpty(cellRef))
            {
                return false;
            }

            var i = 0;
            var colAcc = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
            {
                colAcc = colAcc * 26 + (char.ToUpperInvariant(cellRef[i]) - 'A' + 1);
                i++;
            }

            if (colAcc == 0 || i >= cellRef.Length)
            {
                return false;
            }

            if (!int.TryParse(cellRef.Substring(i), NumberStyles.Integer, CultureInfo.InvariantCulture, out var row1)
                || row1 < 1)
            {
                return false;
            }

            col = colAcc - 1;
            row = row1 - 1;
            return true;
        }
    }
}
#endif
