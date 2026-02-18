using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting.Script;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll import command
/// </summary>
[Command("PayrollImport")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollImportCommand : CommandBase<PayrollImportParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <summary>Import a tenant from a JSON file</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrollImportParameters parameters)
    {
        DisplayTitle(context.Console, "Payroll import");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            if (!string.IsNullOrWhiteSpace(parameters.Namespace))
            {
                context.Console.DisplayTextLine($"Namespace        {parameters.Namespace}");
            }

            context.Console.DisplayTextLine($"Source file      {parameters.SourceFileName}");
            if (!string.IsNullOrWhiteSpace(parameters.OptionsFileName))
            {
                context.Console.DisplayTextLine($"Options file     {parameters.OptionsFileName}");
            }

            context.Console.DisplayTextLine($"Import mode      {parameters.ImportMode}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // single file
            var fileName = parameters.SourceFileName;
            if (File.Exists(fileName))
            {

                return await ImportFileAsync(context, parameters, parameters.SourceFileName);
            }

            // file mask
            var fileInfo = new FileInfo(parameters.SourceFileName);
            var files = Directory.GetFiles(
                path: fileInfo.DirectoryName ?? Directory.GetCurrentDirectory(),
                searchPattern: fileInfo.Name);
            if (!files.Any())
            {
                context.Console.DisplayErrorLine($"Invalid source files {parameters.SourceFileName}.");
                return -3;
            }
            foreach (var file in files)
            {
                var result = await ImportFileAsync(context, parameters, file);
                if (result != 0)
                {
                    return result;
                }
            }

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private async Task<int> ImportFileAsync(CommandContext context, PayrollImportParameters parameters, string fileName)
    {
        // read source file
        var exchange = await FileReader.Read<Exchange>(fileName);

        // apply namespace
        if (!string.IsNullOrWhiteSpace(parameters.Namespace))
        {
            exchange.ChangeNamespace(parameters.Namespace);
        }

        // options
        var options = string.IsNullOrWhiteSpace(parameters.OptionsFileName)
            ? new ExchangeImportOptions()
            : GetImportOptions(context.Console, parameters.OptionsFileName);
        if (options == null)
        {
            return (int)ProgramExitCode.InvalidOptions;
        }

        // import tenant
        var import = new ExchangeImport(context.HttpClient, exchange, ScriptParser, options, parameters.ImportMode);
        await import.ImportAsync();

        context.Console.DisplaySuccessLine($"Payroll successfully imported from {fileName}");
        return (int)ProgramExitCode.Ok;
    }

    private ExchangeImportOptions GetImportOptions(ICommandConsole console, string optionsFileName)
    {
        if (!File.Exists(optionsFileName))
        {
            console.DisplayErrorLine($"Invalid import option file {optionsFileName}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExchangeImportOptions>(File.ReadAllText(optionsFileName));
        }
        catch (Exception exception)
        {
            ProcessError(console, exception);
            return null;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrollImportParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollImport");
        console.DisplayTextLine("      Import payroll data from JSON/AML/zip file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. source file name with support for file masks (JSON/YAML or zip) [SourceFileName]");
        console.DisplayTextLine("          2. import options file name ExchangeImportOptions (optional) [OptionsFileName]");
        console.DisplayTextLine("          3. namespace (optional) [Namespace]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          import mode: /single or /bulk (default: single)");
        console.DisplayTextLine("      Options (json object):");
        console.DisplayTextLine("          load toggles true/false (default: true):");
        console.DisplayTextLine("              TargetLoad, ScriptLoad, CaseDocumentLoad, ReportTemplateLoad, ReportSchemaLoad, LookupValidation");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrollImport MyImportFile.json");
        console.DisplayTextLine("          PayrollImport MyImportFile.zip");
        console.DisplayTextLine("          PayrollImport MyImportFile.json MyImportOptions.json MyNamespace");
        console.DisplayTextLine("          PayrollImport MyImportFile.json MyImportOptions.json /bulk");
    }
}