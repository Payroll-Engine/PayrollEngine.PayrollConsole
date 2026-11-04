using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands.Lex;

/// <summary>
/// Reads the text of a spreadsheet source document for the lex source verification
/// </summary>
internal static class LexWorkbookReader
{
    /// <summary>
    /// Readers by file extension, as expected by <see cref="PayrollEngine.Lex.LexSourceDocument"/>
    /// </summary>
    internal static IReadOnlyDictionary<string, Func<Stream, List<(string Heading, string Text)>>> Readers { get; } =
        new Dictionary<string, Func<Stream, List<(string Heading, string Text)>>>
        {
            [".xlsx"] = Read
        };

    /// <summary>
    /// Text of a spreadsheet (one section per sheet)
    /// </summary>
    internal static List<(string Heading, string Text)> Read(Stream stream)
    {
        var sections = new List<(string, string)>();
        using var workbook = new XSSFWorkbook(stream);
        for (var index = 0; index < workbook.NumberOfSheets; index++)
        {
            var sheet = workbook.GetSheetAt(index);
            var text = new StringBuilder();
            for (var rowIndex = sheet.FirstRowNum; rowIndex <= Math.Min(sheet.LastRowNum, sheet.FirstRowNum + 5000); rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;
                foreach (var cell in row.Cells)
                {
                    var type = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;
                    switch (type)
                    {
                        case CellType.Numeric:
                            text.Append(cell.NumericCellValue.ToString("0.######", CultureInfo.InvariantCulture)).Append(' ');
                            break;
                        case CellType.String:
                            text.Append(cell.StringCellValue).Append(' ');
                            break;
                    }
                }
                text.Append('\n');
            }
            sections.Add((sheet.SheetName, text.ToString()));
        }
        return sections;
    }
}
