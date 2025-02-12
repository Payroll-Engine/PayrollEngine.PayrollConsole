using System;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("HttpPut")]
// ReSharper disable once UnusedType.Global
internal sealed class HttpPutCommand : HttpCommandBase<HttpPutParameters>
{
    /// <summary>Http PUT request</summary>
    protected override async Task<int> Execute(CommandContext context, HttpPutParameters parameters)
    {
        try
        {
            parameters.Url = GetRequestUrl(parameters.Url);
            DisplayTitle(context.Console, $"HTTP PUT - {parameters.Url}");

            var content = await GetFileContent(parameters.FileName);
            await context.HttpClient.PutAsync(parameters.Url, new StringContent(content));

            context.Console.DisplaySuccessLine("PUT request successfully");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            context.Console.DisplayErrorLine($"PUT request failed: {exception.GetBaseMessage()}");
            return (int)ProgramExitCode.HttpError;
        }
    }
    
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HttpPutParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- HttpPut");
        console.DisplayTextLine("      Execute http PUT request");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. End point url [Url]");
        console.DisplayTextLine("          2. Content file name (optional) [FileName]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          HttpPut tenants/1 MyTenant.json");
    }
}