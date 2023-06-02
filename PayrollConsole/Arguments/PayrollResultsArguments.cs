using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrollResultsArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(2);

    public static int TopFilter(int defaultFilter = 1) =>
        ConsoleArguments.GetInt(3, defaultFilter, nameof(TopFilter));

    public static ReportExportMode ResultExportMode(ReportExportMode defaultValue = ReportExportMode.NoExport) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(ReportExportMode)
    };

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;
}