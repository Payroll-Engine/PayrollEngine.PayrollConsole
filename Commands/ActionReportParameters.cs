using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Action report command parameters
/// </summary>
public class ActionReportParameters : ICommandParameters
{
    /// <summary>
    /// Action report file name
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// Action report target (default: markdown)
    /// </summary>
    public ActionReportTarget ReportTarget { get; private init; } = ActionReportTarget.ActionMarkdown;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(ActionReportTarget)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing file name" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static ActionReportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileName = parser.Get(2, nameof(FileName)),
            ReportTarget = parser.GetEnumToggle(ActionReportTarget.ActionMarkdown)
        };
}