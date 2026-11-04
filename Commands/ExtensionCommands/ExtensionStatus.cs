namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension load status
/// </summary>
public enum ExtensionStatus
{
    /// <summary>
    /// Extension loaded, commands registered
    /// </summary>
    Loaded,

    /// <summary>
    /// Extension rejected, no command registered
    /// </summary>
    Rejected
}
