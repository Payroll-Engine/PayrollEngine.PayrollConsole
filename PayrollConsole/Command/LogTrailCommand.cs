using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class LogTrailCommand : HttpCommandBase
{
    internal LogTrailCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> StartLogTrailAsync(string tenantIdentifier, int interval)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }

        DisplayTitle("Trail payroll log");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Interval         {interval} seconds");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");

        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplayTextLine("Press key <X> or <Ctrl-C> to terminate...");

        // tenant
        var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
        if (tenant == null)
        {
            ConsoleTool.DisplayErrorLine($"Unknown tenant {tenantIdentifier}");
            return ProgramExitCode.GenericError;
        }

        var monitor = new ResourcesMonitor<Client.Model.Log, TenantServiceContext, Query>(
            new LogService(HttpClient), new(tenant.Id), WriteLogs)
        {
            Interval = TimeSpan.FromSeconds(interval)
        };

        // log request until abort
        using var cancellationTokenSource = new CancellationTokenSource();
        // ReSharper disable once MethodSupportsCancellation
        var keyBoardTask = Task.Run(() =>
        {
            // listening to key press to cancel
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.X ||
                key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
            {
                // cancel log
                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            }
        });

        try
        {
            // start the log monitoring
            await monitor.Start(cancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Task was cancelled");
        }
        await keyBoardTask;

        return ProgramExitCode.Ok;
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- LogTrail");
        ConsoleTool.DisplayTextLine("      Trail the payroll log");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name");
        ConsoleTool.DisplayTextLine("          2. Query interval in seconds (default: 5, minimum: 1)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          LogTrail MyTenantName");
        ConsoleTool.DisplayTextLine("          LogTrail MyTenantName 1");
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