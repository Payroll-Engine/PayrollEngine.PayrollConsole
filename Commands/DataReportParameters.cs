using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Data report command parameters
/// </summary>
public class DataReportParameters : ICommandParameters
{
    /// <summary>
    /// Output file
    /// </summary>
    public string OutputFile { get; init; }

    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// User
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
    /// Parameters file
    /// </summary>
    public string ParametersFile { get; init; }

    /// <summary>
    /// Culture
    /// </summary>
    public string Culture { get; init; }

    /// <summary>
    /// Post action (default: none)
    /// </summary>
    public ReportPostAction PostAction { get; private init; } = ReportPostAction.NoAction;

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
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

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static DataReportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            OutputFile = parser.Get(2, nameof(OutputFile)),
            Tenant = parser.Get(3, nameof(Tenant)),
            User = parser.Get(4, nameof(User)),
            Regulation = parser.Get(5, nameof(Regulation)),
            Report = parser.Get(6, nameof(Report)),
            ParametersFile = parser.Get(7, nameof(ParametersFile)),
            Culture = parser.Get(8, nameof(Culture)),
            PostAction = parser.GetEnumToggle(ReportPostAction.NoAction)
        };
}