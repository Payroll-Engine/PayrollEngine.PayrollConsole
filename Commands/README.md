# Payroll Engine Console Commands
This library provides the command implementations for the [Payroll Console](../README.md) application.

## Action Commands

### ActionReport
Report actions from an assembly.

| # | Argument     | Description                     |
|---|:-------------|:--------------------------------|
| 1 | `FileName`   | Action assembly file name       |

**Toggles:** action report targets: `/actionMarkdown` or `/actionJson` (default: `actionMarkdown`)

**Examples:**
```cmd
ActionReport MyAssembly.dll
ActionReport MyAssembly.dll /actionJson
```

---

## Case Commands

### CaseChangeExcelImport
Import case changes from Excel file.

| # | Argument     | Description                     |
|---|:-------------|:--------------------------------|
| 1 | `FileName`   | Excel file name                 |
| 2 | `Tenant`     | Tenant name (optional)          |

**Toggles:** import mode: `/single` or `/bulk` (default: `single`)

**Examples:**
```cmd
CaseChangeExcelImport MyImportFile.xlsx
CaseChangeExcelImport MyImportFile.xlsx /noupdate /bulk
```

### CaseTest
Test case availability, build data and user input validation.

| # | Argument     | Description                     |
|---|:-------------|:--------------------------------|
| 1 | `FileMask`   | JSON file name or file mask     |

**Toggles:**
- test display mode: `/showfailed` or `/showall` (default: `showfailed`)
- test precision: `/TestPrecisionOff` or `/TestPrecision1` to `/TestPrecision6` (default: `/TestPrecision2`)

**Examples:**
```cmd
CaseTest *.ct.json
CaseTest Test.ct.json /showall
CaseTest Test.ct.json /TestPrecision3
```

---

## Diagnostics Commands

### LogTrail
Trace the payroll log.

| # | Argument     | Description                                       |
|---|:-------------|:--------------------------------------------------|
| 1 | `Tenant`     | Tenant identifier                                 |
| 2 | `Interval`   | Query interval in seconds (default: 5, minimum: 1)|

**Examples:**
```cmd
LogTrail MyTenantName
LogTrail MyTenantName 1
```

### Stopwatch
Stopwatch based on environment user variable.

| # | Argument       | Description                 |
|---|:---------------|:----------------------------|
| 1 | `VariableName` | Stopwatch variable name     |

**Toggles:** stopwatch mode: `/watchstart`, `/watchstop` or `/watchview` (default: `watchview`)

**Examples:**
```cmd
Stopwatch MyStopwatch /watchstart
Stopwatch MyStopwatch /watchstop
```

### Write
Write to screen and/or log file.

| # | Argument | Description       |
|---|:---------|:------------------|
| 1 | `Text`   | Text to write     |

**Toggles:**
- write mode: `/console` or `/log` or `/consoleAndLog` (default: `console`)
- console write mode: `/consoleNormal`, `/consoleTitle`, `/consoleSuccess`, `/consoleInfo` or `/consoleError` (default: `consoleNormal`)
- logger write mode: `/info`, `/warning` or `/error` (default: `info`)

**Examples:**
```cmd
Write "My quoted text"
Write "My logger warning" /wan
```

---

## Extension Commands

See [Application Extensions](../README.md#application-extensions) for the extension layout and the manifest.

### Extensions
List the console extensions with version, path, status (loaded or rejected with reason) and commands. Offline command.

**Examples:**
```cmd
Extensions
```

### ExtensionManifest
Create or update the extension manifest `extension.json` with the SHA-256 hashes of all files in the extension folder. Values of an existing manifest are kept unless overridden. Offline command.

| # | Argument          | Description                                                        |
|---|:------------------|:-------------------------------------------------------------------|
| 1 | `Path`            | Extension folder                                                   |
|   | `Name`            | Extension name (named, default: folder name)                       |
|   | `Version`         | Extension version (named, default: version of the first assembly)  |
|   | `Description`     | Extension description (named)                                      |
|   | `RequiresConsole` | Required console extension API version (named, default: current)   |
|   | `Assemblies`      | Comma separated command assemblies (named, default: `<Name>.dll`)  |

**Examples:**
```cmd
ExtensionManifest extensions\MyExtension
ExtensionManifest bin\Release\net10.0 Name:MyExtension Version:1.0.0
ExtensionManifest MyExtension Assemblies:MyExtension.dll,MyExtension.Reports.dll
```

---

## HTTP Commands

All HTTP commands support URL placeholders resolved against the backend at runtime.
Placeholders are resolved **left-to-right** — `{tenant:X}` must appear before any tenant-scoped placeholder.

| Placeholder | Resolved to |
|:---|:---|
| `{tenant:Identifier}` | Numeric tenant id |
| `{user:Identifier}` | Numeric user id |
| `{division:Name}` | Numeric division id |
| `{employee:Identifier}` | Numeric employee id |
| `{regulation:Name}` | Numeric regulation id |
| `{payroll:Name}` | Numeric payroll id |
| `{payrun:Name}` | Numeric payrun id |
| `{payrunJob:Name}` | Numeric payrun job id |

**Example:** `tenants/{tenant:MyTenant}/employees/{employee:anna@foo.com}/casevalues`
→ `tenants/42/employees/7/casevalues`

### HttpGet
Execute HTTP GET request.

| # | Argument   | Description                                        |
|---|:-----------|:---------------------------------------------------|
| 1 | `Url`      | End point url                                      |
| 2 | `FileName` | Output file name (optional) — writes response to file |

**Examples:**
```cmd
HttpGet tenants
HttpGet tenants/1
HttpGet tenants/{tenant:MyTenant}/payrollresults/sets
HttpGet tenants/{tenant:MyTenant}/payrollresults/sets Export.json
```

### HttpPost
Execute HTTP POST request.

| # | Argument   | Description                     |
|---|:-----------|:--------------------------------|
| 1 | `Url`      | End point url                   |
| 2 | `FileName` | Content file name (optional)    |

**Examples:**
```cmd
HttpPost tenants/1 MyTenant.json
HttpPost admin/application/stop
HttpPost tenants/{tenant:MyTenant}/payrollresults/sets/import Results.json
```

### HttpPut
Execute HTTP PUT request.

| # | Argument   | Description                     |
|---|:-----------|:--------------------------------|
| 1 | `Url`      | End point url                   |
| 2 | `FileName` | Content file name (optional)    |

**Examples:**
```cmd
HttpPut tenants/1 MyTenant.json
HttpPut tenants/{tenant:MyTenant}/payrolls/1 Payroll.json
```

### HttpDelete
Execute HTTP DELETE request.

| # | Argument | Description       |
|---|:---------|:------------------|
| 1 | `Url`    | End point url     |

**Examples:**
```cmd
HttpDelete tenants/1
HttpDelete tenants/{tenant:MyTenant}/payruns/jobs/1
```

---

## Payroll Commands

### PayrollConvert
Convert payroll JSON from/to YAML.

| # | Argument   | Description                                           |
|---|:-----------|:------------------------------------------------------|
| 1 | `FileName` | File name with support for file masks (JSON/YAML/zip) |

**Toggles:**
- directory mode: `/top` or `/recursive` (default: `top`)
- schema type: `/auto`, `/exchange`, `/casetest` or `/reporttest` (default: `auto`)

**Examples:**
```cmd
PayrollConvert MyPayrollFile.json
PayrollConvert *.yaml /recursive
PayrollConvert *.yaml /recursive /casetest
```

### PayrollExport
Export payroll data to JSON/YAML/zip file.

| # | Argument          | Description                                       |
|---|:------------------|:--------------------------------------------------|
| 1 | `Tenant`          | Tenant name                                       |
| 2 | `TargetFileName`  | Target JSON/YAML file name (default: tenant name) |
| 3 | `OptionsFileName` | Export options file (optional)                     |
| 4 | `Namespace`       | Namespace (optional)                               |

**Options** (JSON object): type filter lists (`Users`, `Divisions`, `Employees`, `Tasks`, `Webhooks`, `Regulations`, `Payrolls`, `Payruns`, `PayrunJobs`) and data filter toggles (`ExportWebhookMessages`, `ExportGlobalCaseValues`, `ExportNationalCaseValues`, `ExportCompanyCaseValues`, `ExportEmployeeCaseValues`, `ExportPayrollResults`).

**Examples:**
```cmd
PayrollExport MyTenantName
PayrollExport MyTenantName MyExportFile.json MyExportOptions.json
PayrollExport MyTenantName MyExportFile.json MyExportOptions.json MyNamespace
```

### PayrollImport
Import payroll data from JSON/YAML/zip file.

| # | Argument          | Description                                               |
|---|:------------------|:----------------------------------------------------------|
| 1 | `SourceFileName`  | Source file name with support for file masks (JSON/YAML/zip)|
| 2 | `OptionsFileName` | Import options file (optional)                             |
| 3 | `Namespace`       | Namespace (optional)                                       |

**Toggles:** import mode: `/single` or `/bulk` (default: `single`)

**Options** (JSON object): load toggles (`TargetLoad`, `ScriptLoad`, `CaseDocumentLoad`, `ReportTemplateLoad`, `ReportSchemaLoad`, `LookupValidation`).

**Examples:**
```cmd
PayrollImport MyImportFile.json
PayrollImport MyImportFile.zip
PayrollImport MyImportFile.json MyImportOptions.json MyNamespace
PayrollImport MyImportFile.json MyImportOptions.json /bulk
```

### PayrollMerge
Merge multiple payroll files into one file.

| # | Argument          | Description          |
|---|:------------------|:---------------------|
| 1 | `SourceFilesMask` | Source files mask     |
| 2 | `TargetFileName`  | Target file name     |

**Toggles:** directory mode: `/top` or `/recursive` (default: `top`)

**Examples:**
```cmd
PayrollMerge *.yaml MergedPayroll.yaml
PayrollMerge *.json MergedPayroll.json /recursive
```

### PayrollResults
Report payroll data to screen and/or file.

| # | Argument    | Description                                          |
|---|:------------|:-----------------------------------------------------|
| 1 | `Tenant`    | Tenant identifier                                    |
| 2 | `TopFilter` | Result of top &lt;count&gt; payrun jobs (default: 1, max: 100)|

**Toggles:** result export mode: `/export` or `/noexport` (CSV report to `results` folder, default: `noexport`)

**Examples:**
```cmd
PayrollResults MyTenantName
PayrollResults MyTenantName 3
PayrollResults MyTenantName 3 /export
```

---

## Payrun Commands

### PayrunEmployeeTest
Execute employee payrun and test the results.

| # | Argument   | Description                          |
|---|:-----------|:-------------------------------------|
| 1 | `FileMask` | JSON/YAML/ZIP file name or file mask |
| 2 | `Owner`    | Owner name (optional)                |

**Toggles:**
- import mode: `/single` or `/bulk` (default: `single`)
- test mode: `/insertemployee` or `/updateemployee` (default: `insertemployee`)
- running mode: `/runtests` or `/skiptests` (default: `runtests`)
- test display mode: `/showfailed` or `/showall` (default: `showfailed`)
- test result mode: `/cleantest`, `/keepfailedtest` or `/keeptest` (default: `cleantest`)
- test precision: `/TestPrecisionOff` or `/TestPrecision1` to `/TestPrecision6` (default: `/TestPrecision2`)

**Examples:**
```cmd
PayrunEmployeeTest Test.json
PayrunEmployeeTest *.et.json
PayrunEmployeeTest Test.json /showall /TestPrecision3
PayrunEmployeeTest Test.json /bulk /showall
```

### PayrunEmployeePreviewTest
Execute employee payrun preview and test the results. Unlike `PayrunEmployeeTest`, this command uses the preview API which does not persist results to the database. No employee duplication or cleanup is needed.

> **Note:** Preview results are not persisted, so wage type expressions that query historical results (e.g. `GetPeriodWageTypeResults`) will not find results from previous preview invocations. If retroactive calculation is triggered during a preview invocation, the endpoint returns HTTP 422 Unprocessable Entity.

| # | Argument   | Description                          |
|---|:-----------|:-------------------------------------|
| 1 | `FileMask` | JSON/YAML/ZIP file name or file mask |
| 2 | `Owner`    | Owner name (optional)                |

**Toggles:**
- test display mode: `/showfailed` or `/showall` (default: `showfailed`)
- test precision: `/TestPrecisionOff` or `/TestPrecision1` to `/TestPrecision6` (default: `/TestPrecision2`)

**Examples:**
```cmd
PayrunEmployeePreviewTest Test.et.json
PayrunEmployeePreviewTest *.et.json
PayrunEmployeePreviewTest Test.et.json /showall /TestPrecision3
```

### PayrunJobDelete
Delete a payrun job with payroll results.

| # | Argument | Description         |
|---|:---------|:--------------------|
| 1 | `Tenant` | Tenant identifier   |

**Examples:**
```cmd
PayrunJobDelete MyTenantName
```

### PayrunRebuild
Rebuild payrun (update scripting binaries).

| # | Argument | Description         |
|---|:---------|:--------------------|
| 1 | `Tenant` | Tenant identifier   |
| 2 | `Payrun` | Payrun name         |

**Examples:**
```cmd
PayrunRebuild MyTenantName MyPayrunName
```

### PayrunStatistics
Display payrun statistics.

| # | Argument              | Description                              |
|---|:----------------------|:-----------------------------------------|
| 1 | `Tenant`              | Tenant identifier                        |
| 2 | `CreatedSinceMinutes` | Query interval in minutes (default: 30)  |

**Examples:**
```cmd
PayrunStatistics MyTenantName
PayrunStatistics MyTenantName 60
```

### PayrunTest
Execute payrun and test the results. Existing tenant will be deleted.

| # | Argument   | Description                          |
|---|:-----------|:-------------------------------------|
| 1 | `FileMask` | JSON/YAML/ZIP file name or file mask |
| 2 | `Owner`    | Owner name (optional)                |

**Toggles:**
- import mode: `/single` or `/bulk` (default: `bulk`)
- running mode: `/runtests` or `/skiptests` (default: `runtests`)
- test display mode: `/showfailed` or `/showall` (default: `showfailed`)
- test result mode: `/cleantest`, `/keepfailedtest` or `/keeptest` (default: `cleantest`)
- test precision: `/TestPrecisionOff` or `/TestPrecision1` to `/TestPrecision6` (default: `/TestPrecision2`)

**Examples:**
```cmd
PayrunTest Test.json
PayrunTest *.pt.json
PayrunTest *.pt.json MyOwner
PayrunTest Test.pt.json /single /showall /keeptest /TestPrecision3
```

---

## Regulation Commands

The Lex commands document and check the provenance of the compliance values in a data regulation satellite. The manifest `lex.json` is stored in the `.lex` folder next to the `Regulation` folder, together with the evidence files and the generated `snapshot.json` (see [lex.json — Source Manifest Schema](#lexjson--source-manifest-schema)).

Lifecycle of a satellite:

| Step | Command | Result |
|:-----|:--------|:-------|
| Collect evidence | `LexFetch` | Source document in `.lex`, declared in `files[]` |
| Record values | `LexAdd` | New entry with value and provenance |
| Verify values | `LexVerifySource`, `LexAttest` | Verification record per entry (auto or manual) |
| Freeze | `LexSnapshot` | `snapshot.json` with hashes of values, files and sources |
| Write regulation | `LexApply` | Lex values in the lookup values of the regulation |
| Check | `LexVerify`, `LexCheck` | Snapshot intact; regulation, lex and evidence consistent |
| Next cycle | `LexRollover` | Satellite of the next cycle with open entries |

Except `LexRollover` (payroll calendar), the Lex commands work on files only and require no backend connection.

### LexFetch
Download a source document unchanged into the `.lex` folder and declare it in `files[]` (`fileName`, `sourceUrls`). Hash and size are shown; `LexSnapshot` records them in the snapshot. An existing file with the same content is kept, a different one is only replaced with `/overwrite`.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `Url` | Source URL (http or https) |
| 2 | `FileName` | Target file name (optional, default from the URL) |

**Named:**

| Name | Default | Description |
|:-----|:--------|:------------|
| `Lex` | `lex.json` or `.lex/lex.json` in the current folder | Path of `lex.json` or of its folder |
| `Description` | none | Description of the file in `files[]` |

**Toggles:**
- overwrite mode: `/skip` (default) or `/overwrite` (replace a file with different content)

Exit codes: `0` ok, `3` source not loaded (HTTP error), `6` `lex.json` missing or file differs, `-1` invalid parameters or error.

**Examples:**
```cmd
LexFetch https://example.org/tax-rates-2026.pdf
LexFetch https://example.org/rates rates-2026.html Lex:Data.Tax.2026 "Description:Tax rates 2026"
```

### LexAdd
Add a lex entry. The value is read from the source by a person; the command checks the format. The key must be new and have the format `Lookup.Key`, structured content must be valid JSON, every source URL must be declared in `sources[]` and the document in `files[]`. Missing `content`, `sourceUrls`, `sourceLocation` and `legalReference` are written empty, `LexCheck` reports them.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `Key` | Entry key (`Lookup.Key`) |

**Named:**

| Name | Description |
|:-----|:------------|
| `Lex` | Path of `lex.json` or of its folder (default: `lex.json` or `.lex/lex.json` in the current folder) |
| `Content` | Value read from the source |
| `Unit` | Unit of the value |
| `SourceUrls` | Source URLs separated by spaces, first the main source (declared in `sources[]`) |
| `SourceLocation` | Location in the source (e.g. page, section, table) |
| `LegalReference` | Legal reference |
| `SourceQuote` | Text passage of the source that contains the value; `LexVerifySource` looks up the numbers in the section of the quote (anchors short numbers and numbers in words) |
| `DocumentRef` | Evidence document (declared in `files[]`) |
| `Description` | Description of the entry |

Exit codes: `0` ok, `6` `lex.json` missing, key exists or source/document not declared, `-1` invalid parameters or error.

**Examples:**
```cmd
LexAdd Rates.Standard Content:0.081 Unit:rate SourceUrls:https://example.org/rates
LexAdd Limits.Ceiling Content:42000 "SourceUrls:https://example.org/rates https://example.org/limits"
LexAdd Rates.Reduced Lex:Data.Tax.2026 "LegalReference:Tax Act s. 12"
LexAdd Overtime.Factor Content:1.5 Unit:factor "SourceQuote:one and one-half times the regular rate"
```

### LexSnapshot

Create an immutable compliance snapshot from a `lex.json` source manifest. All referenced files are copied into the `.lex` folder — after creation, no external access is needed. A file with `sourceUrls` and without `sourcePath` is downloaded from the first URL that answers. The snapshot records a hash of the normalized text of every source URL (`LexCheck /checkurls` reports changed sources); unreadable sources (blocked host, scanned PDF) get no hash. Source hashes are not recorded in dry-run mode.

If `snapshot.json` already exists in the target directory, the folder is **skipped with an info message** (not an error), unless `/overwrite` is set. This enables safe re-runs in recursive mode across an entire regulation repo tree.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `FileMask` | Path to `lex.json`, or file mask (e.g. `lex.json`, `*.json`) |

**Named:**

| Name | Default | Description |
|:-----|:--------|:------------|
| `TargetPath` | Directory of `FileMask` | Output directory for `snapshot.json` and copied files |

**Toggles:**
- snapshot mode: `/execute` (default) or `/dryrun` — dryrun validates and reports without writing
- directory mode: `/top` (default) or `/recursive` — recursive scans all subdirectories
- overwrite mode: `/skip` (default) or `/overwrite` — regenerate an existing `snapshot.json`; files with `sourceUrls` and an existing local copy are not downloaded again
- schema output: `/schema` — print the JSON Schema for `lex.json` and exit

**Examples:**
```cmd
LexSnapshot .lex/lex.json
LexSnapshot lex.json /recursive
LexSnapshot .lex/lex.json /dryrun
LexSnapshot .lex/lex.json /overwrite
LexSnapshot .lex/lex.json /TargetPath:C:\Output
LexSnapshot /schema
```

### LexVerify
Verify that the `snapshot.json` next to a `lex.json` is intact and current. The command recomputes the file hashes and the `sourceHash` from `lex.json` and the files in the `.lex` folder and compares header, values and files with the snapshot. It detects changed or missing evidence files, values edited after the snapshot was created, and a stale snapshot. No backend connection is required.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `FileMask` | Path to `lex.json`, or file mask (e.g. `lex.json`, `**/lex.json`) |

**Toggles:**
- directory mode: `/top` (default) or `/recursive`

Exit code `6` (invalid input) if a snapshot is missing or differs, so the command can gate a CI pipeline.

**Examples:**
```cmd
LexVerify .lex/lex.json
LexVerify lex.json /recursive
```

### LexCheck
Check `lex.json` manifests against the lookup values of their data regulation (the `Regulation` folder next to `.lex`). No backend connection is required.

| Finding | Meaning |
|:--------|:--------|
| Without lex entry | A regulation lookup value is covered by no lex entry |
| Orphan | A lex entry (inline content) matches no regulation value |
| Value difference | Entry content differs from the regulation value (numbers compared numerically, text ignoring case); a candidate to review against the source |
| Missing fields | `sourceUrls` or `legalReference` is empty; `sourceLocation` with `/required` (otherwise information) |
| Location not entry-specific | With `/required` (otherwise information): the same `sourceLocation` longer than 200 characters on 5 or more entries, or a `sourceLocation` longer than 500 characters |
| Secondary main source | With `/release` (otherwise information): the first source URL of an entry is declared with `official: false` |
| Manual verification required | With `/release`: an entry without manual verification is ambiguous or its `sourceQuote` is not found in the local evidence (see `LexVerifySource`), or it has an `autoVerification` exclusion and an `auto` record |
| Evidence | A declared or referenced file is missing in `.lex`, untracked or ignored in git, or a verification evidence is not in the snapshot |
| Snapshot / validFrom | `snapshot.json` is missing, or `validFrom` of lex and regulation differ |
| Verification | A manual verification of a `FourEyes` source without `reviewedBy`/`reviewedOn`; with `/verificationrequired` an entry without verification record |
| Open entry | An entry taken from the previous cycle by `LexRollover`, to be verified again |
| Source URL | With `/checkurls`: 404/410 or unknown host, or a source text changed since the snapshot (check the value); a blocking host is a note |

An entry with `contentFile` covers its whole lookup (e.g. a tariff table stored as file). Satellites whose lookup values are not stored in the `Regulation` folder are reported with a note for manual review. Documented `exclusions` (`coverage`, `sourceLocation`, `orphan`, `autoVerification`) are listed in the report instead of the findings; an `autoVerification` exclusion that matches no entry is a manifest finding. The verification records are counted per satellite (auto, manual, without, open). The freshness of `snapshot.json` is checked by `LexVerify`.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `FileMask` | Path to `lex.json`, or file mask (e.g. `lex.json`, `**/lex.json`) |

**Named:**

| Name | Default | Description |
|:-----|:--------|:------------|
| `Report` | none | Path of a Markdown report; a CSV file with the same name is written next to it |
| `Root` | none | Root folder; the file mask is resolved in every repository folder below it |
| `RepoMask` | `*` | Mask of the repository folders below the root (requires `Root`) |

**Toggles:**
- directory mode: `/top` (default) or `/recursive`
- result mode: `/strict` (default; findings give exit code `4`) or `/lenient` (report only, exit code `0`)
- source location: `/optional` (default, a missing `sourceLocation` is information) or `/required` (finding)
- verification: `/verificationoptional` (default, entries without verification are counted) or `/verificationrequired` (finding)
- source URLs: `/skipurls` (default) or `/checkurls` (load every source URL)
- release gate: `/development` (default) or `/release` — `/required` and `/verificationrequired`, a secondary main source is a finding, and the entries are verified against their local evidence: an entry without manual verification that is ambiguous, whose quote is not found or that is excluded from auto verification but has an `auto` record is a finding

The report (Markdown and CSV) shows the new counts per satellite: `UnspecificLocations`, `SecondaryMainSource`, `Release`, `Ambiguous`, `QuoteNotFound` and `ExcludedAutoVerified` (CSV columns appended at the end).

Exit codes: `0` no findings (or `/lenient`), `4` findings (`/strict`), `6` no `lex.json` found or root folder missing, `-1` invalid parameters or error.

**Examples:**
```cmd
LexCheck .lex/lex.json
LexCheck lex.json /recursive /lenient Report:C:\Reports\LexCheck.md
LexCheck lex.json /recursive Root:C:\Repos RepoMask:Payroll.*
LexCheck lex.json /recursive /release Report:C:\Reports\LexRelease.md
```

### LexVerifySource
Look up the numbers of every lex entry — inline `content` or a JSON `contentFile` — in its sources: per source URL the local evidence file (`documentRef` for the first URL, otherwise the declared file with the same URL in `sourceUrls`) or, with `/live`, the source URL. A number counts as found when any source of the entry holds it. The found location names the evidence file with a single local source (`rates.pdf: Section "Page 24"`) and the host per source with several sources.

The format is detected from the content: HTML, text and CSV, spreadsheets (`xls`, `xlsx`, `xlsb`, `ods`; one section per sheet), PDF with a text layer (one section per page, plus the form field values) and text documents (`docx`, `odt`; one section per heading).

Number search:
- A value below 1 is also looked up as percentage (`0.085` as `8.5%`) when the source writes `%`, `percent` or `per cent` directly after the number. Units that are no rate disable this: `factor`, `hours`, `days` and currency codes (`USD`, `EUR`); an empty unit, `mixed` or a combined unit like `USD / rate` allow it. A factor `0.5` is therefore not found in "50 hours a week".
- Numbers in words are recognized: `$1 million`, `2.5 billion`, `2.5 lakh`, `1 crore`, `one-half`, `½`, `1½`, `one and one-half`, `time and a half`, `double time`, and the Indian grouping `1,50,000`. A scale word needs a numeral (`a million` holds no number).
- A table with more than 40 numbers is checked with 40 numbers evenly distributed over the sorted list (`sampled 40 of 2200 numbers`).

| Result | Meaning |
|:-------|:--------|
| Found | All numbers of the entry are in the source |
| Ambiguous | Weak evidence of a single number: written in the source with at most two digits (separators not counted: `22%`, `1.5`, `50`; `0.012` found as `1.2%`), written with at most three digits and found in more than three sections, or found in words only — confirm with `LexAttest` or add `sourceQuote` |
| Partial / not found | Numbers are missing in the source — review the value |
| Quote not found | The `sourceQuote` of the entry is in no readable source — correct the quote |
| Unreadable / no local evidence | Blocked host, script-only page, scanned or encrypted PDF, other binary format, or no local file — verify the value manually (`LexAttest`) |
| No numbers / no content | The entry holds no number, or its `contentFile` is missing or not JSON |

With `sourceQuote` the quote must be in a section of a source (whitespace, line-end hyphenation and typographic quotes are normalized) and the numbers of the entry in that section or in the quote; the result is found with this section as location, never ambiguous. An entry with an `autoVerification` exclusion is reported as `excluded from auto verification: <reason>` and is never written.

With `/writelocations` the command sets the `sourceLocation` of found entries without one and writes an `auto` verification record for entries whose numbers were all found. `/refreshlocations` does the same and also replaces locations written by the command (`Section "…"`, `Sections "…"`, optionally with a `<file or host>: ` prefix); a location written by a person is kept. Ambiguous entries are never written, a manual record is never replaced. Afterwards regenerate the snapshot with `LexSnapshot /overwrite`.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `FileMask` | Path to `lex.json`, or file mask (e.g. `lex.json`, `**/lex.json`) |

**Toggles:**
- directory mode: `/top` (default) or `/recursive`
- result mode: `/lenient` (default, report only) or `/strict` (exit code `6` for partial, not found, quote not found and ambiguous entries)
- source mode: `/local` (default, local evidence file, offline) or `/live` (source URL)
- write mode: `/reportonly` (default), `/writelocations` or `/refreshlocations`

Exit codes: `0` ok (or `/lenient`), `6` no `lex.json` found or problems (`/strict`), `-1` invalid parameters or error.

**Examples:**
```cmd
LexVerifySource lex.json /recursive
LexVerifySource .lex/lex.json /writelocations
LexVerifySource lex.json /recursive /refreshlocations
LexVerifySource .lex/lex.json /live
```

### LexAttest
Record the manual verification of lex entries: a person confirms the values against the evidence. The evidence must be declared in `files[]` and exist in the `.lex` folder. An automatic record is replaced, a manual record only with `/overwrite`. With `ReviewedBy` the command records the second review (four-eyes) of existing manual records.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `Key` | Entry key or pattern (`Name` matches all keys below it, `Prefix*` matches a prefix) |

**Named:**

| Name | Default | Description |
|:-----|:--------|:------------|
| `Lex` | `lex.json` or `.lex/lex.json` in the current folder | Path of `lex.json` or of its folder |
| `VerifiedBy` | none | Person who verified the values |
| `VerifiedOn` | today | Verification date `yyyy-MM-dd` |
| `Evidence` | none | Evidence document (declared in `files[]`) |
| `ReviewedBy` | none | Second person (review of a manual verification) |
| `ReviewedOn` | today | Review date `yyyy-MM-dd` |

**Toggles:**
- overwrite mode: `/skip` (default) or `/overwrite` (replace a manual record or review)

Exit codes: `0` ok (skipped entries included), `6` `lex.json` missing, no matching entry or evidence missing, `-1` invalid parameters or error.

**Examples:**
```cmd
LexAttest Rates.Standard "VerifiedBy:Jane Doe" Evidence:tax-rates-2026.pdf
LexAttest Rates Lex:Data.Tax.2026 "VerifiedBy:Jane Doe" Evidence:tax-rates-2026.pdf VerifiedOn:2026-11-03
LexAttest Rates.* "ReviewedBy:John Doe"
```

### LexApply
Write the lex values into the lookup values of the data regulation (`Regulation/*.json`). The mapping is the same as in `LexCheck`: the lex key is `Lookup.Key`, a further key part addresses a property of the value object; a `contentFile` key may end with the year of `validFrom`. Only differing values are replaced and missing values of an existing lookup are added; descriptions and file formatting are kept, so a second run changes nothing. Open entries are skipped until they are verified.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `FileMask` | Path to `lex.json`, or file mask (e.g. `lex.json`, `**/lex.json`) |

**Toggles:**
- directory mode: `/top` (default) or `/recursive`
- apply mode: `/write` (default) or `/dryrun` (show the differences only)

Exit codes: `0` applied (or nothing to apply; skipped entries are listed), `6` `lex.json` or regulation JSON invalid, nothing written for that satellite.

**Examples:**
```cmd
LexApply .lex/lex.json /dryrun
LexApply lex.json /recursive
```

### LexRollover
Prepare the data regulation satellite of the next cycle. The cycle containing the date is read from the payroll calendar of the tenant (backend connection): the cycle start becomes the new `validFrom`, its year the new year tag of folder, files and version. A mid-year edition (e.g. `Data.Tax.2026b`) rolls to the year without suffix (`Data.Tax.2027`).

The command creates a new satellite folder; the previous satellite is not changed:
- The folder is copied without `bin`, `obj`, `nupkgs` and `.lex/snapshot.json`; evidence files are copied unchanged.
- Names with the year tag are renamed and their references in the text files updated.
- `validFrom` of lex and regulation and a version starting with the year (`Directory.Build.props`, `regulation-package.json`) are set.
- All lex entries become open: the verification record is removed and `"open": true` is set. `LexCheck` reports open entries and `LexApply` skips them until they are verified again (`LexVerifySource /writelocations` or `LexAttest`).
- Values, descriptions and lookup keys are not changed; lines still mentioning the previous year are listed for review.

An existing target folder is never overwritten. Solution and test projects are not part of the rollover.

| # | Argument | Description |
|---|:---------|:------------|
| 1 | `Satellite` | Satellite directory of the previous cycle |

**Named:**

| Name | Default | Description |
|:-----|:--------|:------------|
| `Date` | none (required) | Date within the new cycle `yyyy-MM-dd` |
| `Tenant` | none (required) | Tenant of the payroll calendar |
| `Calendar` | tenant calendar | Calendar name |
| `Culture` | tenant culture | Culture name |

**Toggles:**
- rollover mode: `/write` (default) or `/dryrun` (show the planned satellite only)

Exit codes: `0` satellite created (or planned with `/dryrun`), `2` backend not reachable, `6` unknown tenant, no cycle, target exists or satellite invalid — nothing written.

Next steps after the rollover: enter and verify the values of the open entries, then `LexSnapshot`, `LexApply` and `LexCheck`.

**Examples:**
```cmd
LexRollover Data.Tax.2026 Date:2027-01-01 Tenant:MyTenant /dryrun
LexRollover Data.Tax.2026 Date:2027-07-01 Tenant:MyTenant Calendar:FiscalYear
```

---

#### lex.json — Source Manifest Schema

The `lex.json` file documents the provenance of compliance values in a data regulation satellite. It is **created manually** before populating a data satellite with statutory values.

**Root structure:**

```json
{
  "$schema": "https://payrollengine.org/schemas/lex/v1.json",
  "authority": "XX",
  "regulation": "Data.Tax",
  "validFrom": "2026-01-01",
  "provider": "Tax authority",
  "sources": [ ],
  "values": [ ],
  "files": [ ]
}
```

**Root properties:**

| Property | Type | Required | Description |
|:---------|:-----|:--------:|:------------|
| `$schema` | string | No | JSON Schema URI for IDE validation |
| `authority` | string | **Yes** | Country/jurisdiction code (`DE`, `CH`, `SG`, `UK`, ...) |
| `regulation` | string | **Yes** | Regulation identifier (`Data.Tax`, `Data.Social`, ...) |
| `validFrom` | string | **Yes** | Effective date in `YYYY-MM-DD` format |
| `provider` | string | No | Issuing authority or data provider (`Tax authority`, `Social security agency`, ...) |
| `sources` | array | No | Document-level source references (general provenance) |
| `values` | array | No | Compliance values with content and precise provenance |
| `files` | array | No | Source documents to copy into the `.lex` folder |
| `exclusions` | array | No | Documented exclusions from the coverage, orphan and `sourceLocation` checks and from the automatic verification |

**`sources[]` — document-level provenance:**

| Property | Required | Description |
|:---------|:--------:|:------------|
| `url` | **Yes** | URL to the source document or page |
| `description` | No | Human-readable description |
| `legalBasis` | No | Legal basis (e.g. `Income Tax Act §12`, `Act 123/2025 Art. 4`) |
| `review` | No | Review rule for the values that list this URL in `sourceUrls`: `SingleAuthor` (default) or `FourEyes` (a manual verification requires `reviewedBy` and `reviewedOn`) |
| `official` | No | `true` (default): publication of the authority; `false`: secondary source (commercial summary, tax calculator, association page). A secondary main source is information for `LexCheck` and a finding with `/release`: replace it with the official publication |

**`values[]` — compliance values:**

Each value must have exactly one of `content` or `contentFile` — they are mutually exclusive.

| Property | Required | Description |
|:---------|:--------:|:------------|
| `key` | **Yes** | Unique identifier within this manifest |
| `content` | Conditional | Inline value — scalar, JSON object, or JSON array |
| `contentFile` | Conditional | Relative path to a source file (copied into `.lex`) |
| `unit` | No | Unit of measure (`EUR`, `%`, `CHF`, `SGD`, ...) |
| `sourceUrls` | No | URLs where this specific value can be found; a value combined from several authorities lists one URL per authority, the first one is the main source |
| `sourceLocation` | No | Precise location within the source document (free text) |
| `sourceQuote` | No | Text passage of the source that contains the value; `LexVerifySource` looks up the numbers in the section of the quote. Anchors short numbers and numbers in words (`one and one-half`). Part of the snapshot hash |
| `legalReference` | No | Legal norm reference (e.g. `Income Tax Act §12(1)`) |
| `documentRef` | No | Local evidence document, declared in `files[]` |
| `description` | No | Human-readable description |
| `verification` | No | Verification record (see below) |
| `open` | No | `true`: value taken from the previous cycle by `LexRollover`, to be verified again; a new verification record removes the flag. Not allowed together with `verification` |

**`verification` — who verified the value and how:**

| Property | Required | Description |
|:---------|:--------:|:------------|
| `mode` | **Yes** | `auto` (verified by a tool against the source, `LexVerifySource`) or `manual` (verified by a person, `LexAttest`) |
| `verifiedOn` | **Yes** | Verification date `YYYY-MM-DD` |
| `method` | `auto` | Tool and version (e.g. `LexVerifySource 1.2`) |
| `verifiedBy` | `manual` | Person who verified the value |
| `evidence` | `manual` | Evidence document declared in `files[]` |
| `reviewedBy`, `reviewedOn` | `FourEyes` | Second person and date of the review; only together |

**`exclusions[]` — documented exceptions:**

| Property | Required | Description |
|:---------|:--------:|:------------|
| `kind` | **Yes** | `coverage` (regulation values without lex entry), `orphan` (lex entries without regulation value, e.g. a rule implemented in code), `sourceLocation` (entries without `sourceLocation`) or `autoVerification` (entries that `LexVerifySource` never verifies automatically, e.g. a value whose number is in the source but means something else; verify with `LexAttest`) |
| `pattern` | **Yes** | `Lookup`, `Lookup.Key` or a prefix with trailing `*`; a name also matches all keys below it |
| `reason` | **Yes** | Justification, shown in the check report |

**`content` vs. `contentFile` — when to use which:**

| Criterion | `content` | `contentFile` |
|:----------|:----------|:--------------|
| Data size | Small — scalars, compact JSON objects | Large — tariff tables, full lookup files |
| Readability | Value visible inline in `lex.json` | Value in separate file |
| Snapshot | Embedded in `snapshot.json` values | File copied into `.lex`, referenced by name |
| Typical examples | `"12000"`, `"0.145"`, `"{\"Rate\":0.2}"` | `"../Lookups/Lookups.Brackets.json"` |

**`sourceLocation` — examples by source type:**

| Source type | Example value |
|:------------|:--------------|
| PDF document | `"Calculation guide 2026, Page 12, Section 3.2.1"` |
| Legal paragraph | `"Income Tax Act §12(1) sentence 2 no. 1"` |
| Government gazette | `"Gazette 153/2023, Annex 1, Table 3, Row 5"` |
| Website with anchors | `"Section 'Tax rates and bands', Table 2026-27"` |
| Circular / directive | `"Circular 2025/42, margin number 18"` |
| Tariff file | `"File tariff-a.txt — converted via pecmd LookupTextImport"` |
| Government portal | `"portal > Employers > Contribution rates, Table 1"` |

**`files[]` — source documents:**

| Property | Required | Description |
|:---------|:--------:|:------------|
| `fileName` | **Yes** | Target file name in the `.lex` folder |
| `sourcePath` | No | Local path to the source file (copied by `LexSnapshot`) |
| `sourceUrls` | No | Original download URLs; when `sourcePath` is empty, `LexSnapshot` downloads the file from the first URL that answers (`LexFetch` downloads and declares the file) |
| `description` | No | Human-readable description |

---

#### snapshot.json — Generated Output

`snapshot.json` is **generated by `LexSnapshot`** and must not be edited manually. It is an immutable compliance record — no external access is needed after creation.

**Root properties:**

| Property | Source | Description |
|:---------|:-------|:------------|
| `created` | Generated | ISO 8601 UTC timestamp |
| `tool` | Generated | Tool name and version |
| `authority` | From `lex.json` | Country/jurisdiction code |
| `regulation` | From `lex.json` | Regulation identifier |
| `validFrom` | From `lex.json` | Effective date |
| `provider` | From `lex.json` | Issuing authority |
| `sourceHash` | Generated | SHA256 over all value contents and file hashes |
| `sources` | From `lex.json` | Copied as-is |
| `values` | From `lex.json` | Copied; `contentFile` paths resolved to `.lex`-local names |
| `files` | Enriched | All copied files with `sha256`, `fileSizeBytes`, `contentType` |
| `liveSources` | Generated | Hash of the normalized text per source URL (`textHash`), or the `reason` why the source is not comparable; compared by `LexCheck /checkurls` |

---

#### Examples

**Minimal (scalar values):**

```json
{
  "$schema": "https://payrollengine.org/schemas/lex/v1.json",
  "authority": "XX",
  "regulation": "Data.Tax",
  "validFrom": "2026-01-01",
  "provider": "Tax authority",
  "sources": [
    {
      "url": "https://example.org/tax/calculation-guide-2026.pdf",
      "description": "Calculation guide 2026",
      "legalBasis": "Income Tax Act §12"
    }
  ],
  "values": [
    {
      "key": "TaxParameter.AllowanceBasic",
      "content": "12000",
      "unit": "EUR",
      "sourceUrls": ["https://example.org/tax/calculation-guide-2026.pdf"],
      "sourceLocation": "Calculation guide 2026, Section 3.2.1",
      "legalReference": "Income Tax Act §12(1) sentence 2 no. 1",
      "description": "Basic tax-free allowance"
    }
  ],
  "files": [
    {
      "fileName": "calculation-guide-2026.pdf",
      "sourcePath": "C:/Sources/calculation-guide-2026.pdf",
      "sourceUrls": ["https://example.org/tax/calculation-guide-2026.pdf"],
      "description": "Calculation guide 2026"
    }
  ]
}
```

**Large data via `contentFile` (region tariff tables):**

```json
{
  "$schema": "https://payrollengine.org/schemas/lex/v1.json",
  "authority": "XX",
  "regulation": "Data.Brackets",
  "validFrom": "2026-01-01",
  "provider": "Tax authority",
  "sources": [
    {
      "url": "https://example.org/tax/tariffs",
      "description": "Withholding tariffs 2026",
      "legalBasis": "Income Tax Act §20"
    }
  ],
  "values": [
    {
      "key": "RegionParameter.A",
      "content": "{\"name\":\"A\",\"Median\":0,\"CalculationCycle\":\"M\"}",
      "sourceUrls": ["https://example.org/tax/tariffs"],
      "sourceLocation": "File tariff-a.txt",
      "legalReference": "Income Tax Act §20(2)",
      "description": "Region A — metadata (monthly model)"
    },
    {
      "key": "RegionTariff.A",
      "contentFile": "../Lookups/Lookups.RegionA.json",
      "sourceUrls": ["https://example.org/tax/tariffs"],
      "sourceLocation": "File tariff-a.txt — converted via pecmd LookupTextImport",
      "legalReference": "Income Tax Act §20",
      "description": "Region A — full tariff bracket table"
    }
  ]
}
```

---

### LookupTextImport
Import regulation lookups to backend and/or file.

| # | Argument          | Description                                   |
|---|:------------------|:----------------------------------------------|
| 1 | `Tenant`          | Tenant identifier (required)                  |
| 2 | `Regulation`      | Regulation name (required)                    |
| 3 | `SourceFileName`  | Text file name with file mask support (required)|
| 4 | `MappingFileName` | Text to lookup mapping JSON file (required)   |
| 5 | `TargetFolder`    | Target output folder (default: current folder)|
| 6 | `SliceSize`       | Lookup slice size (0=off, default: 0)         |

**Toggles:**
- import mode: `/single` or `/bulk` (default: `single`)
- import target: `/backend`, `/file` or `/all` (default: `backend`)

**Mapping JSON object:**
```json
{
  "key": "valueMap",
  "keys": ["valueMap"],
  "rangeValue": "valueMap",
  "value": "valueMap",
  "values": ["valueMap"]
}
```

Value map properties: `name` (string), `valueType` (text|decimal|integer|boolean), `start` (int), `length` (int), `decimalPlaces` (int).

**Examples:**
```cmd
LookupTextImport MyTenant MyRegulation MyTax.txt MyTaxMap.json /bulk
LookupTextImport MyTenant MyRegulation MyTax.txt MyTaxMap.json MyOutputPath /bulk /file
LookupTextImport MyTenant MyRegulation MyTax.txt MyTaxMap.json MyOutputPath 30000 /bulk /all
```

### RegulationExcelImport
Import payroll data from Excel file.

| # | Argument         | Description                            |
|---|:-----------------|:---------------------------------------|
| 1 | `SourceFileName` | Excel file name                        |
| 2 | `TargetFileName` | Target JSON/YAML file name (optional)  |

**Toggles:** import mode: `/file`, `/backend` or `/all` (default: `file`)

**Examples:**
```cmd
RegulationExcelImport MyImportFile.xlsx
RegulationExcelImport MyImportFile.xlsx MyExportFile.json
RegulationExcelImport MyImportFile.xlsx MyExportFile.yaml
```

### RegulationRebuild
Rebuild the regulation objects (update scripting binaries).

| # | Argument     | Description                                                           |
|---|:-------------|:----------------------------------------------------------------------|
| 1 | `Tenant`     | Tenant identifier                                                     |
| 2 | `Regulation` | Regulation name                                                       |
| 3 | `ObjectType` | Case, CaseRelation, Collector, WageType or Report (default: all)      |
| 4 | `ObjectKey`  | Object key, requires the object type                                  |

**Examples:**
```cmd
RegulationRebuild MyTenantName MyRegulationName
RegulationRebuild MyTenantName MyRegulationName Case
RegulationRebuild MyTenantName MyRegulationName Case MyCaseName
RegulationRebuild MyTenantName MyRegulationName CaseRelation SourceCaseName;TargetCaseName
RegulationRebuild MyTenantName MyRegulationName WageType 115
```

### InstallRegulationPackage
Install a regulation NuGet package (`.nupkg`) into a PE backend tenant. The package is opened as a ZIP archive; the `regulation/regulation-package.json` manifest defines the ordered list of files to import. Supports local file paths, wildcards, and HTTP(S) URLs (e.g. GitHub Release assets).

The command validates base regulation dependencies across all tenants before importing. If the target tenant does not yet exist, it is created by the first import file.

| # | Argument      | Description                                                      |
|---|:--------------|:----------------------------------------------------------------|
| 1 | `PackageFile` | Local `.nupkg` path, wildcard (single match), or HTTP(S) URL   |
| 2 | `Tenant`      | Target tenant identifier                                         |

**Toggles:**
- install mode: `/new` (default, fails if regulation exists) or `/overwrite`
- dry-run mode: `/execute` (default) or `/dryrun` (validate only, no changes)

**Examples:**
```cmd
InstallRegulationPackage Acme.Regulation.Country.2026.1.nupkg MyTenant
InstallRegulationPackage Acme.Regulation.Country.2026.1.nupkg MyTenant /dryrun
InstallRegulationPackage Acme.Regulation.Country.2026.2.nupkg MyTenant /overwrite
InstallRegulationPackage Acme.Regulation.Country.2026.2.nupkg MyTenant /overwrite /dryrun
InstallRegulationPackage Acme.Regulation.Country.*.nupkg MyTenant /dryrun
InstallRegulationPackage https://github.com/Acme/Reg/releases/download/v2026.1/Acme.Regulation.Country.2026.1.nupkg MyTenant
```

---

### RegulationShare
Manage regulation shares.

| # | Argument             | Description                                                     |
|---|:---------------------|:----------------------------------------------------------------|
| 1 | `ProviderTenant`     | Provider tenant identifier (optional for `/view`)               |
| 2 | `ProviderRegulation` | Provider regulation name (optional for `/view`)                 |
| 3 | `ConsumerTenant`     | Consumer tenant identifier (mandatory for `/set` and `/remove`) |
| 4 | `ConsumerDivision`   | Consumer division identifier (undefined: all divisions)         |

**Toggles:** share mode: `/view`, `/set` or `/remove` (default: `view`)

**Examples:**
```cmd
RegulationShare
RegulationShare ProviderTenantName ProviderRegulationName
RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName /set
RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName /set
RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName /remove
```

---

## Report Commands

### DataReport
Report data to JSON file.

| # | Argument         | Description                                              |
|---|:-----------------|:---------------------------------------------------------|
| 1 | `OutputFile`     | Output file                                              |
| 2 | `Tenant`         | Tenant identifier                                        |
| 3 | `User`           | User identifier                                          |
| 4 | `Regulation`     | Regulation name                                          |
| 5 | `Report`         | Report name                                              |
| 6 | `ParametersFile` | Report parameter file (JSON string/string dictionary, optional)|
| 7 | `Culture`        | Report culture                                           |

**Toggles:** post action: `/noaction` or `/shellopen` (default: `noaction`)

**Examples:**
```cmd
DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport /german
DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport MyParameters.json /german
```

### Report
Report to file. Based on [FastReports](https://github.com/FastReports).

| # | Argument        | Description                                              |
|---|:----------------|:---------------------------------------------------------|
| 1 | `Tenant`        | Tenant identifier                                        |
| 2 | `User`          | User identifier                                          |
| 3 | `Regulation`    | Regulation name                                          |
| 4 | `Report`        | Report name                                              |
| 5 | `ParameterFile` | Report parameter file (JSON string/string dictionary, optional)|
| 6 | `Culture`       | Report culture                                           |
| 7 | `TargetFile`    | Target file name                                         |

**Toggles:**
- document type: `/word`, `/excel`, `/pdf`, `/xml`, `/xmlraw` (default: `pdf`)
- post action: `/noaction` or `/shellopen` (default: `noaction`)

**Examples:**
```cmd
Report MyTenant MyUser MyRegulation MyReport /german
Report MyTenant MyUser MyRegulation MyReport MyParameters.json /french /xml
Report MyTenant MyUser MyRegulation MyReport /pdf targetFile:MyReport.pdf
```

### ReportBuild
Execute a report and generate a schema document for report template design.

Without `TemplateFile`, generates a new schema document from the DataSet. With `TemplateFile` (CI mode), updates the schema section of the existing template, preserving all design elements. The output format is determined by the document service — the command is format-agnostic.

| # | Argument     | Description                                              |
|---|:-------------|:---------------------------------------------------------|
| 1 | `Tenant`     | Tenant identifier                                        |
| 2 | `User`       | User identifier                                          |
| 3 | `Regulation` | Regulation name                                          |
| 4 | `Report`     | Report name                                              |

**Named:**

| Name            | Description                                                                              |
|:----------------|:-----------------------------------------------------------------------------------------|
| `TemplateFile`  | Existing template file for CI schema update (optional); output inherits its extension    |
| `ParameterFile` | Report parameter file (JSON string/string dictionary, optional)                          |
| `Culture`       | Report culture                                                                            |
| `TargetFile`    | Target file name (default: `{ReportName}_{Timestamp}[{TemplateExtension}]`)              |

**Toggles:** post action: `/noaction` or `/shellopen` (default: `noaction`)

**Examples:**
```cmd
ReportBuild MyTenant MyUser MyRegulation MyReport
ReportBuild MyTenant MyUser MyRegulation MyReport /shellopen
ReportBuild MyTenant MyUser MyRegulation MyReport templateFile:Report.frx
ReportBuild MyTenant MyUser MyRegulation MyReport parameterFile:parameters.json
ReportBuild MyTenant MyUser MyRegulation MyReport targetFile:MyReport.frx
```

---

### ReportTest
Test report output data.

| # | Argument   | Description                          |
|---|:-----------|:-------------------------------------|
| 1 | `FileMask` | JSON/YAML file name or file mask     |

**Toggles:**
- test display mode: `/showfailed` or `/showall` (default: `showfailed`)
- test precision: `/TestPrecisionOff` or `/TestPrecision1` to `/TestPrecision6` (default: `/TestPrecision2`)

**Examples:**
```cmd
ReportTest *.rt.json
ReportTest Test.rt.json /showall
ReportTest Test.rt.json /TestPrecision3
```

---

## Script Commands

### ScriptExport
Export regulation scripts to folder.

| # | Argument       | Description             |
|---|:---------------|:------------------------|
| 1 | `TargetFolder` | Target folder           |
| 2 | `Tenant`       | Tenant identifier       |
| 3 | `User`         | User identifier         |
| 4 | `Employee`     | Employee identifier     |
| 5 | `Payroll`      | Payroll name            |
| 6 | `Regulation`   | Regulation name         |
| 7 | `Namespace`    | Namespace               |

**Toggles:** script export mode: `/existing` or `/all` (default: `existing`)

**Examples:**
```cmd
ScriptExport scripts MyTenant MyUser MyEmployee MyPayroll MyRegulation
ScriptExport scripts MyTenant MyUser MyEmployee MyPayroll MyRegulation /all
ScriptExport scripts\cases MyTenant MyUser MyEmployee MyPayroll MyRegulation Case
```

### ScriptPublish
Publish scripts from C# file.

| # | Argument       | Description                                                      |
|---|:---------------|:-----------------------------------------------------------------|
| 1 | `SourceFile`   | C# source file name                                             |
| 2 | `SourceScript` | Script key e.g. report name, case name (default: all scripts)   |

**Examples:**
```cmd
ScriptPublish c:\test\CaseAvailableFunction.cs
ScriptPublish c:\test\CaseAvailableFunction.cs MyCaseName
```

---

## Tenant Commands

### TenantDelete
Delete tenant.

| # | Argument | Description         |
|---|:---------|:--------------------|
| 1 | `Tenant` | Tenant identifier   |

**Toggles:** object delete mode: `/delete` or `/trydelete` (default: `delete`)

**Examples:**
```cmd
TenantDelete MyTenantName
TenantDelete MyTenantName /trydelete
```

---

## User Commands

### ChangePassword
Change the user password.

| # | Argument           | Description                          |
|---|:-------------------|:-------------------------------------|
| 1 | `Tenant`           | Tenant identifier                    |
| 2 | `User`             | User identifier                      |
| 3 | `NewPassword`      | New password                         |
| 4 | `ExistingPassword` | Existing password (required on change)|

**Examples:**
```cmd
ChangePassword MyTenant MyUser My3irst@assword
ChangePassword MyTenant MyUser My2econd@assword My3irst@assword
```

### UserVariable
View and change the environment user variable.

| # | Argument        | Description              |
|---|:----------------|:-------------------------|
| 1 | `VariableName`  | Variable name            |
| 2 | `VariableValue` | Variable value (optional)|

Variable value supports simple values, JSON file names (variable name is the field name) and predefined expressions: `$FileVersion$`, `$ProductVersion$`, `$ProductMajorPart$`, `$ProductMinorPart$`, `$ProductMajorBuild$`, `$ProductName$`.

**Toggles:** variable mode: `/view`, `/set` or `/remove` (default without value: `view`, default with value: `set`)

**Examples:**
```cmd
UserVariable MyVariable
UserVariable MyVariable MyValue
UserVariable MyVariable /remove
```

---

## Load Test Commands

### LoadTestGenerate
Generate scaled exchange files for payrun load testing from a template.

| # | Argument        | Description                                          |
|---|:----------------|:-----------------------------------------------------|
| 1 | `TemplatePath`  | Path to exchange template file                       |
| 2 | `EmployeeCount` | Target employee count (100, 1000, 10000)             |
| 3 | `OutputDir`     | Output directory (optional, default: LoadTest{count}) |

Multiplies template employees to reach the target count. Each copy receives a unique identifier (e.g. `OriginalId-C0001`) with identical case values. Payrun invocations are extracted from the template and deduplicated by period. Generates `Setup-Employees.json`, `Setup-Cases-NNN.json` (batches of 500 employees), and `Payrun-Invocation.json`.

**Examples:**
```cmd
LoadTestGenerate MyTemplate.et.json 100
LoadTestGenerate MyTemplate.et.json 1000 LoadTest1000
LoadTestGenerate MyTemplate.et.json 10000 LoadTest10000
```

### LoadTestSetupEmployees
Bulk-import employees for load testing via `CreateEmployeesBulkAsync`.

| # | Argument        | Description                    |
|---|:----------------|:-------------------------------|
| 1 | `EmployeesFile` | Path to Setup-Employees.json   |

**Examples:**
```cmd
LoadTestSetupEmployees LoadTest100\Setup-Employees.json
LoadTestSetupEmployees LoadTest10000\Setup-Employees.json
```

### LoadTestSetupCases
Bulk-import case changes for load testing via `AddCasesBulkAsync`. Replaces slow `PayrollImport` for load test setup with direct bulk insert (no script execution).

| # | Argument    | Description                                       |
|---|:------------|:--------------------------------------------------|
| 1 | `CasesPath` | Directory, single file, or glob pattern           |
| 2 | `BatchSize` | HTTP batch size (optional, default: 500)          |

Supports directory input (finds all `Setup-Cases-*.json`), single file, or glob pattern.

**Examples:**
```cmd
LoadTestSetupCases LoadTest100
LoadTestSetupCases LoadTest10000 1000
LoadTestSetupCases LoadTest100\Setup-Cases-001.json
```

### PayrunLoadTest
Execute payrun and measure performance. Supports multiple invocations (periods) per run. Includes a warmup run (not measured) followed by N measured repetitions. Each repetition executes all invocations sequentially. Reports per-period and total timing with per-employee averages.

| # | Argument         | Description                                          |
|---|:-----------------|:-----------------------------------------------------|
| 1 | `InvocationFile` | Path to Payrun-Invocation.json                       |
| 2 | `EmployeeCount`  | Expected employee count (for report)                 |
| 3 | `Repetitions`    | Number of measured runs (optional, default: 3)       |
| 4 | `ResultFile`     | Output CSV path (optional, default: LoadTestResults.csv) |

**Toggles/Named:**

| Name | Description |
|:-----|:------------|
| `/ExcelReport` | Also write an Excel report (.xlsx) alongside the CSV (derived filename) |
| `/MarkdownReport` | Also write a Markdown report (.md) alongside the CSV (derived filename) |
| `/ExcelFile=<path>` | Explicit Excel output path (also enables Excel report) |
| `/MarkdownFile=<path>` | Explicit Markdown output path (also enables Markdown report) |
| `/ParallelSetting=<v>` | Backend `MaxParallelEmployees` value — documented in the Excel setup sheet |

The Markdown report includes a **Test Summary** (median timing, per-run breakdown) and a **Test Infrastructure** section with computer specs (OS, CPU, RAM, disk), Console version/build date, and full Backend information (version, database, authentication mode, runtime configuration, audit trail, CORS, rate limiting) retrieved from `GET /api/admin/information`.

**Examples:**
```cmd
PayrunLoadTest LoadTest100\Payrun-Invocation.json 100
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv /ExcelReport
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv /MarkdownReport
PayrunLoadTest LoadTest1000\Payrun-Invocation.json 1000 5 Results\LT1000.csv /ExcelFile=Reports\LT1000.xlsx /MarkdownFile=Reports\LT1000.md
```

### LoadTestCleanup
Delete load test employees from a tenant. Identifies test employees by a filter pattern in their identifier (default: `-C`, matching generated copies like `TF01-C0001`).

| # | Argument            | Description                                                  |
|---|:--------------------|:-------------------------------------------------------------|
| 1 | `TenantIdentifier`  | Tenant identifier                                            |
| 2 | `FilterPattern`     | Substring filter for employee identifier (optional, default: `-C`) |

**Examples:**
```cmd
LoadTestCleanup Demo.Tenant
LoadTestCleanup Demo.Tenant -C00
```

See `Commands/LoadTestCommands/README.md` for detailed documentation including workflow, PECMD scripts, CSV format, and server-side metrics.

---

## Application Commands

### Help
Show command help.

| # | Argument  | Description  |
|---|:----------|:-------------|
| 1 | `Command` | Command name |

**Examples:**
```cmd
Help
Help PayrollImport
```
