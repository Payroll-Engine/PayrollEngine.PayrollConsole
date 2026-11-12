using System;
using System.Threading.Tasks;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Cycle of the payroll calendar that contains the rollover date: converts the calendar period of the
/// backend into the cycle of the Lex library and reports its error as <see cref="PayrollException"/>
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
    internal static async Task<RolloverCycle> GetAsync(string tenant, DateTime date,
        Func<string, Task<int?>> getTenantId, Func<int, DateTime, Task<DatePeriod>> getCycle)
    {
        var result = await RolloverCycle.GetAsync(tenant, date, getTenantId, async (tenantId, moment) =>
        {
            var period = await getCycle(tenantId, moment);
            return period == null ? null : new RolloverCycle { Start = period.Start, End = period.End };
        });
        return result.Error != null ? throw new PayrollException(result.Error) : result.Cycle;
    }
}
