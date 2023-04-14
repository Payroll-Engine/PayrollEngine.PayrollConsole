using System;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrollResultsCommand : HttpCommandBase
{
    internal PayrollResultsCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Show payroll payrun results
    /// </summary>
    /// <param name="tenantIdentifier">The identifier of the tenant</param>
    /// <param name="topFilter">Number of payruns to show</param>
    /// <param name="exportMode">Export mode</param>
    internal async Task<ProgramExitCode> CreateReportAsync(string tenantIdentifier, int topFilter, ReportExportMode exportMode)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }

        DisplayTitle("Payroll results");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Top filter       {topFilter} Jobs");
        ConsoleTool.DisplayTextLine($"Export mode      {exportMode}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");

        try
        {
            var payrollReport = new PayrollResultsReport(HttpClient, topFilter, exportMode);
            await payrollReport.ConsoleWriteAsync(tenantIdentifier);
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
        ConsoleTool.DisplayTitleLine("- PayrollReport");
        ConsoleTool.DisplayTextLine("      Report payroll data to screen and/or file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name");
        ConsoleTool.DisplayTextLine("          2. result of top <count> payrun jobs (default: 1, max: 100)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine($"         result export mode: /export or /noexport (CSV report {PayrollResultsReport.ResultsFolderName}, default=noexport)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrollReport MyTenantName");
        ConsoleTool.DisplayTextLine("          PayrollReport MyTenantName 3");
        ConsoleTool.DisplayTextLine("          PayrollReport MyTenantName 3 /export");
    }
}