using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Checks lex manifests against the lookup values of their data regulation:
/// coverage, orphan entries, value differences, traceability fields, evidence files and validFrom.
/// </summary>
[Command("LexCheck")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexCheckCommand : CommandBase<LexCheckParameters>
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexCheckParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Check");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File mask        {parameters.FileMask}");
            context.Console.DisplayTextLine($"Report           {(string.IsNullOrWhiteSpace(parameters.Report) ? "-" : parameters.Report)}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
            context.Console.DisplayTextLine($"Check mode       {parameters.CheckMode}");
            context.Console.DisplayTextLine($"Location mode    {parameters.LocationMode}");
            context.Console.DisplayTextLine($"URL mode         {parameters.UrlMode}");
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

            var results = new List<LexCheckResult>();
            using var urlChecker = parameters.UrlMode == LexUrlMode.CheckUrls ? new LexUrlChecker() : null;
            foreach (var lexFile in files)
            {
                results.Add(await CheckFile(lexFile, parameters.LocationMode == LexLocationMode.Required, urlChecker));
            }

            foreach (var result in results)
            {
                Display(context.Console, result);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Report))
            {
                LexCheckReport.Write(results, parameters.Report);
                context.Console.DisplayInfoLine($"Report written: {Path.GetFullPath(parameters.Report)}");
            }

            var withFindings = results.Count(r => r.HasFindings);
            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine($"Summary: {results.Count - withFindings} without findings, {withFindings} with findings");
            return withFindings > 0 && parameters.CheckMode == LexCheckMode.Strict
                ? (int)ProgramExitCode.InvalidInput
                : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<LexCheckResult> CheckFile(string lexFilePath, bool requireLocation, LexUrlChecker urlChecker)
    {
        LexManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<LexManifest>(await File.ReadAllTextAsync(lexFilePath), ReadOptions);
        }
        catch (Exception exception)
        {
            return new LexCheckResult
            {
                LexPath = lexFilePath,
                Satellite = Path.GetFileName(Directory.GetParent(Path.GetDirectoryName(lexFilePath)!)?.FullName ?? string.Empty),
                Error = "lex.json could not be parsed: " + exception.Message
            };
        }
        manifest ??= new LexManifest();
        var result = LexChecker.Check(manifest, lexFilePath, requireLocation);
        if (urlChecker != null)
        {
            await urlChecker.Apply(manifest, result);
        }
        return result;
    }

    private static void Display(ICommandConsole console, LexCheckResult result)
    {
        if (!result.HasFindings)
        {
            console.DisplaySuccessLine($"  [OK] {result.LexPath} - {result.LexEntries} entries, {result.RegulationValues} regulation values");
            return;
        }

        console.DisplayErrorLine($"  [FAIL] {result.LexPath}");
        if (result.Error != null) console.DisplayErrorLine($"         {result.Error}");
        foreach (var error in result.ValidationErrors) console.DisplayErrorLine($"         manifest: {error}");
        if (!result.HasSnapshot) console.DisplayErrorLine("         snapshot.json is missing");
        if (result.Uncovered > 0)
            console.DisplayErrorLine($"         {result.Uncovered} regulation value(s) without lex entry: {string.Join(", ", result.UncoveredSamples)}");
        if (result.Orphans + result.UnresolvedSplit > 0)
            console.DisplayErrorLine($"         {result.Orphans + result.UnresolvedSplit} lex entr(ies) without regulation value: {string.Join(", ", result.OrphanSamples)}");
        if (result.Drift > 0)
            console.DisplayErrorLine($"         {result.Drift} value difference(s): {string.Join("; ", result.DriftSamples)}");
        foreach (var url in result.DeadUrls) console.DisplayErrorLine($"         source URL not found: {url}");
        if (result.MissingSourceUrl + (result.RequireLocation ? result.MissingSourceLocation : 0) + result.MissingLegalReference > 0)
            console.DisplayErrorLine($"         missing fields: sourceUrl {result.MissingSourceUrl}, sourceLocation {result.MissingSourceLocation}, legalReference {result.MissingLegalReference}");
        foreach (var problem in result.FileProblems) console.DisplayErrorLine($"         evidence: {problem}");
        if (!result.ValidFromMatch)
            console.DisplayErrorLine($"         validFrom: lex {result.LexValidFrom}, regulation {result.RegulationValidFrom}");
        if (result.Note != null) console.DisplayInfoLine($"         note: {result.Note}");
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexCheckParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexCheck");
        console.DisplayTextLine("      Check lex manifests against the lookup values of their data regulation (folder Regulation next to .lex).");
        console.DisplayTextLine("      Reports: regulation values without lex entry, lex entries without regulation value,");
        console.DisplayTextLine("      value differences, missing sourceUrl/sourceLocation/legalReference, evidence files");
        console.DisplayTextLine("      (missing, untracked or ignored in git), missing snapshot and validFrom differences.");
        console.DisplayTextLine("      Freshness of snapshot.json is checked by LexVerify.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          Report: path of a Markdown report; a CSV file with the same name is written next to it");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          result mode: /strict (default, findings give exit code 6) or /lenient (report only)");
        console.DisplayTextLine("          source location: /optional (default, a missing sourceLocation is information) or /required (finding)");
        console.DisplayTextLine("          source URLs: /skipurls (default) or /checkurls (404/410 or unknown host is a finding, a blocking host a note)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexCheck .lex/lex.json");
        console.DisplayTextLine("          LexCheck lex.json /recursive /lenient Report:C:\\Reports\\LexCheck.md");
    }
}
