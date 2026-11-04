using PayrollEngine.Client.Command;
using PayrollEngine.Lex;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexAdd command parameters
/// </summary>
public class LexAddParameters : ICommandParameters
{
    /// <summary>
    /// Entry key (Lookup.Key)
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)
    /// </summary>
    public string Lex { get; init; }

    /// <summary>
    /// Value read from the source
    /// </summary>
    public string Content { get; init; }

    /// <summary>
    /// Unit of the value
    /// </summary>
    public string Unit { get; init; }

    /// <summary>
    /// Source URL, declared in sources[]
    /// </summary>
    public string SourceUrl { get; init; }

    /// <summary>
    /// Location of the value in the source (e.g. page, section, table)
    /// </summary>
    public string SourceLocation { get; init; }

    /// <summary>
    /// Legal reference of the value
    /// </summary>
    public string LegalReference { get; init; }

    /// <summary>
    /// Evidence document, declared in files[]
    /// </summary>
    public string DocumentRef { get; init; }

    /// <summary>
    /// Description of the entry
    /// </summary>
    public string Description { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        EntryRules.KeyError(Key) ?? EntryRules.ContentError(Content);

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexAddParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Key = LexArguments.Positional(parser.Get(2, nameof(Key))),
            Lex = parser.GetByName(nameof(Lex)),
            Content = parser.GetByName(nameof(Content)),
            Unit = parser.GetByName(nameof(Unit)),
            SourceUrl = parser.GetByName(nameof(SourceUrl)),
            SourceLocation = parser.GetByName(nameof(SourceLocation)),
            LegalReference = parser.GetByName(nameof(LegalReference)),
            DocumentRef = parser.GetByName(nameof(DocumentRef)),
            Description = parser.GetByName(nameof(Description))
        };
}
