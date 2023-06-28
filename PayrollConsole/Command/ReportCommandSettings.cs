using PayrollEngine.Document;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command
{
    internal sealed class ReportCommandSettings
    {
        internal string TenantIdentifier { get; set; }
        internal string UserIdentifier { get; set; }
        internal string RegulationName { get; set; }
        internal string ReportName { get; set; }
        internal DocumentType DocumentType { get; set; }
        internal string Culture { get; set; }
        internal ReportPostAction PostAction { get; set; }
        internal string ParameterFile { get; set; }
    }
}
