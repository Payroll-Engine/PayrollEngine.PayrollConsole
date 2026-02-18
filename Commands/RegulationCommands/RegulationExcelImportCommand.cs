using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.IO;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation excel import command
/// </summary>
[Command("RegulationExcelImport")]
// ReSharper disable once UnusedType.Global
internal sealed class RegulationExcelImportCommand : CommandBase<RegulationExcelImportParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <inheritdoc />
    public override bool BackendCommand => true;

    /// <summary>Import a tenant from an Excel file</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, RegulationExcelImportParameters parameters)
    {
        // source file
        if (!File.Exists(parameters.SourceFileName))
        {
            context.Console.WriteErrorLine($"Missing source file {parameters.SourceFileName}");
            return (int)ProgramExitCode.InvalidOptions;
        }
        var sourceFileInfo = new FileInfo(parameters.SourceFileName);

        // target file
        var targetFileName = parameters.TargetFileName;
        if (string.IsNullOrWhiteSpace(parameters.TargetFileName))
        {
            targetFileName = sourceFileInfo.Name.Replace(sourceFileInfo.Extension, FileExtensions.Json);
        }
        var targetFileInfo = new FileInfo(targetFileName);

        // display
        DisplayTitle(context.Console, "Regulation Excel convert");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Source file      {sourceFileInfo.Name}");
            context.Console.DisplayTextLine($"Target file      {targetFileInfo.Name}");
            context.Console.DisplayTextLine($"Import mode      {parameters.ImportMode}");
        }
        context.Console.DisplayNewLine();

        try
        {
            context.Console.DisplayInfoLine($"Importing excel {sourceFileInfo.Name}.");
            context.Console.DisplayNewLine();

            // read regulation from excel
            var exchange = await Excel.ExchangeRegulationImport.ReadAsync(parameters.SourceFileName);

            // file import
            var importFile = parameters.ImportMode is ImportMode.File or ImportMode.All;
            if (importFile)
            {
                await FileWriter.Write(exchange, targetFileName);
            }

            // backend import
            var importBackend = parameters.ImportMode is ImportMode.Backend or ImportMode.All;
            if (importBackend)
            {
                await new ExchangeImport(
                    httpClient: context.HttpClient,
                    exchange: exchange,
                    scriptParser: ScriptParser,
                    importOptions: new ExchangeImportOptions()).ImportAsync();
            }

            // notification
            if (importFile && importBackend)
            {
                context.Console.DisplaySuccessLine($"Imported excel {sourceFileInfo.Name} to backend and JSON file {targetFileInfo.FullName}.");
            }
            else if (importFile)
            {
                context.Console.DisplaySuccessLine($"Imported excel {sourceFileInfo.Name} to JSON file {targetFileInfo.FullName}.");
            }
            else if (importBackend)
            {
                context.Console.DisplaySuccessLine($"Imported excel {sourceFileInfo.Name} to backend.");
            }
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
        RegulationExcelImportParameters.ParseFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- RegulationExcelImport");
        console.DisplayTextLine("      Import payroll data from Excel file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Excel file name [SourceFileName]");
        console.DisplayTextLine("          2. Target JSON/YAML file name (optional) [TargetFileName]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          import mode: /file, /backend or /all (default: file)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          RegulationExcelImport MyImportFile.xlsx");
        console.DisplayTextLine("          RegulationExcelImport MyImportFile.xlsx MyExportFile.json");
        console.DisplayTextLine("          RegulationExcelImport MyImportFile.xlsx MyExportFile.yaml");
    }
}