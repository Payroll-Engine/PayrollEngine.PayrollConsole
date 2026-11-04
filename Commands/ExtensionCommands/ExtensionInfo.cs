using System.Collections.Generic;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension load result
/// </summary>
public sealed class ExtensionInfo
{
    /// <summary>
    /// Extension name (manifest name, folder or file name without manifest)
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Extension version
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Extension description
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Extension folder or file path
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Load status
    /// </summary>
    public ExtensionStatus Status { get; init; }

    /// <summary>
    /// Rejection reason
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Registered command names
    /// </summary>
    public List<string> Commands { get; init; } = [];
}
