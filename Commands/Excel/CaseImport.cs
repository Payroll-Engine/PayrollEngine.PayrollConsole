using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class CaseImport
{
    internal static List<CaseSet> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.Case))
        {
            return null;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.Case);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new CaseSheetColumns(worksheet);
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing case column {nameof(Case.Name)}.");
        }
        if (columns.CaseTypeColumn == null)
        {
            throw new PayrollException($"Missing case type colum {nameof(Case.CaseType)}.");
        }

        // cases
        var cases = new List<CaseSet>();
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
                throw new PayrollException($"Missing case name in row #{i}");
            }
            var existing = cases.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated case {name} in row #{i}");
            }

            // case type
            var caseType = row.GetEnumValue<CaseType>(columns.CaseTypeColumn);
            if (caseType == null)
            {
                throw new PayrollException($"Missing case type in row #{i}");
            }

            var caseSet = new CaseSet
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                CaseType = caseType.Value,
                Name = workbook.EnsureNamespace(name),
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                NameSynonyms = row.GetCellStringValues(columns.NameSynonymsColumn),
                Description = row.GetCellValue<string>(columns.DescriptionColumn),
                DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
                DefaultReason = row.GetCellValue<string>(columns.DefaultReasonColumn),
                DefaultReasonLocalizations = row.GetLocalizations(columns.DefaultReasonLocalizationsColumns),
                BaseCase = row.GetCellValue<string>(columns.BaseCaseColumn),
                BaseCaseFields = row.GetJsonObjectArray<CaseFieldReference>(columns.BaseCaseFieldsColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                CancellationType = row.GetEnumValue(columns.CancellationTypeColumn, CaseCancellationType.None),
                Hidden = row.GetCellValue<bool>(columns.HiddenColumn),
                AvailableExpression = row.GetCellValue<string>(columns.AvailableExpressionColumn),
                BuildExpression = row.GetCellValue<string>(columns.BuildExpressionColumn),
                ValidateExpression = row.GetCellValue<string>(columns.ValidateExpressionColumn),
                AvailableActions = row.GetCellStringValues(columns.AvailableActionsColumn),
                BuildActions = row.GetCellStringValues(columns.BuildActionsColumn),
                ValidateActions = row.GetCellStringValues(columns.ValidateActionsColumn),
                Clusters = row.GetCellStringValues(columns.ClustersColumn),
                Lookups = row.GetCellStringValues(columns.LookupsColumn),
                Slots = row.GetJsonObjectArray<CaseSlot>(columns.SlotsColumn),
                Attributes = row.GetAttributes(columns.AttributesColumn)
            };

            cases.Add(caseSet);
        }

        return cases;
    }
}