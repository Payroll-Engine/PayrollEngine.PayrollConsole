using System;
using System.Threading.Tasks;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Cycle of the payroll calendar that contains the rollover date
/// </summary>
internal static class LexRolloverCycle
{
    /// <summary>
    /// Query the cycle from the backend
    /// </summary>
    /// <param name="tenant">Tenant identifier</param>
    /// <param name="date">Date within the new cycle</param>
    /// <param name="getTenantId">Returns the id of a tenant identifier, null when unknown</param>
    /// <param name="getCycle">Returns the calendar cycle of the tenant id that contains the date</param>
    /// <exception cref="PayrollException">Unknown tenant or no cycle</exception>
    internal static async Task<DatePeriod> GetAsync(string tenant, DateTime date,
        Func<string, Task<int?>> getTenantId, Func<int, DateTime, Task<DatePeriod>> getCycle)
    {
        var tenantId = await getTenantId(tenant);
        if (tenantId == null)
        {
            throw new PayrollException($"Unknown tenant {tenant}");
        }
        var cycle = await getCycle(tenantId.Value, date);
        if (cycle == null || cycle.Start > date || cycle.End < date)
        {
            throw new PayrollException($"The calendar of tenant {tenant} returns no cycle for {date:yyyy-MM-dd}");
        }
        return cycle;
    }
}
