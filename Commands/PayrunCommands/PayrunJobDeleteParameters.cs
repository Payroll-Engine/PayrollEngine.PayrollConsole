using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun job delete command parameters
/// </summary>
public class PayrunJobDeleteParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }
   
    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrunJobDeleteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant))
        };
}