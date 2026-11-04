using System;
using System.IO;
using System.Reflection;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands;
using PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

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
        var extensionsPath = Path.Combine(info.DirectoryName, ExtensionLoader.ExtensionsFolderName);

        // extensions
        var extensions = new ExtensionLoader(commandManager, logger).Load(extensionsPath);
        ExtensionCatalog.Set(extensionsPath, extensions);
    }
}
