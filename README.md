# Payroll Engine Console Application

> Part of the [Payroll Engine](https://github.com/Payroll-Engine/PayrollEngine) open-source payroll automation framework.
> Full documentation at [payrollengine.org](https://payrollengine.org).

The Payroll Console application provides API-like commands. See the Payroll Engine samples and tests for examples of how to use this tool. For a better understanding of the working concepts, it is recommended to read the [Payroll Engine Whitepaper](https://github.com/Payroll-Engine/PayrollEngine/blob/main/Documents/PayrollEngineWhitepaper.pdf).

The application is controlled by two types of command line arguments:
- Command: a single command
- Command file: a file containing several commands.

---

## Contents

- [Prerequisites](#prerequisites)
- [Commands](#commands)
- [Payrun Test Statistics](#payrun-test-statistics)
- [Command Files](#command-files)
  - [Command files access path](#command-files-access-path)
  - [Command file parameters](#command-file-parameters)
  - [File Type Registration](#file-type-registration)
  - [Global Toggles](#global-toggles)
- [Application Extensions](#application-extensions)
- [Application Configuration](#application-configuration)
  - [Backend API Configuration](#backend-api-configuration)
- [API Key](#api-key)
- [Application Logs](#application-logs)
- [Docker Support](#docker-support)
- [Build](#build)
- [Solution Projects](#solution-projects)
- [Third Party Components](#third-party-components)
- [See Also](#see-also)

---

## Prerequisites

| Requirement | Minimum |
|:--|:--|
| [.NET](https://dotnet.microsoft.com/download) | 10.0 |
| [Payroll Engine Backend](https://github.com/Payroll-Engine/PayrollEngine.Backend) | running and accessible |

---

## Commands
The Payroll Console provides the following commands, grouped by area. For detailed descriptions including arguments, toggles and examples, see the [Command Reference](Commands/README.md).

### Payroll

| Command | Description |
|:--|:--|
| `PayrollImport` | Import any payroll data from JSON/YAML/zip file |
| `PayrollExport` | Export any payroll data to JSON/YAML file |
| `PayrollConvert` | Convert payroll files between JSON and YAML (supports file masks, recursive) |
| `PayrollMerge` | Merge multiple payroll files into one file |
| `PayrollResults` | Report payroll data to screen and/or file |
| `CaseChangeExcelImport` | Import payroll case changes from Excel file |
| `RegulationExcelImport` | Transfer Excel regulation to file or backend |

### Payrun

| Command | Description |
|:--|:--|
| `PayrunTest` | Execute payrun and test the results |
| `PayrunEmployeeTest` | Execute employee payrun on a copy and test the results |
| `PayrunEmployeePreviewTest` | Execute employee payrun preview (no persistence) and test the results |
| `PayrunJobDelete` | Delete a payrun job with payroll results |
| `PayrunRebuild` | Rebuild payrun |
| `PayrunStatistics` | Display payrun statistics |

### Report

| Command | Description |
|:--|:--|
| `Report` | Report to file <sup>1)</sup> |
| `DataReport` | Report data to JSON file |
| `ReportTest` | Test report output data |

### Case

| Command | Description |
|:--|:--|
| `CaseTest` | Test case availability, build data and user input validation |

### Script

| Command | Description |
|:--|:--|
| `ScriptExport` | Export regulation scripts to folder |
| `ScriptPublish` | Publish scripts from C# file |

### Regulation

| Command | Description |
|:--|:--|
| `LookupTextImport` | Import lookup data from text file |
| `RegulationRebuild` | Rebuild the regulation objects |
| `RegulationShare` | Manage regulation shares |
| `InstallRegulationPackage` | Install a regulation NuGet package into a PE backend tenant |
| `LexFetch` | Download a source document into the `.lex` folder of a data regulation |
| `LexAdd` | Add a lex entry with value and provenance |
| `LexVerifySource` | Look up the lex values in their sources, optionally record an automatic verification |
| `LexAttest` | Record the manual verification and the second review of lex entries |
| `LexSnapshot` | Create the compliance snapshot of a lex manifest |
| `LexVerify` | Verify that the snapshot is intact and current |
| `LexCheck` | Check lex manifests against the lookup values of their data regulation |
| `LexApply` | Write the lex values into the lookup values of the data regulation |
| `LexRollover` | Prepare the data regulation satellite of the next cycle |

### Load Test

| Command | Description |
|:--|:--|
| `LoadTestGenerate` | Generate scaled exchange files from a regulation template |
| `LoadTestSetupEmployees` | Bulk-import employees for load testing |
| `LoadTestSetupCases` | Bulk-import case changes for load testing |
| `PayrunLoadTest` | Execute payrun and measure performance (CSV timing report) |
| `LoadTestCleanup` | Delete load test employees from a tenant |

### Tenant / User

| Command | Description |
|:--|:--|
| `TenantDelete` | Delete tenant |
| `ChangePassword` | Change a user password |
| `UserVariable` | View and change the environment user variable |

### Http / Diagnostics

| Command | Description |
|:--|:--|
| `HttpGet` / `HttpPost` / `HttpPut` / `HttpDelete` | Execute HTTP GET, POST, PUT or DELETE request |
| `LogTrail` | Trace the tenant log <sup>2)</sup> |
| `Stopwatch` | Stopwatch based on environment user variable |
| `ActionReport` | Report custom actions from an assembly |

### App

| Command | Description |
|:--|:--|
| `Write` | Write to the console and/or log file |
| `Help` | Show the command reference |

<sup>1)</sup> Based on [FastReports](https://github.com/FastReports).<br/>
<sup>2)</sup> Tenant logs are generated by the regulations and should not be confused with the application log.<br/>

The required command parameters and options/toggles are passed after the command name. An example of how to bulk import a payroll from a JSON file:
```cmd
PayrollConsole PayrollImport MyPayroll.json /bulk
```

The `Help` command describes the commands and their parameters:
```cmd
PayrollConsole Help PayrollImport
```

---

## Payrun Test Statistics

When a `PayrunTest`, `PayrunEmployeeTest`, or `PayrunEmployeePreviewTest` run covers more than one payrun job, the console prints a statistics block after the individual job results. The block contains the key metrics used in the payroll industry to assess engine performance.

```
---------- Test statistics ----------
File          Test.pt.json
Jobs          12
Results       240
Duration      4.2 sec
Fastest       83 ms - MyPayrun.Jan24
Slowest       1204 ms - MyPayrun.Mar24
ms/Employee   350 ms
Employees/h   10,285
Errors        2
-------------------------------------
```

| Field | Description |
|:--|:--|
| `File` | Test file name |
| `Jobs` | Number of payrun jobs executed |
| `Results` | Total number of wage type and collector assertions |
| `Duration` | Total wall-clock time of all jobs |
| `Fastest` | Shortest job duration with job name |
| `Slowest` | Longest job duration with job name (only shown when more than 2 jobs) |
| `ms/Employee` | Average calculation time per employee-period — the primary payroll benchmark. Each `PayrollTestResult` represents one employee × one payroll period, so this is `total duration / result count`. |
| `Employees/h` | System throughput derived as `3 600 000 / ms/Employee` — the standard capacity metric in payroll operations, indicating how many employees can be processed per hour at the measured speed. |
| `Errors` | Number of failed assertions (only shown when > 0) |

> `ms/Employee` and `Employees/h` are the two standard payroll-industry benchmarks: one measures latency per employee, the other measures system capacity.

---

## Command Files
The command file is a file with the extension `.pecmd` that contains a series of instructions to the Payroll Engine Console. The lines of the file are executed sequentially and contain the following line types
- Comment starting with `#`
- Command instruction
- Start another command file

Example command file `Delete.pecmd` with a comment and a command statement:
```txt
# delete my tenant
TenantDelete MyTenant /trydelete
```

The following example uses the `Delete.pecmd` command file and then runs the command to test the payrun:
```txt
# cleanup and execute test
Delete.pecmd
PayrunTest *.pt.json
```

### Command files access path
If the called command file is in a different folder, it will be activated before execution:
```txt
Test1/Test.pecmd
Test2/Test.pecmd
```

This is necessary if the called command file is to be defined with local references. The `keeppath` option prevents directories from being changed:
```txt
Tools/Import1.pecmd /keeppath
Tools/Import2.pecmd /keeppath
```

In the example above, both import command files are run in the startup folder.

### Command file parameters
Command files can also be controlled by parameters. Such parameters are used with the placeholder `$Name$` in the commands. In the following example, the command file `OwnerTest.pecmd` has the parameter `Owner`:
```txt
# payrun employee test
# argument owner: job owner
PayrunEmployeeTest fileMask:*.et.json owner:$owner$ /wait
```

Calling this command file with the `Owner` parameter looks like this:
```txt
OwnerTest.pecmd owner:Test01
```

### File Type Registration

The release package includes setup scripts that register the `.pecmd` extension with the
operating system. Registration is scoped to the current user and does not require
administrator privileges.

> **Important:** Run the registration script from the **published console folder**
> (e.g. `Bin\Console\`) where `PayrollEngine.PayrollConsole.exe` is located,
> not from the source repository.

**Windows** — run from `Bin\Console\`:
```powershell
.\Register-PecmdExtension.ps1
```

To unregister:
```powershell
.\Register-PecmdExtension.ps1 -Unregister
```

If an older system-wide registration exists (installed via the legacy `FileType.Reg.cmd`
which required admin rights), remove it first before running the new script:
```cmd
FileType.Unreg.cmd
```

**Linux / macOS** — run from the published console folder:
```sh
./register-pecmd.sh
```

To unregister:
```sh
./register-pecmd.sh --unregister
```

If the console binary is not in the same directory as the script, pass the path explicitly:
```powershell
.\Register-PecmdExtension.ps1 -ConsolePath "C:\Tools\PayrollEngine.PayrollConsole.exe"
```
```sh
./register-pecmd.sh --console-path /opt/payrollengine/PayrollEngine.PayrollConsole
```

> In containerized environments, file type registration is not needed. Pass the `.pecmd` file directly as an argument to `docker run`.

### Global Toggles
The following toggles apply to all Payroll Console commands:

| Name           | Description                  | Values                                     | Command file                     |
|:--|:--|:--|:--|
| Display level  | Command information level    | `full` <sup>1)</sup>, `compact`, `silent`  | Preset for executing commands    |
| Error mode     | Show failed tests and errors | `errors`<sup>1)</sup>, `noerrors`          | Preset for executing commands    |
| Wait mode      | Wait at the program end      | `waiterror`<sup>1)</sup>, `wait`, `nowait` | Final application wait mode      |
| Path mode      | Path change mode             | `changepath`<sup>1)</sup>, `keeppath`      | Only for command files           |

<sup>1)</sup> Default value.<br/>

---

## Application Extensions
The Payroll Console provides a plug-in mechanism for integrating custom commands:
- Command parameters with names and options
- Access to the Payroll REST API via the Payroll HTTP client
- Control output to console and logger
- Program exit code definition

The extension point is the command: an extension provides classes with the `[Command("Name")]` attribute, derived from `CommandBase<TParameters>` (`PayrollEngine.Client.Core`). Commands that do not access the backend override `BackendCommand => false`.

> [!IMPORTANT]
> **Breaking change in version 1.2.0:** assemblies placed directly into the `extensions` folder are no longer loaded. Each extension requires its own subfolder with an `extension.json` manifest. See [Migration from earlier versions](#migration-from-earlier-versions).

### Extension Layout
Each extension is a subfolder of the `extensions` folder next to the console executable. The folder contains the command assemblies, all dependencies and the manifest:
```
extensions/
  MyExtension/
    extension.json
    MyExtension.dll
    MyExtension.deps.json
    SomeDependency.dll
```

### Extension Manifest
The manifest `extension.json` is mandatory:
```json
{
  "name": "MyExtension",
  "version": "1.0.0",
  "description": "My console commands",
  "requiresConsole": "1.2",
  "assemblies": [ "MyExtension.dll" ],
  "files": [
    { "path": "MyExtension.dll", "sha256": "3f2a..." },
    { "path": "MyExtension.deps.json", "sha256": "9b41..." },
    { "path": "SomeDependency.dll", "sha256": "c07e..." }
  ]
}
```

| Field             | Description                                                                                         | Required |
|:--|:--|:--:|
| `name`            | Extension name, must match the folder name                                                          | yes |
| `version`         | Extension version                                                                                   | yes |
| `description`     | Extension description                                                                               | no  |
| `requiresConsole` | Required console extension API version (`major.minor`)                                              | yes |
| `assemblies`      | Assemblies containing the commands; only these are scanned, all other assemblies are dependencies  | yes |
| `files`           | Extension files with their SHA-256 hash (relative paths)                                            | yes |

All assemblies (`*.dll`, `*.exe`), native libraries (`*.so`, `*.dylib`) and dependency files (`*.deps.json`) of the extension folder must be listed in `files`. Other files are verified if they are listed.

The manifest is created or updated with the `ExtensionManifest` command, which computes the hashes of all files in the folder:
```cmd
PayrollConsole ExtensionManifest bin\Release\net10.0 Name:MyExtension
```
Values of an existing manifest (version, description, assemblies) are kept unless overridden. Run the command after each build, then copy the folder to `extensions/MyExtension/`.

### Loading
At startup, the console loads the extensions in alphabetical order of their folder names. Per extension:
1. Read and validate the manifest.
2. Check `requiresConsole` against the console extension API version: the major version must be equal and the minor version must not be higher than the console's. The `Extensions` command shows the current API version.
3. Verify the hashes of all files listed in `files`, and reject undeclared files.
4. Load the `assemblies` into a separate load context of the extension. Dependencies are resolved from the extension folder using the `.deps.json` file. The contract assemblies `PayrollEngine.Client.Core`, `PayrollEngine.Core` and `PayrollEngine.Lex` are always shared with the console. Other assemblies the extension does not provide are taken from the console.
5. Register all commands of the extension.

Rules:
- Each extension uses its own versions of its dependencies, independent of the console and of other extensions.
- Core commands take precedence and cannot be replaced.
- Command names are case-insensitive. If a command name is already registered, the whole extension is rejected, and the message names both sources.
- A rejected extension registers no commands. It does not affect the console or other extensions.
- Loaded extensions are logged as information, rejected extensions as warnings, including the reason.

> The hash verification detects modified or unintended files. It is not a signature: replacing an assembly together with its manifest is not detected. Extensions run with the permissions of the console, including its backend configuration; only install extensions from trusted sources.

### Extension Commands
| Command | Description |
|:--|:--|
| `Extensions` | List the extensions with version, path, status (loaded or rejected with reason) and commands |
| `ExtensionManifest` | Create or update the manifest of an extension folder |
| `Help` | Shows the source of extension commands; `Help MyCommand` shows the source of any command (core or extension) |

Common rejection reasons:

| Reason | Solution |
|:--|:--|
| `Missing manifest extension.json` | Create the manifest with `ExtensionManifest` |
| `Hash mismatch of file ...` | The file has changed since the manifest was created; recreate the manifest |
| `Undeclared file(s) ...` | Add the file to the manifest or remove it from the folder |
| `Requires console extension API ...` | Update the console or build the extension for the installed console version |
| `Duplicate command ...` | Rename the command; core commands and previously loaded extensions take precedence |
| `Failed to load types of assembly ...` | A dependency is missing in the extension folder |
| `Assembly without extension folder ...` | Move the assembly into its own extension folder (see migration) |

### Developing an Extension
1. Develop the extension library in C# (example [PayrollEngine.Client.Tutorials](https://github.com/Payroll-Engine/PayrollEngine.Client.Tutorials)).
2. Build the library and run `ExtensionManifest` on the build output folder.
3. Copy the folder to `extensions/MyExtension/` of the console.
4. Check the status with `PayrollConsole Extensions`.
5. Display the command help `PayrollConsole Help MyCommandName`.
6. Run the command with `PayrollConsole MyCommandName`.

### Migration from earlier versions
Earlier versions loaded every assembly in the `extensions` folder, including subfolders. Since version 1.2.0:
1. Create the folder `extensions/<Name>/` per extension, and move the extension assembly together with its dependencies into it.
2. Create the manifest: `PayrollConsole ExtensionManifest extensions\<Name>`. If the command assembly is not named `<Name>.dll`, pass it with `Assemblies:MyCommands.dll`.
3. Verify with `PayrollConsole Extensions`.

Assemblies remaining directly in `extensions/` are reported as rejected.

---

## Application Configuration
The Payroll Console configuration `appsettings.json` contains the following settings:

| Setting            | Description                                            | Type                                       | Default        |
|:--|:--|:--|:--|
| `StartupCulture`   | The payroll console process culture                    | string                                     | System culture |
| `FullErrorLog`     | Show full error details in log                         | bool                                       | `false`        |
| `AllowInsecureSsl` | Skip backend SSL certificate validation (dev only)     | bool                                       | `false`        |
| `ApiSettings`      | The backend api configuration                          | [Backend API](#backend-api-configuration)  |                |
| `Serilog`          | Logger configuration                                   | [Serilog](https://serilog.net/)            |                |

> It is recommended that you save the application settings within your local [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

### Backend API Configuration

| Setting   | Description                     | Type       | Default     |
|:--|:--|:--|:--|
| `BaseUrl` | The backend base url            | string     |             |
| `Port`    | The backend url port            | string     |             |
| `Timeout` | The backend request timeout     | TimeSpan   | 100 seconds |
| `ApiKey`  | The backend [API key](#api-key) | string     |             |

The backend API configuration can be declared in the following locations.

| Priority | Source                                          | Content                                                        |
|--|--|--|
| 1.       | Environment variable `PayrollApiConnection`     | Backend API configuration [connection string](#api-configuration-from-connection-string).           |
| 2.       | Environment variable `PayrollApiConfiguration`  | Path to the backend API configuration [JSON file](#api-configuration-from-json).                    |
| 3.       | File `apisettings.json`                         | Backend API configuration [JSON file](#api-configuration-from-json) located in the program folder.  |
| 4.       | File `appsettings.json`                         | `ApiSettings` from the application configuration [JSON file](#api-configuration-from-json). |


#### API Configuration from connection string
The following is an example of a backend API configuration within a connection string:
```txt
BaseUrl=https://localhost; Port=44354; Timeout=02:46:40; ApiKey=MyApiKey; 
```

#### API Configuration from JSON
The following is an example of a JSON-based backend API configuration:
```json
{
    "BaseUrl": "https://localhost",
    "Port": 44354,
    "Timeout": "02:46:40",
    "ApiKey": "MyApiKey"
}
```

---

## API Key
If a key is required to access the backend API, it must be obtained from one of the following sources (in order of priority):

1. Environment variable `PayrollApiKey`
2. Payroll HTTP configuration (see `PayrollEngine.Client.Core`)

---

## Application Logs
The payroll console stores its logs in the application folder `logs`.

---

## Docker Support

The Payroll Console is available as a pre-built Docker image from the GitHub Container Registry.

### Pre-built image (ghcr.io)
Run a command against a Backend running in Docker Compose:
```bash
docker run --rm --network payroll-engine_default \
  -e PayrollApiConnection="BaseUrl=http://backend-api:8080; Port=8080;" \
  ghcr.io/payroll-engine/payrollengine.payrollconsole:latest \
  PayrollImport MyPayroll.json
```

Run tests:
```bash
docker run --rm --network payroll-engine_default \
  -e PayrollApiConnection="BaseUrl=http://backend-api:8080; Port=8080;" \
  ghcr.io/payroll-engine/payrollengine.payrollconsole:latest \
  PayrollTest --testFile Tests/Test.All.pecmd
```

Run against a Backend on the host machine:
```bash
docker run --rm \
  -e PayrollApiConnection="BaseUrl=http://host.docker.internal; Port=5001;" \
  ghcr.io/payroll-engine/payrollengine.payrollconsole:latest \
  Help
```

### Build from source (development)
```bash
docker build -t payroll-console .
docker run --rm -it payroll-console Help
```

See the [Container Setup](https://payrollengine.org/setup/container-setup) documentation for the full Docker Compose stack.

---

## Build

```shell
# Run from the project folder (PayrollConsole/)
dotnet run -- Help

# Publish self-contained binary
dotnet publish -c Release -r win-x64 --self-contained
```

Environment variable used during build:

| Variable | Description |
|:--|:--|
| `PayrollEnginePackageDir` | Output directory for the NuGet package (optional) |

---

## Solution Projects

| Name                                     | Type     | Description         |
|:--|:--|:--|
| `PayrollEngine.PayrollConsole.Commands`  | Library  | [Console commands](Commands/README.md) |
| `PayrollEngine.PayrollConsole`           | Exe      | Console application                    |

---

## Third Party Components
- Excel conversion with [ClosedXML](https://github.com/ClosedXML/ClosedXML/) — license `MIT`
- Logging with [Serilog](https://github.com/serilog/serilog/) — license `Apache 2.0`

---

## See Also
- [Payroll Console](https://payrollengine.org/tools/payroll-console) — Automator role documentation
- [Command Reference](Commands/README.md) — full argument and toggle reference for all commands
- [Client Services](https://github.com/Payroll-Engine/PayrollEngine.Client.Services) — SDK used by this tool
- [PayrollEngine Examples](https://github.com/Payroll-Engine/PayrollEngine) — example `.pecmd` files and test fixtures
