using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class CaseRelationSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? SourceCaseNameColumn { get; }
    internal Dictionary<int, string> SourceCaseNameLocalizationsColumns { get; }
    internal int? SourceCaseSlotColumn { get; }
    internal Dictionary<int, string> SourceCaseSlotLocalizationsColumns { get; }
    internal int? TargetCaseNameColumn { get; }
    internal Dictionary<int, string> TargetCaseNameLocalizationsColumns { get; }
    internal int? TargetCaseSlotColumn { get; }
    internal Dictionary<int, string> TargetCaseSlotLocalizationsColumns { get; }
    internal int? BuildExpressionColumn { get; }
    internal int? ValidateExpressionColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? OrderColumn { get; }
    internal int? BuildActionsColumn { get; }
    internal int? ValidateActionsColumn { get; }
    internal int? ClustersColumn { get; }
    internal int? AttributesColumn { get; }

    internal CaseRelationSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(CaseRelation.Created));
        SourceCaseNameColumn = sheet.GetHeaderColumn(nameof(CaseRelation.SourceCaseName));
        SourceCaseNameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(CaseRelation.SourceCaseName));
        SourceCaseSlotColumn = sheet.GetHeaderColumn(nameof(CaseRelation.SourceCaseSlot));
        SourceCaseSlotLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(CaseRelation.SourceCaseSlot));
        TargetCaseNameColumn = sheet.GetHeaderColumn(nameof(CaseRelation.TargetCaseName));
        TargetCaseNameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(CaseRelation.TargetCaseName));
        TargetCaseSlotColumn = sheet.GetHeaderColumn(nameof(CaseRelation.TargetCaseSlot));
        TargetCaseSlotLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(CaseRelation.TargetCaseSlot));
        BuildExpressionColumn = sheet.GetHeaderColumn(nameof(CaseRelation.BuildExpression));
        ValidateExpressionColumn = sheet.GetHeaderColumn(nameof(CaseRelation.ValidateExpression));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(CaseRelation.OverrideType));
        OrderColumn = sheet.GetHeaderColumn(nameof(CaseRelation.Order));
        BuildActionsColumn = sheet.GetHeaderColumn(nameof(CaseRelation.BuildActions));
        ValidateActionsColumn = sheet.GetHeaderColumn(nameof(CaseRelation.ValidateActions));
        ClustersColumn = sheet.GetHeaderColumn(nameof(CaseRelation.Clusters));
        AttributesColumn = sheet.GetHeaderColumn(nameof(CaseRelation.Attributes));
    }
}