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
    /// Environment variable with the default person of a verification (used when VerifiedBy is not given)
    /// </summary>
    public const string VerifiedByEnvironmentVariable = "PAYROLL_ENGINE_LEX_USER";

    /// <summary>
    /// True when VerifiedBy was taken from the environment variable
    /// </summary>
    public bool VerifiedByFromEnvironment { get; private init; }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexAttestParameters ParserFrom(CommandLineParser parser) =>
        ParserFrom(parser, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Parse command parameters with an explicit environment lookup (the default person comes from
    /// <see cref="VerifiedByEnvironmentVariable"/>; never used for a review)
    /// </summary>
    public static LexAttestParameters ParserFrom(CommandLineParser parser, Func<string, string> getEnvironmentVariable)
    {
        var verifiedBy = parser.GetByName(nameof(VerifiedBy));
        var reviewedBy = parser.GetByName(nameof(ReviewedBy));
        var reviewedOn = parser.GetByName(nameof(ReviewedOn));
        var fromEnvironment = false;
        if (string.IsNullOrWhiteSpace(verifiedBy) &&
            string.IsNullOrWhiteSpace(reviewedBy) && string.IsNullOrWhiteSpace(reviewedOn))
        {
            var user = getEnvironmentVariable?.Invoke(VerifiedByEnvironmentVariable)?.Trim();
            if (!string.IsNullOrWhiteSpace(user))
            {
                verifiedBy = user;
                fromEnvironment = true;
            }
        }
        return new()
        {
            Key = LexArguments.Positional(parser.Get(2, nameof(Key))),
            Lex = parser.GetByName(nameof(Lex)),
            VerifiedBy = verifiedBy,
            VerifiedByFromEnvironment = fromEnvironment,
            VerifiedOn = parser.GetByName(nameof(VerifiedOn)),
            Evidence = parser.GetByName(nameof(Evidence)),
            ReviewedBy = reviewedBy,
            ReviewedOn = reviewedOn,
            OverwriteMode = parser.GetEnumToggle(LexOverwriteMode.Skip)
        };
    }
}
