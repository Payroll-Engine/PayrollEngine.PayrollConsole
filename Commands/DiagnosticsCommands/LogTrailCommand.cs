using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PayrollEngine.Client.Command;
using Task = System.Threading.Tasks.Task;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.DiagnosticsCommands;

/// <summary>
/// LOg tail command
/// </summary>
[Command("LogTrail")]
// ReSharper disable once UnusedType.Global
internal sealed class LogTrailCommand : CommandBase<LogTrailParameters>
{
    /// <summary>Start log trail</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, LogTrailParameters parameters)
    {
        DisplayTitle(context.Console, "Log trail");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Interval         {parameters.Interval} seconds");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();
        context.Console.DisplayTextLine("Press key <X> or <Ctrl-C> to terminate...");

        // tenant
        var tenant = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.Tenant);
        if (tenant == null)
        {
            context.Console.DisplayErrorLine($"Unknown tenant {parameters.Tenant}");
            return (int)ProgramExitCode.GenericError;
        }

        var monitor = new ResourcesMonitor<Client.Model.Log, TenantServiceContext, Query>(
            new LogService(context.HttpClient), new(tenant.Id), WriteLogs)
        {
            Interval = TimeSpan.FromSeconds(parameters.Interval)
        };

        // log request until abort
        using var cancellationTokenSource = new CancellationTokenSource();
        // ReSharper disable once MethodSupportsCancellation
        var keyBoardTask = Task.Run(() =>
        {
            bool cancel;
            var osx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            if (osx)
            {
                // no read-key support on macOS
                var key = Console.Read();
                cancel = key == 'X' || key == 'x';
            }
            else
            {
                // listening to key press to cancel
                var key = Console.ReadKey();
                cancel = key.Key == ConsoleKey.X ||
                         key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control;
            }
            if (cancel)
            {
                // cancel log
                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            }
        });

        try
        {
            // start the log monitoring
            await monitor.StartAsync(cancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Task was cancelled");
        }
        await keyBoardTask;

        return (int)ProgramExitCode.Ok;
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LogTrailParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LogTrail");
        console.DisplayTextLine("      Trace the payroll log");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. Query interval in seconds (default: 5, minimum: 1) [Interval]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LogTrail MyTenantName");
        console.DisplayTextLine("          LogTrail MyTenantName 1");
    }

    private void WriteLogs(ICollection<Client.Model.Log> logs)
    {
        foreach (var log in logs)
        {
            var message = new StringBuilder();
            message.Append($"{log.Message} [user={log.User}");
            if (!string.IsNullOrWhiteSpace(log.Error))
            {
                message.Append($", error={log.Error}");
            }
            if (!string.IsNullOrWhiteSpace(log.Comment))
            {
                message.Append($", comment={log.Comment}");
            }
            message.Append($", source={log.OwnerType} {log.Owner}]");
            Log.Write(log.Level, message.ToString());
        }
    }
}