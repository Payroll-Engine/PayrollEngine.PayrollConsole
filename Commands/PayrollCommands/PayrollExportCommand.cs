using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.IO;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll export command
/// </summary>
[Command("PayrollExport")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollExportCommand : CommandBase<PayrollExportParameters>
{
    /// <summary>Export a tenant to a JSON file.
    /// Default the file name is the tenant identifier including a timestamp</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrollExportParameters parameters)
    {
        // target file name
        var resolvedFileName = parameters.TargetFileName;
        if (string.IsNullOrWhiteSpace(parameters.TargetFileName))
        {
            resolvedFileName = $"{parameters.Tenant}_{FileTool.CurrentTimeStamp()}{FileExtensions.Json}";
        }
        else if (parameters.TargetFileName.Contains("{timestamp}", StringComparison.InvariantCultureIgnoreCase))
        {
            resolvedFileName = parameters.TargetFileName.Replace("{timestamp}", FileTool.CurrentTimeStamp());
        }

        // display
        DisplayTitle(context.Console, "Payroll export");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            if (!string.IsNullOrWhiteSpace(parameters.Namespace))
            {
                context.Console.DisplayTextLine($"Namespace        {parameters.Namespace}");
            }

            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            if (!string.IsNullOrWhiteSpace(parameters.OptionsFileName))
            {
                context.Console.DisplayTextLine($"Options file     {parameters.OptionsFileName}");
            }

            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
            context.Console.DisplayTextLine($"Target file      {resolvedFileName}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // tenant
            var tenant = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {parameters.Tenant}.");
            }

            // options
            var options = string.IsNullOrWhiteSpace(parameters.OptionsFileName)
                ? new ExchangeExportOptions()
                : GetExportOptions(context.Console, parameters.OptionsFileName);
            if (options == null)
            {
                return (int)ProgramExitCode.InvalidOptions;
            }

            // tenant export
            var export = new ExchangeExport(context.HttpClient, options, parameters.Namespace);
            var exchange = await export.ExportAsync(tenant.Id);
            await FileWriter.Write(exchange, resolvedFileName);

            // notification
            context.Console.DisplaySuccessLine($"Exported tenant {parameters.Tenant} into file {new FileInfo(resolvedFileName).FullName}");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private ExchangeExportOptions GetExportOptions(ICommandConsole console, string optionsFileName)
    {
        if (!File.Exists(optionsFileName))
        {
            console.DisplayErrorLine($"Invalid export option file {optionsFileName}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExchangeExportOptions>(File.ReadAllText(optionsFileName));
        }
        catch (Exception exception)
        {
            ProcessError(console, exception);
            return null;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrollExportParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollExport");
        console.DisplayTextLine("      Export payroll data to JSON/YAML/zip file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant file name [Tenant]");
        console.DisplayTextLine("          2. target JSON/YAML file name (default: tenant name) [TargetFileName]");
        console.DisplayTextLine("          3. export options file name ExchangeExportOptions (optional) [OptionsFileName]");
        console.DisplayTextLine("          4. namespace (optional) [Namespace]");
        console.DisplayTextLine("      Options (JSON object):");
        console.DisplayTextLine("          type filter, list of identifiers or names:");
        console.DisplayTextLine("              Users, Divisions, Employees, Tasks, Webhooks, Regulations, Payrolls, Payruns, PayrunJobs");
        console.DisplayTextLine("          data filter true/false (default: false):");
        console.DisplayTextLine("              ExportWebhookMessages, ExportGlobalCaseValues, ExportNationalCaseValues, ExportCompanyCaseValues, ExportEmployeeCaseValues, ExportPayrollResults");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrollExport MyTenantName");
        console.DisplayTextLine("          PayrollExport MyTenantName MyExportFile.json MyExportOptions.json");
        console.DisplayTextLine("          PayrollExport MyTenantName MyExportFile.json MyExportOptions.json MyNamespace");
    }
}