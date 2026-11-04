using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Looks up the numbers of every lex entry in its source (web page, spreadsheet or local evidence file)
/// and, on request, writes the found source location into the manifest.
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

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
        }
        context.Console.DisplayNewLine();
        try
        {
            var files = LexFileResolver.Resolve(parameters.FileMask, parameters.DirectoryMode);
            if (files.Count == 0)
            {
                context.Console.DisplayErrorLine($"No lex.json files found matching: {parameters.FileMask}");
                return (int)ProgramExitCode.InvalidInput;
            }

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; PayrollEngine-LexVerifySource)");
            var documents = new Dictionary<string, LexSourceDocument>(StringComparer.Ordinal);

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
        HttpClient client, Dictionary<string, LexSourceDocument> documents)
    {
        var json = await File.ReadAllTextAsync(lexFile);
        var manifest = JsonSerializer.Deserialize<LexManifest>(json, ReadOptions) ?? new LexManifest();
        var lexDirectory = Path.GetDirectoryName(lexFile)!;

        var results = new List<LexSourceResult>();
        foreach (var entry in manifest.Values)
        {
            if (string.IsNullOrWhiteSpace(entry.SourceUrl) || !entry.HasInlineContent ||
                entry.Key.StartsWith("OfficialCalculator.", StringComparison.Ordinal))
            {
                continue;
            }
            var localPath = LocalEvidence(manifest, entry, lexDirectory);
            var cacheKey = localPath ?? entry.SourceUrl;
            if (!documents.TryGetValue(cacheKey, out var document))
            {
                documents[cacheKey] = document = await LexSourceDocument.Load(entry.SourceUrl, localPath, client);
            }
            results.Add(LexSourceVerifier.Verify(entry, document));
        }

        int Count(LexSourceMatch m) => results.Count(r => r.Match == m);
        var problems = Count(LexSourceMatch.Partial) + Count(LexSourceMatch.NotFound);
        var line = $"{lexFile} - found {Count(LexSourceMatch.Found)}, partial {Count(LexSourceMatch.Partial)}, " +
                   $"not found {Count(LexSourceMatch.NotFound)}, unreadable {Count(LexSourceMatch.Unreadable)}, no numbers {Count(LexSourceMatch.NoNumbers)}";
        if (problems > 0) context.Console.DisplayErrorLine("  " + line);
        else context.Console.DisplayTextLine("  " + line);
        foreach (var result in results.Where(r => r.Match is LexSourceMatch.Partial or LexSourceMatch.NotFound))
        {
            context.Console.DisplayErrorLine(
                $"         {result.Match}: {result.Key} - {result.NumbersFound}/{result.NumbersTotal} numbers, missing {string.Join(", ", result.Missing.Take(8))}");
        }
        foreach (var group in results.Where(r => r.Match == LexSourceMatch.Unreadable).GroupBy(r => r.Url))
        {
            context.Console.DisplayInfoLine($"         unreadable ({group.First().Reason}): {group.Key} - {group.Count()} entr(ies)");
        }

        if (parameters.WriteMode == LexSourceWriteMode.WriteLocations)
        {
            var locations = results.Where(r => r.Location != null).ToDictionary(r => r.Key, r => r.Location);
            var written = await WriteLocations(lexFile, json, locations);
            if (written > 0)
            {
                context.Console.DisplayInfoLine($"         wrote {written} source location(s)");
            }
        }
        return problems;
    }

    /// <summary>
    /// Local evidence file of the entry: documentRef, otherwise a declared file with the same source URL
    /// </summary>
    private static string LocalEvidence(LexManifest manifest, LexValue entry, string lexDirectory)
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

    private static async Task<int> WriteLocations(string lexFile, string json, Dictionary<string, string> locations)
    {
        var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });
        if (root?["values"] is not JsonArray values)
        {
            return 0;
        }
        var written = 0;
        foreach (var node in values.OfType<JsonObject>())
        {
            var key = node["key"]?.GetValue<string>();
            if (key == null || !locations.TryGetValue(key, out var location))
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(node["sourceLocation"]?.GetValue<string>()))
            {
                continue;
            }
            node["sourceLocation"] = location;
            written++;
        }
        if (written > 0)
        {
            await File.WriteAllTextAsync(lexFile, root.ToJsonString(WriteOptions) + "\n",
                new System.Text.UTF8Encoding(false));
        }
        return written;
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
        console.DisplayTextLine("      Look up the numbers of every lex entry in its source: a local evidence file (documentRef or declared file),");
        console.DisplayTextLine("      otherwise the sourceUrl (web page, xlsx). PDF files and script-only pages are reported as unreadable.");
        console.DisplayTextLine("      found: all numbers are in the source; partial / not found: numbers are missing (review the value);");
        console.DisplayTextLine("      unreadable: the source cannot be read (blocked host, PDF).");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          result mode: /lenient (default, report only) or /strict (exit code 6 when numbers are missing in the source)");
        console.DisplayTextLine("          write mode: /reportonly (default) or /writelocations (set sourceLocation of found entries without one)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexVerifySource lex.json /recursive");
        console.DisplayTextLine("          LexVerifySource .lex/lex.json /writelocations");
    }
}
