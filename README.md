# Payroll Engine Console Application
👉 This repository is part of the [Payroll Engine](https://github.com/Payroll-Engine/PayrollEngine/wiki).

The Payroll Console application provides API-like commands. See the Payroll Engine samples and tests for examples of how to use this tool. For a better understanding of the working concepts, it is recommended to read the [Payroll Engine Whitepaper](https://github.com/Payroll-Engine/PayrollEngine/blob/main/Documents/PayrolEnginelWhitepaper.pdf).

## Commands
Payroll Console commands:
| Command              | Group            | Description                                                  |
|--|--|--|
| *Help*               | Common           | Show the command reference                                   |
| *UserVariable*       | Common           | View and change the environment user variable                |
| *Stopwatch*          | Common           | Stopwatch based on environment user variable                 |
| *ActionReport*       | Action           | Report actions from an assembly                              |
| *HttpGet<br/>HttpPost<br/>HttpPut<br />HttpDelete* | System | Execute http GET/POST/PUT/DELETE request |
| *LogTrail*           | System           | Trace the tenant log <sup>1)</sup>                           |
| *PayrollResults*     | Payroll          | Report payroll data to screen and/or file                    |
| *PayrollImport*      | Payroll          | Import any payroll data from json/zip file                   |
| *PayrollImportExcel* | Payroll          | Import payroll data from Excel file                          |
| *PayrollExport*      | Payroll          | Export any payroll data to json file                         |
| *Report*             | Report           | Report to file <sup>2)</sup>                                 |
| *DataReport*         | Report           | Report data to json file                                     |
| *CaseTest*           | Payroll          | Test case availability, build data and user input validation |
| *ReportTest*         | Test             | Test report output data                                      |
| *PayrunTest*         | Test             | Execute payrun and test the results                          |
| *PayrunEmployeeTest* | Test             | Execute employee payrun and test the results                 |
| *PayrunStatistics*   | Statistics       | Display payrun statistics                                    |
| *RegulationShare*    | Regulation share | Manage regulation shares                                     |
| *TenantDelete*       | Data management  | Delete tenant                                                |
| *PayrunJobDelete*    | Data management  | Delete payrun job with payroll results                       |
| *RegulationRebuild*  | Script           | Rebuild the regulation objects                               |
| *PayrunRebuild*      | Script           | Rebuild payrun                                               |
| *ScriptPublish*      | Script           | Publish scripts from C# file                                 |
| *ScriptExport*       | Script           | Export regulation scripts to folder                          |
<br/>

<sup>1)</sup> Tenant logs are generated by the regulations and should not be confused with the application log.<br/>
<sup>2)</sup> Based on [FastReports](https://github.com/FastReports).<br/>

An example how to import ap payroll from a JSON file:<br />
```
C:> PayrollConsole PayrollImport MyPayroll.json /bulk
```
<br />

## Application Configuration
The Payroll Console configuration `PayrollConsole\appsetings.json` contains the following settings:

**Payroll Console Configuration**
| Setting      | Description            | Default |
|:--|:--|:--|
| `StartupCulture` | The payroll console process culture (string) | System culture |

**Payroll Http Configuration**
| Setting      | Description                          | Default        |
|:--|:--|:--|
| `BaseUrl` | The backend base URL (string)           |                |
| `Port` | The backend url port (string)              |                |
| `Timeout` | The backend request timeout (TimeSpan)  | 100 seconds    |

**Serilog**
File and console logging with [Serilog](https://serilog.net/).

> It is recommended that you save the application settings within your local [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

## Application Logs
Under Windows, the payroll console stores its logs in the system folder `%ProgramData%\PayrollConsole\logs`.

## Third party components
- Excel conversion with [NPOI](https://github.com/dotnetcore/NPOI/) - licence `Apache 2.0`
- Logging with [Serilog](https://github.com/serilog/serilog/) - licence `Apache 2.0`
