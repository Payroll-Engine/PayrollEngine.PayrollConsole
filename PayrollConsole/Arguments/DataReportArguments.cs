using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class DataReportArguments
{
    public static string OutputFile =>
        ConsoleArguments.GetMember(2);

    public static string Tenant =>
        ConsoleArguments.GetMember(3);

    public static string User =>
        ConsoleArguments.GetMember(4);

    public static string Regulation =>
        ConsoleArguments.GetMember(5);

    public static string Report =>
        ConsoleArguments.GetMember(6);

    public static Language Language =>
        ConsoleArguments.GetEnum<Language>(7);

    public static string ParametersFile =>
        ConsoleArguments.GetMember(8);

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