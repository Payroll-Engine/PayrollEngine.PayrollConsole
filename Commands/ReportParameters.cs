using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Document;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Report command parameters
/// </summary>
public class ReportParameters : ICommandParameters
{
    /// <summary>
    /// Default parameter file name
    /// </summary>
    public readonly string DefaultParameterFileName = "parameters.json";

    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// USer
    /// </summary>
    public string User { get; init; }

    /// <summary>
    /// Regulation
    /// </summary>
    public string Regulation { get; init; }

    /// <summary>
    /// Report
    /// </summary>
    public string Report { get; init; }

    /// <summary>
    /// Parameter file
    /// </summary>
    public string ParameterFile { get; init; }

    /// <summary>
    /// Culture
    /// </summary>
    public string Culture { get; init; }

    /// <summary>
    /// Target file
    /// </summary>
    public string TargetFile { get; init; }

    /// <summary>
    /// Document type (default: pdf)
    /// </summary>
    public DocumentType DocumentType { get; private init; } = DocumentType.Pdf;

    /// <summary>
    /// Report post action(default: none)
    /// </summary>
    public ReportPostAction PostAction { get; private init; } = ReportPostAction.NoAction;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(DocumentType),
        typeof(ReportPostAction)
    ];

    /// <inheritdoc />
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

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static ReportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            User = parser.Get(3, nameof(User)),
            Regulation = parser.Get(4, nameof(Regulation)),
            Report = parser.Get(5, nameof(Report)),
            ParameterFile = parser.GetByName(nameof(ParameterFile)),
            Culture = parser.GetByName(nameof(Culture)),
            TargetFile = parser.GetByName(nameof(TargetFile)),
            DocumentType = parser.GetEnumToggle(DocumentType.Pdf),
            PostAction = parser.GetEnumToggle(ReportPostAction.NoAction)
        };
}