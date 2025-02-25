using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Log tail command parameters
/// </summary>
public class LogTrailParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Interval in seconds (default: 5)
    /// </summary>
    public int Interval { get; init; } = 5;
    
    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static LogTrailParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Interval = parser.GetInt(3, 5, nameof(Interval))
        };
}