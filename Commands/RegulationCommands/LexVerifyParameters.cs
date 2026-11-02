using PayrollEngine.Client.Command;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexVerify command parameters
/// </summary>
public class LexVerifyParameters : ICommandParameters
{
    /// <summary>
    /// File name or file mask (e.g. lex.json or *.json)
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Directory scan mode: top-level only or recursive (default: Top)
    /// </summary>
    public DirectoryMode DirectoryMode { get; private init; } = DirectoryMode.Top;

    /// <inheritdoc />
    public Type[] Toggles => [typeof(DirectoryMode)];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file mask (e.g. lex.json)" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexVerifyParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top)
        };
}
