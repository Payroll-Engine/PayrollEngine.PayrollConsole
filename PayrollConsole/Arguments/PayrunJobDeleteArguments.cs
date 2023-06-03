using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunJobDeleteArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(PayrunJobDeleteArguments), 2);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;
}