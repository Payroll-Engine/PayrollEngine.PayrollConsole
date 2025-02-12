using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Scripting.Script;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("PayrollImport")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollImportCommand : CommandBase<PayrollImportParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <summary>Import a tenant from a JSON file</summary>
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
            // read source file
            var exchange = await ExchangeReader.ReadAsync(parameters.SourceFileName, parameters.Namespace);

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

            context.Console.DisplaySuccessLine($"Payroll successfully imported from {new FileInfo(parameters.SourceFileName).FullName}");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
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

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrollImportParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollImport");
        console.DisplayTextLine("      Import payroll data from json/zip file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. source file name (json or zip) [SourceFileName]");
        console.DisplayTextLine("          2. import options file name ExchangeImportOptions json (optional) [OptionsFileName]");
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