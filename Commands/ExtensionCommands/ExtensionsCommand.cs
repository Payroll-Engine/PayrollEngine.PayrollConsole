using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extensions command
/// </summary>
[Command("Extensions")]
// ReSharper disable once UnusedType.Global
internal sealed class ExtensionsCommand : CommandBase<ExtensionsParameters>
{
    /// <summary>List the console extensions</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override Task<int> Execute(CommandContext context, ExtensionsParameters parameters)
    {
        var console = context.Console;
        DisplayTitle(console, "Extensions");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Folder           {ExtensionCatalog.ExtensionsPath ?? "-"}");
            console.DisplayTextLine($"Extension API    {ExtensionManifest.ConsoleApiVersion}");
        }
        console.DisplayNewLine();

        var extensions = ExtensionCatalog.Extensions;
        if (!extensions.Any())
        {
            console.DisplayTextLine("No extensions available.");
            return Task.FromResult((int)ProgramExitCode.Ok);
        }

        foreach (var extension in extensions)
        {
            var title = string.IsNullOrWhiteSpace(extension.Version)
                ? extension.Name
                : $"{extension.Name} {extension.Version}";
            if (extension.Status == ExtensionStatus.Loaded)
            {
                console.DisplaySuccessLine($"{title} - loaded");
            }
            else
            {
                console.DisplayErrorLine($"{title} - rejected");
            }
            if (!string.IsNullOrWhiteSpace(extension.Description))
            {
                console.DisplayTextLine($"  Description: {extension.Description}");
            }
            console.DisplayTextLine($"  Path:        {extension.Path}");
            console.DisplayTextLine(extension.Status == ExtensionStatus.Loaded
                ? $"  Commands:    {string.Join(", ", extension.Commands)}"
                : $"  Reason:      {extension.Reason}");
            console.DisplayNewLine();
        }

        var loaded = extensions.Count(x => x.Status == ExtensionStatus.Loaded);
        console.DisplayTextLine($"{loaded} loaded, {extensions.Count - loaded} rejected.");
        return Task.FromResult((int)ProgramExitCode.Ok);
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ExtensionsParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- Extensions");
        console.DisplayTextLine("      List the console extensions with version, path, status and commands");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          Extensions");
    }
}
