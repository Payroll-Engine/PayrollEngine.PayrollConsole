using System.Linq;
using System.Collections.Generic;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class WorkbookExtensions
{
    extension(ExcelWorkbook workbook)
    {
        internal string EnsureNamespace(string name)
        {
            var @namespace = workbook.GetNamedValue<string>(RegionNames.NamespaceRegionName);
            return string.IsNullOrWhiteSpace(@namespace) ? name : name.EnsureStart(@namespace.EnsureEnd("."));
        }

        internal T GetNamedValue<T>(string nameName)
        {
            var cell = workbook.GetNamedCell(nameName);
            return cell == null ? default : cell.GetCellValue<T>();
        }

        internal IList<ExcelSheet> GetSheetsOf(string mask) =>
            workbook.Sheets.Where(x => x.SheetName.StartsWith(mask)).ToList();

        internal bool HasSheet(string sheetName) =>
            workbook.GetSheet(sheetName) != null;
    }
}
