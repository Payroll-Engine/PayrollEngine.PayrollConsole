using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Authentication;
using PayrollEngine.Client;
using PayrollEngine.Serilog;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands;

namespace PayrollEngine.PayrollConsole;

sealed class Program : ConsoleProgram<Program>
{
    private static ILogger Logger { get; } = Log.Logger;
    private static ICommandConsole Console { get; } = new CommandConsole();

    private CommandManager CommandManager { get; set; }

    /// <inheritdoc />
    protected override bool LogLifecycle => false;

    /// <summary>Mandatory argument: command</summary>
    protected override int MandatoryArgumentCount => 1;

    private Program()
    {
    }

    /// <inheritdoc />
    protected override bool UseHttpClient()
    {
        var command = CommandManager.GetCommandLineCommand();
        return command == null || command.BackendCommand;
    }

    /// <inheritdoc />
    protected override Task SetupLogAsync()
    {
        // logger setup
        Configuration.Configuration.SetupSerilog();

        // command manager
        CommandManager = new CommandManager(Console, Logger);
        CommandProvider.RegisterCommands(CommandManager, Logger);

        return base.SetupLogAsync();
    }

    /// <inheritdoc />
    protected override bool FullErrorLog()
    {
        var setting = Configuration.Get("FullErrorLog");
        if (bool.TryParse(setting, out var fullErrorLog))
        {
            return fullErrorLog;
        }
        return base.FullErrorLog();
    }

    /// <summary>Override the default implementation, allowing access to https</summary>
    /// <returns>The http client handler</returns>
    protected override async Task<HttpClientHandler> GetHttpClientHandlerAsync() =>
        // TODO http client handler by configuration
        await Task.FromResult(new HttpClientHandler
        {
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });

    /// <inheritdoc />
    protected override async Task RunAsync()
    {
        try
        {
            // execution command
            var exitCode = await CommandManager.ExecuteAsync(HttpClient);
            ProgramEnd(exitCode);
        }
        catch (Exception exception)
        {
            Log.Critical(exception, exception.GetBaseException().Message);
        }
    }

    /// <summary>Show the help screen</summary>
    protected override async Task HelpAsync()
    {
        var context = new CommandContext(
            commandManager: CommandManager,
            console: Console);
        var helpCommand = new HelpCommand();
        await helpCommand.ExecuteAsync(context, new HelpParameters());
        await base.HelpAsync();
    }

    private static void ProgramEnd(int exitCode, bool failedCommand = false)
    {
        // enforced wait
        SetExitCode(exitCode);

        if (Console.WaitMode == WaitMode.Wait ||
            // system error
            (exitCode != 0 && Console.WaitMode != WaitMode.NoWait) ||
            // failed command
            (Console.WaitMode == WaitMode.WaitError && failedCommand))
        {
            PressAnyKey();
        }
        else if (exitCode != 0)
        {
            Console.DisplayInfoLine($"Program exit code #{ExitCode}.");
        }
        Console.DisplayNewLine();
    }

    protected override Task NotifyGlobalErrorAsync(Exception exception)
    {
        SetExitCode((int)ProgramExitCode.GenericError);
        return base.NotifyGlobalErrorAsync(exception);
    }

    protected override Task NotifyConnectionErrorAsync()
    {
        SetExitCode((int)ProgramExitCode.ConnectionError);
        return base.NotifyConnectionErrorAsync();
    }

    protected override Task<string> GetProgramCultureAsync()
    {
        var culture = Configuration.Get("StartupCulture");
        if (!string.IsNullOrWhiteSpace(culture))
        {
            return Task.FromResult(culture);
        }
        return base.GetProgramCultureAsync();
    }

    private static void SetExitCode(int exitCode)
    {
        ExitCode = exitCode;
    }

    static async Task Main()
    {
        using var program = new Program();
        await program.ExecuteAsync();
    }
}