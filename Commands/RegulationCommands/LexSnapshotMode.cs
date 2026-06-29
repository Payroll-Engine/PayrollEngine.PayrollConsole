namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexSnapshot execution mode
/// </summary>
public enum LexSnapshotMode
{
    /// <summary>
    /// Create snapshot.json and copy files (default)
    /// </summary>
    Execute,

    /// <summary>
    /// Validate lex.json and report what would be created — no files written
    /// </summary>
    DryRun
}
