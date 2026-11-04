using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Looks up the numbers of every lex entry in its source (local evidence file or live web page/spreadsheet)
/// and, on request, writes the found source location and the automatic verification record into the manifest.
/// </summary>
[Command("LexVerifySource")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexVerifySourceCommand : CommandBase<LexVerifySourceParameters>
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexVerifySourceParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Verify Source");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File mask        {parameters.FileMask}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
            context.Console.DisplayTextLine($"Check mode       {parameters.CheckMode}");
            context.Console.DisplayTextLine($"Write mode       {parameters.WriteMode}");
            context.Console.DisplayTextLine($"Source mode      {parameters.SourceMode}");
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

            using var client = parameters.SourceMode == LexSourceMode.Live ? SourceDocument.CreateHttpClient() : null;
            var documents = new Dictionary<string, SourceDocument>(StringComparer.Ordinal);

            var problems = 0;
            foreach (var lexFile in files)
            {
                problems += await VerifyFile(context, lexFile, parameters, client, documents);
            }

            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine($"Summary: {problems} entr(ies) with numbers missing in the source");
            return problems > 0 && parameters.CheckMode == LexCheckMode.Strict
                ? (int)ProgramExitCode.InvalidInput
                : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<int> VerifyFile(CommandContext context, string lexFile, LexVerifySourceParameters parameters,
        HttpClient client, Dictionary<string, SourceDocument> documents)
    {
        var json = await File.ReadAllTextAsync(lexFile);
        var manifest = JsonSerializer.Deserialize<Manifest>(json, ReadOptions) ?? new Manifest();
        var lexDirectory = Path.GetDirectoryName(lexFile)!;
        var live = parameters.SourceMode == LexSourceMode.Live;

        var results = new List<SourceResult>();
        var evidence = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in manifest.Values)
        {
            if (string.IsNullOrWhiteSpace(entry.SourceUrl) || !entry.HasInlineContent ||
                entry.Key.StartsWith("OfficialCalculator.", StringComparison.Ordinal))
            {
                continue;
            }
            SourceDocument document;
            if (live)
            {
                if (!documents.TryGetValue(entry.SourceUrl, out document))
                {
                    documents[entry.SourceUrl] = document =
                        await SourceDocument.LoadLive(entry.SourceUrl, client, LexWorkbookReader.Readers);
                }
            }
            else
            {
                var localPath = LocalEvidence(manifest, entry, lexDirectory);
                if (localPath != null)
                {
                    evidence[entry.Key] = Path.GetFileName(localPath);
                }
                var cacheKey = localPath ?? string.Empty;
                if (!documents.TryGetValue(cacheKey, out document))
                {
                    documents[cacheKey] = document = await SourceDocument.LoadLocal(localPath, LexWorkbookReader.Readers);
                }
            }
            results.Add(SourceVerifier.Verify(entry, document));
        }

        int Count(SourceMatch m) => results.Count(r => r.Match == m);
        var problems = Count(SourceMatch.Partial) + Count(SourceMatch.NotFound);
        var line = $"{lexFile} - found {Count(SourceMatch.Found)}, partial {Count(SourceMatch.Partial)}, " +
                   $"not found {Count(SourceMatch.NotFound)}, unreadable {Count(SourceMatch.Unreadable)}, " +
                   $"no local evidence {Count(SourceMatch.NoEvidence)}, no numbers {Count(SourceMatch.NoNumbers)}";
        if (problems > 0) context.Console.DisplayErrorLine("  " + line);
        else context.Console.DisplayTextLine("  " + line);
        foreach (var result in results.Where(r => r.Match is SourceMatch.Partial or SourceMatch.NotFound))
        {
            context.Console.DisplayErrorLine(
                $"         {result.Match}: {result.Key} - {result.NumbersFound}/{result.NumbersTotal} numbers, missing {string.Join(", ", result.Missing.Take(8))}");
        }
        foreach (var group in results.Where(r => r.Match == SourceMatch.Unreadable).GroupBy(r => r.Url))
        {
            context.Console.DisplayInfoLine($"         unreadable ({group.First().Reason}), verify manually: {group.Key} - {group.Count()} entr(ies)");
        }
        var withoutEvidence = Count(SourceMatch.NoEvidence);
        if (withoutEvidence > 0)
        {
            context.Console.DisplayInfoLine($"         {withoutEvidence} entr(ies) without local evidence: verify manually or use /live");
        }

        if (parameters.WriteMode == LexSourceWriteMode.WriteLocations)
        {
            await WriteResults(context, lexFile, json, results, evidence);
        }
        return problems;
    }

    /// <summary>
    /// Write the found source locations and the automatic verification records of fully found entries
    /// </summary>
    private static async Task WriteResults(CommandContext context, string lexFile, string json,
        List<SourceResult> results, Dictionary<string, string> evidence)
    {
        var editor = ManifestEditor.Parse(json);
        var today = DateOnly.FromDateTime(DateTime.Today);
        int locations = 0, verifications = 0;
        foreach (var result in results)
        {
            if (result.Location != null && editor.SetSourceLocation(result.Key, result.Location))
            {
                locations++;
            }
            if (result.Match == SourceMatch.Found &&
                editor.SetAutoVerification(result.Key, SourceVerifier.AutoVerification(today, evidence.GetValueOrDefault(result.Key))))
            {
                verifications++;
            }
        }
        if (editor.Changes == 0)
        {
            return;
        }
        await editor.SaveAsync(lexFile);
        context.Console.DisplayInfoLine(
            $"         wrote {locations} source location(s) and {verifications} verification record(s): update the snapshot (LexSnapshot /overwrite)");
    }

    /// <summary>
    /// Local evidence file of the entry: documentRef, otherwise a declared file with the same source URL
    /// </summary>
    private static string LocalEvidence(Manifest manifest, Entry entry, string lexDirectory)
    {
        var name = !string.IsNullOrWhiteSpace(entry.DocumentRef)
            ? entry.DocumentRef
            : manifest.Files.FirstOrDefault(f => string.Equals(f.SourceUrl, entry.SourceUrl, StringComparison.Ordinal))?.FileName;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var path = Path.Combine(lexDirectory, name);
        return File.Exists(path) ? path : null;
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexVerifySourceParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexVerifySource");
        console.DisplayTextLine("      Look up the numbers of every lex entry in its source: the local evidence file (documentRef or declared");
        console.DisplayTextLine("      file with the same sourceUrl) or, with /live, the sourceUrl (web page, xlsx).");
        console.DisplayTextLine("      found: all numbers are in the source; partial / not found: numbers are missing (review the value);");
        console.DisplayTextLine("      unreadable (blocked host, PDF, script-only page) and no local evidence: verify the value manually.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          result mode: /lenient (default, report only) or /strict (exit code 6 when numbers are missing in the source)");
        console.DisplayTextLine("          source mode: /local (default, local evidence file, offline) or /live (source URL)");
        console.DisplayTextLine("          write mode: /reportonly (default) or /writelocations (set sourceLocation of found entries without one");
        console.DisplayTextLine("                      and the verification record (mode auto) of entries whose numbers were all found)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexVerifySource lex.json /recursive");
        console.DisplayTextLine("          LexVerifySource .lex/lex.json /writelocations");
        console.DisplayTextLine("          LexVerifySource .lex/lex.json /live");
    }
}
