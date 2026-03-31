using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.HttpCommands;

/// <summary>
/// Http GET command
/// </summary>
[Command("HttpGet")]
// ReSharper disable once UnusedType.Global
internal sealed class HttpGetCommand : HttpCommandBase<HttpGetParameters>
{
    /// <summary>Http GET request</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, HttpGetParameters parameters)
    {
        try
        {
            parameters.Url = await ResolveUrlAsync(context, parameters.Url);
            DisplayTitle(context.Console, $"HTTP GET - {parameters.Url}");

            using var response = await context.HttpClient.GetAsync(parameters.Url);

            context.Console.DisplaySuccessLine($"GET request successfully ({response.StatusCode})");
            await DisplayResponseContent(context.Console, response);
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            context.Console.DisplayErrorLine($"GET request failed: {exception.GetBaseMessage()}");
            return (int)ProgramExitCode.HttpError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HttpGetParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- HttpGet");
        console.DisplayTextLine("      Execute http GET request");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. End point url [Url]");
        console.DisplayTextLine("      URL placeholders (resolved left-to-right):");
        console.DisplayTextLine("          {tenant:Identifier}   - numeric tenant id");
        console.DisplayTextLine("          {user:Identifier}     - numeric user id");
        console.DisplayTextLine("          {division:Name}       - numeric division id");
        console.DisplayTextLine("          {employee:Identifier} - numeric employee id");
        console.DisplayTextLine("          {regulation:Name}     - numeric regulation id");
        console.DisplayTextLine("          {payroll:Name}        - numeric payroll id");
        console.DisplayTextLine("          {payrun:Name}         - numeric payrun id");
        console.DisplayTextLine("          {payrunJob:Name}      - numeric payrun job id");
        console.DisplayTextLine("      Note: tenant-scoped placeholders require {tenant:X} earlier in the URL");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          HttpGet tenants");
        console.DisplayTextLine("          HttpGet tenants/1");
        console.DisplayTextLine("          HttpGet tenants/{tenant:MyTenant}/payrollresults/sets");
        console.DisplayTextLine("          HttpGet tenants/{tenant:MyTenant}/employees/{employee:anna@foo.com}/casevalues");
    }
}