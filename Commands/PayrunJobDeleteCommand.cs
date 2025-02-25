using System;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Payrun job delete command
/// </summary>
[Command("PayrunJobDelete")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunJobDeleteCommand : CommandBase<PayrunJobDeleteParameters>
{
    /// <summary>Delete payrun job</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrunJobDeleteParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new ArgumentException("Missing tenant identifier.", nameof(parameters.Tenant));
        }

        // display
        DisplayTitle(context.Console, "Payrun job delete");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // tenant
            var tenants = new TenantService(context.HttpClient);
            var tenant = await tenants.GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                throw new PayrollException($"Invalid tenant {parameters.Tenant}.");
            }

            // payrun jobs
            var jobsService = new PayrunJobService(context.HttpClient);
            var payrunJobs = (await jobsService.QueryAsync<PayrunJob>(new(tenant.Id))).ToList();
            if (!payrunJobs.Any())
            {
                context.Console.DisplayInfoLine($"No payrun jobs available for tenant {tenant.Identifier}");
            }
            else
            {
                // delete all payrun jobs
                foreach (var payrunJob in payrunJobs)
                {
                    context.Console.DisplayInfo($"Deleting payrun job {payrunJob.Name}...");
                    await context.HttpClient.DeleteAsync(PayrunApiEndpoints.PayrunJobUrl(tenant.Id, payrunJob.Id));
                    context.Console.DisplayInfoLine("done.");
                }
                context.Console.DisplayNewLine();
                context.Console.DisplaySuccessLine($"Deleted {payrunJobs.Count} payrun jobs");
            }

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunJobDeleteParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunJobDelete");
        console.DisplayTextLine("      Delete a payrun job with payroll results");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunJobDelete MyTenantName");
    }
}