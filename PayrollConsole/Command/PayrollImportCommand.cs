using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrollImportCommand : HttpCommandBase
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    internal PayrollImportCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Import a tenant from a JSON file
    /// </summary>
    /// <param name="fileName">The import file name</param>
    /// <param name="importMode">The import mode</param>
    /// <param name="namespace">The import namespace</param>
    internal async Task<ProgramExitCode> ImportAsync(string fileName, DataImportMode importMode,
        string @namespace = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new PayrollException($"Missing import file {fileName}");
        }

        DisplayTitle("Import tenant");
        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            ConsoleTool.DisplayTextLine($"Namespace        {@namespace}");
        }
        ConsoleTool.DisplayTextLine($"File             {fileName}");
        ConsoleTool.DisplayTextLine($"Import mode      {importMode}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // read file
            var exchange = await ExchangeReader.ReadAsync(fileName, @namespace);
            // import tenant
            var import = new ExchangeImport(HttpClient, exchange, ScriptParser, importMode);
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
        ConsoleTool.DisplayTitleLine("- PayrollImport");
        ConsoleTool.DisplayTextLine("      Import payroll data from JSON/ZIP file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. JSON/ZIP file name");
        ConsoleTool.DisplayTextLine("          2. namespace (optional)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          import mode: /single or /bulk (default: single)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.json");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.zip");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.json MyNamespace");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.json /noupdate /bulk");
    }
}