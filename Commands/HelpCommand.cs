using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("Help")]
// ReSharper disable once UnusedType.Global
public sealed class HelpCommand : CommandBase<HelpParameters>
{
    protected override Task<int> Execute(CommandContext context, HelpParameters parameters)
    {
        Show(context.Console, context.CommandManager, parameters.Command);
        return Task.FromResult((int)ProgramExitCode.Ok);
    }

    /// <summary>
    /// Show the application help
    /// </summary>
    public override void ShowHelp(ICommandConsole console) { }

    /// <summary>
    /// Show the application help
    /// </summary>
    private void Show(ICommandConsole console, CommandManager commandManager, string command = null)
    {
        console.DisplayNewLine();
        console.DisplayTextLine("Usage:");
        console.DisplayTextLine("  PayrollConsole Command [Arg1]...[argX] [/Toggle]");
        console.DisplayTextLine("  PayrollConsole Command [Arg1Name:Arg1Value]...[ArgXName:ArgXValue] [/Toggle]");
        console.DisplayTextLine("  PayrollConsole CommandFile [Arg1Name:Arg1Value]...[ArgXName:ArgXValue] [/Toggle]");
        console.DisplayNewLine();

        // registered commands
        if (command != null)
        {
            commandManager.GetCommand(command)?.ShowHelp(console);
        }
        else
        {
            commandManager.GetCommands().ForEach(x => x.ShowHelp(console));
        }

        // help command
        if (command == null || "help".Equals(command, StringComparison.InvariantCultureIgnoreCase))
        {
            console.DisplayTitleLine("- Help");
            console.DisplayTextLine("      Show command help");
            console.DisplayTextLine("      Arguments:");
            console.DisplayTextLine("          1. Command name");
            console.DisplayTextLine("      Examples:");
            console.DisplayTextLine("          Help");
            console.DisplayTextLine("          Help PayrollImport");
        }

        // command file
        console.DisplayNewLine();
        console.DisplayTitleLine("- Payroll Engine command file (.pecmd)");
        console.DisplayTextLine("      Place named parameters with the $Name$ placeholder");
        console.DisplayTextLine("      Example command file test.pecmd:");
        console.DisplayTextLine("        PayrunEmployeeTest fileMask:*.et.json owner:$owner$ /wait");
        console.DisplayTextLine("      Usage:");
        // ReSharper disable once StringLiteralTypo
        console.DisplayTextLine("        test.pecmd onwer:Customer1");
        console.DisplayNewLine();

        // command toggles
        console.DisplayNewLine();
        console.DisplayTitleLine("- Global toggles");
        console.DisplayTextLine("      Display level   Command information level (command file preset)");
        console.DisplayTextLine("                      /full (default)");
        console.DisplayTextLine("                      /compact");
        console.DisplayTextLine("                      /silent");
        console.DisplayTextLine("      Error mode      Show failed tests and errors (command file preset)");
        console.DisplayTextLine("                      /errors (default)");
        console.DisplayTextLine("                      /noerrors");
        console.DisplayTextLine("      Wait mode       Wait at the program end (command file final wait mode)");
        console.DisplayTextLine("                      /waiterror (default)");
        console.DisplayTextLine("                      /wait");
        console.DisplayTextLine("                      /nowait");
        console.DisplayTextLine("      Path mode       Path change mode for command files");
        console.DisplayTextLine("                      /changepath (default)");
        console.DisplayTextLine("                      /keeppath");

        // program exit codes
        console.DisplayNewLine();
        console.DisplayTitleLine("- Exit codes");
        foreach (var value in Enum.GetValues<ProgramExitCode>())
        {
            console.DisplayTextLine($"      {(int)value}    {Enum.GetName(typeof(ProgramExitCode), value)}");
        }
    }

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HelpParameters.ParserFrom(parser);
}