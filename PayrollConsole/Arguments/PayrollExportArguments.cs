using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrollExportArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(PayrollExportArguments), 2);

    public static string TargetFileName =>
        ConsoleArguments.GetMember(typeof(PayrollExportArguments), 3);

    public static string OptionsFileName =>
        ConsoleArguments.GetMember(typeof(PayrollExportArguments), 4);

    public static string Namespace =>
        ConsoleArguments.GetMember(typeof(PayrollExportArguments), 5);

    public static Type[] Toggles => null;

    public static string TestArguments()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(TargetFileName))
        {
            return "Missing target file name";
        }
        return null;
    }
}