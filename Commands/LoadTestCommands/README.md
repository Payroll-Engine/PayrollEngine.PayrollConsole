# PayrollEngine Load Test Commands

Built-in console commands for payrun performance testing with any regulation.

## Summary

Load test of the Payrun API endpoint under variable employee counts (100, 1'000, 10'000). **Performance measurement focuses exclusively on payrun execution.** Loading employees and case values is setup and is not measured.

## Principle: Two Separate Phases

```
Phase 1 – Setup (not measured)            Phase 2 – Measurement (payrun only)
┌──────────────────────────────────┐      ┌──────────────────────────────┐
│ LoadTestSetupEmployees Employees │      │ PayrunLoadTest               │
│ LoadTestSetupCases  Cases        │      │                              │
│                                  │      │ • PayrunJobInvocation        │
│ • Employees via Bulk API         │      │ • Warmup + measured runs     │
│ • Cases via AddCasesBulkAsync    │      │ • Client + server timing     │
│ • Can be split across files      │      │ • CSV report                 │
│                                  │      │                              │
│ NOT MEASURED                     │      │ ONLY THIS PHASE IS MEASURED  │
└──────────────────────────────────┘      └──────────────────────────────┘
```

### Phase 1 – Setup (not measured)

- Employees are bulk-imported via `LoadTestSetupEmployees` using `IEmployeeService.CreateEmployeesBulkAsync()` (SQL BulkInsert, 5'000 item chunks)
- Case values are bulk-imported via `LoadTestSetupCases` using `IPayrollService.AddCasesBulkAsync()` (fast bulk insert, no script execution)
- Setup JSON can be split across multiple files (500 employees per case batch)

### Phase 2 – Measurement (payrun only)

- Payrun is started directly via `PayrunJobService.StartJobAsync<PayrunJob>()` – no ExchangeImport needed
- Server-side timing extracted from the returned `PayrunJob` (JobStart/JobEnd)
- Warmup run before measured repetitions

## Test Data Generation: Multiplied Template Approach

Existing employee test cases from any exchange template file are used as templates and multiplied:

```
100 Employees   =  N Templates × K copies  = 100
1'000 Employees =  N Templates × K copies  = 1'000
10'000 Employees = N Templates × K copies   = 10'000
```

Each copy receives a unique identifier (e.g. `OriginalId-C0001`) but identical case values. This yields realistic data distributions with all constellations present in the template.

Employee identifiers are extracted from `CaseChangeSetup.EmployeeIdentifier` in the template cases. If the template contains an `Employees` section, those objects are used; otherwise employees are created from the identifiers.

## Commands

| Command | Description | Backend |
|:--|:--|:--|
| `LoadTestGenerate` | Generate scaled exchange files from a template | No |
| `LoadTestSetupEmployees` | Bulk-import employees via bulk API | Yes |
| `LoadTestSetupCases` | Bulk-import case changes via AddCasesBulkAsync | Yes |
| `PayrunLoadTest` | Execute payrun and measure performance | Yes |
| `LoadTestCleanup` | Delete load test employees from a tenant | Yes |

### LoadTestGenerate

Generates setup and payrun files from an exchange template file.

**Parameters:**

| # | Name | Required | Default | Description |
|---|------|----------|---------|-------------|
| 1 | TemplatePath | Yes | – | Path to exchange template file |
| 2 | EmployeeCount | Yes | – | Target employee count (100, 1000, 10000) |
| 3 | OutputDir | No | LoadTest{count} | Output directory |

Payrun invocations are extracted from the template and deduplicated by period start. Employee identifiers are removed so all employees are included.

**Generated files:**

```
LoadTest100/
  Setup-Employees.json        # employee records for bulk import
  Setup-Cases-001.json        # case values batch 1 (500 employees/batch)
  Payrun-Invocation.json      # payrun job invocation

LoadTest10000/
  Setup-Employees.json
  Setup-Cases-001..020.json   # 20 batches for 10'000 employees
  Payrun-Invocation.json
```

Why split: A large template can yield very large files at scale. Splitting avoids memory spikes during import.

**Examples:**

```shell
LoadTestGenerate MyTemplate.et.json 100
LoadTestGenerate MyTemplate.et.json 1000 LoadTest1000
LoadTestGenerate MyTemplate.et.json 10000 LoadTest10000
```

### LoadTestSetupEmployees

Bulk-imports employees using `IEmployeeService.CreateEmployeesBulkAsync()`. Sends employees in 1'000-item HTTP batches; the backend processes them via `SqlBulkCopy` in 5'000-item chunks.

**Parameters:**

| # | Name | Required | Description |
|---|------|----------|-------------|
| 1 | EmployeesFile | Yes | Path to Setup-Employees.json |

**Examples:**

```shell
LoadTestSetupEmployees LoadTest100\Setup-Employees.json
LoadTestSetupEmployees LoadTest10000\Setup-Employees.json
```

### LoadTestSetupCases

Bulk-imports case changes using `IPayrollService.AddCasesBulkAsync()`. Replaces the slow `PayrollImport` for load test setup. Processes Setup-Cases-*.json files with direct bulk insert (no script execution).

**Parameters:**

| # | Name | Required | Default | Description |
|---|------|----------|---------|-------------|
| 1 | CasesPath | Yes | – | Directory, single file, or glob pattern |
| 2 | BatchSize | No | 500 | HTTP batch size for bulk API calls |

**Input resolution:**

- Single file: `LoadTestSetupCases LoadTest100\Setup-Cases-001.json`
- Directory: `LoadTestSetupCases LoadTest100` (finds all Setup-Cases-*.json)
- Glob pattern: `LoadTestSetupCases LoadTest100\Setup-Cases-*.json`

**Examples:**

```shell
LoadTestSetupCases LoadTest100
LoadTestSetupCases LoadTest10000 1000
LoadTestSetupCases LoadTest100\Setup-Cases-001.json
```

### PayrunLoadTest

Executes the payrun via `PayrunJobService.StartJobAsync<PayrunJob>()` and measures performance. Supports multiple invocations (periods) from a single invocation file. Includes a warmup run (not measured) followed by N measured repetitions. Each repetition executes all invocations sequentially.

**Parameters:**

| # | Name | Required | Default | Description |
|---|------|----------|---------|-------------|
| 1 | InvocationFile | Yes | – | Path to Payrun-Invocation.json |
| 2 | EmployeeCount | Yes | – | Expected employee count (for report) |
| 3 | Repetitions | No | 3 | Number of measured runs |
| 4 | ResultFile | No | LoadTestResults.csv | Output CSV path |

**Toggles/Options:**

| Name | Description |
|------|-------------|
| `/ExcelReport` | Also write an Excel report (.xlsx) alongside the CSV (derived filename) |
| `/ExcelFile=<path>` | Explicit Excel output path (also enables Excel report) |
| `/MarkdownReport` | Also write a Markdown report (.md) alongside the CSV (derived filename) |
| `/MarkdownFile=<path>` | Explicit Markdown output path (also enables Markdown report) |
| `/ParallelSetting=<v>` | Backend `MaxParallelEmployees` value — documented in the Excel setup sheet |

**Timing metrics:**

| Metric | Source | Description |
|--------|--------|-------------|
| ClientDuration_ms | Stopwatch | End-to-end including HTTP overhead |
| ServerJobDuration_ms | PayrunJob.JobEnd - JobStart | Pure server-side processing |
| ServerAvgMs_PerEmployee | Computed | Server duration / employee count |

**Examples:**

```shell
PayrunLoadTest LoadTest100\Payrun-Invocation.json 100
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv /ExcelReport
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv /MarkdownReport
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv /ExcelFile=Reports\LT1000.xlsx /MarkdownFile=Reports\LT1000.md
```

### LoadTestCleanup

Deletes load test employees from a tenant. Identifies generated employee copies by a substring filter on the employee identifier (default: `-C`, matching identifiers like `TF01 Herz Monica-C0001`). Original template employees are not affected.

Uses `IEmployeeService.QueryAsync()` with OData `contains()` filter, then deletes each matching employee via `IEmployeeService.DeleteAsync()`.

**Parameters:**

| # | Name | Required | Default | Description |
|---|------|----------|---------|-------------|
| 1 | TenantIdentifier | Yes | – | Tenant identifier |
| 2 | FilterPattern | No | -C | Substring filter for employee identifier |

**Examples:**

```shell
LoadTestCleanup CH.Swissdec
LoadTestCleanup CH.Swissdec -C00
```

## Workflow

### 1. Generate test data

```shell
LoadTestGenerate MyTemplate.et.json 100 LoadTest100
LoadTestGenerate MyTemplate.et.json 1000 LoadTest1000
LoadTestGenerate MyTemplate.et.json 10000 LoadTest10000
```

### 2. Setup (not measured)

```shell
# bulk-import employees
LoadTestSetupEmployees LoadTest100\Setup-Employees.json

# bulk-import case changes (fast, no script execution)
LoadTestSetupCases LoadTest100
```

### 3. Run load test (measured)

> **Cleanup:** To re-run setup without recreating the tenant, delete test employees first:
> ```shell
> LoadTestCleanupAll.pecmd
> ```

```shell
PayrunLoadTest LoadTest100\Payrun-Invocation.json 100 3 Results\LoadTest100.csv
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 3 Results\LoadTest1000.csv
PayrunLoadTest LoadTest10000\Payrun-Invocation.json 10000 3 Results\LoadTest10000.csv
```

## PECMD Scripts

```
Tests/
  LoadTest/
    Generate.pecmd                # generate all exchange files
    Setup100.pecmd                # import employees + cases for 100
    Setup1000.pecmd               # import employees + cases for 1'000
    Setup10000.pecmd              # import employees + cases for 10'000
    Run100.pecmd                  # payrun measurement 100 employees
    Run1000.pecmd                 # payrun measurement 1'000 employees
    Run10000.pecmd                # payrun measurement 10'000 employees
    LoadTest100/                  # (generated)
    LoadTest1000/                 # (generated)
    LoadTest10000/                # (generated)
    Results/                      # output
```

### Generate.pecmd

```
LoadTestGenerate MyTemplate.et.json 100 LoadTest100
LoadTestGenerate MyTemplate.et.json 1000 LoadTest1000
LoadTestGenerate MyTemplate.et.json 10000 LoadTest10000
```

### Setup100.pecmd (not measured)

```
LoadTestSetupEmployees LoadTest100\Setup-Employees.json
LoadTestSetupCases LoadTest100
```

### Run100.pecmd (measured)

```
PayrunLoadTest LoadTest100\Payrun-Invocation.json 100 3 Results\LoadTest100.csv /ExcelReport /MarkdownReport
```

## Report Formats

### CSV Report

```csv
Timestamp;Run;Period;EmployeeCount;ClientDuration_ms;ServerJobDuration_ms;ServerAvgMs_PerEmployee
2026-03-01 10:15:00;1;2021-11;100;1200;1050;10.5
2026-03-01 10:15:02;1;2021-12;100;1300;1100;11.0
...
2026-03-01 10:15:30;1;2023-02;100;1100;950;9.5
```

### Excel Report

Enabled via `/ExcelReport` (derived filename) or `/ExcelFile=<path>`. Contains three sheets:

| Sheet | Content |
|:--|:--|
| **Setup** | Machine name, OS, `ProcessorCount`, `MaxParallelEmployees`, test parameters, median summary |
| **Results** | All rows identical to CSV — formatted, with AutoFilter and freeze pane |
| **Avg ms/Employee** | Pivot: Period × Run; outliers (>2× median) highlighted in yellow/red |

### Markdown Report

Enabled via `/MarkdownReport` (derived filename) or `/MarkdownFile=<path>`. Backend information is retrieved from `GET /api/admin/information` at the end of the test run. Contains two sections:

**Test Summary**

| Element | Content |
|:--|:--|
| Summary table | Test date, invocation file, periods, employee count, repetitions, median server total, median avg ms/employee |
| Run Results table | Per-run breakdown: server total (ms), employees, avg ms/employee |

**Test Infrastructure**

| Subsection | Content |
|:--|:--|
| Computer | Machine name, OS, framework, CPU cores, RAM total + available (Windows: `GlobalMemoryStatusEx`), disk total + free |
| Console | Version, build date |
| Backend | Version, build date, API version, auth mode, max parallel employees, timeouts, script safety analysis |
| Backend — Database | Type, catalog name, server version |
| Backend — Audit Trail | Per-area flags (Script, Lookup, Input, Payrun, Report) |
| Backend — CORS | Allowed origins (only when active) |
| Backend — Rate Limiting | Per-policy permit limit and window (only when active) |

## Expected Scaling (Estimate)

| Employees | Payrun (est.) | Avg ms/Employee | Scaling |
|-----------|--------------|-----------------|---------|
| 100 | ~14s | ~140 | 1.0× |
| 1'000 | ~140s (~2.3 min) | ~140 | linear? |
| 10'000 | ~1'400s (~23 min) | ~140 | linear? |

Key question: Does scaling remain **linear** or does it become **superlinear** due to SQL connection pool limits, memory pressure / GC stalls, disk I/O bottlenecks, or transaction log growth?

## Server-Side Metrics

### SQL Server (delta measurement before/after payrun)

```sql
-- database I/O stats
SELECT
    DB_NAME(database_id) AS DatabaseName,
    SUM(num_of_reads) AS TotalReads,
    SUM(num_of_writes) AS TotalWrites,
    SUM(num_of_bytes_read) AS BytesRead,
    SUM(num_of_bytes_written) AS BytesWritten,
    SUM(io_stall_read_ms) AS ReadStallMs,
    SUM(io_stall_write_ms) AS WriteStallMs
FROM sys.dm_io_virtual_file_stats(NULL, NULL)
WHERE DB_NAME(database_id) = 'PayrollEngine'
GROUP BY database_id;

-- wait stats snapshot
SELECT wait_type, waiting_tasks_count, wait_time_ms
FROM sys.dm_os_wait_stats
WHERE wait_type IN (
    'WRITELOG', 'PAGEIOLATCH_SH', 'PAGEIOLATCH_EX',
    'IO_COMPLETION', 'ASYNC_NETWORK_IO', 'SOS_SCHEDULER_YIELD'
)
ORDER BY wait_time_ms DESC;
```

### API Server (via dotnet-counters)

```bash
dotnet-counters monitor \
    --process-id <PID> \
    --counters \
        System.Runtime[cpu-usage,working-set,gc-heap-size,gen-0-gc-count,gen-2-gc-count,threadpool-queue-length] \
        Microsoft.AspNetCore.Hosting[requests-per-second,current-requests] \
    --refresh-interval 5
```

## Notes

1. **SQL Server configuration**: At 10'000 employees the DB is significantly loaded. Check tempdb, transaction log size, and max memory before testing.
2. **Setup duration**: Import of 10'000 employees with case values takes time (not measured, but required). Employee bulk API (~0.1ms/item) and case bulk API accelerate this significantly vs single-item import.
3. **Payrun timeout**: At 10'000 employees the payrun can take >20 minutes. Configure server-side timeout accordingly.
4. **Warmup**: First run serves as warmup (JIT, connection pool, SQL plan cache).
5. **Repeatability**: 3 repetitions per level, use median.
6. **Tenant isolation**: Use separate tenant or cleanup between levels for clean measurements.

## File Structure

```
Commands/
  LoadTestCommands/
    LoadTestGenerateCommand.cs        # LoadTestGenerate
    LoadTestGenerateParameters.cs
    LoadTestSetupEmployeesCommand.cs   # LoadTestSetupEmployees
    LoadTestSetupEmployeesParameters.cs
    LoadTestSetupCasesCommand.cs      # LoadTestSetupCases
    LoadTestSetupCasesParameters.cs
    LoadTestCleanupCommand.cs         # LoadTestCleanup
    LoadTestCleanupParameters.cs
    PayrunLoadTestCommand.cs          # PayrunLoadTest
    PayrunLoadTestParameters.cs
    PayrunLoadTestResult.cs
    PayrunLoadTestExcelWriter.cs      # Excel report writer
    PayrunLoadTestMarkdownWriter.cs   # Markdown report writer
    README.md
```
