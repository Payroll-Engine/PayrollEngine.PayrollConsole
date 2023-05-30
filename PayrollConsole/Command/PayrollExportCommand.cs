using System;
using System.IO;
using System.Text.Json;
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
    /// <param name="targetFileName">The target file name</param>
    /// <param name="optionsFileName">The options file name</param>
    /// <param name="namespace">The export namespace</param>
    internal async Task<ProgramExitCode> ExportAsync(string tenantIdentifier, string targetFileName,
        string optionsFileName = null, string @namespace = null)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }

        // target file name
        var resolvedFileName = targetFileName;
        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            resolvedFileName = $"{tenantIdentifier}_{FileTool.CurrentTimeStamp()}{FileExtensions.Json}";
        }
        else if (targetFileName.Contains("{timestamp}", StringComparison.InvariantCultureIgnoreCase))
        {
            resolvedFileName = targetFileName.Replace("{timestamp}", FileTool.CurrentTimeStamp());
        }

        // display
        DisplayTitle("Export tenant");
        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            ConsoleTool.DisplayTextLine($"Namespace        {@namespace}");
        }
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        if (!string.IsNullOrWhiteSpace(optionsFileName))
        {
            ConsoleTool.DisplayTextLine($"Options file     {optionsFileName}");
        }
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayTextLine($"Target file      {resolvedFileName}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // tenant
            var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {tenantIdentifier}");
            }

            // options
            var options = string.IsNullOrWhiteSpace(optionsFileName)
                ? new ExchangeExportOptions()
                : GetExportOptions(optionsFileName);
            if (options == null)
            {
                return ProgramExitCode.InvalidOptions;
            }

            // tenant export
            var export = new ExchangeExport(HttpClient, options, @namespace);
            var exchange = await export.ExportAsync(tenant.Id);
            await ExchangeWriter.WriteAsync(exchange, resolvedFileName);

            // notification
            ConsoleTool.DisplaySuccessLine($"Exported tenant {tenantIdentifier} into file {new FileInfo(resolvedFileName).FullName}");
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return ProgramExitCode.GenericError;
        }
    }

    private ExchangeExportOptions GetExportOptions(string optionsFileName)
    {
        if (!File.Exists(optionsFileName))
        {
            ConsoleTool.DisplayErrorLine($"Invalid export option file {optionsFileName}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExchangeExportOptions>(File.ReadAllText(optionsFileName));
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return null;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- PayrollExport");
        ConsoleTool.DisplayTextLine("      Export payroll data to json/zip file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant file name");
        ConsoleTool.DisplayTextLine("          2. target json file name (default: tenant name)");
        ConsoleTool.DisplayTextLine("          3. export options file name ExchangeExportOptions json (optional)");
        ConsoleTool.DisplayTextLine("          4. namespace (optional)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          export results: /results or /noresults (default: noresults)");
        ConsoleTool.DisplayTextLine("      Options (json object):");
        ConsoleTool.DisplayTextLine("          type filter, list of identifiers or names:");
        ConsoleTool.DisplayTextLine("              Users, Divisions, Employees, Tasks, Webhooks, Regulations, Payrolls, Payruns, PayrunJobs");
        ConsoleTool.DisplayTextLine("          data filter true/false (default: false):");
        ConsoleTool.DisplayTextLine("              ExportWebhookMessages, ExportGlobalCaseValues, ExportNationalCaseValues, ExportCompanyCaseValues, ExportEmployeeCaseValues, ExportPayrollResults");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrollExport MyTenantName");
        ConsoleTool.DisplayTextLine("          PayrollExport MyTenantName MyExportFile.json MyExportOptions.json MyNamespace");
        ConsoleTool.DisplayTextLine("          PayrollExport MyTenantName MyExportFile.json MyExportOptions.json /results");
    }
}