using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrollExportArguments
{
    public static string Tenant =>
        ConsoleArguments.Get(2);

    public static string FileName =>
        ConsoleArguments.Get(3);

    public static string Namespace =>
        ConsoleArguments.Get(4);

    public static ResultExportMode ResultExportMode(ResultExportMode defaultValue = Client.Exchange.ResultExportMode.NoResults) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(ResultExportMode)
    };

    public static string TestArguments()
    {
        return string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;
    }
}