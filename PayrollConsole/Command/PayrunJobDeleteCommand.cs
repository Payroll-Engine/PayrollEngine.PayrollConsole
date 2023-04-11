using System;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrunJobDeleteCommand : HttpCommandBase
{
    internal PayrunJobDeleteCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> DeleteAsync(string tenantIdentifier)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new ArgumentException("Missing tenant identifier", nameof(tenantIdentifier));
        }

        // display
        DisplayTitle("Delete payrun jobs");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // tenant
            var tenants = new TenantService(HttpClient);
            var tenant = await tenants.GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                throw new PayrollException($"Invalid tenant {tenantIdentifier}");
            }

            // payrun jobs
            var jobsService = new PayrunJobService(HttpClient);
            var payrunJobs = (await jobsService.QueryAsync<PayrunJob>(new(tenant.Id))).ToList();
            if (!payrunJobs.Any())
            {
                ConsoleTool.DisplayInfoLine($"No payrun jobs available for tenant {tenant.Identifier}");
            }
            else
            {
                // delete all payrun jobs
                foreach (var payrunJob in payrunJobs)
                {
                    ConsoleTool.DisplayInfo($"Deleting payrun job {payrunJob.Name}...");
                    await HttpClient.DeleteAsync(PayrunApiEndpoints.PayrunJobUrl(tenant.Id, payrunJob.Id));
                    ConsoleTool.DisplayInfoLine("done.");
                }
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplaySuccessLine($"Deleted {payrunJobs.Count} payrun jobs");
            }

            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- PayrunJobDelete");
        ConsoleTool.DisplayTextLine("      Delete a payrun jobs including all payroll results");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrunJobDelete MyTenantName");
    }
}