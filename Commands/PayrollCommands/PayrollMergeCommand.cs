using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Merge multiple payroll files into one file
/// </summary>
[Command("PayrollMerge")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrollMergeCommand : CommandBase<PayrollMergeParameters>
{
    /// <summary>Convert a JSON or YAML file</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrollMergeParameters parameters)
    {
        DisplayTitle(context.Console, "Payroll convert");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Source files     {parameters.SourceFilesMask}");
            context.Console.DisplayTextLine($"Target file      {parameters.TargetFileName}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }
        context.Console.DisplayNewLine();

        try
        {
            // file mask
            var fileInfo = new FileInfo(parameters.SourceFilesMask);
            var files = Directory.GetFiles(
                path: fileInfo.DirectoryName ?? Directory.GetCurrentDirectory(),
                searchPattern: fileInfo.Name,
                searchOption: parameters.DirectoryMode == DirectoryMode.Recursive ?
                    SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (!files.Any())
            {
                context.Console.DisplayErrorLine($"Missing merge source files {parameters.SourceFilesMask}.");
                return -3;
            }

            var target = new Exchange();
            var index = 0;
            foreach (var file in files)
            {
                var fileName = new FileInfo(file).FullName;
                context.Console.DisplayInfoLine($"Merging {fileName} ({index + 1} of {files.Length})");
                try
                {
                    // read source file
                    var source = await FileReader.ReadAsync<Exchange>(fileName);
                    if (source.Tenants == null)
                    {
                        // no exchange content, skip
                        continue;
                    }

                    // merge exchange
                    var merge = new ExchangeMerge(source);
                    await merge.MergeToAsync(target);
                }
                catch (Exception exception)
                {
                    context.Console.DisplayErrorLine($"Error merging file {fileName}: {exception.GetBaseMessage()}.");
                    return -3;
                }
                index++;
            }

            if (!target.Tenants.Any())
            {
                context.Console.DisplayErrorLine("Empty merge output.");
                return -4;
            }

            // save merged file
            await FileWriter.WriteAsync(target, parameters.TargetFileName);

            context.Console.DisplaySuccessLine($"Merged {files.Length} files into {new FileInfo(parameters.TargetFileName).FullName}");

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrollMergeParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrollMerge");
        console.DisplayTextLine("      Merge multiple payroll files into one file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. source files mask [SourceFilesMask]");
        console.DisplayTextLine("          2. target file name [TargetFileName]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          directory mode: scope with file mask /top or /recursive (default: top)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrollMerge *.yaml MergedPayroll.yaml");
        console.DisplayTextLine("          PayrollMerge *.json MergedPayroll.json /recursive");
    }
}