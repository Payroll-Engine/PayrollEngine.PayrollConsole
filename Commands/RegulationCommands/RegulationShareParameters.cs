using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation share command parameters
/// </summary>
public class RegulationShareParameters : ICommandParameters
{
    /// <summary>
    /// Provider tenant
    /// </summary>
    public string ProviderTenant { get; init; }

    /// <summary>
    /// Provider regulation
    /// </summary>
    public string ProviderRegulation { get; init; }

    /// <summary>
    /// Consumer tenant
    /// </summary>
    public string ConsumerTenant { get; init; }

    /// <summary>
    /// Consumer division
    /// </summary>
    public string ConsumerDivision { get; init; }

    /// <summary>
    /// Regulation share mode (default: view)
    /// </summary>
    public ShareMode ShareMode { get; private init; } = ShareMode.View;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(ShareMode)
    ];

    /// <inheritdoc />
    public string Test() => null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static RegulationShareParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            ProviderTenant = parser.Get(2, nameof(ProviderTenant)),
            ProviderRegulation = parser.Get(3, nameof(ProviderRegulation)),
            ConsumerTenant = parser.Get(4, nameof(ConsumerTenant)),
            ConsumerDivision = parser.Get(5, nameof(ConsumerDivision)),
            ShareMode = parser.GetEnumToggle(ShareMode.View)
        };
}