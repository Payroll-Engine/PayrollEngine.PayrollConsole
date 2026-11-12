using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Adds a lex entry with the required fields; missing fields are written empty and reported by LexCheck
/// </summary>
[Command("LexAdd")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexAddCommand : CommandBase<LexAddParameters>
{
    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexAddParameters parameters)
    {
        DisplayTitle(context.Console, "Lex Add");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Key              {parameters.Key}");
            context.Console.DisplayTextLine($"Lex              {(string.IsNullOrWhiteSpace(parameters.Lex) ? "(current folder)" : parameters.Lex)}");
            context.Console.DisplayTextLine($"Content          {parameters.Content ?? "-"}");
            context.Console.DisplayTextLine($"Source URLs      {(parameters.SourceUrls.Count > 0 ? string.Join(" ", parameters.SourceUrls) : "-")}");
            context.Console.DisplayTextLine($"Legal reference  {parameters.LegalReference ?? "-"}");
        }
        context.Console.DisplayNewLine();

        try
        {
            var result = await AddEntryOperation.AddAsync(parameters.Lex, new Entry
            {
                Key = parameters.Key,
                Content = parameters.Content,
                Unit = parameters.Unit,
                SourceUrls = parameters.SourceUrls,
                SourceLocation = parameters.SourceLocation,
                LegalReference = parameters.LegalReference,
                SourceQuote = parameters.SourceQuote,
                DocumentRef = parameters.DocumentRef,
                Description = parameters.Description
            });
            if (!result.Added)
            {
                foreach (var error in result.Errors)
                {
                    context.Console.DisplayErrorLine(error);
                }
                return (int)ProgramExitCode.InvalidInput;
            }
            context.Console.DisplaySuccessLine($"  [OK] {parameters.Key} added to {result.LexPath}");
            if (result.Missing.Count > 0)
            {
                context.Console.DisplayInfoLine($"         to fill: {string.Join(", ", result.Missing)}");
            }
            context.Console.DisplayInfoLine("Verify the entry (LexVerifySource, LexAttest) and update the snapshot (LexSnapshot /overwrite).");
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
        LexAddParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexAdd");
        console.DisplayTextLine("      Add a lex entry. The value is read from the source by a person; the command checks the format.");
        console.DisplayTextLine("      The key must be new and have the format Lookup.Key; structured content must be valid JSON;");
        console.DisplayTextLine("      every source URL must be declared in sources[], the document in files[].");
        console.DisplayTextLine("      Missing content, sourceUrls, sourceLocation and legalReference are written empty: LexCheck reports them.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. entry key [Key] (Lookup.Key)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          Lex: path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)");
        console.DisplayTextLine("          Content: value read from the source");
        console.DisplayTextLine("          Unit: unit of the value");
        console.DisplayTextLine("          SourceUrls: source URLs separated by spaces, first the main source (declared in sources[])");
        console.DisplayTextLine("          SourceLocation: location in the source (e.g. page, section, table)");
        console.DisplayTextLine("          LegalReference: legal reference");
        console.DisplayTextLine("          SourceQuote: text passage of the source with the value; LexVerifySource looks up the numbers");
        console.DisplayTextLine("                       in the section of the quote (anchors short numbers and numbers in words)");
        console.DisplayTextLine("          DocumentRef: evidence document (declared in files[])");
        console.DisplayTextLine("          Description: description of the entry");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0 ok, 6 lex.json missing, key exists or source/document not declared, -1 invalid parameters or error");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexAdd Rates.Standard Content:0.081 Unit:rate SourceUrls:https://example.org/rates");
        console.DisplayTextLine("          LexAdd Limits.Ceiling Content:42000 \"SourceUrls:https://example.org/rates https://example.org/limits\"");
        console.DisplayTextLine("          LexAdd Rates.Reduced Lex:Data.Tax.2026 \"LegalReference:Tax Act s. 12\"");
        console.DisplayTextLine("          LexAdd Overtime.Factor Content:1.5 Unit:factor \"SourceQuote:one and one-half times the regular rate\"");
    }
}
