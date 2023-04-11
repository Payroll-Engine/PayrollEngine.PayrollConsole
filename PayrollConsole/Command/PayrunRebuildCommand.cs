using System;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrunRebuildCommand : HttpCommandBase
{
    internal PayrunRebuildCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Rebuild the payrun
    /// </summary>
    /// <param name="tenantIdentifier">The identifier of the tenant</param>
    /// <param name="payrunName">The payrun name</param>
    internal async Task<ProgramExitCode> RebuildAsync(string tenantIdentifier, string payrunName)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }
        if (string.IsNullOrWhiteSpace(payrunName))
        {
            throw new PayrollException("Missing payrun name");
        }

        // display
        DisplayTitle("Rebuild payrun binaries");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Payrun           {payrunName}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        ConsoleTool.DisplayText("Building payrun script...");
        try
        {
            // tenant
            var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {tenantIdentifier}");
            }

            // rebuild
            await new ScriptRebuild(HttpClient, tenant.Id).RebuildPayrunAsync(payrunName);

            // notification
            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplaySuccessLine($"Rebuilt payrun {payrunName} for tenant {tenantIdentifier}");
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (ConsoleTool.DisplayMode == ConsoleDisplayMode.Silent)
            {
                ConsoleTool.WriteErrorLine($"Payrun script build error: {exception.GetBaseMessage()}");
            }
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- PayrunRebuild");
        ConsoleTool.DisplayTextLine("      Rebuild a payrun (update scripting binaries)");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name");
        ConsoleTool.DisplayTextLine("          2. payrun name");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrunRebuild MyTenantName MyPayrunName");
    }
}