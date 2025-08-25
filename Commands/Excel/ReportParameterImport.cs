using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class ReportParameterImport
{
    internal static void Import(IWorkbook workbook, List<ReportSet> reports)
    {
        // case worksheet
        if (!workbook.HasSheet(SheetSpecification.ReportParameter))
        {
            return;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.ReportParameter);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return;
        }

        // columns
        var columns = new ReportParameterSheetColumns(worksheet);
        if (columns.ReportColumn == null)
        {
            throw new PayrollException($"Missing report column {SheetSpecification.ReportRefName}.");
        }
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing report parameter column {nameof(Case.Name)}.");
        }

        // report parameters
        var parameters = new List<ReportParameter>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // report
            var reportName = row.GetCellValue<string>(columns.ReportColumn);
            if (string.IsNullOrWhiteSpace(reportName))
            {
                throw new PayrollException($"Missing report name in row #{i}");
            }
            reportName = workbook.EnsureNamespace(reportName);
            var report = reports.FirstOrDefault(x => string.Equals(x.Name, reportName));
            if (report == null)
            {
                throw new PayrollException($"Unknown report {reportName} in row #{i}");
            }

            // name
            var name = row.GetCellValue<string>(columns.NameColumn);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PayrollException($"Missing report parameter name in row #{i}");
            }
            var existing = parameters.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated report parameter {name} in row #{i}");
            }

            var parameter = new ReportParameter
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),

                Name = name,
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                Description = row.GetCellValue<string>(columns.DescriptionColumn),
                DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
                Mandatory = row.GetCellValue<bool>(columns.MandatoryColumn),
                Hidden = row.GetCellValue<bool>(columns.HiddenColumn),
                Value = row.GetCellValue<string>(columns.ValueColumn),
                ValueType = row.GetEnumValue(columns.ValueTypeColumn, ValueType.String),
                ParameterType = row.GetEnumValue(columns.ParameterTypeColumn, ReportParameterType.Value),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                Attributes = row.GetAttributes(columns.AttributesColumn),
            };

            // unique check
            parameters.Add(parameter);

            // case
            report.Parameters ??= [];
            report.Parameters.Add(parameter);
        }
    }
}