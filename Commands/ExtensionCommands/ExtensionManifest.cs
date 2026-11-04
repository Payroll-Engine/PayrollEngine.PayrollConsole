using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension manifest (extension.json)
/// </summary>
public sealed class ExtensionManifest
{
    /// <summary>
    /// Manifest file name
    /// </summary>
    public const string FileName = "extension.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// File extensions of executable content, which must be declared in <see cref="Files"/>
    /// </summary>
    private static readonly string[] ExecutableFileExtensions = [".dll", ".exe", ".so", ".dylib"];

    /// <summary>
    /// Extension name, must match the extension folder name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Extension version
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Extension description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Required extension API version of the console (major.minor)
    /// </summary>
    public string RequiresConsole { get; set; }

    /// <summary>
    /// Assemblies containing the extension commands, relative to the extension folder.
    /// Other assemblies are dependencies and are not scanned for commands.
    /// </summary>
    public List<string> Assemblies { get; set; } = [];

    /// <summary>
    /// Extension files with hashes
    /// </summary>
    public List<ExtensionFile> Files { get; set; } = [];

    /// <summary>
    /// Read the manifest from a file
    /// </summary>
    /// <param name="fileName">Manifest file name</param>
    public static ExtensionManifest Load(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<ExtensionManifest>(json, JsonOptions) ??
               throw new PayrollException($"Empty extension manifest {fileName}.");
    }

    /// <summary>
    /// Write the manifest to a file
    /// </summary>
    /// <param name="fileName">Manifest file name</param>
    public void Save(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        File.WriteAllText(fileName, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>
    /// Parse the required console extension API version
    /// </summary>
    /// <returns>The required version, null if missing or invalid</returns>
    public Version GetRequiredConsoleVersion()
    {
        if (string.IsNullOrWhiteSpace(RequiresConsole) ||
            !System.Version.TryParse(RequiresConsole, out var version))
        {
            return null;
        }
        return version;
    }

    /// <summary>
    /// Validate the manifest structure
    /// </summary>
    /// <param name="extensionPath">Extension folder</param>
    /// <returns>Error message, null for a valid manifest</returns>
    public string Validate(string extensionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionPath);

        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Missing manifest name.";
        }
        if (string.IsNullOrWhiteSpace(Version))
        {
            return "Missing manifest version.";
        }
        if (GetRequiredConsoleVersion() == null)
        {
            return $"Missing or invalid manifest requiresConsole '{RequiresConsole}' (expected major.minor).";
        }
        if (Assemblies == null || !Assemblies.Any())
        {
            return "Missing manifest assemblies.";
        }
        if (Files == null || !Files.Any())
        {
            return "Missing manifest files.";
        }

        // files
        var filePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Files)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Path))
            {
                return "Manifest file without path.";
            }
            if (GetFullPath(extensionPath, file.Path) == null)
            {
                return $"Manifest file {file.Path} is outside of the extension folder.";
            }
            if (!IsHash(file.Sha256))
            {
                return $"Missing or invalid sha256 of manifest file {file.Path}.";
            }
            if (!filePaths.Add(GetFullPath(extensionPath, file.Path)))
            {
                return $"Duplicate manifest file {file.Path}.";
            }
        }

        // assemblies
        foreach (var assembly in Assemblies)
        {
            if (string.IsNullOrWhiteSpace(assembly))
            {
                return "Empty manifest assembly.";
            }
            var assemblyPath = GetFullPath(extensionPath, assembly);
            if (assemblyPath == null)
            {
                return $"Manifest assembly {assembly} is outside of the extension folder.";
            }
            if (!filePaths.Contains(assemblyPath))
            {
                return $"Manifest assembly {assembly} is missing in the manifest files.";
            }
        }
        return null;
    }

    /// <summary>
    /// Verify the extension files against the manifest hashes.
    /// Executable files (assemblies, native libraries) and dependency files (*.deps.json)
    /// must be declared in the manifest.
    /// </summary>
    /// <param name="extensionPath">Extension folder</param>
    /// <returns>Error message, null if all files are valid</returns>
    public string VerifyFiles(string extensionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionPath);

        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Files)
        {
            var fullPath = GetFullPath(extensionPath, file.Path);
            declared.Add(fullPath);
            if (!File.Exists(fullPath))
            {
                return $"Missing file {file.Path}.";
            }
            var hash = FileHash(fullPath);
            if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return $"Hash mismatch of file {file.Path} (expected {file.Sha256.ToLowerInvariant()}, actual {hash}).";
            }
        }

        // undeclared executable files
        var undeclared = Directory.GetFiles(extensionPath, "*", SearchOption.AllDirectories)
            .Where(RequiresDeclaration)
            .Where(x => !declared.Contains(Path.GetFullPath(x)))
            .Select(x => Path.GetRelativePath(extensionPath, x))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (undeclared.Any())
        {
            return $"Undeclared file(s) {string.Join(", ", undeclared)}.";
        }
        return null;
    }

    /// <summary>
    /// Test if a file must be declared in the manifest files
    /// </summary>
    /// <param name="fileName">File name</param>
    public static bool RequiresDeclaration(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase) ||
               ExecutableFileExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// SHA-256 of a file as lower-case hex string
    /// </summary>
    /// <param name="fileName">File name</param>
    public static string FileHash(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>
    /// Resolve a manifest path within the extension folder
    /// </summary>
    /// <param name="extensionPath">Extension folder</param>
    /// <param name="relativePath">Relative manifest path</param>
    /// <returns>The full path, null for rooted paths or paths not within the extension folder</returns>
    public static string GetFullPath(string extensionPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return null;
        }
        var basePath = Path.GetFullPath(extensionPath);
        var portablePath = relativePath.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(basePath, portablePath));
        var relative = Path.GetRelativePath(basePath, fullPath);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar) ||
            Path.IsPathRooted(relative))
        {
            return null;
        }
        return fullPath;
    }

    private static bool IsHash(string hash) =>
        hash is { Length: 64 } && hash.All(Uri.IsHexDigit);

    /// <summary>
    /// Format a version for the requiresConsole field (major.minor)
    /// </summary>
    /// <param name="version">Version</param>
    public static string FormatApiVersion(Version version) =>
        $"{version.Major}.{version.Minor}";

    /// <summary>
    /// Current console extension API version (major.minor)
    /// </summary>
    public static string ConsoleApiVersion => FormatApiVersion(CommandManager.ExtensionApiVersion);
}
