using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class SheetSpecification
{
    internal static readonly string Regulation = nameof(Regulation);
    internal static readonly string Case = nameof(Case);
    internal static readonly string CaseFieldCaseRefName = "CaseName";
    internal static readonly string CaseField = nameof(CaseField);
    internal static readonly string CaseRelation = nameof(CaseRelation);
    internal static readonly string Collector = nameof(Collector);
    internal static readonly string WageType = nameof(WageType);
    internal static readonly string Report = nameof(Report);
    internal static readonly string ReportRefName = "ReportName";
    internal static readonly string ReportParameter = nameof(ReportParameter);
    internal static readonly string ReportTemplate = nameof(ReportTemplate);
    internal static readonly string Lookup = nameof(Lookup);
    internal static readonly string LookupRefName = "LookupName";
    internal static readonly string LookupValue = nameof(LookupValue);
    internal static readonly string Script = nameof(Script);
    internal static readonly string LookupMask = nameof(Lookup) + ".";
    internal static readonly string GlobalCaseValues = nameof(CaseValue) + ".Global";
    internal static readonly string NationalCaseValues = nameof(CaseValue) + ".National";
    internal static readonly string CompanyCaseValues = nameof(CaseValue) + ".Company";
    internal static readonly string EmployeeCaseValues = nameof(CaseValue) + "." + nameof(Employee);
}