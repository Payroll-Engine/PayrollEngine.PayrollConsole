using PayrollEngine.PayrollConsole.Shared;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class RegulationShareCommandSettings
{
    internal string ProviderTenant { get; set; }
    internal string ProviderRegulation { get; set; }
    internal string ConsumerTenant { get; set; }
    internal string ConsumerDivision { get; set; }
    internal ShareMode ShareMode { get; set; }
}