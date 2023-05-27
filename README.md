<h1>Payroll Engine Console Application</h1>

The Payroll Konsolenapplikation bietet API nahe Operationen an. In den Beispielen und Tests der Payroll Engine finden sich Besipiele, wie dieses Tool verwendet wird. Zum Verständnis der Arbeitskonzepte empfieht sich das **[Payroll Engine White Paper](https://github.com/Payroll-Engine/PayrollEngine/blob/main/Documents/PayrolEnginelWhitePaper.pdf)** zu lesen.

<br />

## Console Operations

Folgende Operationen stehen zur Verfügung:
| Feature              | Group            | Description                                                  |
|--|--|--|
| *Help*               | Common           | Manage the user tasks                                        |
| *UserVariable*       | Common           | View and change environment user variable                    |
| *Stopwatch*          | Common           | Time measure based on environment user variable              |
| *ActionReport*       | Action           | Report assembly actions                                      |
| *HttpGet<br/>HttpPost<br/>HttpPut<br />HttpDelete* | System | Execute http GET/POST/PUT/DELETE request |
| *LogTrail*           | System           | Trail the tenant log <sup>1)</sup>                           |
| *PayrollResults*     | Payroll          | Report payroll data to screen and/or file                    |
| *PayrollImport*      | Payroll          | Import payroll data from JSON/ZIP file                       |
| *PayrollImportExcel* | Payroll          | Import payroll data from Excel file                          |
| *PayrollExport*      | Payroll          | Export payroll data to JSON file                             |
| *Report*             | Report           | Report to file <sup>2)</sup>                                 |
| *DataReport*         | Report           | Report data to JSON file                                     |
| *CaseTest*           | Payroll          | Test a case                                                  |
| *ReportTest*         | Test             | Test a report                                                |
| *PayrunTest*         | Test             | Test payrun results                                          |
| *PayrunEmployeeTest* | Test             | Test employee payrun results                                 |
| *PayrunStatistics*   | Statistics       | Show payrun statistics                                       |
| *RegulationShare*    | Regulation share | Manage the regulation shares                                 |
| *TenantDelete*       | Data Management  | Delete a tenant                                              |
| *PayrunJobDelete*    | Data Management  | Delete a payrun jobs with payroll results                    |
| *RegulationRebuild*  | Script           | Rebuild the regulation objects                               |
| *PayrunRebuild*      | Script           | Rebuild a payrun                                             |
| *ScriptPublish*      | Script           | Publish scripts from C-Sharp file                            |
| *ScriptExport*       | Script           | Export regulation scripts to folder                          |
<br/>

<sup>1)</sup> Tenant Logs werden von den Regulierungen generiert und sind nicht mit dem Applikations-Log zu verwechseln.<br/>
<sup>2)</sup> Based on [FastReports](https://github.com/FastReports).<br/>

An example how to import ap payroll from a JSON file:<br />
`
C:> PayrollConsole PayrollImport MyPayroll.json /bulk
`
<br />

## Configuration
Die Applikations-Konfiguration *appsetings.json* beinhaltet neben der Backend-Serververbindung auch die Einstellungen zum Systemlog.
Neben [Serilog](https://serilog.net/) sind weitere Logging-Tools einbindbar.

> It is recommended to save the application settings within your local [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).
