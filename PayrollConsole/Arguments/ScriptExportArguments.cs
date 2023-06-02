using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Script;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ScriptExportArguments
{
    public static string TargetFolder =>
        ConsoleArguments.GetMember(2);

    public static string Tenant =>
        ConsoleArguments.GetMember(3);

    public static string User =>
        ConsoleArguments.GetMember(4);

    public static string Employee =>
        ConsoleArguments.GetMember(5);

    public static string Payroll =>
        ConsoleArguments.GetMember(6);

    public static string Regulation =>
        ConsoleArguments.GetMember(7);

    public static string Namespace =>
        ConsoleArguments.GetMember(8);

    public static ScriptExportMode ScriptExportMode(ScriptExportMode exportMode = Client.Script.ScriptExportMode.Existing) =>
        ConsoleArguments.GetEnumToggle(exportMode);

    public static ScriptExportObject ScriptObject() =>
        ConsoleArguments.GetEnumToggle(ScriptExportObject.All);

    public static Type[] Toggles => new[]
    {
        typeof(ScriptExportMode),
        typeof(ObjectDeleteMode)
    };

    public static string TestArguments()
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
}