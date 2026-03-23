namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation package installation mode
/// </summary>
public enum RegulationInstallMode
{
    /// <summary>
    /// Fail if the regulation already exists in the target tenant (default)
    /// </summary>
    New,

    /// <summary>
    /// Overwrite existing regulation objects
    /// </summary>
    Overwrite
}
