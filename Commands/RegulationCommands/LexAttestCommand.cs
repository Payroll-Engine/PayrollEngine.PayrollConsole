using System;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Records the manual verification of lex entries (verifiedBy, verifiedOn, evidence)
/// or the second review of a manual verification (reviewedBy, reviewedOn)
/// </summary>
[Command("LexAttest")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexAttestCommand : CommandBase<LexAttestParameters>
{
    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexAttestParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Attest");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Key              {parameters.Key}");
            context.Console.DisplayTextLine($"Lex              {(string.IsNullOrWhiteSpace(parameters.Lex) ? "(current folder)" : parameters.Lex)}");
            if (parameters.IsReview)
            {
                context.Console.DisplayTextLine($"Reviewed by      {parameters.ReviewedBy}");
                context.Console.DisplayTextLine($"Reviewed on      {parameters.ReviewedOn ?? "(today)"}");
            }
            else
            {
                context.Console.DisplayTextLine($"Verified by      {parameters.VerifiedBy}");
                context.Console.DisplayTextLine($"Verified on      {parameters.VerifiedOn ?? "(today)"}");
                context.Console.DisplayTextLine($"Evidence         {parameters.Evidence}");
            }
            context.Console.DisplayTextLine($"Overwrite mode   {parameters.OverwriteMode}");
        }
        context.Console.DisplayNewLine();

        try
        {
            var attestation = await AttestOperation.AttestAsync(parameters.Lex, parameters.ToRequest());
            if (attestation.Error != null)
            {
                context.Console.DisplayErrorLine(attestation.Error);
                return (int)ProgramExitCode.InvalidInput;
            }

            foreach (var (key, result, reviewNotRequired) in attestation.Entries.Select(x => (x.Key, x.Result, x.ReviewNotRequired)))
            {
                switch (result)
                {
                    case AttestResult.Written:
                        context.Console.DisplaySuccessLine($"  [OK] {key}");
                        break;
                    case AttestResult.ManualExists:
                        context.Console.DisplayInfoLine($"  [SKIP] {key} - manual verification exists (use /overwrite)");
                        break;
                    case AttestResult.ReviewExists:
                        context.Console.DisplayInfoLine($"  [SKIP] {key} - review exists (use /overwrite)");
                        break;
                    case AttestResult.NoManualRecord:
                        context.Console.DisplayInfoLine($"  [SKIP] {key} - no manual verification to review");
                        break;
                }
                if (reviewNotRequired)
                {
                    context.Console.DisplayInfoLine($"         note: the source of {key} does not require a review ({Source.ReviewFourEyes})");
                }
            }

            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine(
                $"Summary: {attestation.Written} written, {attestation.Unchanged} unchanged, {attestation.Skipped} skipped");
            if (attestation.Written > 0)
            {
                context.Console.DisplayInfoLine("Update the snapshot (LexSnapshot /overwrite).");
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
        LexAttestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexAttest");
        console.DisplayTextLine("      Record the manual verification of lex entries: a person confirms the values against the evidence.");
        console.DisplayTextLine("      The evidence must be declared in files[] and exist in the .lex folder. An automatic record is");
        console.DisplayTextLine("      replaced, a manual record only with /overwrite.");
        console.DisplayTextLine("      With ReviewedBy the command records the second review (four-eyes) of existing manual records.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. entry key or pattern [Key] (Name matches all keys below it, Prefix* matches a prefix)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          Lex: path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)");
        console.DisplayTextLine("          VerifiedBy: person who verified the values");
        console.DisplayTextLine("          VerifiedOn: verification date yyyy-MM-dd (default: today)");
        console.DisplayTextLine("          Evidence: evidence document (declared in files[])");
        console.DisplayTextLine("          ReviewedBy: second person (review of a manual verification)");
        console.DisplayTextLine("          ReviewedOn: review date yyyy-MM-dd (default: today)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          overwrite mode: /skip (default) or /overwrite (replace a manual record or review)");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0 ok (skipped entries included), 6 lex.json missing, no matching entry or evidence missing,");
        console.DisplayTextLine("          -1 invalid parameters or error");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexAttest Rates.Standard \"VerifiedBy:Jane Doe\" Evidence:tax-rates-2026.pdf");
        console.DisplayTextLine("          LexAttest Rates Lex:Data.Tax.2026 \"VerifiedBy:Jane Doe\" Evidence:tax-rates-2026.pdf VerifiedOn:2026-11-03");
        console.DisplayTextLine("          LexAttest Rates.* \"ReviewedBy:John Doe\"");
    }
}
