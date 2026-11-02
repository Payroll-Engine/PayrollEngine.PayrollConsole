using PayrollEngine.Client.Command;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexVerifySource write mode
/// </summary>
public enum LexSourceWriteMode
{
    /// <summary>
    /// Report only (default)
    /// </summary>
    ReportOnly,

    /// <summary>
    /// Write the found source location into entries without sourceLocation
    /// </summary>
    WriteLocations
}

/// <summary>
/// LexVerifySource command parameters
/// </summary>
public class LexVerifySourceParameters : ICommandParameters
{
    /// <summary>
    /// File name or file mask (e.g. lex.json or **/lex.json)
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Directory scan mode: top-level only or recursive (default: Top)
    /// </summary>
    public DirectoryMode DirectoryMode { get; private init; } = DirectoryMode.Top;

    /// <summary>
    /// Result handling: lenient only reports (default), strict fails when a value is missing in its source
    /// </summary>
    public LexCheckMode CheckMode { get; private init; } = LexCheckMode.Lenient;

    /// <summary>
    /// Write mode (default: report only)
    /// </summary>
    public LexSourceWriteMode WriteMode { get; private init; } = LexSourceWriteMode.ReportOnly;

    /// <inheritdoc />
    public Type[] Toggles => [typeof(DirectoryMode), typeof(LexCheckMode), typeof(LexSourceWriteMode)];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file mask (e.g. lex.json)" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexVerifySourceParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top),
            CheckMode = parser.GetEnumToggle(LexCheckMode.Lenient),
            WriteMode = parser.GetEnumToggle(LexSourceWriteMode.ReportOnly)
        };
}
