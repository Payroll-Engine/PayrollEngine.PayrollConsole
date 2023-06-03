using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ActionReportArguments
{
    public static string FileName =>
        ConsoleArguments.GetMember(typeof(ActionReportArguments), 2);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing file name" : null;
}