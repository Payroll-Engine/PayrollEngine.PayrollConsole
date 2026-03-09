using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Setup employees for load testing via bulk API</summary>
[Command("LoadTestSetupEmployees")]
// ReSharper disable once UnusedType.Global
internal sealed class LoadTestSetupEmployeesCommand : CommandBase<LoadTestSetupEmployeesParameters>
{
    public override bool BackendCommand => true;

    /// <summary>Bulk-import employees for load testing</summary>
    protected override async Task<int> Execute(CommandContext context, LoadTestSetupEmployeesParameters parameters)
    {
        var console = context.Console;

        DisplayTitle(console, "Load test setup (bulk employee import)");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Employees file   {parameters.EmployeesFile}");
        }
        console.DisplayNewLine();

        try
        {
            // load exchange file
            var exchange = await FileReader.ReadAsync<Exchange>(parameters.EmployeesFile);
            if (exchange?.Tenants == null || exchange.Tenants.Count == 0)
            {
                console.DisplayErrorLine("Invalid file: no tenants found.");
                return -1;
            }

            var exchangeTenant = exchange.Tenants[0];
            var employees = exchangeTenant.Employees;
            if (employees == null || employees.Count == 0)
            {
                console.DisplayErrorLine("No employees found in exchange file.");
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

            console.DisplayTextLine($"Importing {employees.Count} employees via bulk API...");

            // bulk import in batches
            const int batchSize = 1000;
            var stopwatch = Stopwatch.StartNew();
            var totalCreated = 0;
            var tenantContext = new TenantServiceContext(tenant.Id);
            var employeeService = new EmployeeService(context.HttpClient);

            foreach (var batch in employees.Chunk(batchSize))
            {
                var employeeList = batch.Cast<Employee>().ToList();
                var created = await employeeService.CreateEmployeesBulkAsync(
                    tenantContext, employeeList);
                totalCreated += created;
                console.DisplayTextLine($"  Batch: {created} employees created ({totalCreated}/{employees.Count})");
            }
            stopwatch.Stop();

            console.DisplayNewLine();
            console.DisplaySuccessLine(
                $"Created {totalCreated} employees in {stopwatch.ElapsedMilliseconds}ms " +
                $"({stopwatch.ElapsedMilliseconds / (double)totalCreated:F1}ms/employee)");

            return 0;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return -2;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LoadTestSetupEmployeesParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LoadTestSetupEmployees");
        console.DisplayTextLine("      Bulk-import employees for load testing");
        console.DisplayTextLine("      Uses the bulk API for fast employee creation.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Path to Setup-Employees.json [EmployeesFile]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LoadTestSetupEmployees LoadTest100\\Setup-Employees.json");
        console.DisplayTextLine("          LoadTestSetupEmployees LoadTest10000\\Setup-Employees.json");
    }
}
