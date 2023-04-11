using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ReportExecuteArguments
{
    public static string OutputFile =>
        ConsoleArguments.Get(2);

    public static string Tenant =>
        ConsoleArguments.Get(3);

    public static string User =>
        ConsoleArguments.Get(4);

    public static string Regulation =>
        ConsoleArguments.Get(5);

    public static string Report =>
        ConsoleArguments.Get(6);

    public static Language Language =>
        ConsoleArguments.GetEnum<Language>(7);

    public static string ParametersFile =>
        ConsoleArguments.Get(8);

    public static Type[] Toggles => null;

    public static string TestArguments()
    {
        if (string.IsNullOrWhiteSpace(OutputFile))
        {
            return "Missing output file";
        }
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(User))
        {
            return "Missing user";
        }
        if (string.IsNullOrWhiteSpace(Regulation))
        {
            return "Missing regulation";
        }
        if (string.IsNullOrWhiteSpace(Report))
        {
            return "Missing report";
        }
        return null;
    }
}