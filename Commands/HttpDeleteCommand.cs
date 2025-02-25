using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Http delete command
/// </summary>
[Command("HttpDelete")]
// ReSharper disable once UnusedType.Global
internal sealed class HttpDeleteCommand : HttpCommandBase<HttpDeleteParameters>
{
    /// <summary>Http DELETE request</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, HttpDeleteParameters parameters)
    {
        try
        {
            parameters.Url = GetRequestUrl(parameters.Url);
            DisplayTitle(context.Console, $"HTTP DELETE - {parameters.Url}");

            await context.HttpClient.DeleteAsync(parameters.Url);

            context.Console.DisplaySuccessLine("DELETE request successfully");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            context.Console.DisplayErrorLine($"DELETE request failed: {exception.GetBaseMessage()}");
            return (int)ProgramExitCode.HttpError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        HttpDeleteParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- HttpDelete");
        console.DisplayTextLine("      Execute http DELETE request");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. End point url [Url]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          HttpDelete tenants/1");
    }
}