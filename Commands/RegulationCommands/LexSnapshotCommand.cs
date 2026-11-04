using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Creates a compliance snapshot from a lex.json source manifest.
/// Supports file masks and recursive directory scanning.
/// </summary>
[Command("LexSnapshot")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexSnapshotCommand : CommandBase<LexSnapshotParameters>
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexSnapshotParameters parameters)
    {
        // /schema — print JSON Schema and exit
        if (parameters.SchemaMode == LexSchemaMode.Schema)
        {
            context.Console.DisplayTextLine(LexSchema.Json);
            return (int)ProgramExitCode.Ok;
        }

        DisplayTitle(context.Console, "Lex Snapshot");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File mask        {parameters.FileMask}");
            context.Console.DisplayTextLine($"Target path      {(string.IsNullOrWhiteSpace(parameters.TargetPath) ? "-" : parameters.TargetPath)}");
            context.Console.DisplayTextLine($"Snapshot mode    {parameters.SnapshotMode}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
            context.Console.DisplayTextLine($"Overwrite mode   {parameters.OverwriteMode}");
        }
        var dryRun = parameters.SnapshotMode == LexSnapshotMode.DryRun;
        if (dryRun)
        {
            context.Console.DisplayInfoLine("Dry-run mode — no files will be written");
        }
        context.Console.DisplayNewLine();

        try
        {
            var files = LexFileResolver.Resolve(parameters.FileMask, parameters.DirectoryMode == DirectoryMode.Recursive);
            if (files.Count == 0)
            {
                context.Console.DisplayErrorLine($"No lex.json files found matching: {parameters.FileMask}");
                return (int)ProgramExitCode.InvalidInput;
            }

            int processed = 0, skipped = 0, failed = 0;

            foreach (var lexFile in files)
            {
                var result = await ProcessLexFile(context, lexFile, parameters, dryRun);
                switch (result)
                {
                    case ProcessResult.Created:
                        processed++;
                        break;
                    case ProcessResult.Skipped:
                        skipped++;
                        break;
                    case ProcessResult.Failed:
                        failed++;
                        break;
                }
            }

            context.Console.DisplayNewLine();
            var processedLabel = dryRun ? "validated" : "created";
            context.Console.DisplayTextLine($"Summary: {processed} {processedLabel}, {skipped} skipped (snapshot exists), {failed} failed");

            return failed > 0 ? (int)ProgramExitCode.InvalidInput : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private enum ProcessResult { Created, Skipped, Failed }

    private static async Task<ProcessResult> ProcessLexFile(
        CommandContext context,
        string lexFilePath,
        LexSnapshotParameters parameters,
        bool dryRun)
    {
        var manifestDir = Path.GetDirectoryName(lexFilePath)!;
        var targetDir = string.IsNullOrWhiteSpace(parameters.TargetPath)
            ? manifestDir
            : parameters.TargetPath;

        // check for existing snapshot
        var snapshotPath = Path.Combine(targetDir, "snapshot.json");
        if (File.Exists(snapshotPath) && parameters.OverwriteMode == LexOverwriteMode.Skip)
        {
            context.Console.DisplayInfoLine($"  [SKIP] {lexFilePath} — snapshot.json already exists");
            return ProcessResult.Skipped;
        }

        // load manifest
        LexManifest manifest;
        try
        {
            var json = await File.ReadAllTextAsync(lexFilePath);
            manifest = JsonSerializer.Deserialize<LexManifest>(json, ReadOptions);
        }
        catch (Exception ex)
        {
            context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} — failed to parse lex.json: {ex.Message}");
            return ProcessResult.Failed;
        }

        // validate
        var errors = LexValidator.Validate(manifest, manifestDir);
        if (errors.Count > 0)
        {
            context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} — validation errors:");
            foreach (var error in errors)
            {
                context.Console.DisplayErrorLine($"         {error}");
            }
            return ProcessResult.Failed;
        }

        // write snapshot
        try
        {
            if (!dryRun && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var writer = new LexSnapshotWriter();
            var snapshot = await writer.WriteAsync(manifest, manifestDir, targetDir, dryRun,
                (fileName, dryRunFile) =>
                {
                    if (!dryRunFile)
                    {
                        context.Console.DisplayInfoLine($"         Downloading {fileName} ...");
                    }
                },
                keepLocalFiles: parameters.OverwriteMode == LexOverwriteMode.Overwrite);

            var label = dryRun ? "[DRY-RUN]" : "[OK]";
            context.Console.DisplaySuccessLine(
                $"  {label} {lexFilePath} — {snapshot.Values.Count} values, {snapshot.Files.Count} files");

            return ProcessResult.Created;
        }
        catch (Exception ex)
        {
            context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} — {ex.Message}");
            return ProcessResult.Failed;
        }
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexSnapshotParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexSnapshot");
        console.DisplayTextLine("      Create a compliance snapshot from a lex.json source manifest.");
        console.DisplayTextLine("      The snapshot is self-contained: all referenced files are copied into the .lex folder.");
        console.DisplayTextLine("      If snapshot.json already exists, the folder is skipped (info message only), unless /overwrite is set.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          TargetPath: output directory for snapshot.json and files (default: directory of lex.json)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          snapshot mode: /execute (default) or /dryrun");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          overwrite mode: /skip (default) or /overwrite (regenerate an existing snapshot.json;");
        console.DisplayTextLine("                          files with a sourceUrl and an existing local copy are not downloaded again)");
        console.DisplayTextLine("      Special:");
        console.DisplayTextLine("          /schema  — print the JSON Schema for lex.json and exit");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexSnapshot .lex/lex.json");
        console.DisplayTextLine("          LexSnapshot lex.json /recursive");
        console.DisplayTextLine("          LexSnapshot .lex/lex.json /dryrun");
        console.DisplayTextLine("          LexSnapshot .lex/lex.json /overwrite");
        console.DisplayTextLine("          LexSnapshot .lex/lex.json /TargetPath:C:\\Output");
        console.DisplayTextLine("          LexSnapshot /schema");
    }
}
