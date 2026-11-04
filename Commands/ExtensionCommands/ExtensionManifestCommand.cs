using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension manifest command
/// </summary>
[Command("ExtensionManifest")]
// ReSharper disable once UnusedType.Global
internal sealed class ExtensionManifestCommand : CommandBase<ExtensionManifestParameters>
{
    /// <summary>Create or update the extension manifest</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override Task<int> Execute(CommandContext context, ExtensionManifestParameters parameters)
    {
        var console = context.Console;
        DisplayTitle(console, "Extension manifest");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Path             {parameters.Path}");
            console.DisplayTextLine($"Name             {parameters.Name ?? "-"}");
            console.DisplayTextLine($"Version          {parameters.Version ?? "-"}");
            console.DisplayTextLine($"Description      {parameters.Description ?? "-"}");
            console.DisplayTextLine($"Requires console {parameters.RequiresConsole ?? "-"}");
            console.DisplayTextLine($"Assemblies       {(parameters.Assemblies == null ? "-" : string.Join(", ", parameters.Assemblies))}");
        }
        console.DisplayNewLine();

        try
        {
            var manifest = ExtensionManifestBuilder.Build(parameters);
            var manifestFile = Path.Combine(Path.GetFullPath(parameters.Path), ExtensionManifest.FileName);
            manifest.Save(manifestFile);

            console.DisplaySuccessLine(
                $"Extension manifest {manifestFile} written: {manifest.Name} {manifest.Version}, " +
                $"{manifest.Files.Count} files, requires console {manifest.RequiresConsole}.");
            return Task.FromResult((int)ProgramExitCode.Ok);
        }
        catch (Exception exception)
        {
            ProcessError(console, exception);
            return Task.FromResult((int)ProgramExitCode.InvalidInput);
        }
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ExtensionManifestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ExtensionManifest");
        console.DisplayTextLine("      Create or update the extension manifest (extension.json) with the hashes of all files");
        console.DisplayTextLine("      Values of an existing manifest are kept unless overridden");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. extension folder [Path]");
        console.DisplayTextLine("      Named arguments (optional):");
        console.DisplayTextLine("          Name:            extension name, default: folder name");
        console.DisplayTextLine("          Version:         extension version, default: version of the first assembly");
        console.DisplayTextLine("          Description:     extension description");
        console.DisplayTextLine($"          RequiresConsole: required console extension API version, default: {ExtensionManifest.ConsoleApiVersion}");
        console.DisplayTextLine("          Assemblies:      comma separated command assemblies, default: <Name>.dll");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ExtensionManifest extensions\\MyExtension");
        console.DisplayTextLine("          ExtensionManifest bin\\Release\\net10.0 Name:MyExtension Version:1.0.0");
        console.DisplayTextLine("          ExtensionManifest MyExtension Assemblies:MyExtension.dll,MyExtension.Reports.dll");
    }
}
