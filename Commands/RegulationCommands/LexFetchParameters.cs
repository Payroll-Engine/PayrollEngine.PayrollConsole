using PayrollEngine.Client.Command;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexFetch command parameters
/// </summary>
public class LexFetchParameters : ICommandParameters
{
    /// <summary>
    /// Source URL of the document
    /// </summary>
    public string Url { get; init; }

    /// <summary>
    /// Target file name in the .lex folder (default: from the URL)
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// Path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)
    /// </summary>
    public string Lex { get; init; }

    /// <summary>
    /// Description of the file in files[]
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Handling of an existing file with different content (default: skip)
    /// </summary>
    public LexOverwriteMode OverwriteMode { get; private init; } = LexOverwriteMode.Skip;

    /// <inheritdoc />
    public Type[] Toggles => [typeof(LexOverwriteMode)];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            return "Missing source URL";
        }
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return $"Invalid source URL: {Url}";
        }
        return null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexFetchParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Url = LexArguments.Positional(parser.Get(2, nameof(Url))),
            FileName = LexArguments.Positional(parser.Get(3, nameof(FileName))),
            Lex = parser.GetByName(nameof(Lex)),
            Description = parser.GetByName(nameof(Description)),
            OverwriteMode = parser.GetEnumToggle(LexOverwriteMode.Skip)
        };
}
