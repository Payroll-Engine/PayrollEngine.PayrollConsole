using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.PayrollConsole.Commands.Excel;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Payroll import excel command
/// </summary>
[Command("PayrollImportExcel")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollImportExcelCommand : CommandBase<PayrollImportExcelParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <summary>Import a tenant from an Excel file</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrollImportExcelParameters parameters)
    {
        DisplayTitle(context.Console, "Payroll import from Excel");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            if (!string.IsNullOrWhiteSpace(parameters.Tenant))
            {
                context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            }

            context.Console.DisplayTextLine($"File             {parameters.FileName}");
            context.Console.DisplayTextLine($"Import mode      {parameters.ImportMode}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // read Excel
            var exchange = await new ExchangeExcelReader(context.HttpClient).ReadAsync(parameters.FileName, parameters.Tenant);
            // import tenant
            var import = new ExchangeImport(context.HttpClient, exchange, ScriptParser, importMode: parameters.ImportMode);
            await import.ImportAsync();

            context.Console.DisplaySuccessLine($"Payroll successfully imported from {new FileInfo(parameters.FileName).FullName}");
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
        PayrollImportExcelParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollImportExcel");
        console.DisplayTextLine("      Import payroll data from Excel file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Excel file name [FileName]");
        console.DisplayTextLine("          2. tenant name (optional) [Tenant]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          import mode: /single or /bulk (default: single)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrollImportExcel MyImportFile.xlsx");
        console.DisplayTextLine("          PayrollImportExcel MyImportFile.xlsx /noupdate /bulk");
    }
}