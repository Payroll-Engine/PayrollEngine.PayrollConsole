using PayrollEngine.Client.Command;
using PayrollEngine.Lex;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexAttest command parameters
/// </summary>
public class LexAttestParameters : ICommandParameters
{
    /// <summary>
    /// Entry key or key pattern (Name matches all keys below it, trailing * matches a prefix)
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Path of lex.json or of its folder (default: lex.json or .lex/lex.json in the current folder)
    /// </summary>
    public string Lex { get; init; }

    /// <summary>
    /// Person who verified the values against the evidence
    /// </summary>
    public string VerifiedBy { get; init; }

    /// <summary>
    /// Verification date yyyy-MM-dd (default: today)
    /// </summary>
    public string VerifiedOn { get; init; }

    /// <summary>
    /// Evidence document, declared in files[]
    /// </summary>
    public string Evidence { get; init; }

    /// <summary>
    /// Second person who reviewed the manual verification (four-eyes)
    /// </summary>
    public string ReviewedBy { get; init; }

    /// <summary>
    /// Review date yyyy-MM-dd (default: today)
    /// </summary>
    public string ReviewedOn { get; init; }

    /// <summary>
    /// Handling of an existing manual record or review (default: skip)
    /// </summary>
    public LexOverwriteMode OverwriteMode { get; private init; } = LexOverwriteMode.Skip;

    /// <summary>
    /// Review mode: the second confirmation of an existing manual record
    /// </summary>
    public bool IsReview => !string.IsNullOrWhiteSpace(ReviewedBy);

    /// <inheritdoc />
    public Type[] Toggles => [typeof(LexOverwriteMode)];

    /// <inheritdoc />
    public string Test() =>
        ToRequest().Test();

    /// <summary>
    /// The attestation of the library operation; the parameter rules are those of <see cref="AttestRequest.Test"/>
    /// </summary>
    public AttestRequest ToRequest() =>
        new()
        {
            Key = Key,
            VerifiedBy = VerifiedBy,
            VerifiedOn = VerifiedOn,
            Evidence = Evidence,
            ReviewedBy = ReviewedBy,
            ReviewedOn = ReviewedOn,
            Overwrite = OverwriteMode == LexOverwriteMode.Overwrite
        };

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexAttestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Key = LexArguments.Positional(parser.Get(2, nameof(Key))),
            Lex = parser.GetByName(nameof(Lex)),
            VerifiedBy = parser.GetByName(nameof(VerifiedBy)),
            VerifiedOn = parser.GetByName(nameof(VerifiedOn)),
            Evidence = parser.GetByName(nameof(Evidence)),
            ReviewedBy = parser.GetByName(nameof(ReviewedBy)),
            ReviewedOn = parser.GetByName(nameof(ReviewedOn)),
            OverwriteMode = parser.GetEnumToggle(LexOverwriteMode.Skip)
        };
}
