using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;
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
            if (!string.IsNullOrWhiteSpace(parameters.Root))
            {
                context.Console.DisplayTextLine($"Root             {parameters.Root}");
                context.Console.DisplayTextLine($"Repository mask  {(string.IsNullOrWhiteSpace(parameters.RepoMask) ? "*" : parameters.RepoMask)}");
            }
            context.Console.DisplayTextLine($"Report           {(string.IsNullOrWhiteSpace(parameters.Report) ? "-" : parameters.Report)}");
            context.Console.DisplayTextLine($"Directory mode   {parameters.DirectoryMode}");
            context.Console.DisplayTextLine($"Check mode       {parameters.CheckMode}");
            var release = parameters.ReleaseMode == LexReleaseMode.Release;
            context.Console.DisplayTextLine($"Location mode    {(release ? $"{LexLocationMode.Required} (release)" : parameters.LocationMode)}");
            context.Console.DisplayTextLine($"Verification     {(release ? $"{LexVerificationMode.VerificationRequired} (release)" : parameters.VerificationMode)}");
            context.Console.DisplayTextLine($"URL mode         {parameters.UrlMode}");
            context.Console.DisplayTextLine($"Release mode     {parameters.ReleaseMode}");
        }
        context.Console.DisplayNewLine();

        try
        {
            if (!string.IsNullOrWhiteSpace(parameters.Root) && !Directory.Exists(parameters.Root))
            {
                context.Console.DisplayErrorLine($"Root folder not found: {parameters.Root}");
                return (int)ProgramExitCode.InvalidInput;
            }
            var files = FileResolver.Resolve(parameters.Root, parameters.RepoMask, parameters.FileMask,
                parameters.DirectoryMode == DirectoryMode.Recursive);
            if (files.Count == 0)
            {
                context.Console.DisplayErrorLine($"No lex.json files found matching: {parameters.FileMask}");
                return (int)ProgramExitCode.InvalidInput;
            }

            var results = new List<CheckResult>();
            using var urlChecker = parameters.UrlMode == LexUrlMode.CheckUrls ? new UrlChecker() : null;
            foreach (var lexFile in files)
            {
                results.Add(await CheckFile(lexFile, parameters.RequireLocation, parameters.RequireVerification,
                    parameters.ReleaseMode == LexReleaseMode.Release, urlChecker));
            }

            foreach (var result in results)
            {
                Display(context.Console, result);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Report))
            {
                CheckReport.Write(results, parameters.Report);
                context.Console.DisplayInfoLine($"Report written: {Path.GetFullPath(parameters.Report)}");
            }

            var withFindings = results.Count(r => r.HasFindings);
            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine($"Verification: auto {results.Sum(r => r.VerifiedAuto)}, " +
                                            $"manual {results.Sum(r => r.VerifiedManual)}, without {results.Sum(r => r.Unverified)}, " +
                                            $"open {results.Sum(r => r.Open)}");
            context.Console.DisplayTextLine($"Summary: {results.Count - withFindings} without findings, {withFindings} with findings");
            return withFindings > 0 && parameters.CheckMode == LexCheckMode.Strict
                ? (int)ProgramExitCode.FailedTest
                : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<CheckResult> CheckFile(string lexFilePath, bool requireLocation,
        bool requireVerification, bool release, UrlChecker urlChecker)
    {
        Manifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(lexFilePath), ReadOptions);
        }
        catch (Exception exception)
        {
            return new CheckResult
            {
                LexPath = lexFilePath,
                Satellite = Path.GetFileName(Directory.GetParent(Path.GetDirectoryName(lexFilePath)!)?.FullName ?? string.Empty),
                Error = "lex.json could not be parsed: " + exception.Message
            };
        }
        manifest ??= new Manifest();
        var result = Checker.Check(manifest, lexFilePath, requireLocation, requireVerification, release);
        if (release)
        {
            await Checker.CheckSources(manifest, result);
        }
        if (urlChecker != null)
        {
            await urlChecker.Apply(manifest, result);
        }
        return result;
    }

    private static void Display(ICommandConsole console, CheckResult result)
    {
        if (!result.HasFindings)
        {
            console.DisplaySuccessLine($"  [OK] {result.LexPath} - {result.LexEntries} entries, {result.RegulationValues} regulation values");
            DisplayInformation(console, result);
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
        foreach (var url in result.ChangedSources) console.DisplayErrorLine($"         source changed, check value: {url}");
        if (result.MissingSourceUrls + (result.RequireLocation ? result.MissingSourceLocation : 0) + result.MissingLegalReference > 0)
            console.DisplayErrorLine($"         missing fields: sourceUrls {result.MissingSourceUrls}, sourceLocation {result.MissingSourceLocation}, legalReference {result.MissingLegalReference}");
        if (result.RequireVerification && result.Unverified > 0)
            console.DisplayErrorLine($"         {result.Unverified} entr(ies) without verification: {string.Join(", ", result.UnverifiedSamples)}");
        if (result.Open > 0)
            console.DisplayErrorLine($"         {result.Open} open entr(ies), verify again: {string.Join(", ", result.OpenSamples)}");
        if (result.MissingReview > 0)
            console.DisplayErrorLine($"         {result.MissingReview} manual verification(s) without second review: {string.Join(", ", result.MissingReviewSamples)}");
        if (result.RequireLocation && result.UnspecificLocations > 0)
            console.DisplayErrorLine($"         {result.UnspecificLocations} location(s) not entry-specific: {string.Join(", ", result.UnspecificLocationSamples)}");
        if (result.Release && result.SecondaryMainSource > 0)
            console.DisplayErrorLine($"         {result.SecondaryMainSource} secondary main source(s): {string.Join(", ", result.SecondaryMainSourceSamples)}");
        if (result.Release && result.ManualVerificationRequired > 0)
            console.DisplayErrorLine($"         {result.ManualVerificationRequired} entr(ies) need a manual verification (LexAttest): " +
                                     string.Join(", ", result.ManualVerificationSamples));
        if (result.Release && result.UnjustifiedZero > 0)
            console.DisplayErrorLine($"         {result.UnjustifiedZero} entr(ies) with {SourceVerifier.UnjustifiedZeroHint}: " +
                                     string.Join(", ", result.UnjustifiedZeroSamples));
        if (result.Release && result.UnusedEvidence > 0)
            console.DisplayErrorLine($"         {result.UnusedEvidence} evidence file(s) that back no entry (set documentRef or the source URL): " +
                                     string.Join(", ", result.UnusedEvidenceSamples));
        foreach (var problem in result.FileProblems) console.DisplayErrorLine($"         evidence: {problem}");
        if (!result.ValidFromMatch)
            console.DisplayErrorLine($"         validFrom: lex {result.LexValidFrom}, regulation {result.RegulationValidFrom}");
        if (result.Note != null) console.DisplayInfoLine($"         note: {result.Note}");
        DisplayInformation(console, result);
    }

    /// <summary>
    /// Information that is a finding only with /required or /release
    /// </summary>
    private static void DisplayInformation(ICommandConsole console, CheckResult result)
    {
        if (!result.RequireLocation && result.UnspecificLocations > 0)
            console.DisplayInfoLine($"         info: {result.UnspecificLocations} location(s) not entry-specific: {string.Join(", ", result.UnspecificLocationSamples)}");
        if (!result.Release && result.SecondaryMainSource > 0)
            console.DisplayInfoLine($"         info: {result.SecondaryMainSource} secondary main source(s): {string.Join(", ", result.SecondaryMainSourceSamples)}");
        if (result.OtherReferenceYear > 0)
            console.DisplayInfoLine($"         info: {result.OtherReferenceYear} entr(ies) with a reference year other than the validFrom year: " +
                                    string.Join(", ", result.OtherReferenceYearSamples));
        if (!result.Release && result.UnjustifiedZero > 0)
            console.DisplayInfoLine($"         info: {result.UnjustifiedZero} entr(ies) with {SourceVerifier.UnjustifiedZeroHint}: " +
                                    string.Join(", ", result.UnjustifiedZeroSamples));
        if (!result.Release && result.UnusedEvidence > 0)
            console.DisplayInfoLine($"         info: {result.UnusedEvidence} evidence file(s) that back no entry (set documentRef or the source URL): " +
                                    string.Join(", ", result.UnusedEvidenceSamples));
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
        console.DisplayTextLine("      value differences, missing sourceUrls/sourceLocation/legalReference, evidence files");
        console.DisplayTextLine("      (missing, untracked or ignored in git, verification evidence not in the snapshot), missing snapshot");
        console.DisplayTextLine("      and validFrom differences. Verification records are counted (auto, manual, without); a manual");
        console.DisplayTextLine("      verification of a source with review FourEyes needs reviewedBy and reviewedOn.");
        console.DisplayTextLine("      Information (a finding with /required): a location that is not entry-specific (the same location");
        console.DisplayTextLine("      longer than 200 characters on 5 or more entries, or a location longer than 500 characters).");
        console.DisplayTextLine("      Information (a finding with /release): a secondary main source (first source URL with official: false)");
        console.DisplayTextLine("      a bundle property with the value 0 without notApplicable exclusion (zero values without justification)");
        console.DisplayTextLine("      and an evidence file that backs no entry (no documentRef, no source URL of an entry, no contentFile,");
        console.DisplayTextLine("      no verification evidence, or a file in .lex not declared in files[]).");
        console.DisplayTextLine("      Information: entries with a referenceYear other than the validFrom year.");
        console.DisplayTextLine("      Exclusions of all kinds (coverage, sourceLocation, orphan, autoVerification, derived, notApplicable) are");
        console.DisplayTextLine("      listed in the report; a derived or notApplicable exclusion whose pattern matches no property of an entry,");
        console.DisplayTextLine("      and a notApplicable exclusion of a property that is not 0, are manifest findings.");
        console.DisplayTextLine("      Freshness of snapshot.json is checked by LexVerify.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. file name or file mask [FileMask] (e.g. lex.json or **/lex.json)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          Report: path of a Markdown report; a CSV file with the same name is written next to it");
        console.DisplayTextLine("          Root: root folder; the file mask is resolved in every repository folder below it");
        console.DisplayTextLine("          RepoMask: mask of the repository folders below the root (default *, requires Root)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: /top (default) or /recursive");
        console.DisplayTextLine("          result mode: /strict (default, findings give exit code 4) or /lenient (report only)");
        console.DisplayTextLine("          source location: /optional (default, a missing sourceLocation is information) or /required (finding)");
        console.DisplayTextLine("          verification: /verificationoptional (default, entries without verification are counted)");
        console.DisplayTextLine("                        or /verificationrequired (an entry without verification is a finding)");
        console.DisplayTextLine("          source URLs: /skipurls (default) or /checkurls (404/410 or unknown host is a finding, a blocking host a note;");
        console.DisplayTextLine("                       a source whose normalized text differs from the snapshot hash is a finding: check the value)");
        console.DisplayTextLine("          release gate: /development (default) or /release (/required and /verificationrequired; a secondary");
        console.DisplayTextLine("                        main source is a finding; the entries are verified against their local evidence and");
        console.DisplayTextLine("                        an entry without manual verification is a finding when it is ambiguous, its quote is");
        console.DisplayTextLine("                        not found or it is excluded from auto verification but has an automatic record;");
        console.DisplayTextLine("                        zero values without justification and evidence files that back no entry are a finding)");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0 no findings (or /lenient), 4 findings (/strict), 6 no lex.json found or root folder missing,");
        console.DisplayTextLine("          -1 invalid parameters or unexpected error");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexCheck .lex/lex.json");
        console.DisplayTextLine("          LexCheck lex.json /recursive /lenient Report:C:\\Reports\\LexCheck.md");
        console.DisplayTextLine("          LexCheck lex.json /recursive Root:C:\\Repos RepoMask:Payroll.*");
        console.DisplayTextLine("          LexCheck lex.json /recursive /release Report:C:\\Reports\\LexRelease.md");
    }
}
