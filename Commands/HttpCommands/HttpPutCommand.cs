using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.HttpCommands;

/// <summary>
/// Http put command
/// </summary>
[Command("HttpPut")]
// ReSharper disable once UnusedType.Global
internal sealed class HttpPutCommand : HttpCommandBase<HttpPutParameters>
{
    /// <summary>Http PUT request</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, HttpPutParameters parameters)
    {
        try
        {
            parameters.Url = await ResolveUrlAsync(context, parameters.Url);
            DisplayTitle(context.Console, $"HTTP PUT - {parameters.Url}");

            var content = await GetFileContent(parameters.FileName);
            var httpContent = new StringContent(content);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            await context.HttpClient.PutAsync(parameters.Url, httpContent);

            context.Console.DisplaySuccessLine("PUT request successfully");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            context.Console.DisplayErrorLine($"PUT request failed: {exception.GetBaseMessage()}");
            return (int)ProgramExitCode.HttpError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HttpPutParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- HttpPut");
        console.DisplayTextLine("      Execute http PUT request");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. End point url [Url]");
        console.DisplayTextLine("          2. Content file name (optional) [FileName]");
        console.DisplayTextLine("      URL placeholders: {tenant:X}, {user:X}, {division:X}, {employee:X}, {regulation:X}, {payroll:X}, {payrun:X}, {payrunJob:X}");
        console.DisplayTextLine("      Note: tenant-scoped placeholders require {tenant:X} earlier in the URL");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          HttpPut tenants/1 MyTenant.json");
        console.DisplayTextLine("          HttpPut tenants/{tenant:MyTenant}/payrolls/1 Payroll.json");
    }
}