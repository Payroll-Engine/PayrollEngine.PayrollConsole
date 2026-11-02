namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexCheck result handling
/// </summary>
public enum LexCheckMode
{
    /// <summary>
    /// Findings fail the command with exit code 6 (default)
    /// </summary>
    Strict,

    /// <summary>
    /// Findings are reported, the exit code stays 0 (e.g. to write a report only)
    /// </summary>
    Lenient
}

/// <summary>
/// LexCheck handling of entries without sourceLocation
/// </summary>
public enum LexLocationMode
{
    /// <summary>
    /// A missing sourceLocation is reported as information, not as finding (default)
    /// </summary>
    Optional,

    /// <summary>
    /// A missing sourceLocation is a finding
    /// </summary>
    Required
}

/// <summary>
/// LexCheck handling of source URLs
/// </summary>
public enum LexUrlMode
{
    /// <summary>
    /// The source URLs are not requested (default, no network access)
    /// </summary>
    SkipUrls,

    /// <summary>
    /// Every source URL is requested: a missing page (404/410, unknown host) is a finding,
    /// a host that blocks scripts (401/403/429) is only a note
    /// </summary>
    CheckUrls
}
