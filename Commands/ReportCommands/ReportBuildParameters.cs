using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ReportCommands;

/// <summary>Report build command parameters</summary>
public class ReportBuildParameters : ICommandParameters
{
    /// <summary>Default parameter file name</summary>
    public readonly string DefaultParameterFileName = "parameters.json";

    /// <summary>Tenant</summary>
    public string Tenant { get; init; }

    /// <summary>User</summary>
    public string User { get; init; }

    /// <summary>Regulation</summary>
    public string Regulation { get; init; }

    /// <summary>Report</summary>
    public string Report { get; init; }

    /// <summary>
    /// Existing FastReport template file (.frx).
    /// When provided, the command updates the Dictionary section of this template
    /// and writes the result to the target file (CI mode).
    /// When omitted, a new skeleton .frx is generated.
    /// </summary>
    public string TemplateFile { get; init; }

    /// <summary>Parameter file</summary>
    public string ParameterFile { get; init; }

    /// <summary>Culture</summary>
    public string Culture { get; init; }

    /// <summary>Target file</summary>
    public string TargetFile { get; init; }

    /// <summary>Report post action (default: none)</summary>
    public ReportPostAction PostAction { get; private init; } = ReportPostAction.NoAction;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
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

    /// <summary>Parse command parameters</summary>
    /// <param name="parser">Parameter parser</param>
    public static ReportBuildParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            User = parser.Get(3, nameof(User)),
            Regulation = parser.Get(4, nameof(Regulation)),
            Report = parser.Get(5, nameof(Report)),
            TemplateFile = parser.GetByName(nameof(TemplateFile)),
            ParameterFile = parser.GetByName(nameof(ParameterFile)),
            Culture = parser.GetByName(nameof(Culture)),
            TargetFile = parser.GetByName(nameof(TargetFile)),
            PostAction = parser.GetEnumToggle(ReportPostAction.NoAction)
        };
}
