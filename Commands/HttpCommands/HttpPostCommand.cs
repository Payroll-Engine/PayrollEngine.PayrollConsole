using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.HttpCommands;

/// <summary>
/// Http post command
/// </summary>
[Command("HttpPost")]
// ReSharper disable once UnusedType.Global
internal sealed class HttpPostCommand : HttpCommandBase<HttpPostParameters>
{
    /// <summary>Http POST request</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, HttpPostParameters parameters)
    {
        try
        {
            parameters.Url = GetRequestUrl(parameters.Url);
            DisplayTitle(context.Console, $"HTTP POST - {parameters.Url}");

            var content = await GetFileContent(parameters.FileName);
            using var response = await context.HttpClient.PostAsync(parameters.Url, new(content));

            context.Console.DisplaySuccessLine($"POST request successfully ({response.StatusCode})");
            await DisplayResponseContent(context.Console, response);
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            context.Console.DisplayErrorLine($"POST request failed: {exception.GetBaseMessage()}");
            return (int)ProgramExitCode.HttpError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HttpPostParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- HttpPost");
        console.DisplayTextLine("      Execute http POST request");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. End point url [Url]");
        console.DisplayTextLine("          2. Content file name (optional) [FileName]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          HttpPost tenants/1 MyTenant.json");
        console.DisplayTextLine("          HttpPost admin/application/stop");
    }
}