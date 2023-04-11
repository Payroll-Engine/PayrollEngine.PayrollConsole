using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class RegulationPermissionArguments
{
    public static string Tenant =>
        ConsoleArguments.Get(2);

    public static string Regulation =>
        ConsoleArguments.Get(3);

    public static string PermissionTenant =>
        ConsoleArguments.Get(4);

    public static string PermissionDivision =>
        ConsoleArguments.Get(5);

    public static PermissionMode PermissionMode(PermissionMode defaultValue = Shared.PermissionMode.View) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(PermissionMode)
    };
}