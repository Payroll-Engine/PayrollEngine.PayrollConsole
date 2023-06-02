using System;
using PayrollEngine.Client;
using PayrollEngine.Document;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ReportArguments
{
    public static readonly string DefaultParameterFileName = "parameters.json";

    public static string Tenant =>
        ConsoleArguments.GetMember(2);

    public static string User =>
        ConsoleArguments.GetMember(3);

    public static string Regulation =>
        ConsoleArguments.GetMember(4);

    public static string Report =>
        ConsoleArguments.GetMember(5);

    public static string ParameterFile =>
        ConsoleArguments.GetMember(6);

    public static Language Language(Language language = PayrollEngine.Language.English) =>
        ConsoleArguments.GetEnumToggle(language);

    public static DocumentType DocumentType(DocumentType defaultTestMode = Document.DocumentType.Pdf) =>
        ConsoleArguments.GetEnumToggle(defaultTestMode);

    public static Type[] Toggles => new[]
    {
        typeof(Language),
        typeof(DocumentType)
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