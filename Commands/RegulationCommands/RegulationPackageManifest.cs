using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation package manifest — embedded as <c>regulation-package.json</c> in the NuGet package.
/// Defines install metadata and the ordered list of files to import.
/// </summary>
public sealed class RegulationPackageManifest
{
    /// <summary>NuGet package identifier (e.g. Acme.Regulation.Country)</summary>
    [JsonPropertyName("packageId")]
    public string PackageId { get; set; }

    /// <summary>PE regulation name as registered in the backend (e.g. Acme.Regulation.Country)</summary>
    [JsonPropertyName("regulationName")]
    public string RegulationName { get; set; }

    /// <summary>Package version (calendar-semantic, e.g. 2026.1 or 2026.1-beta.1)</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    /// PE regulation names this package depends on. Each must already be installed in the
    /// target tenant before this package can be installed.
    /// </summary>
    [JsonPropertyName("baseRegulations")]
    public List<string> BaseRegulations { get; set; } = [];

    /// <summary>
    /// Files to import, in order, relative to the package root within the NuGet ZIP
    /// (under the <c>regulation/</c> folder). The first entry must create the regulation;
    /// subsequent entries extend it.
    /// </summary>
    [JsonPropertyName("installFiles")]
    public List<string> InstallFiles { get; set; } = [];
}
