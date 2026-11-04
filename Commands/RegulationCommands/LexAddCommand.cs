using System;
using System.Collections.Generic;
using System.IO;
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
            context.Console.DisplayTextLine($"Source URL       {parameters.SourceUrl ?? "-"}");
            context.Console.DisplayTextLine($"Legal reference  {parameters.LegalReference ?? "-"}");
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
            var editor = ManifestEditor.Parse(await File.ReadAllTextAsync(lexPath));

            var errors = new List<string>();
            if (editor.HasKey(parameters.Key))
            {
                errors.Add($"Entry {parameters.Key} exists");
            }
            if (!string.IsNullOrWhiteSpace(parameters.SourceUrl) && !editor.HasSource(parameters.SourceUrl))
            {
                errors.Add($"Source URL is not declared in sources[]: {parameters.SourceUrl}");
            }
            if (!string.IsNullOrWhiteSpace(parameters.DocumentRef) && !editor.HasFile(parameters.DocumentRef))
            {
                errors.Add($"Document is not declared in files[] (LexFetch): {parameters.DocumentRef}");
            }
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    context.Console.DisplayErrorLine(error);
                }
                return (int)ProgramExitCode.InvalidInput;
            }

            editor.AddValue(new Entry
            {
                Key = parameters.Key,
                Content = parameters.Content,
                Unit = parameters.Unit,
                SourceUrl = parameters.SourceUrl,
                SourceLocation = parameters.SourceLocation,
                LegalReference = parameters.LegalReference,
                DocumentRef = parameters.DocumentRef,
                Description = parameters.Description
            });
            await editor.SaveAsync(lexPath);
            context.Console.DisplaySuccessLine($"  [OK] {parameters.Key} added to {lexPath}");

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(parameters.Content)) missing.Add("content");
            if (string.IsNullOrWhiteSpace(parameters.SourceUrl)) missing.Add("sourceUrl");
            if (string.IsNullOrWhiteSpace(parameters.SourceLocation)) missing.Add("sourceLocation");
            if (string.IsNullOrWhiteSpace(parameters.LegalReference)) missing.Add("legalReference");
            if (missing.Count > 0)
            {
                context.Console.DisplayInfoLine($"         to fill: {string.Join(", ", missing)}");
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
        console.DisplayTextLine("      the source URL must be declared in sources[], the document in files[].");
        console.DisplayTextLine("      Missing content, sourceUrl, sourceLocation and legalReference are written empty: LexCheck reports them.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. entry key [Key] (Lookup.Key)");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          Lex: path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)");
        console.DisplayTextLine("          Content: value read from the source");
        console.DisplayTextLine("          Unit: unit of the value");
        console.DisplayTextLine("          SourceUrl: source URL (declared in sources[])");
        console.DisplayTextLine("          SourceLocation: location in the source (e.g. page, section, table)");
        console.DisplayTextLine("          LegalReference: legal reference");
        console.DisplayTextLine("          DocumentRef: evidence document (declared in files[])");
        console.DisplayTextLine("          Description: description of the entry");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0 ok, 6 lex.json missing, key exists or source/document not declared, -1 invalid parameters or error");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexAdd Rates.Standard Content:0.081 Unit:rate SourceUrl:https://example.org/rates");
        console.DisplayTextLine("          LexAdd Rates.Reduced Lex:Data.Tax.2026 \"LegalReference:Tax Act s. 12\"");
    }
}
