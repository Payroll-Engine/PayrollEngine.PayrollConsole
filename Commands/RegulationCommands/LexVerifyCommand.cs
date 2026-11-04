using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Verifies that the snapshot of a lex manifest is intact and current:
/// file hashes, source hash and content of the values are compared with lex.json and the .lex folder.
/// </summary>
[Command("LexVerify")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexVerifyCommand : CommandBase<LexVerifyParameters>
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexVerifyParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Verify");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File mask        {parameters.FileMask}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
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

            int verified = 0, failed = 0;
            foreach (var lexFile in files)
            {
                if (await VerifyFile(context, lexFile))
                {
                    verified++;
                }
                else
                {
                    failed++;
                }
            }

            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine($"Summary: {verified} verified, {failed} failed");
            return failed > 0 ? (int)ProgramExitCode.InvalidInput : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<bool> VerifyFile(CommandContext context, string lexFilePath)
    {
        var directory = Path.GetDirectoryName(lexFilePath)!;
        var snapshotPath = Path.Combine(directory, "snapshot.json");
        if (!File.Exists(snapshotPath))
        {
            context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} - snapshot.json is missing (create it with LexSnapshot)");
            return false;
        }

        LexManifest manifest;
        LexSnapshot snapshot;
        try
        {
            manifest = JsonSerializer.Deserialize<LexManifest>(await File.ReadAllTextAsync(lexFilePath), ReadOptions);
            snapshot = JsonSerializer.Deserialize<LexSnapshot>(await File.ReadAllTextAsync(snapshotPath), ReadOptions);
        }
        catch (Exception exception)
        {
            context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} - failed to parse: {exception.Message}");
            return false;
        }
        if (manifest == null || snapshot == null)
        {
            context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} - lex.json or snapshot.json is empty");
            return false;
        }

        var errors = LexVerifier.Verify(manifest, snapshot, directory);
        if (errors.Count == 0)
        {
            context.Console.DisplaySuccessLine($"  [OK] {lexFilePath} - snapshot {snapshot.Created} is intact and current");
            return true;
        }

        context.Console.DisplayErrorLine($"  [FAIL] {lexFilePath} - {errors.Count} difference(s):");
        foreach (var error in errors)
        {
            context.Console.DisplayErrorLine($"         {error}");
        }
        return false;
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexVerifyParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexVerify");
        console.DisplayTextLine("      Verify that snapshot.json is intact and current.");
        console.DisplayTextLine("      Recomputes the file hashes and the source hash from lex.json and the .lex folder");
        console.DisplayTextLine("      and compares header, values and files with the snapshot.");
        console.DisplayTextLine("      Exit code 6 if any snapshot differs or is missing.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexVerify .lex/lex.json");
        console.DisplayTextLine("          LexVerify lex.json /recursive");
    }
}
