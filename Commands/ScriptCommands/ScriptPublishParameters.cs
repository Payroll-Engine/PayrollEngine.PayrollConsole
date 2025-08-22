using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ScriptCommands;

/// <summary>
/// Script publish command parameters
/// </summary>
public class ScriptPublishParameters : ICommandParameters
{
    /// <summary>
    /// Source file
    /// </summary>
    public string SourceFile { get; init; }

    /// <summary>
    /// Source script
    /// </summary>
    public string SourceScript { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(SourceFile) ? "Missing source file" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static ScriptPublishParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            SourceFile = parser.Get(2, nameof(SourceFile)),
            SourceScript = parser.Get(3, nameof(SourceScript))
        };
}