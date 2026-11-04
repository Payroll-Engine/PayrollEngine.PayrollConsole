using PayrollEngine.Client.Command;
using System;
using System.IO;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexCheck command parameters
/// </summary>
public class LexCheckParameters : ICommandParameters
{
    /// <summary>
    /// File name or file mask (e.g. lex.json or *.json); with a root folder relative to each repository folder
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Optional path of the Markdown report; a CSV file with the same name is written next to it
    /// </summary>
    public string Report { get; init; }

    /// <summary>
    /// Optional root folder: the file mask is resolved in every repository folder below it
    /// </summary>
    public string Root { get; init; }

    /// <summary>
    /// Mask of the repository folders below the root folder (default: *)
    /// </summary>
    public string RepoMask { get; init; }

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
    /// Handling of entries without verification record: optional (default) or required
    /// </summary>
    public LexVerificationMode VerificationMode { get; private init; } = LexVerificationMode.VerificationOptional;

    /// <summary>
    /// Request the source URLs (default: skip)
    /// </summary>
    public LexUrlMode UrlMode { get; private init; } = LexUrlMode.SkipUrls;

    /// <inheritdoc />
    public Type[] Toggles =>
        [typeof(DirectoryMode), typeof(LexCheckMode), typeof(LexLocationMode), typeof(LexVerificationMode), typeof(LexUrlMode)];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(FileMask))
        {
            return "Missing file mask (e.g. lex.json)";
        }
        if (!string.IsNullOrWhiteSpace(RepoMask) && string.IsNullOrWhiteSpace(Root))
        {
            return "RepoMask requires a root folder (Root:<folder>)";
        }
        if (!string.IsNullOrWhiteSpace(Root) && Path.IsPathRooted(FileMask))
        {
            return "With a root folder the file mask must be relative (e.g. lex.json)";
        }
        return null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexCheckParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            Report = parser.GetByName(nameof(Report)),
            Root = parser.GetByName(nameof(Root)),
            RepoMask = parser.GetByName(nameof(RepoMask)),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top),
            CheckMode = parser.GetEnumToggle(LexCheckMode.Strict),
            LocationMode = parser.GetEnumToggle(LexLocationMode.Optional),
            VerificationMode = parser.GetEnumToggle(LexVerificationMode.VerificationOptional),
            UrlMode = parser.GetEnumToggle(LexUrlMode.SkipUrls)
        };
}
