namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexApply execution mode
/// </summary>
public enum LexApplyMode
{
    /// <summary>
    /// Write the lex values into the regulation files (default)
    /// </summary>
    Write,

    /// <summary>
    /// Show the differences only — no files written
    /// </summary>
    DryRun
}
