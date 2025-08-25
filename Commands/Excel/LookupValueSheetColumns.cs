using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class LookupValueSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? LookupColumn { get; }

    internal int? KeyColumn { get; }
    internal int? ValueColumn { get; }
    internal Dictionary<int, string> ValueLocalizationsColumns { get; }
    internal int? RangeValueColumn { get; }
    internal int? OverrideTypeColumn { get; }

    internal Dictionary<int,string> KeyColumns { get; }
    internal Dictionary<int,string> ValueColumns { get; }
    internal int? RangeColumn { get; }

    internal LookupValueSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }

        CreatedColumn = sheet.GetHeaderColumn(nameof(LookupValue.Created));
        LookupColumn = sheet.GetHeaderColumn(SheetSpecification.LookupRefName);
        KeyColumn = sheet.GetHeaderColumn(nameof(LookupValue.Key));
        ValueColumn = sheet.GetHeaderColumn(nameof(LookupValue.Value));
        ValueLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(LookupValue.Value));
        RangeValueColumn = sheet.GetHeaderColumn(nameof(LookupValue.RangeValue));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(CaseRelation.OverrideType));

        KeyColumns = sheet.GetHeaderMultiColumns(nameof(LookupValue.Key), dotSeparator: false);
        ValueColumns = sheet.GetHeaderMultiColumns(nameof(LookupValue.Value));
        if (!ValueColumns.Any() && ValueColumn != null)
        {
            ValueColumns.Add(ValueColumn.Value, string.Empty);
        }
        RangeColumn = sheet.GetHeaderColumn(LookupSheetSpecification.Range);
    }
}