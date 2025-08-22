using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun statistics command
/// </summary>
[Command("PayrunStatistics")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunStatisticsCommand : CommandBase<PayrunStatisticsParameters>
{
    /// <summary>Show the payrun statistics</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrunStatisticsParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new ArgumentException("Missing tenant identifier.", nameof(parameters.Tenant));
        }

        parameters.CreatedSinceMinutes = Math.Max(1, parameters.CreatedSinceMinutes);
        var createdSince = Date.Now.Subtract(TimeSpan.FromMinutes(parameters.CreatedSinceMinutes));

        // display
        DisplayTitle(context.Console, "Payrun statistics");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
            context.Console.DisplayTextLine($"Executed since   {parameters.CreatedSinceMinutes} minutes");
        }

        context.Console.DisplayNewLine();

        try
        {
            // tenant
            var tenantService = new TenantService(context.HttpClient);
            var tenant = await tenantService.GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                throw new PayrollException($"Invalid tenant {parameters.Tenant}.");
            }

            // payrun job
            var payrunJobService = new PayrunJobService(context.HttpClient);
            context.Console.DisplayTextLine($"Payrun statistics since {createdSince.ToShortTimeString()} " +
                                        $"(local {createdSince.ToLocalTime().ToShortTimeString()})");
            context.Console.DisplayNewLine();

            // query all tenant jobs
            var query = new Query
            {
                Filter = $"Created gt '{createdSince.ToUtcString(CultureInfo.CurrentCulture)}'"
            };
            var payrunJobs = await payrunJobService.QueryAsync<PayrunJob>(new(tenant.Id), query);
            if (payrunJobs.Any())
            {
                var durationJobs = new List<Tuple<TimeSpan, PayrunJob>>();
                foreach (var payrunJob in payrunJobs)
                {
                    if (!payrunJob.JobEnd.HasValue)
                    {
                        continue;
                    }
                    durationJobs.Add(new(payrunJob.JobEnd.Value.Subtract(payrunJob.JobStart), payrunJob));
                }

                // overall
                context.Console.DisplayTextLine($"Total payruns: {durationJobs.Count}");
                context.Console.DisplayTextLine($"Average: {durationJobs.Select(x => x.Item1.TotalMilliseconds).Average():#0} ms");
                context.Console.DisplayNewLine();

                // top ten
                context.Console.DisplayTextLine("Top 10 most expensive payruns");
                var jobsByDuration = durationJobs.OrderByDescending(x => x.Item1).Take(10).ToList();
                var index = 1;
                foreach (var item in jobsByDuration)
                {
                    var job = item.Item2;
                    var owner = string.Empty;
                    if (!string.IsNullOrWhiteSpace(job.Owner))
                    {
                        owner = $"[{job.Owner}] ";
                    }
                    context.Console.DisplayTextLine($"{index,2:##}. {owner}{job.Name}: {job.Created:HH:mm:ss}: {item.Item1.TotalMilliseconds,6:#####0} ms");
                    index++;
                }
                context.Console.DisplayNewLine();
                context.Console.DisplayTextLine($"Average: {jobsByDuration.Select(x => x.Item1.TotalMilliseconds).Average():#0} ms");
            }
            else
            {
                context.Console.DisplayInfoLine("No payruns executed");
            }

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunStatisticsParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunStatistics");
        console.DisplayTextLine("      Display payrun statistics");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. Query interval in minutes (default: 30) [CreatedSinceMinutes]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunStatistics MyTenantName");
        console.DisplayTextLine("          PayrunStatistics MyTenantName 60");
    }
}