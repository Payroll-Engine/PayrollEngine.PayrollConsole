using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("Write")]
// ReSharper disable once UnusedType.Global
internal sealed class WriteCommand : CommandBase<WriteParameters>
{
    /// <summary>Process the variable</summary>
    protected override Task<int> Execute(CommandContext context, WriteParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Text))
        {
            throw new ArgumentException(nameof(parameters.Text));
        }

        var text = parameters.Text;
        if (text.StartsWith('"') || text.EndsWith('"'))
        {
            text = text.Trim('"');
        }
        else if (text.StartsWith('\'') || text.EndsWith('\''))
        {
            text = text.Trim('\'');
        }

        switch (parameters.WriteMode)
        {
            case WriteMode.Console:
                WriteConsole(context.Console, text, parameters.ConsoleWriteMode);
                break;
            case WriteMode.Log:
                WriteLog(context.Logger, text, parameters.LogWriteMode);
                break;
            case WriteMode.LogAndConsole:
                WriteLog(context.Logger, text, parameters.LogWriteMode);
                WriteConsole(context.Console, text, parameters.ConsoleWriteMode);
                break;
        }

        return Task.FromResult((int)ProgramExitCode.Ok);
    }

    private static void WriteConsole(ICommandConsole console, string text, ConsoleWriteMode consoleWriteMode)
    {
        switch (consoleWriteMode)
        {
            case ConsoleWriteMode.ConsoleNormal:
                console.DisplayTextLine(text);
                break;
            case ConsoleWriteMode.ConsoleTitle:
                console.DisplayTitleLine(text);
                break;
            case ConsoleWriteMode.ConsoleSuccess:
                console.DisplaySuccessLine(text);
                break;
            case ConsoleWriteMode.ConsoleInfo:
                console.DisplayInfoLine(text);
                break;
            case ConsoleWriteMode.ConsoleError:
                console.DisplayErrorLine(text);
                break;
        }
    }

    private void WriteLog(ILogger logger, string text, LogWriteMode logWriteMode)
    {
        switch (logWriteMode)
        {
            case LogWriteMode.LogInfo:
                logger.Information(text);
                break;
            case LogWriteMode.LogWarning:
                logger.Warning(text);
                break;
            case LogWriteMode.LogError:
                logger.Error(text);
                break;
        }
    }

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        WriteParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- Write");
        console.DisplayTextLine("      Write to screen and/or log file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. text to write [Text]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          write mode: /console or /log or /consoleAndLog (default: console)");
        console.DisplayTextLine("          console write mode: /consoleNormal or /consoleTitle or /consoleSuccess or /consoleInfo or /consoleError (default: consoleNormal)");
        console.DisplayTextLine("          logger write mode: /info or /warning or /error (default: info)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          Write \"My quoted text\"");
        console.DisplayTextLine("          Write \"My logger warning\" /wan");
    }
}