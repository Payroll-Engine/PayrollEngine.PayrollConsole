using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;
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
    private const int MaxQuoteLength = 120;

    /// <summary>Environment variable with the folder of additional language files</summary>
    private const string VocabularyVariable = "PAYROLL_ENGINE_LEX_VOCABULARY";

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
        var vocabulary = string.IsNullOrWhiteSpace(parameters.Vocabulary)
            ? Environment.GetEnvironmentVariable(VocabularyVariable)
            : parameters.Vocabulary;
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File mask        {parameters.FileMask}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
            context.Console.DisplayTextLine($"Check mode       {parameters.CheckMode}");
            context.Console.DisplayTextLine($"Write mode       {parameters.WriteMode}");
            context.Console.DisplayTextLine($"Source mode      {parameters.SourceMode}");
            context.Console.DisplayTextLine($"Vocabulary       {vocabulary}");
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

            int problems = 0, ambiguous = 0;
            foreach (var lexFile in files)
            {
                var results = await VerifyFile(context, lexFile, parameters, vocabulary, client, documents);
                problems += results.Count(r => r.Match is SourceMatch.Partial or SourceMatch.NotFound or SourceMatch.QuoteNotFound);
                ambiguous += results.Count(r => r.Match == SourceMatch.Ambiguous);
            }

            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine(
                $"Summary: {problems} entr(ies) with numbers or quote missing in the source, {ambiguous} ambiguous entr(ies)");
            return problems + ambiguous > 0 && parameters.CheckMode == LexCheckMode.Strict
                ? (int)ProgramExitCode.InvalidInput
                : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<List<SourceResult>> VerifyFile(CommandContext context, string lexFile, LexVerifySourceParameters parameters,
        string vocabulary, HttpClient client, Dictionary<string, SourceDocument> documents)
    {
        var json = await File.ReadAllTextAsync(lexFile);
        var manifest = JsonSerializer.Deserialize<Manifest>(json, ReadOptions) ?? new Manifest();
        var results = await ManifestVerifier.VerifyManifest(manifest, Path.GetDirectoryName(lexFile)!,
            client != null ? url => SourceDocument.LoadLive(url, client) : null, documents, vocabulary);

        int Count(SourceMatch m) => results.Count(r => r.Match == m);
        var problems = Count(SourceMatch.Partial) + Count(SourceMatch.NotFound) + Count(SourceMatch.QuoteNotFound);
        var line = $"{lexFile} - found {Count(SourceMatch.Found)}, ambiguous {Count(SourceMatch.Ambiguous)}, " +
                   $"partial {Count(SourceMatch.Partial)}, not found {Count(SourceMatch.NotFound)}, " +
                   $"quote not found {Count(SourceMatch.QuoteNotFound)}, unreadable {Count(SourceMatch.Unreadable)}, " +
                   $"no local evidence {Count(SourceMatch.NoEvidence)}, no numbers {Count(SourceMatch.NoNumbers)}, " +
                   $"no content {Count(SourceMatch.NoContent)}" +
                   (results.Count > 0 ? $", languages {results[0].Languages}" : string.Empty);
        if (problems > 0) context.Console.DisplayErrorLine("  " + line);
        else context.Console.DisplayTextLine("  " + line);
        foreach (var result in results.Where(r => r.Match is SourceMatch.Partial or SourceMatch.NotFound))
        {
            var unreadable = result.Reason != null ? $" (unreadable: {result.Reason})" : string.Empty;
            context.Console.DisplayErrorLine(
                $"         {result.Match}: {result.Key} - {result.NumbersFound}/{result.NumbersTotal} numbers{Sampled(result)}, " +
                $"missing {string.Join(", ", result.Missing.Take(8))}{unreadable}");
        }
        foreach (var result in results.Where(r => r.Match == SourceMatch.QuoteNotFound))
        {
            context.Console.DisplayErrorLine($"         QuoteNotFound: {result.Key} - \"{Shorten(result.Quote)}\"");
        }
        foreach (var result in results.Where(r => r.Match == SourceMatch.Ambiguous))
        {
            context.Console.DisplayInfoLine(
                $"         Ambiguous: {result.Key} - {result.Ambiguity}: confirm with LexAttest or add sourceQuote");
        }
        foreach (var result in results.Where(r => r.Match == SourceMatch.Found && r.Sampled))
        {
            context.Console.DisplayTextLine($"         Found: {result.Key} -{Sampled(result)}");
        }
        foreach (var result in results.Where(r => r.Match == SourceMatch.NoContent))
        {
            context.Console.DisplayInfoLine($"         no content: {result.Key} - {result.Reason}");
        }
        foreach (var result in results.Where(r => r.Derived.Count > 0 || r.NotApplicable.Count > 0 ||
                                                  r.UnjustifiedZero.Count > 0 || r.Note != null))
        {
            foreach (var derived in result.Derived)
            {
                context.Console.DisplayTextLine($"         {result.Key} - derived: {derived.Property} = {derived.Value} ({derived.Reason})");
            }
            foreach (var notApplicable in result.NotApplicable)
            {
                context.Console.DisplayTextLine(
                    $"         {result.Key} - not applicable: {notApplicable.Property} = {notApplicable.Value} ({notApplicable.Reason})");
            }
            var unjustified = result.UnjustifiedZero;
            if (unjustified.Count > 0)
            {
                context.Console.DisplayInfoLine(
                    $"         {result.Key} - {SourceVerifier.UnjustifiedZeroHint}: {string.Join(", ", unjustified)}");
            }
            if (result.Note != null)
            {
                context.Console.DisplayInfoLine($"         {result.Key} - {result.Note}");
            }
        }
        foreach (var result in results.Where(r => r.ExcludedReason != null))
        {
            context.Console.DisplayInfoLine($"         excluded from auto verification: {result.Key} - {result.ExcludedReason}");
        }
        foreach (var group in results.Where(r => r.Match == SourceMatch.Unreadable).GroupBy(r => string.Join(" ", r.Urls)))
        {
            context.Console.DisplayInfoLine($"         unreadable ({group.First().Reason}), verify manually: {group.Key} - {group.Count()} entr(ies)");
        }
        var withoutEvidence = Count(SourceMatch.NoEvidence);
        if (withoutEvidence > 0)
        {
            context.Console.DisplayInfoLine($"         {withoutEvidence} entr(ies) without local evidence: verify manually or use /live");
        }
        var unused = EvidenceCheck.UnusedEvidence(manifest, Path.GetDirectoryName(lexFile)!);
        if (unused.Count > 0)
        {
            context.Console.DisplayInfoLine($"         evidence file(s) that back no entry (set documentRef or the source URL): {string.Join(", ", unused)}");
        }

        if (parameters.WriteMode != LexSourceWriteMode.ReportOnly)
        {
            await WriteResults(context, lexFile, json, results, parameters.WriteMode == LexSourceWriteMode.RefreshLocations);
        }
        return results;
    }

    private static string Sampled(SourceResult result) =>
        result.Sampled ? $" sampled {result.NumbersTotal} of {result.ContentNumbers} numbers" : string.Empty;

    private static string Shorten(string text) =>
        text is { Length: > MaxQuoteLength } ? text[..MaxQuoteLength] + "..." : text;

    /// <summary>
    /// Write the found source locations and the automatic verification records of fully found entries;
    /// ambiguous entries and entries excluded from auto verification are not written
    /// </summary>
    private static async Task WriteResults(CommandContext context, string lexFile, string json,
        List<SourceResult> results, bool refresh)
    {
        var editor = ManifestEditor.Parse(json);
        var today = DateOnly.FromDateTime(DateTime.Today);
        int locations = 0, verifications = 0;
        foreach (var result in results.Where(r => r.CanWrite))
        {
            if (result.Location != null && editor.SetSourceLocation(result.Key, result.Location, refresh))
            {
                locations++;
            }
            if (editor.SetAutoVerification(result.Key,
                    SourceVerifier.AutoVerification(today, result.Evidence, result.Derived.Select(x => x.Property),
                        result.Languages)))
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

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexVerifySourceParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexVerifySource");
        console.DisplayTextLine("      Look up the numbers of every lex entry (inline content or JSON contentFile) in its sources: per source");
        console.DisplayTextLine("      URL the local evidence file (documentRef for the first URL, otherwise the declared file with the same URL");
        console.DisplayTextLine("      in sourceUrls) or, with /live, the source URL. A number counts as found when any source holds it.");
        console.DisplayTextLine("      A value below 1 is also looked up as percentage when the source writes %, percent or per cent after");
        console.DisplayTextLine("      the number, except for units that are no rate (factor, hours, days, currency code such as USD).");
        console.DisplayTextLine("      Numbers in words count ($1 million, 2.5 lakh, one and one-half).");
        console.DisplayTextLine("      A table with more than 40 numbers is checked with 40 numbers evenly distributed over the table.");
        console.DisplayTextLine("      With sourceQuote, the quote must be in the source and the numbers in the section of the quote.");
        console.DisplayTextLine("      The numbers of a property with a derived exclusion (Lookup.Key.Property, formula and legal reference");
        console.DisplayTextLine("      as reason) are not looked up and are listed as derived: <property> = <value> (<reason>); a number");
        console.DisplayTextLine("      that is also in another property is looked up there. The verification record lists the derived");
        console.DisplayTextLine("      properties. An entry with derived numbers only gives no numbers: confirm it with LexAttest.");
        console.DisplayTextLine("      A property of a bundle with the value 0 is not looked up. Document a rule that does not apply");
        console.DisplayTextLine("      with a notApplicable exclusion (Lookup.Key.Property, reason): not applicable: <property> = 0 (<reason>);");
        console.DisplayTextLine("      without it the entry is reported as zero values without justification: <property>, ...");
        console.DisplayTextLine("      The location names at most three sections per source, with several sources one part per source");
        console.DisplayTextLine("      (<host>: Section \"...\"; <host>: Sections \"...\", \"...\"). A location longer than 500 characters is");
        console.DisplayTextLine("      not written: location too long: set sourceLocation manually.");
        console.DisplayTextLine("      Number and year words (percent, million, 2024 and 2025, effective, Finance Act 2025) are read in the");
        console.DisplayTextLine("      languages of the manifest (languages, e.g. [\"de\", \"fr\"]), without languages in all available");
        console.DisplayTextLine("      languages. A language is a file {language}.json in the LexVocabulary folder of pecmd or in the folder");
        console.DisplayTextLine("      of additional language files, which adds or replaces languages by language code. The verification");
        console.DisplayTextLine("      record names the languages and the hash of their words (method ...; languages de,en 3f2a9c1b).");
        console.DisplayTextLine("      Readable: HTML, text, csv, xls/xlsx/xlsb/ods, PDF with text layer, docx/odt.");
        console.DisplayTextLine("      Results:");
        console.DisplayTextLine("          found: all numbers are in the sources");
        console.DisplayTextLine("          ambiguous: a single number written with two digits at most (22%, 1.5), a single number with three");
        console.DisplayTextLine("                     digits at most in more than three sections, or a number in words only:");
        console.DisplayTextLine("                     confirm with LexAttest or add sourceQuote");
        console.DisplayTextLine("          partial / not found: numbers are missing (review the value)");
        console.DisplayTextLine("          quote not found: the sourceQuote is not in any readable source");
        console.DisplayTextLine("          unreadable (blocked host, script-only page, scanned or encrypted PDF, other binary format),");
        console.DisplayTextLine("          no local evidence, no numbers, no content (contentFile missing or not JSON): verify manually");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("          Vocabulary: folder of additional language files (default: environment variable");
        console.DisplayTextLine("                      PAYROLL_ENGINE_LEX_VOCABULARY)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          result mode: /lenient (default, report only) or /strict (exit code 6 for partial, not found,");
        console.DisplayTextLine("                       quote not found and ambiguous entries)");
        console.DisplayTextLine("          source mode: /local (default, local evidence file, offline) or /live (source URL)");
        console.DisplayTextLine("          write mode: /reportonly (default), /writelocations (set sourceLocation of found entries without one");
        console.DisplayTextLine("                      and the verification record (mode auto) of found entries) or /refreshlocations");
        console.DisplayTextLine("                      (as /writelocations, and replace the locations written by LexVerifySource);");
        console.DisplayTextLine("                      ambiguous entries and entries with an autoVerification exclusion are never written");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0 ok (or /lenient), 6 no lex.json found or problems (/strict), -1 invalid parameters or error");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexVerifySource lex.json /recursive");
        console.DisplayTextLine("          LexVerifySource .lex/lex.json /writelocations");
        console.DisplayTextLine("          LexVerifySource lex.json /recursive /refreshlocations");
        console.DisplayTextLine("          LexVerifySource .lex/lex.json /live");
        console.DisplayTextLine("          LexVerifySource lex.json /recursive Vocabulary:C:\\Lex\\LexVocabulary");
    }
}
