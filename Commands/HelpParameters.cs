using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Help command parameters
/// </summary>
public class HelpParameters : ICommandParameters
{
    /// <summary>
    /// Command
    /// </summary>
    public string Command { get; init; }
  
    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() => null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static HelpParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Command = parser.Get(2, nameof(Command))
        };
}