using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class ScriptImport
{
    internal static List<Client.Model.Script> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.Script))
        {
            return null;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.Script);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new ScriptSheetColumns(worksheet);
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing script name column {nameof(Case.Name)}.");
        }

        // scripts
        var scripts = new List<Client.Model.Script>();
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
                throw new PayrollException($"Missing script name in row #{i}");
            }
            var existing = scripts.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated script {name} in row #{i}");
            }

            var script = new Client.Model.Script
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                Name = workbook.EnsureNamespace(name),
                FunctionTypes = row.GetEnumArrayValue<FunctionType>(columns.FunctionTypesColumn),
                Value = row.GetCellValue<string>(columns.ValueColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active)
            };

            scripts.Add(script);
        }

        return scripts;
    }
}