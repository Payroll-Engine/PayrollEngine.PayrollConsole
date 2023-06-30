using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace PayrollEngine.PayrollConsole.Command
{
    internal sealed class PayrunEmployeeTestCommandSettings
    {
        internal string FileMask { get; set; }
        internal TestDisplayMode DisplayMode { get; set; }
        internal EmployeeTestMode TestMode { get; set; }
        internal string Namespace { get; set; }
        internal string Owner { get; set; }
    }
}
