using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Imports <see cref="CaseRelation"/> objects from the case relation Excel worksheet.</summary>
internal static class CaseRelationImport
{
    /// <summary>Imports case relations from the <see cref="SheetSpecification.CaseRelation"/> sheet.</summary>
    /// <param name="workbook">The source workbook.</param>
    /// <returns>The imported case relations, or <c>null</c> if the sheet is missing or empty.</returns>
    /// <exception cref="PayrollException">Thrown when required columns are missing or case names are empty.</exception>
    internal static List<CaseRelation> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.CaseRelation))
        {
            return null;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.CaseRelation);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new CaseRelationSheetColumns(worksheet);
        if (columns.SourceCaseNameColumn == null)
        {
            throw new PayrollException($"Missing source case column {nameof(Case.Name)}.");
        }
        if (columns.TargetCaseNameColumn == null)
        {
            throw new PayrollException($"Missing target case column {nameof(Case.Name)}.");
        }

        // case relations
        var caseRelations = new List<CaseRelation>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // source case
            var sourceCaseName = row.GetCellValue<string>(columns.SourceCaseNameColumn);
            if (string.IsNullOrWhiteSpace(sourceCaseName))
            {
                throw new PayrollException($"Missing case name in row #{i}");
            }

            // target case
            var targetCaseName = row.GetCellValue<string>(columns.TargetCaseNameColumn);
            if (string.IsNullOrWhiteSpace(targetCaseName))
            {
                throw new PayrollException($"Missing case name in row #{i}");
            }

            var caseField = new CaseRelation
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                SourceCaseName = workbook.EnsureNamespace(sourceCaseName),
                SourceCaseNameLocalizations = row.GetLocalizations(columns.SourceCaseNameLocalizationsColumns),
                SourceCaseSlot = row.GetCellValue<string>(columns.SourceCaseSlotColumn),
                SourceCaseSlotLocalizations = row.GetLocalizations(columns.SourceCaseSlotLocalizationsColumns),
                TargetCaseName = workbook.EnsureNamespace(targetCaseName),
                TargetCaseNameLocalizations = row.GetLocalizations(columns.TargetCaseNameLocalizationsColumns),
                TargetCaseSlot = row.GetCellValue<string>(columns.TargetCaseSlotColumn),
                TargetCaseSlotLocalizations = row.GetLocalizations(columns.TargetCaseSlotLocalizationsColumns),
                BuildExpression = row.GetCellValue<string>(columns.BuildExpressionColumn),
                ValidateExpression = row.GetCellValue<string>(columns.ValidateExpressionColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                Order = row.GetCellValue<int>(columns.OrderColumn),
                BuildActions = row.GetCellStringValues(columns.BuildActionsColumn),
                ValidateActions = row.GetCellStringValues(columns.ValidateActionsColumn),
                Clusters = row.GetCellStringValues(columns.ClustersColumn),
                Attributes = row.GetAttributes(columns.AttributesColumn),
            };

            caseRelations.Add(caseField);
        }

        return caseRelations;
    }
}