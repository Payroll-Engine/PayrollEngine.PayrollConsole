﻿namespace PayrollEngine.PayrollConsole.Shared;

public enum Operation
{
    // common operations
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

    // scripting
    RegulationRebuild,
    PayrunRebuild,
    ScriptPublish,
    ScriptExport
}