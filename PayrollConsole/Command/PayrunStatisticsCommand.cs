using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrunStatisticsCommand : HttpCommandBase
{
    internal PayrunStatisticsCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Show the payrun statistics
    /// </summary>
    /// <param name="tenantIdentifier">The tenant identifier</param>
    /// <param name="createdSinceMinutes">The job creation time since now in minutes</param>
    /// <returns>The program exit code</returns>
    internal async Task<ProgramExitCode> PayrunStatisticsAsync(string tenantIdentifier, int createdSinceMinutes)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new ArgumentException("Missing tenant identifier", nameof(tenantIdentifier));
        }

        createdSinceMinutes = Math.Max(1, createdSinceMinutes);
        var createdSince = Date.Now.Subtract(TimeSpan.FromMinutes(createdSinceMinutes));

        // display
        DisplayTitle("Payrun statistics");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayTextLine($"Executed since   {createdSinceMinutes} minutes");
        ConsoleTool.DisplayNewLine();

        try
        {
            // tenant
            var tenantService = new TenantService(HttpClient);
            var tenant = await tenantService.GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                throw new PayrollException($"Invalid tenant {tenantIdentifier}");
            }

            // payrun job
            var payrunJobService = new PayrunJobService(HttpClient);
            ConsoleTool.DisplayTextLine($"Payrun statistics since {createdSince.ToShortTimeString()} " +
                                        $"(local {createdSince.ToLocalTime().ToShortTimeString()})");
            ConsoleTool.DisplayNewLine();

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
                ConsoleTool.DisplayTextLine($"Total payruns: {durationJobs.Count}");
                ConsoleTool.DisplayTextLine($"Average: {durationJobs.Select(x => x.Item1.TotalMilliseconds).Average():#0} ms");
                ConsoleTool.DisplayNewLine();

                // top ten
                ConsoleTool.DisplayTextLine("Top 10 most expensive payruns");
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
                    ConsoleTool.DisplayTextLine($"{index,2:##}. {owner}{job.Name}: {job.Created:HH:mm:ss}: {item.Item1.TotalMilliseconds,6:#####0} ms");
                    index++;
                }
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplayTextLine($"Average: {jobsByDuration.Select(x => x.Item1.TotalMilliseconds).Average():#0} ms");
            }
            else
            {
                ConsoleTool.DisplayInfoLine("No payruns executed");
            }

            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- PayrunStatistics");
        ConsoleTool.DisplayTextLine("      Show payrun statistics");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name");
        ConsoleTool.DisplayTextLine("          2. Query interval in minutes (default: 30)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrunStatistics MyTenantName");
        ConsoleTool.DisplayTextLine("          PayrunStatistics MyTenantName 60");
    }
}