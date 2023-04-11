using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunJobDeleteArguments
{
    public static string Tenant =>
        ConsoleArguments.Get(2);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;
}