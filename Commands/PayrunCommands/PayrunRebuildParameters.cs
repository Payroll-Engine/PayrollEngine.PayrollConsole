using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun rebuild command parameters
/// </summary>
public class PayrunRebuildParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Payrun
    /// </summary>
    public string Payrun { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        return string.IsNullOrWhiteSpace(Payrun) ? "Missing payrun name" : null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrunRebuildParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Payrun = parser.Get(3, nameof(Payrun))
        };
}