using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class WorkbookExtensions
{
    extension(IWorkbook workbook)
    {
        internal string EnsureNamespace(string name)
        {
            var @namespace = workbook.GetNamedValue<string>(RegionNames.NamespaceRegionName);
            return string.IsNullOrWhiteSpace(@namespace) ? name : name.EnsureStart(@namespace.EnsureEnd("."));
        }

        internal T GetNamedValue<T>(string nameName)
        {
            var names = workbook.GetAllNames();
            var name = names.FirstOrDefault(x => string.Equals(x.NameName, nameName));
            if (name == null)
            {
                return default;
            }
            return workbook.GetCellValue<T>(name.RefersToFormula);
        }

        internal IList<ISheet> GetSheetsOf(string mask) =>
            workbook.GetSheets().Where(x => x.SheetName.StartsWith(mask)).ToList();

        private IList<ISheet> GetSheets()
        {
            var sheets = new List<ISheet>();
            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                sheets.Add(workbook.GetSheetAt(i));
            }
            return sheets;
        }

        internal bool HasSheet(string sheetName) =>
            workbook.GetSheets().Any(x => string.Equals(x.SheetName, sheetName));

        private T GetCellValue<T>(string address)
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
}