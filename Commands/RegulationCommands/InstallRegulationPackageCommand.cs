using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// InstallRegulationPackage command — installs a regulation NuGet package into a PE backend tenant.
/// The package source can be a local file path, a file wildcard, or an HTTP(S) URL.
/// </summary>
[Command("InstallRegulationPackage")]
// ReSharper disable once UnusedType.Global
internal sealed class InstallRegulationPackageCommand : CommandBase<InstallRegulationPackageParameters>
{
    /// <summary>Install a regulation package into the backend</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>0 on success</returns>
    protected override async Task<int> Execute(CommandContext context,
        InstallRegulationPackageParameters parameters)
    {
        DisplayTitle(context.Console, "Install regulation package");

        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Package          {parameters.PackageFile}");
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Install mode     {parameters.InstallMode}");
            context.Console.DisplayTextLine($"Dry run          {parameters.DryRun}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // resolve package source — URL passes through, local path supports wildcards
            var packageSource = ResolvePackageSource(context.Console, parameters.PackageFile);
            if (packageSource == null)
            {
                return (int)ProgramExitCode.InvalidInput;
            }

            var installer = new RegulationPackageInstaller(context.HttpClient, context.Console);
            return await installer.InstallAsync(
                packageSource,
                parameters.Tenant,
                parameters.InstallMode,
                parameters.DryRun);
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <summary>
    /// Returns the resolved package source:
    /// - HTTP(S) URLs are passed through unchanged
    /// - Existing local paths are returned as-is
    /// - Wildcards are resolved to a single match
    /// </summary>
    private static string ResolvePackageSource(ICommandConsole console, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            console.DisplayErrorLine("Missing package file or URL.");
            return null;
        }

        // URL — pass through; download happens inside the installer
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return source;
        }

        // exact local file
        if (File.Exists(source))
        {
            return source;
        }

        // wildcard — resolve to a single match
        var info = new FileInfo(source);
        var dir = info.DirectoryName ?? Directory.GetCurrentDirectory();
        var matches = Directory.GetFiles(dir, info.Name);

        switch (matches.Length)
        {
            case 0:
                console.DisplayErrorLine($"Package file not found: {source}");
                return null;
            case 1:
                return matches[0];
            default:
                console.DisplayErrorLine(
                    $"Ambiguous package path '{source}' matches {matches.Length} files. " +
                    "Specify the exact file name or URL.");
                return null;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        InstallRegulationPackageParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- InstallRegulationPackage");
        console.DisplayTextLine("      Install a regulation NuGet package (.nupkg) into a PE backend tenant");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. local .nupkg file path, file wildcard, or HTTP(S) URL [PackageFile]");
        console.DisplayTextLine("          2. target tenant identifier [Tenant]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          install mode: /new (default) or /overwrite");
        console.DisplayTextLine("          dry-run mode: /execute (default) or /dryrun");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          InstallRegulationPackage Acme.Regulation.Country.2026.1.nupkg MyTenant");
        console.DisplayTextLine("          InstallRegulationPackage Acme.Regulation.Country.*.nupkg MyTenant /dryrun");
        console.DisplayTextLine("          InstallRegulationPackage Acme.Regulation.Country.2026.2.nupkg MyTenant /overwrite");
        console.DisplayTextLine("          InstallRegulationPackage https://github.com/.../releases/download/v2026.1/Acme.Regulation.Country.2026.1.nupkg MyTenant");
        console.DisplayTextLine("          InstallRegulationPackage https://github.com/.../releases/download/v2026.1/Acme.Regulation.Country.2026.1.nupkg MyTenant /dryrun");
    }
}
