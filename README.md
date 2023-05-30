<h1>Payroll Engine Console Application</h1>

The Payroll Konsolenapplikation bietet API nahe Kommandos an. In den Beispielen und Tests der Payroll Engine finden sich Besipiele, wie dieses Tool verwendet wird. Zum Verständnis der Arbeitskonzepte empfieht sich das **[Payroll Engine White Paper](https://github.com/Payroll-Engine/PayrollEngine/blob/main/Documents/PayrolEnginelWhitePaper.pdf)** zu lesen.

<br />

## Console Commands

Folgende Kommandos stehen zur Verfügung:
| Command              | Group            | Description                                                  |
|--|--|--|
| *Help*               | Common           | Show the command reference                                   |
| *UserVariable*       | Common           | View and change environment user variable                    |
| *Stopwatch*          | Common           | Stopwatch based on environment user variable                 |
| *ActionReport*       | Action           | Report actions from an assembly                              |
| *HttpGet<br/>HttpPost<br/>HttpPut<br />HttpDelete* | System | Execute http GET/POST/PUT/DELETE request |
| *LogTrail*           | System           | Trail the tenant log <sup>1)</sup>                           |
| *PayrollResults*     | Payroll          | Report payroll data to screen and/or file                    |
| *PayrollImport*      | Payroll          | Import any payroll data from json/zip file                   |
| *PayrollImportExcel* | Payroll          | Import payroll data from Excel file                          |
| *PayrollExport*      | Payroll          | Export any payroll data to json file                         |
| *Report*             | Report           | Report to file <sup>2)</sup>                                 |
| *DataReport*         | Report           | Report data to json file                                     |
| *CaseTest*           | Payroll          | Test case availability, build data and user input validation |
| *ReportTest*         | Test             | Test the report output data                                  |
| *PayrunTest*         | Test             | Execute payrun and test the results                          |
| *PayrunEmployeeTest* | Test             | Execute employee payrun and test the results                 |
| *PayrunStatistics*   | Statistics       | Show payrun statistics                                       |
| *RegulationShare*    | Regulation share | Manage the regulation shares                                 |
| *TenantDelete*       | Data Management  | Delete a tenant                                              |
| *PayrunJobDelete*    | Data Management  | Delete a payrun job with payroll results                     |
| *RegulationRebuild*  | Script           | Rebuild the regulation objects                               |
| *PayrunRebuild*      | Script           | Rebuild a payrun                                             |
| *ScriptPublish*      | Script           | Publish scripts from C-Sharp file                            |
| *ScriptExport*       | Script           | Export regulation scripts to folder                          |
<br/>

<sup>1)</sup> Tenant Logs werden von den Regulierungen generiert und sind nicht mit dem Applikations-Log zu verwechseln.<br/>
<sup>2)</sup> Based on [FastReports](https://github.com/FastReports).<br/>

An example how to import ap payroll from a JSON file:<br />
```
C:> PayrollConsole PayrollImport MyPayroll.json /bulk
```
<br />

## Configuration
Die Applikations-Konfiguration *appsetings.json* beinhaltet neben der Backend-Serververbindung auch die Einstellungen zum Systemlog.
Neben [Serilog](https://serilog.net/) sind weitere Logging-Tools einbindbar.

> It is recommended to save the application settings within your local [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).
