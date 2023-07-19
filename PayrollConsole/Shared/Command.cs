namespace PayrollEngine.PayrollConsole.Shared;

public enum Command
{
    // common commands
    Help,
    UserVariable,
    Stopwatch,

    // action
    ActionReport,

    // system
    HttpGet,
    HttpPost,
    HttpPut,
    HttpDelete,
    LogTrail,

    // payroll
    PayrollResults,
    PayrollImport,
    PayrollImportExcel,
    PayrollExport,

    // report
    Report,
    DataReport,

    // test
    CaseTest,
    ReportTest,
    PayrunTest,
    PayrunEmployeeTest,

    // statistics
    PayrunStatistics,

    // regulation share
    RegulationShare,

    // data management
    TenantDelete,
    PayrunJobDelete,

    // user
    ChangePassword,

    // scripting
    RegulationRebuild,
    PayrunRebuild,
    ScriptPublish,
    ScriptExport
}