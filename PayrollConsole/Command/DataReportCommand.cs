using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.IO;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class DataReportCommand : HttpCommandBase
{
    internal DataReportCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> ReportAsync(DataReportCommandSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        if (string.IsNullOrWhiteSpace(settings.OutputFile))
        {
            throw new PayrollException("Missing output file");
        }
        if (string.IsNullOrWhiteSpace(settings.TenantIdentifier))
        {
            throw new PayrollException("Missing tenant");
        }
        if (string.IsNullOrWhiteSpace(settings.UserIdentifier))
        {
            throw new PayrollException("Missing user");
        }
        if (string.IsNullOrWhiteSpace(settings.RegulationName))
        {
            throw new PayrollException("Missing regulation");
        }
        if (string.IsNullOrWhiteSpace(settings.ReportName))
        {
            throw new PayrollException("Missing report");
        }

        DisplayTitle("Data report");
        ConsoleTool.DisplayTextLine($"Output file      {settings.OutputFile}");
        ConsoleTool.DisplayTextLine($"Tenant           {settings.TenantIdentifier}");
        ConsoleTool.DisplayTextLine($"User             {settings.UserIdentifier}");
        ConsoleTool.DisplayTextLine($"Regulation       {settings.RegulationName}");
        ConsoleTool.DisplayTextLine($"Report           {settings.ReportName}");
        ConsoleTool.DisplayTextLine($"Culture          {settings.Culture}");
        ConsoleTool.DisplayTextLine($"Post action      {settings.PostAction}");
        if (!string.IsNullOrWhiteSpace(settings.ParameterFile))
        {
            ConsoleTool.DisplayTextLine($"Parameter file   {settings.ParameterFile}");
        }
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        // stopwatch
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        ConsoleTool.DisplayText("Building data report...");

        // tenant
        var tenant = await new TenantService(HttpClient)
            .GetAsync<Tenant>(new(), settings.TenantIdentifier);
        if (tenant == null)
        {
            throw new PayrollException($"Invalid tenant {settings.TenantIdentifier}");
        }
        // user
        var user = await new UserService(HttpClient)
            .GetAsync<User>(new(tenant.Id), settings.UserIdentifier);
        if (user == null)
        {
            throw new PayrollException($"Invalid user {settings.UserIdentifier}");
        }
        // regulation
        var regulation = await new RegulationService(HttpClient)
            .GetAsync<Regulation>(new(tenant.Id), settings.RegulationName);
        if (regulation == null)
        {
            throw new PayrollException($"Invalid regulation {settings.RegulationName}");
        }
        // report
        var report = await new ReportService(HttpClient)
            .GetAsync<Report>(new(tenant.Id, regulation.Id), settings.ReportName);
        if (report == null)
        {
            throw new PayrollException($"Invalid report {settings.ReportName}");
        }

        // execute report
        try
        {
            // report parameter
            Dictionary<string, string> parameters = null;
            if (!string.IsNullOrWhiteSpace(settings.ParameterFile) && File.Exists(settings.ParameterFile))
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    await File.ReadAllTextAsync(settings.ParameterFile));
            }

            // report request
            var reportRequest = new ReportRequest
            {
                UserId = user.Id,
                Culture = settings.Culture,
                Parameters = parameters
            };

            // report response
            var reportResponse = await new ReportService(HttpClient).ExecuteReportAsync(
                    new(tenant.Id, regulation.Id), report.Id, reportRequest);

            // result data set
            if (reportResponse.Result == null)
            {
                ConsoleTool.DisplayErrorLine($"Empty result in report {settings.ReportName}.");
            }
            else
            {
                var fileInfo = new FileInfo(settings.OutputFile);
                var fileName = $"{settings.OutputFile.Replace(fileInfo.Extension, string.Empty)}" +
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
                ConsoleTool.DisplayTextLine($"done in {stopwatch.ElapsedMilliseconds} ms");
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplaySuccessLine($"Report {settings.ReportName} to data file {new FileInfo(fileName).FullName}");
                ConsoleTool.DisplayNewLine();

                // post action
                switch (settings.PostAction)
                {
                    case ReportPostAction.ShellOpen:
                        Process.Start("cmd.exe", $"/C start {fileName}");
                        break;
                    default:
                    case ReportPostAction.NoAction:
                        break;
                }
            }
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (ConsoleTool.DisplayMode == ConsoleDisplayMode.Silent)
            {
                ConsoleTool.WriteErrorLine($"Report error: {exception.GetBaseMessage()}");
            }
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- DataReport");
        ConsoleTool.DisplayTextLine("      Report data to json file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. target file");
        ConsoleTool.DisplayTextLine("          2. tenant identifier");
        ConsoleTool.DisplayTextLine("          3. user identifier");
        ConsoleTool.DisplayTextLine("          4. regulation name");
        ConsoleTool.DisplayTextLine("          5. report name");
        ConsoleTool.DisplayTextLine("          6. report parameter file with a json string/string dictionary (optional)");
        ConsoleTool.DisplayTextLine("          7. report culture");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          language: default is english)");
        ConsoleTool.DisplayTextLine("          post action: /noaction or /shellopen (default: noaction)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport /german");
        ConsoleTool.DisplayTextLine("          DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport MyParameters.json /german");
    }
}