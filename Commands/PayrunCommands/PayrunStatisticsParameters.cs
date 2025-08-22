using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun statistics command parameters
/// </summary>
public class PayrunStatisticsParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Created since minutes (default: 30)
    /// </summary>
    public int CreatedSinceMinutes { get; set; } = 30;

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() => null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrunStatisticsParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            CreatedSinceMinutes = parser.GetInt(3, 30, nameof(CreatedSinceMinutes))
        };
}