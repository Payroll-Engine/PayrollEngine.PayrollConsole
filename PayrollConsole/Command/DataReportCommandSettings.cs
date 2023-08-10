using PayrollEngine.PayrollConsole.Shared;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class DataReportCommandSettings
{
    internal string OutputFile { get; set; }
    internal string TenantIdentifier { get; set; }
    internal string UserIdentifier { get; set; }
    internal string RegulationName { get; set; }
    internal string ReportName { get; set; }
    internal string Culture { get; set; }
    internal ReportPostAction PostAction { get; set; }
    internal string ParameterFile { get; set; }
}