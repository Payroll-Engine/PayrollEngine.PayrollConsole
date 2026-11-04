using System.Collections.Generic;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension load results of the running console
/// </summary>
public static class ExtensionCatalog
{
    /// <summary>
    /// Extensions folder
    /// </summary>
    public static string ExtensionsPath { get; private set; }

    /// <summary>
    /// Extension load results, ordered by load order
    /// </summary>
    public static IReadOnlyList<ExtensionInfo> Extensions { get; private set; } = [];

    /// <summary>
    /// Set the extension load results
    /// </summary>
    /// <param name="extensionsPath">Extensions folder</param>
    /// <param name="extensions">Extension load results</param>
    public static void Set(string extensionsPath, IReadOnlyList<ExtensionInfo> extensions)
    {
        ExtensionsPath = extensionsPath;
        Extensions = extensions ?? [];
    }
}
