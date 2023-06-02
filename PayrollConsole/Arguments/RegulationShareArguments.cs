using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class RegulationShareArguments
{
    public static string ProviderTenant =>
        ConsoleArguments.GetMember(2);

    public static string ProviderRegulation =>
        ConsoleArguments.GetMember(3);

    public static string ConsumerTenant =>
        ConsoleArguments.GetMember(4);

    public static string ConsumerDivision =>
        ConsoleArguments.GetMember(5);

    public static ShareMode ShareMode(ShareMode defaultValue = Shared.ShareMode.View) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(ShareMode)
    };
}