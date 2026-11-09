using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Import lookup from raw text file, converting a file lines into lookup values.
/// </summary>
[Command("LookupTextImport")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
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
        DisplayTitle(console, "Convert text file to lookup");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            console.DisplayTextLine($"Regulation       {parameters.Regulation}");
            console.DisplayTextLine($"Source file      {parameters.SourceFileName}");
            console.DisplayTextLine($"Mapping file     {parameters.MappingFileName}");
            console.DisplayTextLine($"Target folder    {targetFolder}");
            console.DisplayTextLine($"Slice size       {parameters.SliceSize}");
            console.DisplayTextLine($"Import mode      {parameters.ImportMode}");
            console.DisplayTextLine($"Import target    {parameters.ImportTarget}");
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

    private static TextTableMap ReadLookupMapping(string fileName)
    {
        // mapping from json text file
        var mapping = TextTableMap.Load(fileName);

        // validate
        var errors = mapping.Validate();
        if (errors.Count > 0)
        {
            throw new PayrollException($"Invalid text to lookup mapping {fileName}: {string.Join("; ", errors)}.");
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
        console.DisplayTextLine("            'filters': [filterMap]");
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
        console.DisplayTextLine(string.Empty);
        console.DisplayTextLine("          Mapping JSON object filter map (optional, all filters must match):");
        console.DisplayTextLine("          - start [int]: column start index in line (0...n), column index for tab-delimited lines");
        console.DisplayTextLine("          - length [int]: column character count (1...n)");
        console.DisplayTextLine("          - values [string]: accepted column texts (trimmed, case-sensitive)");
        console.DisplayTextLine("          Lines rejected by a filter are skipped without warning (e.g. header and trailer records):");
        console.DisplayTextLine("            'filters': [ { 'start': 0, 'length': 2, 'values': ['06', '11'] } ]");
    }
}
