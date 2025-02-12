using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Document;

namespace PayrollEngine.PayrollConsole.Commands;

public class ReportParameters : ICommandParameters
{
    public readonly string DefaultParameterFileName = "parameters.json";

    public string Tenant { get; init; }
    public string User { get; init; }
    public string Regulation { get; init; }
    public string Report { get; init; }
    public string ParameterFile { get; init; }
    public string Culture { get; init; }
    public string TargetFile { get; init; }
    public DocumentType DocumentType { get; private init; } = DocumentType.Pdf;
    public ReportPostAction PostAction { get; private init; } = ReportPostAction.NoAction;

    public Type[] Toggles =>
    [
        typeof(DocumentType),
        typeof(ReportPostAction)
    ];

    public string Test()
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

    public static ReportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            User = parser.Get(3, nameof(User)),
            Regulation = parser.Get(4, nameof(Regulation)),
            Report = parser.Get(5, nameof(Report)),
            ParameterFile = parser.Get(6, nameof(ParameterFile)),
            Culture = parser.Get(7, nameof(Culture)),
            TargetFile = parser.Get(8, nameof(TargetFile)),
            DocumentType = parser.GetEnumToggle(DocumentType.Pdf),
            PostAction = parser.GetEnumToggle(ReportPostAction.NoAction)
        };
}