using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("HttpGet")]
// ReSharper disable once UnusedType.Global
internal sealed class HttpGetCommand : HttpCommandBase<HttpGetParameters>
{
    /// <summary>Http GET request</summary>
    protected override async Task<int> Execute(CommandContext context, HttpGetParameters parameters)
    {
        try
        {
            parameters.Url = GetRequestUrl(parameters.Url);
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

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HttpGetParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- HttpGet");
        console.DisplayTextLine("      Execute http GET request");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. End point url [Url]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          HttpGet tenants");
        console.DisplayTextLine("          HttpGet tenants/1");
        console.DisplayTextLine("          HttpGet admin/application/stop /post");
    }
}