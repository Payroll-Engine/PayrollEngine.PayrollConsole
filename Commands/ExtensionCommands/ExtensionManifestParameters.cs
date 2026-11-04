using System;
using System.Linq;
using System.Collections.Generic;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension manifest command parameters
/// </summary>
public class ExtensionManifestParameters : ICommandParameters
{
    /// <summary>
    /// Extension folder
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Extension name (default: existing manifest name or folder name)
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Extension version (default: existing manifest version or version of the first assembly)
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Extension description (default: existing manifest description)
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Required console extension API version (default: existing manifest value or current API version)
    /// </summary>
    public string RequiresConsole { get; init; }

    /// <summary>
    /// Command assemblies (default: existing manifest assemblies or Name.dll)
    /// </summary>
    public List<string> Assemblies { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Path) ? "Missing extension folder" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static ExtensionManifestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Path = parser.Get(2, nameof(Path)),
            Name = parser.GetByName(nameof(Name)),
            Version = parser.GetByName(nameof(Version)),
            Description = parser.GetByName(nameof(Description)),
            RequiresConsole = parser.GetByName(nameof(RequiresConsole)),
            Assemblies = parser.GetByName(nameof(Assemblies))?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        };
}
