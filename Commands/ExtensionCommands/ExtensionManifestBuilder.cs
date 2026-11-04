using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Builds the extension manifest of an extension folder, including the hashes of all files
/// </summary>
internal static class ExtensionManifestBuilder
{
    /// <summary>
    /// Build the manifest. Values of an existing manifest are kept unless overridden by the parameters.
    /// </summary>
    /// <param name="parameters">Manifest parameters</param>
    /// <returns>The manifest</returns>
    internal static ExtensionManifest Build(ExtensionManifestParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.Path);

        var extensionPath = Path.GetFullPath(parameters.Path);
        if (!Directory.Exists(extensionPath))
        {
            throw new PayrollException($"Missing extension folder {extensionPath}.");
        }

        var manifestFile = Path.Combine(extensionPath, ExtensionManifest.FileName);
        var existing = File.Exists(manifestFile) ? ExtensionManifest.Load(manifestFile) : null;

        var manifest = new ExtensionManifest
        {
            Name = parameters.Name ?? existing?.Name ?? Path.GetFileName(extensionPath),
            Description = parameters.Description ?? existing?.Description,
            RequiresConsole = parameters.RequiresConsole ?? existing?.RequiresConsole ??
                ExtensionManifest.ConsoleApiVersion
        };

        // assemblies
        if (parameters.Assemblies != null && parameters.Assemblies.Any())
        {
            manifest.Assemblies = parameters.Assemblies;
        }
        else if (existing?.Assemblies != null && existing.Assemblies.Any())
        {
            manifest.Assemblies = existing.Assemblies;
        }
        else if (File.Exists(Path.Combine(extensionPath, $"{manifest.Name}.dll")))
        {
            manifest.Assemblies = [$"{manifest.Name}.dll"];
        }
        else
        {
            throw new PayrollException($"Missing assembly {manifest.Name}.dll, declare the command assemblies.");
        }

        // version
        manifest.Version = parameters.Version ?? existing?.Version ?? GetAssemblyVersion(extensionPath, manifest);

        // files
        manifest.Files = Directory.GetFiles(extensionPath, "*", SearchOption.AllDirectories)
            .Where(x => !string.Equals(Path.GetFullPath(x), manifestFile, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ExtensionFile
            {
                Path = Path.GetRelativePath(extensionPath, x).Replace('\\', '/'),
                Sha256 = ExtensionManifest.FileHash(x)
            })
            .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Path, StringComparer.Ordinal)
            .ToList();

        var error = manifest.Validate(extensionPath);
        if (error != null)
        {
            throw new PayrollException($"Invalid extension manifest: {error}");
        }
        return manifest;
    }

    private static string GetAssemblyVersion(string extensionPath, ExtensionManifest manifest)
    {
        var assemblyPath = ExtensionManifest.GetFullPath(extensionPath, manifest.Assemblies.First());
        if (assemblyPath == null || !File.Exists(assemblyPath))
        {
            throw new PayrollException($"Missing assembly {manifest.Assemblies.First()}.");
        }
        var version = AssemblyName.GetAssemblyName(assemblyPath).Version;
        return version == null ? throw new PayrollException($"Missing version of assembly {assemblyPath}.") :
            version.ToString(3);
    }
}
