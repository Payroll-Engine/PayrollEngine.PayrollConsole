using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation excel import command parameters
/// </summary>
public class RegulationExcelImportParameters : ICommandParameters
{
    /// <summary>
    /// Source file name
    /// </summary>
    public string SourceFileName { get; init; }

    /// <summary>
    /// Target file name
    /// </summary>
    public string TargetFileName { get; init; }

    /// <summary>
    /// Import mode
    /// </summary>
    public ImportMode ImportMode { get; private init; } = ImportMode.File;

    /// <inheritdoc />
    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(ImportMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(SourceFileName) ? "Missing source file name" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static RegulationExcelImportParameters ParseFrom(CommandLineParser parser) =>
        new()
        {
            SourceFileName = parser.Get(2, nameof(SourceFileName)),
            TargetFileName = parser.Get(3, nameof(TargetFileName)),
            ImportMode = parser.GetEnumToggle(ImportMode.File)
        };
}