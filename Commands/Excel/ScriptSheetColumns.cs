using System;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class ScriptSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? NameColumn { get; }
    internal int? FunctionTypesColumn { get; }
    internal int? ValueColumn { get; }
    internal int? OverrideTypeColumn { get; }

    internal ScriptSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(Client.Model.Script.Created));
        NameColumn = sheet.GetHeaderColumn(nameof(Client.Model.Script.Name));
        FunctionTypesColumn = sheet.GetHeaderColumn(nameof(Client.Model.Script.FunctionTypes));
        ValueColumn = sheet.GetHeaderColumn(nameof(Client.Model.Script.Value));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(CaseRelation.OverrideType));
    }
}