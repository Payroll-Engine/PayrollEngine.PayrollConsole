using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Loads the console extensions from the extensions folder.
/// Each extension is a sub folder with an extension manifest, loaded into its own load context.
/// A failing extension is rejected without affecting the console or other extensions.
/// </summary>
/// <param name="commandManager">Command manager</param>
/// <param name="logger">Logger</param>
public sealed class ExtensionLoader(CommandManager commandManager, ILogger logger = null)
{
    /// <summary>
    /// Extensions folder name, located in the application folder
    /// </summary>
    public const string ExtensionsFolderName = "extensions";

    private CommandManager CommandManager { get; } = commandManager ??
                                                     throw new ArgumentNullException(nameof(commandManager));
    private ILogger Logger { get; } = logger;

    /// <summary>
    /// Load all extensions in alphabetical order of the folder names
    /// </summary>
    /// <param name="extensionsPath">Extensions folder</param>
    /// <returns>Load result per extension</returns>
    public List<ExtensionInfo> Load(string extensionsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionsPath);
        var results = new List<ExtensionInfo>();
        if (!Directory.Exists(extensionsPath))
        {
            return results;
        }

        // loose assemblies without folder and manifest
        var looseFiles = Directory.GetFiles(extensionsPath, "*.dll", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x, StringComparer.Ordinal);
        foreach (var looseFile in looseFiles)
        {
            results.Add(Reject(new ExtensionInfo
            {
                Name = Path.GetFileNameWithoutExtension(looseFile),
                Path = looseFile,
                Status = ExtensionStatus.Rejected,
                Reason = $"Assembly without extension folder and {ExtensionManifest.FileName} is not supported, " +
                         $"move it to {ExtensionsFolderName}/<Name>/ and add a manifest."
            }));
        }

        // extension folders
        var extensionPaths = Directory.GetDirectories(extensionsPath)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x, StringComparer.Ordinal);
        foreach (var extensionPath in extensionPaths)
        {
            results.Add(LoadExtension(extensionPath));
        }
        return results;
    }

    private ExtensionInfo LoadExtension(string extensionPath)
    {
        var folderName = Path.GetFileName(extensionPath);
        ExtensionManifest manifest = null;
        try
        {
            // manifest
            var manifestFile = Path.Combine(extensionPath, ExtensionManifest.FileName);
            if (!File.Exists(manifestFile))
            {
                return Reject(folderName, extensionPath, null, $"Missing manifest {ExtensionManifest.FileName}.");
            }
            manifest = ExtensionManifest.Load(manifestFile);
            var error = manifest.Validate(extensionPath);
            if (error != null)
            {
                return Reject(folderName, extensionPath, manifest, $"Invalid manifest: {error}");
            }
            if (!string.Equals(manifest.Name, folderName, StringComparison.OrdinalIgnoreCase))
            {
                return Reject(folderName, extensionPath, manifest,
                    $"Manifest name {manifest.Name} does not match the folder name {folderName}.");
            }

            // extension api version
            var requiredVersion = manifest.GetRequiredConsoleVersion();
            if (!CommandManager.IsExtensionApiSupported(requiredVersion))
            {
                return Reject(folderName, extensionPath, manifest,
                    $"Requires console extension API {manifest.RequiresConsole}, " +
                    $"the console provides {ExtensionManifest.ConsoleApiVersion}.");
            }

            // integrity
            error = manifest.VerifyFiles(extensionPath);
            if (error != null)
            {
                return Reject(folderName, extensionPath, manifest, error);
            }

            // load context
            var assemblyPaths = manifest.Assemblies
                .Select(x => ExtensionManifest.GetFullPath(extensionPath, x))
                .ToList();
            var context = new ExtensionLoadContext(manifest.Name, extensionPath, assemblyPaths.First());
            var assemblies = new List<Assembly>();
            foreach (var assemblyPath in assemblyPaths)
            {
                assemblies.Add(context.LoadFromAssemblyPath(assemblyPath));
            }

            // commands
            var origin = new CommandOrigin(manifest.Name, manifest.Version, extensionPath);
            var commands = CommandManager.RegisterAssemblies(assemblies, origin);
            if (!commands.Any())
            {
                return Reject(folderName, extensionPath, manifest, "No commands found in the manifest assemblies.");
            }

            var info = new ExtensionInfo
            {
                Name = manifest.Name,
                Version = manifest.Version,
                Description = manifest.Description,
                Path = extensionPath,
                Status = ExtensionStatus.Loaded,
                Commands = commands
            };
            Logger?.Information($"Extension {info.Name} {info.Version} loaded from {info.Path}: " +
                                $"{string.Join(", ", info.Commands)}");
            return info;
        }
        catch (Exception exception)
        {
            var reason = exception is CommandException ? exception.Message : exception.GetBaseException().Message;
            return Reject(folderName, extensionPath, manifest, reason);
        }
    }

    private ExtensionInfo Reject(string folderName, string extensionPath, ExtensionManifest manifest, string reason) =>
        Reject(new ExtensionInfo
        {
            Name = string.IsNullOrWhiteSpace(manifest?.Name) ? folderName : manifest.Name,
            Version = manifest?.Version,
            Description = manifest?.Description,
            Path = extensionPath,
            Status = ExtensionStatus.Rejected,
            Reason = reason
        });

    private ExtensionInfo Reject(ExtensionInfo info)
    {
        Logger?.Warning($"Extension {info.Name} rejected ({info.Path}): {info.Reason}");
        return info;
    }
}
