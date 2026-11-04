using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extensions command parameters
/// </summary>
public class ExtensionsParameters : ICommandParameters
{
    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() => null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="_">Parameter parser</param>
    public static ExtensionsParameters ParserFrom(CommandLineParser _) => new();
}
