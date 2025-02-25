using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Tenant delete command parameters
/// </summary>
public class TenantDeleteParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Tenant delete mode (default: delete)
    /// </summary>
    public ObjectDeleteMode DeleteMode { get; private init; } = ObjectDeleteMode.Delete;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(ObjectDeleteMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static TenantDeleteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            DeleteMode = parser.GetEnumToggle(ObjectDeleteMode.Delete)
        };
}