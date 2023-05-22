using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class RegulationShareArguments
{
    public static string ProviderTenant =>
        ConsoleArguments.Get(2);

    public static string ProviderRegulation =>
        ConsoleArguments.Get(3);

    public static string ConsumerTenant =>
        ConsoleArguments.Get(4);

    public static string ConsumerDivision =>
        ConsoleArguments.Get(5);

    public static ShareMode ShareMode(ShareMode defaultValue = Shared.ShareMode.View) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(ShareMode)
    };
}