using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class TenantDeleteArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(TenantDeleteArguments), 2);

    public static ObjectDeleteMode ObjectDeleteMode(ObjectDeleteMode defaultValue = Shared.ObjectDeleteMode.Delete) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(ObjectDeleteMode)
    };

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;
}