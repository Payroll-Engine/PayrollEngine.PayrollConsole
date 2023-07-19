using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ChangePasswordArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(PayrunRebuildArguments), 2);

    public static string User =>
        ConsoleArguments.GetMember(typeof(PayrunRebuildArguments), 3);

    public static string NewPassword =>
        ConsoleArguments.GetMember(typeof(PayrunRebuildArguments), 4);

    public static string ExistingPassword =>
        ConsoleArguments.GetMember(typeof(PayrunRebuildArguments), 5);

    public static Type[] Toggles => null;

    public static string TestArguments()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(User))
        {
            return "Missing user";
        }
        return string.IsNullOrWhiteSpace(NewPassword) ? "Missing new password" : null;
    }
}