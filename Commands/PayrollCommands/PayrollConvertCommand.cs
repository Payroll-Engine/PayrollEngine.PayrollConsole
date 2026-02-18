using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.IO;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Scripting.Script;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Convert payroll JSON from/to YAML command
/// </summary>
[Command("PayrollConvert")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollConvertCommand : CommandBase<PayrollConvertParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <summary>Convert a JSON or YAML file</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrollConvertParameters parameters)
    {
        DisplayTitle(context.Console, "Payroll convert");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File             {parameters.FileName}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }
        context.Console.DisplayNewLine();

        try
        {
            // single file
            var fileName = parameters.FileName;
            if (File.Exists(fileName))
            {

                return await ConvertFileAsync(context, parameters.FileName, parameters.SchemaType);
            }

            // file mask
            var fileInfo = new FileInfo(parameters.FileName);
            var files = Directory.GetFiles(
                path: fileInfo.DirectoryName ?? Directory.GetCurrentDirectory(),
                searchPattern: fileInfo.Name,
                searchOption: parameters.DirectoryMode == DirectoryMode.Recursive ?
                    SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (!files.Any())
            {
                context.Console.DisplayErrorLine($"Missing source file {parameters.FileName}.");
                return -3;
            }
            foreach (var file in files)
            {
                try
                {
                    var result = await ConvertFileAsync(context, file, parameters.SchemaType);
                    if (result != 0)
                    {
                        return result;
                    }
                }
                catch (Exception exception)
                {
                    context.Console.DisplayErrorLine($"Error in {new FileInfo(file).FullName}: {exception.GetBaseMessage()}");
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

    /// <summary>
    /// Convert a file between json and yaml
    /// </summary>
    /// <param name="context">Convert context</param>
    /// <param name="fileName">File to convert</param>
    /// <param name="schema">Schema type</param>
    private async Task<int> ConvertFileAsync(CommandContext context, string fileName, SchemaType schema)
    {
        var extension = Path.GetExtension(fileName);

        string result = null;

        // json
        if (string.Equals(extension, FileExtensions.Json, StringComparison.InvariantCultureIgnoreCase))
        {
            result = await JsonToYamlAsync(fileName, schema);
        }

        // yaml
        if (string.Equals(extension, FileExtensions.Yaml, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(extension, FileExtensions.Yml, StringComparison.InvariantCultureIgnoreCase))
        {
            result = await YamlToJsonAsync(fileName, schema);
        }

        if (result == null)
        {
            throw new PayrollException($"Unsupported file type {fileName}.");
        }

        context.Console.DisplaySuccessLine($"Converted payroll successfully to {result}");
        return (int)ProgramExitCode.Ok;
    }

    /// <summary>
    /// Convert json to yaml
    /// </summary>
    /// <param name="fileName">File to convert</param>
    /// <param name="schema">Schema type</param>
    /// <returns>Converted output file name</returns>
    private async Task<string> JsonToYamlAsync(string fileName, SchemaType schema)
    {
        // auto schema detection
        if (schema == SchemaType.Auto)
        {
            schema = GetFileSchema(fileName);
        }

        // read json
        object obj;
        switch (schema)
        {
            case SchemaType.Exchange:
                var exchange = await JsonReader.FromFileAsync<Client.Model.Exchange>(fileName);
                await new AttachmentsLoader(exchange, ScriptParser).ReadAsync();
                obj = exchange;
                break;
            case SchemaType.CaseTest:
                obj = await JsonReader.FromFileAsync<Client.Test.Case.CaseTest>(fileName);
                break;
            case SchemaType.ReportTest:
                obj = await JsonReader.FromFileAsync<Client.Test.Report.ReportTest>(fileName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(schema), schema, null);
        }

        // save yaml
        var targetFileName = Path.ChangeExtension(fileName, FileExtensions.Yaml);
        await YamlWriter.ToFileAsync(obj, targetFileName);
        return targetFileName;
    }

    /// <summary>
    /// Convert yaml to json
    /// </summary>
    /// <param name="fileName">File to convert</param>
    /// <param name="schema">Schema type</param>
    /// <returns>Converted output file name</returns>
    private async Task<string> YamlToJsonAsync(string fileName, SchemaType schema)
    {
        // auto schema detection
        if (schema == SchemaType.Auto)
        {
            schema = GetFileSchema(fileName);
        }

        // read yaml
        object obj;
        switch (schema)
        {
            case SchemaType.Exchange:
                var exchange = await YamlReader.FromFileAsync<Client.Model.Exchange>(fileName);
                await new AttachmentsLoader(exchange, ScriptParser).ReadAsync();
                obj = exchange;
                break;
            case SchemaType.CaseTest:
                obj = await YamlReader.FromFileAsync<Client.Test.Case.CaseTest>(fileName);
                break;
            case SchemaType.ReportTest:
                obj = await YamlReader.FromFileAsync<Client.Test.Report.ReportTest>(fileName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(schema), schema, null);
        }

        // save json
        var targetFileName = Path.ChangeExtension(fileName, FileExtensions.Json);
        await JsonWriter.ToFileAsync(obj, targetFileName);
        return targetFileName;
    }

    private static SchemaType GetFileSchema(string fileName)
    {
        var fileInfo = new FileInfo(fileName);
        var baseName = fileInfo.Name.RemoveFromEnd(fileInfo.Extension);

        // payrun and employee test
        if (baseName.EndsWith(TestFileExtensions.PayrunTest) ||
            baseName.EndsWith(TestFileExtensions.EmployeeTest))
        {
            return SchemaType.Exchange;
        }

        // case test
        if (baseName.EndsWith(TestFileExtensions.CaseTest))
        {
            return SchemaType.CaseTest;
        }

        // report test
        if (baseName.EndsWith(TestFileExtensions.ReportTest))
        {
            return SchemaType.ReportTest;
        }

        // default schema
        return SchemaType.Exchange;
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrollConvertParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollConvert");
        console.DisplayTextLine("      Convert payroll JSON from/to YAML");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name with support for file masks (JSON/YAML or zip) [FileName]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: scope with file mask /top or /recursive (default: top)");
        console.DisplayTextLine("          schema type: /auto, /exchange, /casetest or /reporttest (default: auto)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrollConvert MyPayrollFile.json");
        console.DisplayTextLine("          PayrollConvert *.yaml /recursive");
        console.DisplayTextLine("          PayrollConvert *.yaml /recursive /casetest");
    }
}