using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Downloads a source document unchanged into the .lex folder and declares it in files[]
/// </summary>
[Command("LexFetch")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexFetchCommand : CommandBase<LexFetchParameters>
{
    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexFetchParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Fetch");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"URL              {parameters.Url}");
            context.Console.DisplayTextLine($"File name        {(string.IsNullOrWhiteSpace(parameters.FileName) ? "(from URL)" : parameters.FileName)}");
            context.Console.DisplayTextLine($"Lex              {(string.IsNullOrWhiteSpace(parameters.Lex) ? "(current folder)" : parameters.Lex)}");
            context.Console.DisplayTextLine($"Overwrite mode   {parameters.OverwriteMode}");
        }
        context.Console.DisplayNewLine();

        try
        {
            var lexPath = FileResolver.ResolveManifest(parameters.Lex);
            if (lexPath == null)
            {
                context.Console.DisplayErrorLine($"lex.json not found: {parameters.Lex ?? Directory.GetCurrentDirectory()}");
                return (int)ProgramExitCode.InvalidInput;
            }
            var lexDirectory = Path.GetDirectoryName(lexPath)!;
            var editor = ManifestEditor.Parse(await File.ReadAllTextAsync(lexPath));

            using var client = SourceDocument.CreateHttpClient();
            FetchResult result;
            try
            {
                result = await Fetcher.FetchAsync(client, parameters.Url, lexDirectory, parameters.FileName,
                    parameters.OverwriteMode == LexOverwriteMode.Overwrite);
            }
            catch (HttpRequestException exception)
            {
                context.Console.DisplayErrorLine($"Source could not be loaded: {exception.Message}");
                context.Console.DisplayInfoLine("Save the document manually into the .lex folder and declare it in files[].");
                return (int)ProgramExitCode.HttpError;
            }
            catch (ArgumentException exception)
            {
                context.Console.DisplayErrorLine(exception.Message);
                return (int)ProgramExitCode.InvalidInput;
            }
            catch (FetchTargetLinkException exception)
            {
                context.Console.DisplayErrorLine(exception.Message);
                return (int)ProgramExitCode.InvalidInput;
            }

            if (result.State == FetchState.Differs)
            {
                context.Console.DisplayErrorLine($"{result.FileName} exists with a different content (use /overwrite to replace it)");
                return (int)ProgramExitCode.InvalidInput;
            }
            context.Console.DisplaySuccessLine($"  [{result.State}] {result.FilePath}");
            context.Console.DisplayTextLine($"         sha256 {result.Sha256}, {result.Size} bytes");

            var declared = editor.AddFile(new EvidenceFile
            {
                FileName = result.FileName,
                SourceUrls = [parameters.Url],
                Description = parameters.Description
            });
            if (declared)
            {
                await editor.SaveAsync(lexPath);
                context.Console.DisplayTextLine($"         declared in files[] of {lexPath}");
            }
            else
            {
                context.Console.DisplayTextLine("         already declared in files[]");
            }
            if (declared || result.State != FetchState.Unchanged)
            {
                context.Console.DisplayInfoLine("Reference the file (documentRef, LexAttest Evidence:) and update the snapshot (LexSnapshot /overwrite).");
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
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexFetchParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexFetch");
        console.DisplayTextLine("      Download a source document unchanged into the .lex folder and declare it in files[]");
        console.DisplayTextLine("      (fileName, sourceUrls). Hash and size are shown; LexSnapshot records them in the snapshot.");
        console.DisplayTextLine("      An existing file with the same content is kept, a different one only replaced with /overwrite.");
        console.DisplayTextLine("      The manifest (lex.json) and the snapshot (snapshot.json) are no fetch target and are refused.");
        console.DisplayTextLine("      A target that is a link pointing out of the .lex folder is refused too.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. source URL [Url]");
        console.DisplayTextLine("          2. target file name [FileName] (optional, default from the URL)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          Lex: path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)");
        console.DisplayTextLine("          Description: description of the file in files[]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          overwrite mode: /skip (default) or /overwrite (replace a file with different content)");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0 ok, 3 source not loaded (HTTP error), 6 lex.json missing, file differs, reserved file name or target link out of the .lex folder, -1 invalid parameters or error");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexFetch https://example.org/tax-rates-2026.pdf");
        console.DisplayTextLine("          LexFetch https://example.org/rates rates-2026.html Lex:Data.Tax.2026 \"Description:Tax rates 2026\"");
    }
}
