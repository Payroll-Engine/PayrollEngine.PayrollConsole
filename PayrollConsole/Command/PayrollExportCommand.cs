using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.IO;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrollExportCommand : HttpCommandBase
{
    internal PayrollExportCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Export a tenant to a JSON file
    /// By default the file name is the tenant name including a timestamp
    /// </summary>
    /// <param name="tenantIdentifier">The identifier of the tenant</param>
    /// <param name="fileName">The target file</param>
    /// <param name="exportMode">Exclude payroll results to the target file</param>
    /// <param name="namespace">The export namespace</param>
    internal async Task<ProgramExitCode> ExportAsync(string tenantIdentifier, string fileName,
        ResultExportMode exportMode, string @namespace = null)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }

        // display
        DisplayTitle("Export tenant");
        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            ConsoleTool.DisplayTextLine($"Namespace        {@namespace}");
        }
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"File             {fileName}");
        ConsoleTool.DisplayTextLine($"Export mode      {exportMode}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // tenant
            var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {tenantIdentifier}");
            }

            // target file
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"{tenant.Identifier}_{FileTool.CurrentTimeStamp()}{FileExtensions.Json}";
            }

            // tenant export
            var export = new TenantExport(HttpClient, tenant.Id, exportMode, @namespace);
            var provider = await export.ExportAsync();
            await PayrollJsonWriter.WriteAsync(provider, fileName);

            // notification
            ConsoleTool.DisplaySuccessLine($"Exported tenant {tenantIdentifier} into file {new FileInfo(fileName).FullName}");
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
        ConsoleTool.DisplayTitleLine("- PayrollExport");
        ConsoleTool.DisplayTextLine("      Export payroll data to JSON file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name");
        ConsoleTool.DisplayTextLine("          2. target JSON file name (default: tenant name)");
        ConsoleTool.DisplayTextLine("          3. namespace (optional)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          export results: /results or /noresults (default: noresults)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrollExport MyTenantName");
        ConsoleTool.DisplayTextLine("          PayrollExport MyTenantName MyExportFile.json MyNamespace");
        ConsoleTool.DisplayTextLine("          PayrollExport MyTenantName MyExportFile.json /results");
    }
}