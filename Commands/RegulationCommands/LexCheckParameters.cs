using PayrollEngine.Client.Command;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexCheck command parameters
/// </summary>
public class LexCheckParameters : ICommandParameters
{
    /// <summary>
    /// File name or file mask (e.g. lex.json or *.json)
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Optional path of the Markdown report; a CSV file with the same name is written next to it
    /// </summary>
    public string Report { get; init; }

    /// <summary>
    /// Directory scan mode: top-level only or recursive (default: Top)
    /// </summary>
    public DirectoryMode DirectoryMode { get; private init; } = DirectoryMode.Top;

    /// <summary>
    /// Result handling: strict fails on findings (default), lenient only reports
    /// </summary>
    public LexCheckMode CheckMode { get; private init; } = LexCheckMode.Strict;

    /// <summary>
    /// Handling of entries without sourceLocation: optional (default) or required
    /// </summary>
    public LexLocationMode LocationMode { get; private init; } = LexLocationMode.Optional;

    /// <summary>
    /// Request the source URLs (default: skip)
    /// </summary>
    public LexUrlMode UrlMode { get; private init; } = LexUrlMode.SkipUrls;

    /// <inheritdoc />
    public Type[] Toggles => [typeof(DirectoryMode), typeof(LexCheckMode), typeof(LexLocationMode), typeof(LexUrlMode)];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file mask (e.g. lex.json)" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexCheckParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            Report = parser.GetByName(nameof(Report)),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top),
            CheckMode = parser.GetEnumToggle(LexCheckMode.Strict),
            LocationMode = parser.GetEnumToggle(LexLocationMode.Optional),
            UrlMode = parser.GetEnumToggle(LexUrlMode.SkipUrls)
        };
}
