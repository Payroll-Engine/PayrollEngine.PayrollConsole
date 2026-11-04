using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Loader;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Isolated load context of an extension. Dependencies are resolved from the extension folder,
/// the shared contract assemblies always from the default context of the console.
/// </summary>
internal sealed class ExtensionLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Contract assemblies shared between the console and all extensions.
    /// Loading them into the extension context would break the type identity of the command contract.
    /// </summary>
    internal static IReadOnlySet<string> SharedAssemblies { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PayrollEngine.Client.Core",
            "PayrollEngine.Core",
            "PayrollEngine.Lex"
        };

    private readonly AssemblyDependencyResolver resolver;
    private readonly string extensionPath;

    internal ExtensionLoadContext(string name, string extensionPath, string mainAssemblyPath) :
        base($"Extension.{name}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(mainAssemblyPath);
        this.extensionPath = extensionPath;
        resolver = new(mainAssemblyPath);
    }

    /// <inheritdoc />
    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null || SharedAssemblies.Contains(assemblyName.Name))
        {
            // default context
            return null;
        }

        // dependency resolver (deps.json), fallback to the extension folder
        var path = resolver.ResolveAssemblyToPath(assemblyName);
        if (path == null)
        {
            var localPath = Path.Combine(extensionPath, $"{assemblyName.Name}.dll");
            if (File.Exists(localPath))
            {
                path = localPath;
            }
        }

        // unresolved assemblies are provided by the default context
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
