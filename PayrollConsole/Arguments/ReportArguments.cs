using System;
using PayrollEngine.Client;
using PayrollEngine.Document;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ReportArguments
{
    public static readonly string DefaultParameterFileName = "parameters.json";

    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 2);

    public static string User =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 3);

    public static string Regulation =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 4);

    public static string Report =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 5);

    public static string ParameterFile =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 6);

    public static string Culture =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 7);

    public static string TargetFile =>
        ConsoleArguments.GetMember(typeof(ReportArguments), 8);

    public static DocumentType DocumentType(DocumentType defaultTestMode = Document.DocumentType.Pdf) =>
        ConsoleArguments.GetEnumToggle(defaultTestMode);

    public static ReportPostAction PostAction(ReportPostAction action = ReportPostAction.NoAction) =>
        ConsoleArguments.GetEnumToggle(action);

    public static Type[] Toggles => new[]
    {
        typeof(DocumentType),
        typeof(ReportPostAction)
    };

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