using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Writes the lex values into the lookup values of the data regulation.
/// The mapping is the one of LexCheck (Lookup.Key); only differing values are replaced.
/// </summary>
[Command("LexApply")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexApplyCommand : CommandBase<LexApplyParameters>
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexApplyParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Apply");
        var dryRun = parameters.ApplyMode == LexApplyMode.DryRun;
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File mask        {parameters.FileMask}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
            context.Console.DisplayTextLine($"Apply mode       {parameters.ApplyMode}");
        }
        context.Console.DisplayNewLine();

        try
        {
            var files = FileResolver.Resolve(parameters.FileMask, parameters.DirectoryMode == DirectoryMode.Recursive);
            if (files.Count == 0)
            {
                context.Console.DisplayErrorLine($"No lex.json files found matching: {parameters.FileMask}");
                return (int)ProgramExitCode.InvalidInput;
            }

            int changed = 0, added = 0, tables = 0, unchanged = 0, skipped = 0, failed = 0;
            foreach (var lexFile in files)
            {
                var result = await ApplyFile(lexFile, dryRun);
                Display(context.Console, result, dryRun);
                if (result.Error != null)
                {
                    failed++;
                    continue;
                }
                foreach (var change in result.Changes)
                {
                    switch (change.Kind)
                    {
                        case ApplyChangeKind.Added:
                            added++;
                            break;
                        case ApplyChangeKind.Table:
                            tables++;
                            break;
                        default:
                            changed++;
                            break;
                    }
                }
                unchanged += result.Unchanged;
                skipped += result.Skipped.Count;
            }

            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine(
                $"Summary: {changed} changed, {added} added, {tables} table(s), {unchanged} unchanged, {skipped} skipped, {failed} failed" +
                (dryRun ? " (dry run, nothing written)" : string.Empty));
            if (!dryRun && changed + added + tables > 0)
            {
                context.Console.DisplayInfoLine("Check the regulation (LexCheck) and run the regulation tests.");
            }
            return failed > 0 ? (int)ProgramExitCode.InvalidInput : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<ApplyResult> ApplyFile(string lexFilePath, bool dryRun)
    {
        Manifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(lexFilePath), ReadOptions);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return new ApplyResult { LexPath = lexFilePath, Error = "lex.json could not be read: " + exception.Message };
        }
        if (manifest == null)
        {
            return new ApplyResult { LexPath = lexFilePath, Error = "lex.json is empty" };
        }
        var errors = Validator.Validate(manifest, Path.GetDirectoryName(lexFilePath)!);
        if (errors.Count > 0)
        {
            return new ApplyResult { LexPath = lexFilePath, Error = $"lex.json is invalid: {errors[0]}" };
        }
        return Applier.Apply(manifest, lexFilePath, dryRun);
    }

    private static void Display(ICommandConsole console, ApplyResult result, bool dryRun)
    {
        if (result.Error != null)
        {
            console.DisplayErrorLine($"  [FAIL] {result.LexPath} - {result.Error}");
            return;
        }
        if (result.Note != null)
        {
            console.DisplayInfoLine($"  [NOTE] {result.LexPath} - {result.Note}");
            return;
        }
        if (result.Changes.Count == 0)
        {
            console.DisplaySuccessLine($"  [OK] {result.LexPath} - regulation is current ({result.Unchanged} values)");
        }
        else
        {
            console.DisplayInfoLine($"  [{(dryRun ? "DIFF" : "WRITTEN")}] {result.LexPath} - {result.Changes.Count} change(s)");
            foreach (var change in result.Changes)
            {
                var marker = change.Kind == ApplyChangeKind.Added ? "+" : "~";
                console.DisplayTextLine($"         {marker} {change.File}: {change.Target} ({change.Detail})");
            }
        }
        // a lookup file with a text table is stored as archive: its references are updated by hand
        foreach (var replaced in result.ReplacedFiles)
        {
            console.DisplayInfoLine($"         [ZIP] {replaced}: update installFiles (regulation-package.json), the project file and Setup.pecmd");
        }
        foreach (var skip in result.Skipped)
        {
            console.DisplayTextLine($"         [SKIP] {skip}");
        }
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexApplyParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexApply");
        console.DisplayTextLine("      Write the lex values into the lookup values of the data regulation (Regulation/*.json and the");
        console.DisplayTextLine("      JSON entries of Regulation/*.zip).");
        console.DisplayTextLine("      Mapping as in LexCheck: lex key = Lookup.Key, a further key part addresses a property");
        console.DisplayTextLine("      of the value object; a contentFile key may end with the year of validFrom.");
        console.DisplayTextLine("      Only differing values are replaced, missing values of an existing lookup are added;");
        console.DisplayTextLine("      descriptions and file formatting are kept. A second run changes nothing.");
        console.DisplayTextLine("      Text table (entry with contentFile and contentMap, key = lookup name): the text file (or");
        console.DisplayTextLine("      archive entry tables.zip!table.txt) is converted with the map and replaces all values of the");
        console.DisplayTextLine("      lookup when they differ (~ file: lookup (+added, -removed, ~changed)). A lookup file that holds");
        console.DisplayTextLine("      lookups only is written as ZIP archive (Lookups.Name.json becomes Lookups.Name.zip, [ZIP]);");
        console.DisplayTextLine("      the same table gives the same archive bytes.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          apply mode: /write (default) or /dryrun (show the differences only)");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0  applied (or nothing to apply); skipped entries are listed");
        console.DisplayTextLine("          6  lex.json or regulation JSON invalid, nothing written for that satellite");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexApply .lex/lex.json /dryrun");
        console.DisplayTextLine("          LexApply lex.json /recursive");
    }
}
