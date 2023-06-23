using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class DataReportArguments
{
    public static string OutputFile =>
        ConsoleArguments.GetMember(typeof(DataReportArguments), 2);

    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(DataReportArguments), 3);

    public static string User =>
        ConsoleArguments.GetMember(typeof(DataReportArguments), 4);

    public static string Regulation =>
        ConsoleArguments.GetMember(typeof(DataReportArguments), 5);

    public static string Report =>
        ConsoleArguments.GetMember(typeof(DataReportArguments), 6);

    public static string ParametersFile =>
        ConsoleArguments.GetMember(typeof(DataReportArguments), 7);

    public static Language Language(Language language = PayrollEngine.Language.English) =>
        ConsoleArguments.GetEnumToggle(language);

    public static ReportPostAction PostAction(ReportPostAction language = ReportPostAction.NoAction) =>
        ConsoleArguments.GetEnumToggle(language);

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