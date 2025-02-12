using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class RegulationShareParameters : ICommandParameters
{
    public string ProviderTenant { get; init; }
    public string ProviderRegulation { get; init; }
    public string ConsumerTenant { get; init; }
    public string ConsumerDivision { get; init; }
    public ShareMode ShareMode { get; private init; } = ShareMode.View;

    public Type[] Toggles =>
    [
        typeof(ShareMode)
    ];

    public string Test() => null;

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