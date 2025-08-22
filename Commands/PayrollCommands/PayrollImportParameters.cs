using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll import command parameters
/// </summary>
public class PayrollImportParameters : ICommandParameters
{
    /// <summary>
    /// Source file name
    /// </summary>
    public string SourceFileName { get; init; }

    /// <summary>
    /// Options file name
    /// </summary>
    public string OptionsFileName { get; init; }

    /// <summary>
    /// Namespace
    /// </summary>
    public string Namespace { get; init; }

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
        string.IsNullOrWhiteSpace(SourceFileName) ? "Missing source file name or file mask" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrollImportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            SourceFileName = parser.Get(2, nameof(SourceFileName)),
            OptionsFileName = parser.Get(3, nameof(OptionsFileName)),
            Namespace = parser.Get(4, nameof(Namespace)),
            ImportMode = parser.GetEnumToggle(DataImportMode.Single)
        };
}