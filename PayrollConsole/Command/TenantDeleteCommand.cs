using System;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class TenantDeleteCommand : HttpCommandBase
{
    internal TenantDeleteCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> DeleteAsync(string tenantIdentifier, ObjectDeleteMode deleteMode)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new ArgumentException("Missing tenant identifier", nameof(tenantIdentifier));
        }

        // user info
        DisplayTitle("Delete tenant");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayTextLine($"Delete mode      {deleteMode}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // tenant
            var service = new TenantService(HttpClient);
            var tenant = await service.GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                switch (deleteMode)
                {
                    // enforced delete
                    case ObjectDeleteMode.Delete:
                        ConsoleTool.DisplayErrorLine($"Unknown tenant {tenantIdentifier}");
                        break;
                    // optional delete
                    case ObjectDeleteMode.TryDelete:
                        ConsoleTool.DisplayInfoLine($"Tenant {tenantIdentifier} not available for deletion");
                        break;
                }
                return ProgramExitCode.Ok;
            }

            // delete
            ConsoleTool.DisplayInfo($"Deleting tenant {tenant.Identifier}...");
            await HttpClient.DeleteAsync(TenantApiEndpoints.TenantsUrl(), tenant.Id);
            ConsoleTool.DisplayInfoLine("done.");

            // test
            tenant = await service.GetAsync<Tenant>(new(), tenantIdentifier);
            ConsoleTool.DisplayNewLine();
            if (tenant == null)
            {
                ConsoleTool.DisplaySuccessLine($"Tenant {tenantIdentifier} successfully deleted");
            }
            else
            {
                ConsoleTool.DisplayErrorLine($"Tenant {tenantIdentifier} not deleted");
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
        ConsoleTool.DisplayTitleLine("- TenantDelete");
        ConsoleTool.DisplayTextLine("      Delete a tenant");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant identifier [Tenant]");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          object delete mode: /delete or /trydelete (default: delete)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          TenantDelete MyTenantName");
        ConsoleTool.DisplayTextLine("          TenantDelete MyTenantName /trydelete");
    }
}