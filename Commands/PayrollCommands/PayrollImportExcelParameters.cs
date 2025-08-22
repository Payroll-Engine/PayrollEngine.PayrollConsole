using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll import excel command parameters
/// </summary>
public class PayrollImportExcelParameters : ICommandParameters
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Import mode (default: single)
    /// </summary>
    public DataImportMode ImportMode { get; private init; } = DataImportMode.Single;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(DataImportMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing tenant" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrollImportExcelParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileName = parser.Get(2, nameof(FileName)),
            Tenant = parser.Get(3, nameof(Tenant)),
            ImportMode = parser.GetEnumToggle(DataImportMode.Single)
        };
}