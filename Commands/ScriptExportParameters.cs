using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Script;

namespace PayrollEngine.PayrollConsole.Commands;

public class ScriptExportParameters : ICommandParameters
{
    public string TargetFolder { get; init; }
    public string Tenant { get; init; }
    public string User { get; init; }
    public string Employee { get; init; }
    public string Payroll { get; init; }
    public string Regulation { get; init; }
    public string Namespace { get; init; }
    public ScriptExportMode ExportMode { get; private init; } = ScriptExportMode.Existing;
    public ScriptExportObject ScriptObject { get; private init; } = ScriptExportObject.All;

    public Type[] Toggles =>
    [
        typeof(ScriptExportMode)
    ];

    public string Test()
    {
        if (string.IsNullOrWhiteSpace(TargetFolder))
        {
            return "Missing target folder";
        }
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(User))
        {
            return "Missing user";
        }
        if (string.IsNullOrWhiteSpace(Employee))
        {
            return "Missing employee";
        }
        if (string.IsNullOrWhiteSpace(Payroll))
        {
            return "Missing payroll";
        }
        if (string.IsNullOrWhiteSpace(Regulation))
        {
            return "Missing regulation";
        }
        return null;
    }

    public static ScriptExportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            TargetFolder = parser.Get(2, nameof(TargetFolder)),
            Tenant = parser.Get(3, nameof(Tenant)),
            User = parser.Get(4, nameof(User)),
            Employee = parser.Get(5, nameof(Employee)),
            Payroll = parser.Get(6, nameof(Payroll)),
            Regulation = parser.Get(7, nameof(Regulation)),
            Namespace = parser.Get(8, nameof(Namespace)),
            ExportMode = parser.GetEnumToggle(ScriptExportMode.Existing),
            ScriptObject = parser.GetEnumToggle(ScriptExportObject.All)
        };
}