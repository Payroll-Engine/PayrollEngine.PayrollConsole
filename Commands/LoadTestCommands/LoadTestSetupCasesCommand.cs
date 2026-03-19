using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Bulk-import case changes for load testing via AddCasesBulkAsync</summary>
[Command("LoadTestSetupCases")]
// ReSharper disable once UnusedType.Global
internal sealed class LoadTestSetupCasesCommand : CommandBase<LoadTestSetupCasesParameters>
{
    public override bool BackendCommand => true;

    /// <summary>Bulk-import case changes for load testing</summary>
    protected override async Task<int> Execute(CommandContext context, LoadTestSetupCasesParameters parameters)
    {
        var console = context.Console;

        DisplayTitle(console, "Load test case setup (bulk case change import)");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Cases path       {parameters.CasesPath}");
            console.DisplayTextLine($"Batch size       {parameters.BatchSize}");
        }
        console.DisplayNewLine();

        try
        {
            // resolve case files
            var caseFiles = ResolveCaseFiles(parameters.CasesPath);
            if (caseFiles.Count == 0)
            {
                console.DisplayErrorLine($"No Setup-Cases files found at {parameters.CasesPath}");
                return -1;
            }

            console.DisplayTextLine($"Found {caseFiles.Count} case file(s)");

            // process first file to resolve tenant and payroll
            var firstExchange = await FileReader.ReadAsync<Exchange>(caseFiles[0]);
            if (firstExchange?.Tenants == null || firstExchange.Tenants.Count == 0)
            {
                console.DisplayErrorLine("Invalid exchange file: no tenants found.");
                return -1;
            }

            var exchangeTenant = firstExchange.Tenants[0];
            var exchangePayroll = exchangeTenant.Payrolls?.FirstOrDefault();
            if (exchangePayroll == null)
            {
                console.DisplayErrorLine("Invalid exchange file: no payroll found.");
                return -1;
            }

            // resolve tenant
            var tenantService = new TenantService(context.HttpClient);
            var tenant = await tenantService.GetAsync<Tenant>(new(), exchangeTenant.Identifier);
            if (tenant == null)
            {
                console.DisplayErrorLine($"Tenant {exchangeTenant.Identifier} not found.");
                return -1;
            }

            // resolve payroll by name
            var payrollService = new PayrollService(context.HttpClient);
            var tenantContext = new TenantServiceContext(tenant.Id);
            var payroll = await payrollService.GetAsync<Payroll>(tenantContext, exchangePayroll.Name);
            if (payroll == null)
            {
                console.DisplayErrorLine($"Payroll '{exchangePayroll.Name}' not found in tenant {exchangeTenant.Identifier}.");
                return -1;
            }

            var payrollContext = new PayrollServiceContext(tenant.Id, payroll.Id);

            // resolve identifiers to IDs (cached)
            var userService = new UserService(context.HttpClient);
            var employeeService = new EmployeeService(context.HttpClient);
            var divisionService = new DivisionService(context.HttpClient);
            var userIdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var employeeIdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var divisionIdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var totalStopwatch = Stopwatch.StartNew();
            var totalCreated = 0;
            var totalCaseChanges = 0;

            // process each case file
            foreach (var caseFile in caseFiles)
            {
                var exchange = caseFile == caseFiles[0]
                    ? firstExchange
                    : await FileReader.ReadAsync<Exchange>(caseFile);

                var cases = exchange?.Tenants?.FirstOrDefault()?.Payrolls?.FirstOrDefault()?.Cases;
                if (cases == null || cases.Count == 0)
                {
                    console.DisplayTextLine($"  {Path.GetFileName(caseFile)}: no cases, skipping");
                    continue;
                }

                // resolve identifiers to IDs
                foreach (var caseChange in cases)
                {
                    // resolve user
                    if (caseChange.UserId <= 0 && !string.IsNullOrWhiteSpace(caseChange.UserIdentifier))
                    {
                        if (!userIdCache.TryGetValue(caseChange.UserIdentifier, out var userId))
                        {
                            var user = await userService.GetAsync<User>(tenantContext, caseChange.UserIdentifier);
                            userId = user?.Id ?? throw new PayrollException(
                                $"User '{caseChange.UserIdentifier}' not found in tenant {exchangeTenant.Identifier}");
                            userIdCache[caseChange.UserIdentifier] = userId;
                        }
                        caseChange.UserId = userId;
                    }

                    // resolve employee
                    if (!caseChange.EmployeeId.HasValue && !string.IsNullOrWhiteSpace(caseChange.EmployeeIdentifier))
                    {
                        if (!employeeIdCache.TryGetValue(caseChange.EmployeeIdentifier, out var employeeId))
                        {
                            var employee = await employeeService.GetAsync<Employee>(tenantContext, caseChange.EmployeeIdentifier);
                            employeeId = employee?.Id ?? throw new PayrollException(
                                $"Employee '{caseChange.EmployeeIdentifier}' not found in tenant {exchangeTenant.Identifier}");
                            employeeIdCache[caseChange.EmployeeIdentifier] = employeeId;
                        }
                        caseChange.EmployeeId = employeeId;
                    }

                    // resolve division
                    if (!caseChange.DivisionId.HasValue && !string.IsNullOrWhiteSpace(caseChange.DivisionName))
                    {
                        if (!divisionIdCache.TryGetValue(caseChange.DivisionName, out var divisionId))
                        {
                            var division = await divisionService.GetAsync<Division>(tenantContext, caseChange.DivisionName);
                            divisionId = division?.Id ?? throw new PayrollException(
                                $"Division '{caseChange.DivisionName}' not found in tenant {exchangeTenant.Identifier}");
                            divisionIdCache[caseChange.DivisionName] = divisionId;
                        }
                        caseChange.DivisionId = divisionId;
                    }
                }

                totalCaseChanges += cases.Count;
                var fileStopwatch = Stopwatch.StartNew();

                // send in batches
                foreach (var batch in cases.Chunk(parameters.BatchSize))
                {
                    var batchList = batch.ToList();
                    var created = await payrollService
                        .AddCasesBulkAsync<CaseChangeSetup, CaseChange>(payrollContext, batchList);
                    totalCreated += created;
                }
                fileStopwatch.Stop();

                console.DisplayTextLine(
                    $"  {Path.GetFileName(caseFile)}: {cases.Count} case changes " +
                    $"({fileStopwatch.ElapsedMilliseconds}ms)");
            }
            totalStopwatch.Stop();

            // summary
            console.DisplayNewLine();
            var avgMs = totalCaseChanges > 0
                ? totalStopwatch.ElapsedMilliseconds / (double)totalCaseChanges
                : 0;
            console.DisplaySuccessLine(
                $"Imported {totalCreated} case changes from {caseFiles.Count} file(s) " +
                $"in {totalStopwatch.ElapsedMilliseconds}ms " +
                $"({avgMs:F1}ms/case change)");

            return 0;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return -2;
        }
    }

    /// <summary>Resolve case files from a path (directory or single file)</summary>
    private static List<string> ResolveCaseFiles(string path)
    {
        var fullPath = Path.GetFullPath(path);

        // single file
        if (File.Exists(fullPath))
        {
            return [fullPath];
        }

        // directory: find Setup-Cases-*.json files
        if (Directory.Exists(fullPath))
        {
            return Directory.GetFiles(fullPath, "Setup-Cases-*.json")
                .OrderBy(f => f)
                .ToList();
        }

        // glob pattern (e.g. LoadTest100\Setup-Cases-*.json)
        var dir = Path.GetDirectoryName(fullPath) ?? ".";
        var pattern = Path.GetFileName(fullPath);
        if (Directory.Exists(dir))
        {
            return Directory.GetFiles(dir, pattern)
                .OrderBy(f => f)
                .ToList();
        }

        return [];
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LoadTestSetupCasesParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LoadTestSetupCases");
        console.DisplayTextLine("      Bulk-import case changes for load testing via AddCasesBulkAsync");
        console.DisplayTextLine("      Replaces slow PayrollImport for Setup-Cases-*.json files.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Path to directory or Setup-Cases-*.json file(s) [CasesPath]");
        console.DisplayTextLine("          2. HTTP batch size (optional, default: 500) [BatchSize]");
        console.DisplayTextLine("      Supports:");
        console.DisplayTextLine("          - Single file:  LoadTestSetupCases LoadTest100\\Setup-Cases-001.json");
        console.DisplayTextLine("          - Directory:    LoadTestSetupCases LoadTest100");
        console.DisplayTextLine("          - Glob pattern: LoadTestSetupCases LoadTest100\\Setup-Cases-*.json");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LoadTestSetupCases LoadTest100");
        console.DisplayTextLine("          LoadTestSetupCases LoadTest10000 1000");
    }
}
