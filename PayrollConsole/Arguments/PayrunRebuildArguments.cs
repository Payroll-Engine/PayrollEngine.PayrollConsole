using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunRebuildArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(2);

    public static string PayrunName =>
        ConsoleArguments.GetMember(3);

    public static Type[] Toggles => null;

    public static string TestArguments()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        return string.IsNullOrWhiteSpace(PayrunName) ? "Missing payrun name" : null;
    }
}