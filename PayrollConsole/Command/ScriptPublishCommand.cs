using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Script;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ScriptPublishCommand : HttpCommandBase
{
    internal ScriptPublishCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> PublishAsync(string sourceFile, string sourceScript = null)
    {
        if (!File.Exists(sourceFile))
        {
            throw new ArgumentException($"Missing script file {sourceFile}");
        }

        DisplayTitle("Publish script");
        ConsoleTool.DisplayTextLine($"File             {sourceFile}");
        ConsoleTool.DisplayTextLine($"Script           {sourceScript}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        // publish
        ConsoleTool.DisplayText("Publishing scripts...");
        try
        {
            var publisher = new ScriptPublisher(HttpClient);
            var publishCount = await publisher.Publish(sourceFile, sourceScript);
            ConsoleTool.DisplayNewLine();
            if (publishCount > 0)
            {
                ConsoleTool.DisplaySuccessLine($"Script {sourceFile} successfully published: {publishCount} scripts");
            }
            else
            {
                ConsoleTool.DisplayInfoLine("No scripts were published");
            }
            ConsoleTool.DisplayNewLine();
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (ConsoleTool.DisplayMode == ConsoleDisplayMode.Silent)
            {
                ConsoleTool.WriteErrorLine($"Publish error in script {sourceFile}: {exception.GetBaseMessage()}");
            }
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- ScriptPublish");
        ConsoleTool.DisplayTextLine("      Publish scripts from C-Sharp file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. C-Sharp source file name");
        ConsoleTool.DisplayTextLine("          2. script key e.g. report name, case name... (default: all scripts in file)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          ScriptPublish c:\\test\\CaseAvailableFunction.cs");
        ConsoleTool.DisplayTextLine("          ScriptPublish c:\\test\\CaseAvailableFunction.cs MyCaseName");
    }
}