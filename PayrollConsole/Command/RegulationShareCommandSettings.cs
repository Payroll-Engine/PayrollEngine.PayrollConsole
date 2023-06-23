using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command
{
    internal sealed class RegulationShareCommandSettings
    {
        internal string ProviderTenant { get; set; }
        internal string ProviderRegulation { get; set; }
        internal string ConsumerTenant { get; set; }
        internal string ConsumerDivision { get; set; }
        internal ShareMode ShareMode { get; set; }
    }
}
