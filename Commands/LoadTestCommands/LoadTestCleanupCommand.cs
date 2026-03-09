using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Delete load test employees (identified by filter pattern in identifier)</summary>
[Command("LoadTestCleanup")]
// ReSharper disable once UnusedType.Global
internal sealed class LoadTestCleanupCommand : CommandBase<LoadTestCleanupParameters>
{
    public override bool BackendCommand => true;

    /// <summary>Delete load test employees</summary>
    protected override async Task<int> Execute(CommandContext context, LoadTestCleanupParameters parameters)
    {
        var console = context.Console;

        DisplayTitle(console, "Load test cleanup (delete test employees)");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Tenant           {parameters.TenantIdentifier}");
            console.DisplayTextLine($"Filter pattern   {parameters.FilterPattern}");
        }
        console.DisplayNewLine();

        try
        {
            // resolve tenant
            var tenantService = new TenantService(context.HttpClient);
            var tenant = await tenantService.GetAsync<Tenant>(new(), parameters.TenantIdentifier);
            if (tenant == null)
            {
                console.DisplayErrorLine($"Tenant {parameters.TenantIdentifier} not found.");
                return -1;
            }

            var tenantContext = new TenantServiceContext(tenant.Id);
            var employeeService = new EmployeeService(context.HttpClient);

            // query employees matching the filter pattern
            var query = new DivisionQuery
            {
                Filter = $"contains(Identifier, '{parameters.FilterPattern}')"
            };
            var employees = await employeeService.QueryAsync<Employee>(tenantContext, query);

            if (employees == null || employees.Count == 0)
            {
                console.DisplayTextLine("No matching employees found.");
                return 0;
            }

            console.DisplayTextLine($"Found {employees.Count} employees matching '{parameters.FilterPattern}'");

            // delete employees
            var stopwatch = Stopwatch.StartNew();
            var deleted = 0;
            foreach (var employee in employees)
            {
                await employeeService.DeleteAsync(tenantContext, employee.Id);
                deleted++;

                // progress every 100
                if (deleted % 100 == 0)
                {
                    console.DisplayTextLine($"  Deleted {deleted}/{employees.Count}...");
                }
            }
            stopwatch.Stop();

            console.DisplayNewLine();
            console.DisplaySuccessLine(
                $"Deleted {deleted} employees in {stopwatch.ElapsedMilliseconds}ms " +
                $"({(deleted > 0 ? stopwatch.ElapsedMilliseconds / (double)deleted : 0):F1}ms/employee)");

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
        LoadTestCleanupParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LoadTestCleanup");
        console.DisplayTextLine("      Delete load test employees from a tenant");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Tenant identifier [TenantIdentifier]");
        console.DisplayTextLine("          2. Filter pattern for employee identifier (optional, default: -C) [FilterPattern]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LoadTestCleanup MyTenant");
        console.DisplayTextLine("          LoadTestCleanup MyTenant -C00");
    }
}
