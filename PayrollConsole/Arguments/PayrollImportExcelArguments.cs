using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrollImportExcelArguments
{
    public static string FileName =>
        ConsoleArguments.GetMember(typeof(PayrollImportExcelArguments), 2);

    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(PayrollImportExcelArguments), 3);

    public static DataImportMode DataImportMode(DataImportMode defaultValue = Client.Exchange.DataImportMode.Single) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(DataImportMode)
    };

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing tenant" : null;
}