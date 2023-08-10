using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ReportTestCommandSettings
{
    internal string FileMask { get; set; }
    internal DataImportMode ImportMode { get; set; }
    internal TestRunMode RunMode { get; set; }
    internal TestDisplayMode DisplayMode { get; set; }
    internal TestResultMode ResultMode { get; set; }
    internal string Owner { get; set; }
}