using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// InstallRegulationPackage command parameters
/// </summary>
public sealed class InstallRegulationPackageParameters : ICommandParameters
{
    /// <summary>
    /// Path to the .nupkg file to install
    /// </summary>
    public string PackageFile { get; init; }

    /// <summary>
    /// Target tenant identifier — the tenant into which the regulation is imported
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Install mode: New (default) fails if regulation already exists; Overwrite replaces
    /// </summary>
    public RegulationInstallMode InstallMode { get; init; } = RegulationInstallMode.New;

    /// <summary>
    /// Dry-run mode: Execute (default) performs the import; DryRun validates without changes
    /// </summary>
    public RegulationDryRun DryRun { get; init; } = RegulationDryRun.Execute;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(RegulationInstallMode),
        typeof(RegulationDryRun)
    ];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(PackageFile))
        {
            return "Missing package file path";
        }
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant identifier";
        }
        return null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static InstallRegulationPackageParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            PackageFile = parser.Get(2, nameof(PackageFile)),
            Tenant = parser.Get(3, nameof(Tenant)),
            InstallMode = parser.GetEnumToggle(RegulationInstallMode.New),
            DryRun = parser.GetEnumToggle(RegulationDryRun.Execute)
        };
}
