using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("PayrollResults")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollResultsCommand : CommandBase<PayrollResultsParameters>
{
    /// <summary>Show payroll payrun results</summary>
    protected override async Task<int> Execute(CommandContext context, PayrollResultsParameters parameters)
    {
        DisplayTitle(context.Console, "Payroll results");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Top filter       {parameters.TopFilter} Jobs");
            context.Console.DisplayTextLine($"Export mode      {parameters.ResultExportMode}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        try
        {
            var payrollReport = new PayrollResultsReport(context.HttpClient, parameters.TopFilter, parameters.ResultExportMode);
            await payrollReport.ConsoleWriteAsync(parameters.Tenant);
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrollResultsParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollReport");
        console.DisplayTextLine("      Report payroll data to screen and/or file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. result of top <count> payrun jobs (default: 1, max: 100) [TopFilter]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine($"         result export mode: /export or /noexport (CSV report {PayrollResultsReport.ResultsFolderName}, default=noexport)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrollReport MyTenantName");
        console.DisplayTextLine("          PayrollReport MyTenantName 3");
        console.DisplayTextLine("          PayrollReport MyTenantName 3 /export");
    }
}