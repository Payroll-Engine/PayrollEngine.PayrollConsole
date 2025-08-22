using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll results command parameters
/// </summary>
public class PayrollResultsParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Top filter count
    /// </summary>
    public int TopFilter { get; init; }

    /// <summary>
    /// Result export mode (default: no export)
    /// </summary>
    public ReportExportMode ResultExportMode { get; private init; } = ReportExportMode.NoExport;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(ReportExportMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrollResultsParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            TopFilter = parser.GetInt(3, 1, nameof(TopFilter)),
            ResultExportMode = parser.GetEnumToggle(ReportExportMode.NoExport)
        };
}