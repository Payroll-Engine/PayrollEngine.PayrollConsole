using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Prepares the data regulation satellite of the next cycle. The cycle is determined by the payroll
/// calendar of the backend; its start is the new validFrom and its year the new year tag.
/// All lex entries of the new satellite are open until verified again.
/// </summary>
[Command("LexRollover")]
// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal sealed class LexRolloverCommand : CommandBase<LexRolloverParameters>
{
    private const int MentionLinesPerFile = 3;
    private const int MaxRenamedLines = 10;

    /// <inheritdoc />
    public override bool BackendCommand => true;

    /// <inheritdoc />
    protected override async Task<int> Execute(CommandContext context, LexRolloverParameters parameters)
    {
        var console = context.Console;
        DisplayTitle(console, "Lex Rollover");
        var dryRun = parameters.RolloverMode == LexRolloverMode.DryRun;
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Satellite        {parameters.Satellite}");
            console.DisplayTextLine($"Date             {parameters.Date}");
            console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            console.DisplayTextLine($"Calendar         {parameters.Calendar ?? "(tenant calendar)"}");
            console.DisplayTextLine($"Culture          {parameters.Culture ?? "(tenant culture)"}");
            console.DisplayTextLine($"Rollover mode    {parameters.RolloverMode}");
        }
        console.DisplayNewLine();

        try
        {
            var date = DateTime.ParseExact(parameters.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DatePeriod cycle;
            try
            {
                var calendars = new CalendarService(context.HttpClient);
                cycle = await LexRolloverCycle.GetAsync(parameters.Tenant, date,
                    async identifier => (await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), identifier))?.Id,
                    (tenantId, moment) => calendars.GetCycleAsync(tenantId, parameters.Culture, parameters.Calendar, moment));
            }
            catch (PayrollException exception)
            {
                console.DisplayErrorLine($"  [FAIL] {exception.Message}");
                return (int)ProgramExitCode.InvalidInput;
            }
            console.DisplayTextLine($"Cycle            {cycle.Start:yyyy-MM-dd} - {cycle.End:yyyy-MM-dd}");

            var result = Rollover.Run(parameters.Satellite, cycle.Start.Date, dryRun);
            Display(console, result, dryRun);
            return result.Error != null ? (int)ProgramExitCode.InvalidInput : (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static void Display(ICommandConsole console, RolloverResult result, bool dryRun)
    {
        if (result.Error != null)
        {
            console.DisplayErrorLine($"  [FAIL] {result.SourceDirectory} - {result.Error}");
            return;
        }
        console.DisplaySuccessLine($"  [{(dryRun ? "DRY RUN" : "CREATED")}] {result.TargetDirectory} - validFrom {result.ValidFrom} " +
                                   $"(previous {result.PreviousValidFrom}), {result.Files.Count} files, {result.OpenEntries} open entries");
        foreach (var renamed in result.Renamed.Take(MaxRenamedLines))
        {
            console.DisplayTextLine($"         renamed: {renamed}");
        }
        if (result.Renamed.Count > MaxRenamedLines)
        {
            console.DisplayTextLine($"         renamed: ... {result.Renamed.Count - MaxRenamedLines} more");
        }
        foreach (var changed in result.Changed)
        {
            console.DisplayTextLine($"         changed: {changed}");
        }
        if (result.YearMentions.Count > 0)
        {
            var files = result.YearMentions.GroupBy(x => x.File).ToList();
            console.DisplayInfoLine($"  [REVIEW] {result.YearMentions.Count} line(s) in {files.Count} file(s) still mention the previous year:");
            foreach (var file in files)
            {
                console.DisplayTextLine($"         {file.Key} ({file.Count()})");
                foreach (var mention in file.Take(MentionLinesPerFile))
                {
                    console.DisplayTextLine($"           {mention.Line}: {mention.Text}");
                }
            }
        }
        foreach (var note in result.Notes)
        {
            console.DisplayInfoLine($"  [NOTE] {note}");
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LexRolloverParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LexRollover");
        console.DisplayTextLine("      Prepare the data regulation satellite of the next cycle (backend command).");
        console.DisplayTextLine("      The cycle containing the date is read from the payroll calendar of the tenant:");
        console.DisplayTextLine("      cycle start = new validFrom, its year = new year tag of folder, file and version.");
        console.DisplayTextLine("      The satellite folder is copied without bin, obj, nupkgs and snapshot.json; names with");
        console.DisplayTextLine("      the year tag are renamed and their references updated, validFrom and a year-based");
        console.DisplayTextLine("      version are set. All lex entries are open (no verification) until verified again;");
        console.DisplayTextLine("      LexCheck reports open entries, LexApply skips them. Values, descriptions and lookup");
        console.DisplayTextLine("      keys are not changed; lines still mentioning the previous year are listed for review.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. satellite directory of the previous cycle [Satellite]");
        console.DisplayTextLine("      Named arguments:");
        console.DisplayTextLine("          Date:<yyyy-MM-dd>    date within the new cycle (required)");
        console.DisplayTextLine("          Tenant:<identifier>  tenant of the payroll calendar (required)");
        console.DisplayTextLine("          Calendar:<name>      calendar name (default: tenant calendar)");
        console.DisplayTextLine("          Culture:<name>       culture name (default: tenant culture)");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          rollover mode: /write (default) or /dryrun (show the planned satellite only)");
        console.DisplayTextLine("      Exit codes:");
        console.DisplayTextLine("          0  satellite created (or planned with /dryrun)");
        console.DisplayTextLine("          2  backend not reachable, nothing written");
        console.DisplayTextLine("          6  unknown tenant, no cycle, target exists or satellite invalid, nothing written");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LexRollover Data.Tax.2026 Date:2027-01-01 Tenant:MyTenant /dryrun");
        console.DisplayTextLine("          LexRollover Data.Tax.2026 Date:2027-07-01 Tenant:MyTenant Calendar:FiscalYear");
    }
}
