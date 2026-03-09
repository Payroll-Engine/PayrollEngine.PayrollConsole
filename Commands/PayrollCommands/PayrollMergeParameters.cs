using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll merge command parameters
/// </summary>
public class PayrollMergeParameters : ICommandParameters
{
    /// <summary>
    /// Source file mask
    /// </summary>
    public string SourceFilesMask { get; init; }

    /// <summary>
    /// Target file name
    /// </summary>
    public string TargetFileName { get; init; }

    /// <summary>
    /// DirectoryMode mode (default: top)
    /// </summary>
    public DirectoryMode DirectoryMode { get; private init; } = DirectoryMode.Top;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(DirectoryMode)
    ];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(SourceFilesMask))
        {
            return "Missing source files mask";
        }
        if (string.IsNullOrWhiteSpace(TargetFileName))
        {
            return "Missing target file name";
        }
        return null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrollMergeParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            SourceFilesMask = parser.Get(2, nameof(SourceFilesMask)),
            TargetFileName = parser.Get(3, nameof(TargetFileName)),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top)
        };
}