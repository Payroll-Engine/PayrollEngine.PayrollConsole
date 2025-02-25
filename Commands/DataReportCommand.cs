using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PayrollEngine.IO;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Data report command
/// </summary>
[Command("DataReport")]
// ReSharper disable once UnusedType.Global
internal sealed class DataReportCommand : CommandBase<DataReportParameters>
{
    /// <summary>Build report</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, DataReportParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.OutputFile))
        {
            throw new PayrollException("Missing output file.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new PayrollException("Missing tenant.");
        }
        if (string.IsNullOrWhiteSpace(parameters.User))
        {
            throw new PayrollException("Missing user.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Regulation))
        {
            throw new PayrollException("Missing regulation.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Report))
        {
            throw new PayrollException("Missing report.");
        }

        DisplayTitle(context.Console, $"Data report - {parameters.Report}");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Output file      {parameters.OutputFile}");
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"User             {parameters.User}");
            context.Console.DisplayTextLine($"Regulation       {parameters.Regulation}");
            context.Console.DisplayTextLine($"Report           {parameters.Report}");
            context.Console.DisplayTextLine($"Culture          {parameters.Culture}");
            context.Console.DisplayTextLine($"Post action      {parameters.PostAction}");
            if (!string.IsNullOrWhiteSpace(parameters.ParametersFile))
            {
                context.Console.DisplayTextLine($"Parameter file   {parameters.ParametersFile}");
            }
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }
        context.Console.DisplayNewLine();

        // stopwatch
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        context.Console.DisplayText("Building data report...");

        // tenant
        var tenant = await new TenantService(context.HttpClient)
            .GetAsync<Tenant>(new(), parameters.Tenant);
        if (tenant == null)
        {
            throw new PayrollException($"Invalid tenant {parameters.Tenant}.");
        }
        // user
        var user = await new UserService(context.HttpClient)
            .GetAsync<User>(new(tenant.Id), parameters.User);
        if (user == null)
        {
            throw new PayrollException($"Invalid user {parameters.User}.");
        }
        // regulation
        var regulation = await new RegulationService(context.HttpClient)
            .GetAsync<Regulation>(new(tenant.Id), parameters.Regulation);
        if (regulation == null)
        {
            throw new PayrollException($"Invalid regulation {parameters.Regulation}.");
        }
        // report
        var report = await new ReportService(context.HttpClient)
            .GetAsync<Report>(new(tenant.Id, regulation.Id), parameters.Report);
        if (report == null)
        {
            throw new PayrollException($"Invalid report {parameters.Report}.");
        }

        // execute report
        try
        {
            // report parameter
            Dictionary<string, string> reportParameters = null;
            if (!string.IsNullOrWhiteSpace(parameters.ParametersFile) && File.Exists(parameters.ParametersFile))
            {
                reportParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    await File.ReadAllTextAsync(parameters.ParametersFile));
            }

            // report request
            var reportRequest = new ReportRequest
            {
                UserId = user.Id,
                Culture = parameters.Culture,
                Parameters = reportParameters
            };

            // report response
            var reportResponse = await new ReportService(context.HttpClient).ExecuteReportAsync(
                    new(tenant.Id, regulation.Id), report.Id, reportRequest);

            // result data set
            if (reportResponse.Result == null)
            {
                context.Console.DisplayErrorLine($"Empty result in report {parameters.Report}.");
            }
            else
            {
                var fileInfo = new FileInfo(parameters.OutputFile);
                var fileName = $"{parameters.OutputFile.Replace(fileInfo.Extension, string.Empty)}" +
                                $"_{FileTool.CurrentTimeStamp()}{fileInfo.Extension}";

                // cleanup
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                // serialize data set
                var dataSetJson = JsonSerializer.Serialize(reportResponse.Result,
                     new JsonSerializerOptions
                     {
                         // camel case
                         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                         // ignore null
                         DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                         // unicode (see https://stackoverflow.com/a/58003397/15659039)
                         Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                         // formatted
                         WriteIndented = true
                     });
                await File.WriteAllTextAsync(fileName, dataSetJson);

                stopwatch.Stop();
                context.Console.DisplayTextLine($"done in {stopwatch.ElapsedMilliseconds} ms");
                context.Console.DisplayNewLine();
                context.Console.DisplaySuccessLine($"Report {parameters.Report} to data file {new FileInfo(fileName).FullName}");
                context.Console.DisplayNewLine();

                // post action
                switch (parameters.PostAction)
                {
                    case ReportPostAction.ShellOpen:
                        Process.Start("cmd.exe", $"/C start {fileName}");
                        break;
                    default:
                    case ReportPostAction.NoAction:
                        break;
                }
            }
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (context.Console.DisplayLevel == DisplayLevel.Silent)
            {
                context.Console.WriteErrorLine($"Report error: {exception.GetBaseMessage().Trim('"')}");
            }
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        DataReportParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- DataReport");
        console.DisplayTextLine("      Report data to json file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. output file [OutputFile]");
        console.DisplayTextLine("          2. tenant identifier [Tenant]");
        console.DisplayTextLine("          3. user identifier [User]");
        console.DisplayTextLine("          4. regulation name [Regulation]");
        console.DisplayTextLine("          5. report name [Report]");
        console.DisplayTextLine("          6. report parameter file with a json string/string dictionary (optional) [ParametersFile]");
        console.DisplayTextLine("          7. report culture [Culture]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          language (default: english");
        console.DisplayTextLine("          post action: /noaction or /shellopen (default: noaction)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport /german");
        console.DisplayTextLine("          DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport MyParameters.json /german");
    }
}