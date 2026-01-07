using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Serialization;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Import lookup from raw text file, converting a file lines into lookup values.
/// </summary>
[Command("LookupTextImport")]
// ReSharper disable once UnusedType.Global
public sealed class LookupTextImportCommand : CommandBase<LookupTextImportParameters>
{
    /// <inheritdoc />
    public override bool BackendCommand => true;

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LookupTextImportParameters parameters)
    {
        var console = context.Console;

        // target folder
        var targetFolder = parameters.TargetFolder;
        if (string.IsNullOrWhiteSpace(targetFolder))
        {
            targetFolder = Directory.GetCurrentDirectory();
        }


        // header
        console.DisplayTitleLine("Convert text file to lookup");
        console.DisplayTextLine($"Mapping file     {targetFolder}");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Target folder    {targetFolder}");
        }

        console.DisplayNewLine();

        try
        {
            // tenant
            var tenant = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                console.DisplayErrorLine($"Invalid tenant {parameters.Tenant}.");
                return -3;
            }

            // regulation
            var regulation = await new RegulationService(context.HttpClient).GetAsync<Regulation>(
                new(tenant.Id), parameters.Regulation);
            if (regulation == null)
            {
                console.DisplayErrorLine($"Invalid regulation {parameters.Regulation}.");
                return -3;
            }

            // convert context
            var convertContext = new LookupTextImportContext
            {
                HttpClient = context.HttpClient,
                Logger = context.Logger,
                Console = console,
                Mapping = ReadLookupMapping(parameters.MappingFileName),
                TargetFolder = targetFolder
            };

            // rule instruction convert
            var converter = new LookupTextImport(convertContext, parameters);
            var fileCount = await converter.ImportAsync();
            if (fileCount == 0)
            {
                console.DisplayInfoLine("No tax files converted.");
                return 0;
            }
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return -2;
        }

        return 0;
    }

    private static LookupTextMap ReadLookupMapping(string fileName)
    {
        // mapping from json text file
        var json = File.ReadAllText(fileName);
        var mapping = DefaultJsonSerializer.Deserialize<LookupTextMap>(json);

        // validate
        if (mapping?.Key == null && (mapping?.Keys == null || !mapping.Keys.Any()))
        {
            throw new PayrollException($"Text value map without key mapping: {mapping}.");
        }
        if (mapping.Value == null && (mapping.Values == null || !mapping.Values.Any()))
        {
            throw new PayrollException($"Text value map without value mapping: {mapping}.");
        }

        return mapping;
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LookupTextImportParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LookupTextImport");
        console.DisplayTextLine("      Import regulation lookups to backend and/or file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Tenant identifier [Tenant] (required)");
        console.DisplayTextLine("          2. Regulation name [Regulation] (required)");
        console.DisplayTextLine("          3. Text file name with file mask support [SourceFileName] (required)");
        console.DisplayTextLine("          4. Text to lookup mapping JSON file [MappingFileName] (required)");
        console.DisplayTextLine("          5. Target output folder [TargetFolder] (default: current folder)");
        console.DisplayTextLine("          6. Lookup slice size as integer, 0=off [SliceSize] (default: 0/off)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          import mode: /single or /bulk (default: single)");
        console.DisplayTextLine("          import target: /backend, /file or /all (default: backend)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LookupTextImport MyTenant MyRegulation MyTax.txt MyTaxMap.json /bulk");
        console.DisplayTextLine("          LookupTextImport MyTenant MyRegulation MyTax.txt MyTaxMap.json MyOutputPath /bulk /file");
        console.DisplayTextLine("          LookupTextImport MyTenant MyRegulation MyTax.txt MyTaxMap.json MyOutputPath 30000 /bulk /all");
        console.DisplayTextLine(string.Empty);
        console.DisplayTextLine("      Mapping JSON object:");
        console.DisplayTextLine("          {");
        console.DisplayTextLine("            'key': valueMap");
        console.DisplayTextLine("            'keys': [valueMap]");
        console.DisplayTextLine("            'rangeValue': valueMap");
        console.DisplayTextLine("            'value': valueMap");
        console.DisplayTextLine("            'values': [valueMap]");
        console.DisplayTextLine("          }");
        console.DisplayTextLine(string.Empty);
        console.DisplayTextLine("          Mapping JSON object value map:");
        console.DisplayTextLine("          - name [string]: value field name (required for 'values' map)");
        console.DisplayTextLine("          - valueType [string]: text | decimal | integer | boolean (default: text)");
        console.DisplayTextLine("          - start [int]: value start index in line (0...n)");
        console.DisplayTextLine("          - length [int]: value character count (1...n)");
        console.DisplayTextLine("          - decimalPlaces [int]: decimal places for value type 'decimal' (default: 0)");
    }
}