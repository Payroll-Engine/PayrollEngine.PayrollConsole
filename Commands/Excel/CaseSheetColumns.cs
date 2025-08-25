using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class CaseSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? CaseTypeColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? NameSynonymsColumn { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int, string> DescriptionLocalizationsColumns { get; }
    internal int? DefaultReasonColumn { get; }
    internal Dictionary<int, string> DefaultReasonLocalizationsColumns { get; }
    internal int? BaseCaseColumn { get; }
    internal int? BaseCaseFieldsColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? CancellationTypeColumn { get; }
    internal int? HiddenColumn { get; }
    internal int? AvailableExpressionColumn { get; }
    internal int? BuildExpressionColumn { get; }
    internal int? ValidateExpressionColumn { get; }
    internal int? AvailableActionsColumn { get; }
    internal int? BuildActionsColumn { get; }
    internal int? ValidateActionsColumn { get; }
    internal int? ClustersColumn { get; }
    internal int? LookupsColumn { get; }
    internal int? SlotsColumn { get; }
    internal int? AttributesColumn { get; }

    internal CaseSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(Case.Created));
        CaseTypeColumn = sheet.GetHeaderColumn(nameof(Case.CaseType));
        NameColumn = sheet.GetHeaderColumn(nameof(Case.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Case.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(Case.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Case.Description));
        DefaultReasonColumn = sheet.GetHeaderColumn(nameof(Case.DefaultReason));
        DefaultReasonLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Case.DefaultReason));
        NameSynonymsColumn = sheet.GetHeaderColumn(nameof(Case.NameSynonyms));
        BaseCaseColumn = sheet.GetHeaderColumn(nameof(Case.BaseCase));
        BaseCaseFieldsColumn = sheet.GetHeaderColumn(nameof(Case.BaseCaseFields));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(Case.OverrideType));
        CancellationTypeColumn = sheet.GetHeaderColumn(nameof(Case.CancellationType));
        HiddenColumn = sheet.GetHeaderColumn(nameof(Case.Hidden));
        AvailableExpressionColumn = sheet.GetHeaderColumn(nameof(Case.AvailableExpression));
        BuildExpressionColumn = sheet.GetHeaderColumn(nameof(Case.BuildExpression));
        ValidateExpressionColumn = sheet.GetHeaderColumn(nameof(Case.ValidateExpression));
        AvailableActionsColumn = sheet.GetHeaderColumn(nameof(Case.AvailableActions));
        BuildActionsColumn = sheet.GetHeaderColumn(nameof(Case.BuildActions));
        ValidateActionsColumn = sheet.GetHeaderColumn(nameof(Case.ValidateActions));
        ClustersColumn = sheet.GetHeaderColumn(nameof(Case.Clusters));
        LookupsColumn = sheet.GetHeaderColumn(nameof(Case.Lookups));
        SlotsColumn = sheet.GetHeaderColumn(nameof(Case.Slots));
        AttributesColumn = sheet.GetHeaderColumn(nameof(Case.Attributes));
    }
}