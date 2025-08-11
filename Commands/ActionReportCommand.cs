using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Scripting;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("ActionReport")]
// ReSharper disable once UnusedType.Global
internal sealed class ActionReportCommand : CommandBase<ActionReportParameters>
{
    /// <summary>
    /// Execute action report
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override Task<int> Execute(CommandContext context, ActionReportParameters parameters)
    {
        try
        {
            // file
            var displayName = new FileInfo(parameters.FileName).Name;
            if (!File.Exists(parameters.FileName))
            {
                context.Console.DisplayErrorLine($"Missing assembly {parameters.FileName}");
                return Task.FromResult((int)ProgramExitCode.GenericError);
            }
            context.Console.DisplayText($"Analyzing actions in assembly {displayName}...");

            // actions
            var assemblyWithActions = ActionReflector.LoadFrom(parameters.FileName);
            var assembly = assemblyWithActions.Assembly;
            var actions = assemblyWithActions.Actions;
            context.Console.DisplayNewLine();
            if (actions == null)
            {

                context.Console.DisplayErrorLine($"Error in assembly {parameters.FileName}");
                return Task.FromResult((int)ProgramExitCode.GenericError);
            }

            if (!actions.Any())
            {
                context.Console.DisplayInfoLine($"Assembly {parameters.FileName} without actions");
            }
            else
            {
                switch (parameters.ReportTarget)
                {
                    case ActionReportTarget.ActionMarkdown:
                        var markdown = ActionMarkdownWriter.Write(actions, assembly);
                        var mdFileInfo = new FileInfo(parameters.FileName);
                        var mdFileName = mdFileInfo.Name.Replace(mdFileInfo.Extension, ".md");
                        File.WriteAllText(mdFileName, markdown);
                        context.Console.DisplaySuccessLine($"{actions.Count} actions successfully exported to {new FileInfo(mdFileName).FullName}.");
                        break;
                    case ActionReportTarget.ActionJson:
                        var jsonFileInfo = new FileInfo(parameters.FileName);
                        var jsonFileName = jsonFileInfo.Name.Replace(jsonFileInfo.Extension, ".json");
                        WriteJsonFile(jsonFileName, actions);
                        context.Console.DisplaySuccessLine($"{actions.Count} actions successfully exported to {new FileInfo(jsonFileName).FullName}.");
                        break;
                }
            }
            return Task.FromResult((int)ProgramExitCode.Ok);
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return Task.FromResult((int)ProgramExitCode.GenericError);
        }
    }

    /// <summary>
    /// Save action to JSON file
    /// </summary>
    /// <param name="fileName">Target file name</param>
    /// <param name="actions">Actions to write</param>
    /// <exception cref="ArgumentException"></exception>
    private static void WriteJsonFile(string fileName, List<ActionInfo> actions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(nameof(fileName));
        }

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        File.WriteAllText(fileName, JsonSerializer.Serialize(actions,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ActionReportParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ActionReport");
        console.DisplayTextLine("      Report actions from an assembly");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. action assembly file name [FileName]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          action report targets: /actionMarkdown or /actionJson (default: actionMarkdown)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ActionReport MyAssembly.dll");
        console.DisplayTextLine("          ActionReport MyAssembly.dll /actionJson");
    }
}