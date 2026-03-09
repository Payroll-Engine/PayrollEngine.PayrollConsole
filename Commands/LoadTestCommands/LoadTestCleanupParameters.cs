using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Load test cleanup parameters</summary>
public class LoadTestCleanupParameters : ICommandParameters
{
    /// <summary>Tenant identifier</summary>
    public string TenantIdentifier { get; init; }

    /// <summary>Employee identifier filter pattern (substring match, e.g. "-C" for load test copies)</summary>
    public string FilterPattern { get; init; } = "-C";

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(TenantIdentifier))
        {
            return "Missing tenant identifier";
        }
        return null;
    }

    /// <summary>Parse command parameters</summary>
    public static LoadTestCleanupParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            TenantIdentifier = parser.Get(2, nameof(TenantIdentifier)),
            FilterPattern = parser.Get(3, nameof(FilterPattern)) ?? "-C"
        };
}
