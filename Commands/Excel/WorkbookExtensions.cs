using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

public static class WorkbookExtensions
{
    public static T GetNamedValue<T>(this IWorkbook workbook, string nameName)
    {
        var names = workbook.GetAllNames();
        var name = names.FirstOrDefault(x => string.Equals(x.NameName, nameName));
        if (name == null)
        {
            return default;
        }
        return workbook.GetCellValue<T>(name.RefersToFormula);
    }

    public static IList<ISheet> GetSheetsOf(this IWorkbook workbook, string mask) =>
        workbook.GetSheets().Where(x => x.SheetName.StartsWith(mask)).ToList();

    private static IList<ISheet> GetSheets(this IWorkbook workbook)
    {
        var sheets = new List<ISheet>();
        for (var i = 0; i < workbook.NumberOfSheets; i++)
        {
            sheets.Add(workbook.GetSheetAt(i));
        }
        return sheets;
    }

    public static bool HasSheet(this IWorkbook workbook, string sheetName) =>
        workbook.GetSheets().Any(x => string.Equals(x.SheetName, sheetName));

    private static T GetCellValue<T>(this IWorkbook workbook, string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Invalid cell address.", nameof(address));
        }

        var sheetIndex = address.IndexOf("!", StringComparison.Ordinal);
        if (sheetIndex < 0)
        {
            // missing sheet reference
            return default;
        }

        var sheetName = address.Substring(0, sheetIndex);
        var sheet = workbook.GetSheet(sheetName);
        return sheet == null ? default : sheet.GetCellValue<T>(address);
    }

}