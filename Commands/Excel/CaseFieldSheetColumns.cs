using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class CaseFieldSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? CaseColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int, string> DescriptionLocalizationsColumns { get; }
    internal int? ValueTypeColumn { get; }
    internal int? ValueScopeColumn { get; }
    internal int? TimeTypeColumn { get; }
    internal int? TimeUnitColumn { get; }
    internal int? PeriodAggregationColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? CancellationModeColumn { get; }
    internal int? ValueCreationModeColumn { get; }
    internal int? CultureColumn { get; }
    internal int? ValueMandatoryColumn { get; }
    internal int? OrderColumn { get; }
    internal int? StartDateTypeColumn { get; }
    internal int? EndDateTypeColumn { get; }
    internal int? EndMandatoryColumn { get; }
    internal int? DefaultStartColumn { get; }
    internal int? DefaultEndColumn { get; }
    internal int? DefaultValueColumn { get; }
    internal int? TagsColumn { get; }
    internal int? LookupSettingsColumn { get; }
    internal int? ClustersColumn { get; }
    internal int? BuildActionsColumn { get; }
    internal int? ValidateActionsColumn { get; }
    internal int? AttributesColumn { get; }
    internal int? ValueAttributesColumn { get; }

    internal CaseFieldSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(CaseField.Created));
        CaseColumn = sheet.GetHeaderColumn(SheetSpecification.CaseFieldCaseRefName);
        NameColumn = sheet.GetHeaderColumn(nameof(CaseField.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(CaseField.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(CaseField.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(CaseField.Description));
        ValueTypeColumn = sheet.GetHeaderColumn(nameof(CaseField.ValueType));
        ValueScopeColumn = sheet.GetHeaderColumn(nameof(CaseField.ValueScope));
        TimeTypeColumn = sheet.GetHeaderColumn(nameof(CaseField.TimeType));
        TimeUnitColumn = sheet.GetHeaderColumn(nameof(CaseField.TimeUnit));
        PeriodAggregationColumn = sheet.GetHeaderColumn(nameof(CaseField.PeriodAggregation));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(CaseField.OverrideType));
        CancellationModeColumn = sheet.GetHeaderColumn(nameof(CaseField.CancellationMode));
        ValueCreationModeColumn = sheet.GetHeaderColumn(nameof(CaseField.ValueCreationMode));
        CultureColumn = sheet.GetHeaderColumn(nameof(CaseField.Culture));
        ValueMandatoryColumn = sheet.GetHeaderColumn(nameof(CaseField.ValueMandatory));
        OrderColumn = sheet.GetHeaderColumn(nameof(CaseField.Order));
        StartDateTypeColumn = sheet.GetHeaderColumn(nameof(CaseField.StartDateType));
        EndDateTypeColumn = sheet.GetHeaderColumn(nameof(CaseField.EndDateType));
        EndMandatoryColumn = sheet.GetHeaderColumn(nameof(CaseField.EndMandatory));
        DefaultStartColumn = sheet.GetHeaderColumn(nameof(CaseField.DefaultStart));
        DefaultEndColumn = sheet.GetHeaderColumn(nameof(CaseField.DefaultEnd));
        DefaultValueColumn = sheet.GetHeaderColumn(nameof(CaseField.DefaultValue));
        TagsColumn = sheet.GetHeaderColumn(nameof(CaseField.Tags));
        LookupSettingsColumn = sheet.GetHeaderColumn(nameof(CaseField.LookupSettings));
        ClustersColumn = sheet.GetHeaderColumn(nameof(CaseField.Clusters));
        BuildActionsColumn = sheet.GetHeaderColumn(nameof(CaseField.BuildActions));
        ValidateActionsColumn = sheet.GetHeaderColumn(nameof(CaseField.ValidateActions));
        AttributesColumn = sheet.GetHeaderColumn(nameof(CaseField.Attributes));
        ValueAttributesColumn = sheet.GetHeaderColumn(nameof(CaseField.ValueAttributes));
    }
}