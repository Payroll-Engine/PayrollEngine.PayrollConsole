using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class RegulationShareArguments
{
    public static string ProviderTenant =>
        ConsoleArguments.GetMember(typeof(RegulationShareArguments), 2);

    public static string ProviderRegulation =>
        ConsoleArguments.GetMember(typeof(RegulationShareArguments), 3);

    public static string ConsumerTenant =>
        ConsoleArguments.GetMember(typeof(RegulationShareArguments), 4);

    public static string ConsumerDivision =>
        ConsoleArguments.GetMember(typeof(RegulationShareArguments), 5);

    public static ShareMode ShareMode(ShareMode defaultValue = Shared.ShareMode.View) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(ShareMode)
    };
}