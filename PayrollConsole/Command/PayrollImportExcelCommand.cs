using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrollImportExcelCommand : HttpCommandBase
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    internal PayrollImportExcelCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Import a tenant from an Excel file
    /// </summary>
    /// <param name="fileName">The Excel file</param>
    /// <param name="importMode">The import mode</param>
    /// <param name="overrideTenant">The override tenant name</param>
    internal async Task<ProgramExitCode> ImportAsync(string fileName, DataImportMode importMode,
        string overrideTenant = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new PayrollException($"Missing Excel import file {fileName}");
        }

        DisplayTitle("Import tenant from Excel");
        if (!string.IsNullOrWhiteSpace(overrideTenant))
        {
            ConsoleTool.DisplayTextLine($"Tenant           {overrideTenant}");
        }
        ConsoleTool.DisplayTextLine($"File             {fileName}");
        ConsoleTool.DisplayTextLine($"Import mode      {importMode}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // read Excel
            var exchange = await new ExchangeExcelReader(HttpClient).ReadAsync(fileName, overrideTenant);
            // import tenant
            var import = new ExchangeImport(HttpClient, exchange, ScriptParser, importMode: importMode);
            await import.ImportAsync();

            ConsoleTool.DisplaySuccessLine($"Payroll successfully imported from {new FileInfo(fileName).FullName}");
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
        ConsoleTool.DisplayTitleLine("- PayrollImportExcel");
        ConsoleTool.DisplayTextLine("      Import payroll data from Excel file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. Excel file name [FileName]");
        ConsoleTool.DisplayTextLine("          2. tenant name (optional) [Tenant]");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          import mode: /single or /bulk (default: single)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrollImportExcel MyImportFile.xlsx");
        ConsoleTool.DisplayTextLine("          PayrollImportExcel MyImportFile.xlsx /noupdate /bulk");
    }
}