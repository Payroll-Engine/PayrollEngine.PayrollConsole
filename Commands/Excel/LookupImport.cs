using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class LookupImport
{
    internal static List<LookupSet> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.Lookup))
        {
            return null;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.Lookup);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new LookupSheetColumns(worksheet);
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing lookup column {nameof(Case.Name)}.");
        }

        // lookups
        var lookups = new List<LookupSet>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // name
            var name = row.GetCellValue<string>(columns.NameColumn);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PayrollException($"Missing report name in row #{i}");
            }
            var existing = lookups.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated report {name} in row #{i}");
            }

            var lookup = new LookupSet
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                Name = workbook.EnsureNamespace(name),
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                Description = row.GetCellValue<string>(columns.DescriptionColumn),
                DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
                RangeSize = row.GetCellValue<decimal?>(columns.RangeSizeColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                Attributes = row.GetAttributes(columns.AttributesColumn)
            };

            lookups.Add(lookup);
        }

        return lookups;
    }
}