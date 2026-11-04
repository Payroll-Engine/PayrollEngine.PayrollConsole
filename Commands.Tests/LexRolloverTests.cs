using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class LexRolloverTests
{
    private static LexRolloverParameters Parse(params string[] arguments) =>
        LexRolloverParameters.ParserFrom(new CommandLineParser(["pecmd", "LexRollover", .. arguments]));

    private static readonly DateTime Date = new(2027, 3, 15);

    [Fact]
    public void Parameters_Defaults()
    {
        var parameters = Parse("Data.Tax.2026", "Date:2027-01-01", "Tenant:XX.Test");
        Assert.Equal("Data.Tax.2026", parameters.Satellite);
        Assert.Equal("2027-01-01", parameters.Date);
        Assert.Equal("XX.Test", parameters.Tenant);
        Assert.Null(parameters.Calendar);
        Assert.Null(parameters.Culture);
        Assert.Equal(LexRolloverMode.Write, parameters.RolloverMode);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void Parameters_CalendarCultureAndDryRun()
    {
        var parameters = Parse("Data.Tax.2026", "Date:2027-07-01", "Tenant:XX.Test", "Calendar:FiscalYear", "Culture:en-US", "/dryrun");
        Assert.Equal("FiscalYear", parameters.Calendar);
        Assert.Equal("en-US", parameters.Culture);
        Assert.Equal(LexRolloverMode.DryRun, parameters.RolloverMode);
        Assert.Contains(typeof(LexRolloverMode), parameters.Toggles);
    }

    [Theory]
    [InlineData("Missing satellite", "Date:2027-01-01", "Tenant:XX.Test")]
    [InlineData("Missing Date", "Data.Tax.2026", "Tenant:XX.Test")]
    [InlineData("Invalid Date", "Data.Tax.2026", "Date:01.01.2027", "Tenant:XX.Test")]
    [InlineData("Missing Tenant", "Data.Tax.2026", "Date:2027-01-01")]
    public void Parameters_AreValidated(string expected, params string[] arguments) =>
        Assert.StartsWith(expected, Parse(arguments).Test());

    [Fact]
    public async Task Cycle_IsQueriedForTheTenantAndDate()
    {
        string queriedTenant = null;
        (int Id, DateTime Moment)? queriedCycle = null;

        var cycle = await LexRolloverCycle.GetAsync("XX.Test", Date,
            identifier =>
            {
                queriedTenant = identifier;
                return Task.FromResult<int?>(7);
            },
            (id, moment) =>
            {
                queriedCycle = (id, moment);
                return Task.FromResult(new DatePeriod(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31, 23, 59, 59)));
            });

        Assert.Equal("XX.Test", queriedTenant);
        Assert.Equal((7, Date), queriedCycle);
        Assert.Equal(new DateTime(2027, 1, 1), cycle.Start);
    }

    [Fact]
    public async Task UnknownTenant_IsAnError()
    {
        var exception = await Assert.ThrowsAsync<PayrollException>(() =>
            LexRolloverCycle.GetAsync("XX.Unknown", Date,
                _ => Task.FromResult<int?>(null),
                (_, _) => throw new InvalidOperationException("no cycle query expected")));
        Assert.Contains("Unknown tenant", exception.Message);
    }

    [Fact]
    public async Task MissingCycle_IsAnError()
    {
        var exception = await Assert.ThrowsAsync<PayrollException>(() =>
            LexRolloverCycle.GetAsync("XX.Test", Date,
                _ => Task.FromResult<int?>(7),
                (_, _) => Task.FromResult<DatePeriod>(null)));
        Assert.Contains("no cycle", exception.Message);
    }

    [Fact]
    public async Task CycleWithoutTheDate_IsAnError()
    {
        await Assert.ThrowsAsync<PayrollException>(() =>
            LexRolloverCycle.GetAsync("XX.Test", Date,
                _ => Task.FromResult<int?>(7),
                (_, _) => Task.FromResult(new DatePeriod(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)))));
    }
}
