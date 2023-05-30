using System;
using System.IO;
using System.Text.Json;
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
    /// <param name="sourceFileName">The import file name</param>
    /// <param name="importMode">The import mode</param>
    /// <param name="optionsFileName">The options file name</param>
    /// <param name="namespace">The import namespace</param>
    internal async Task<ProgramExitCode> ImportAsync(string sourceFileName, DataImportMode importMode,
        string optionsFileName = null, string @namespace = null)
    {
        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            throw new PayrollException($"Missing import file {sourceFileName}");
        }

        DisplayTitle("Import tenant");
        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            ConsoleTool.DisplayTextLine($"Namespace        {@namespace}");
        }
        ConsoleTool.DisplayTextLine($"Source file      {sourceFileName}");
        if (!string.IsNullOrWhiteSpace(optionsFileName))
        {
            ConsoleTool.DisplayTextLine($"Options file     {optionsFileName}");
        }
        ConsoleTool.DisplayTextLine($"Import mode      {importMode}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // read source file
            var exchange = await ExchangeReader.ReadAsync(sourceFileName, @namespace);

            // options
            var options = string.IsNullOrWhiteSpace(optionsFileName)
                ? new ExchangeImportOptions()
                : GetImportOptions(optionsFileName);
            if (options == null)
            {
                return ProgramExitCode.InvalidOptions;
            }

            // import tenant
            var import = new ExchangeImport(HttpClient, exchange, ScriptParser, options, importMode);
            await import.ImportAsync();

            ConsoleTool.DisplaySuccessLine($"Payroll successfully imported from {new FileInfo(sourceFileName).FullName}");
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return ProgramExitCode.GenericError;
        }
    }

    private ExchangeImportOptions GetImportOptions(string optionsFileName)
    {
        if (!File.Exists(optionsFileName))
        {
            ConsoleTool.DisplayErrorLine($"Invalid import option file {optionsFileName}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExchangeImportOptions>(File.ReadAllText(optionsFileName));
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return null;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- PayrollImport");
        ConsoleTool.DisplayTextLine("      Import payroll data from json/zip file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. source file name (json or zip)");
        ConsoleTool.DisplayTextLine("          2. import options file name ExchangeImportOptions json (optional)");
        ConsoleTool.DisplayTextLine("          3. namespace (optional)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          import mode: /single or /bulk (default: single)");
        ConsoleTool.DisplayTextLine("      Options (json object):");
        ConsoleTool.DisplayTextLine("          load toggles true/false (default: true):");
        ConsoleTool.DisplayTextLine("              TargetLoad, ScriptLoad, CaseDocumentLoad, ReportTemplateLoad, ReportSchemaLoad, LookupValidation");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.json");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.zip");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.json MyImportOptions.json MyNamespace");
        ConsoleTool.DisplayTextLine("          PayrollImport MyImportFile.json MyImportOptions.json /bulk");
    }
}