using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.Script;

namespace PayrollEngine.PayrollConsole.Commands.ScriptCommands;

/// <summary>
/// Script publish command
/// </summary>
[Command("ScriptPublish")]
// ReSharper disable once UnusedType.Global
internal sealed class ScriptPublishCommand : CommandBase<ScriptPublishParameters>
{
    /// <summary>Publish script</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, ScriptPublishParameters parameters)
    {
        if (!File.Exists(parameters.SourceFile))
        {
            throw new ArgumentException($"Missing script file {parameters.SourceFile}.");
        }

        DisplayTitle(context.Console, $"Script publish - {parameters.SourceFile}");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"File             {parameters.SourceFile}");
            context.Console.DisplayTextLine($"Script           {parameters.SourceScript}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        // publish
        context.Console.DisplayText("Publishing scripts...");
        try
        {
            var publisher = new Publisher(context.HttpClient);
            var publishCount = await publisher.Publish(parameters.SourceFile, parameters.SourceScript);
            context.Console.DisplayNewLine();
            if (publishCount > 0)
            {
                context.Console.DisplaySuccessLine($"Script {parameters.SourceFile} successfully published: {publishCount} scripts");
            }
            else
            {
                context.Console.DisplayInfoLine("No scripts were published");
            }
            context.Console.DisplayNewLine();
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (context.Console.DisplayLevel == DisplayLevel.Silent)
            {
                context.Console.WriteErrorLine($"Publish error in script {parameters.SourceFile}: {exception.GetBaseMessage()}");
            }
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ScriptPublishParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ScriptPublish");
        console.DisplayTextLine("      Publish scripts from C-Sharp file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. C-Sharp source file name [SourceFile]");
        console.DisplayTextLine("          2. script key e.g. report name, case name... (default: all scripts in file) [SourceScript]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ScriptPublish c:\\test\\CaseAvailableFunction.cs");
        console.DisplayTextLine("          ScriptPublish c:\\test\\CaseAvailableFunction.cs MyCaseName");
    }
}