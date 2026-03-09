using System;
using System.IO;
using System.Reflection;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands;

namespace PayrollEngine.PayrollConsole;

internal static class CommandProvider
{
    internal static void RegisterCommands(CommandManager commandManager, ILogger logger = null)
    {
        ArgumentNullException.ThrowIfNull(commandManager);

        // internal commands
        commandManager.RegisterAssembly(typeof(HelpCommand).Assembly);

        // program path
        var path = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        // extensions path
        var info = new FileInfo(path);
        if (string.IsNullOrWhiteSpace(info.DirectoryName))
        {
            return;
        }
        var extensionsPath = Path.Combine(info.DirectoryName, "extensions");
        if (!Directory.Exists(extensionsPath))
        {
            return;
        }

        // load extension assemblies
        var assemblies = Directory.GetFiles(extensionsPath, "*.dll", SearchOption.AllDirectories);
        foreach (var assembly in assemblies)
        {
            try
            {
                logger?.Debug($"Loading extension {assembly}");
                var extensionAssembly = Assembly.LoadFrom(assembly);
                commandManager.RegisterAssembly(extensionAssembly);
                logger?.Debug($"Extension loaded: {extensionAssembly.GetName().Name} v{extensionAssembly.GetName().Version}");
            }
            catch (Exception exception)
            {
                logger?.Error($"Failed to load extension {assembly}: {exception.GetBaseException().Message}");
            }
        }
    }
}
