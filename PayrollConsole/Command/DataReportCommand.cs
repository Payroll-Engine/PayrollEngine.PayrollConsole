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

    internal async Task<ProgramExitCode> ReportAsync(string outputFile, string tenantIdentifier,
        string userIdentifier, string regulationName, string reportName, Language language,
        string parameterFile = null)
    {
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            throw new PayrollException("Missing output file");
        }
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant");
        }
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            throw new PayrollException("Missing user");
        }
        if (string.IsNullOrWhiteSpace(regulationName))
        {
            throw new PayrollException("Missing regulation");
        }
        if (string.IsNullOrWhiteSpace(reportName))
        {
            throw new PayrollException("Missing report");
        }

        DisplayTitle("Data report");
        ConsoleTool.DisplayTextLine($"Output file      {outputFile}");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"User             {userIdentifier}");
        ConsoleTool.DisplayTextLine($"Regulation       {regulationName}");
        ConsoleTool.DisplayTextLine($"Report           {reportName}");
        ConsoleTool.DisplayTextLine($"Language         {language}");
        if (!string.IsNullOrWhiteSpace(parameterFile))
        {
            ConsoleTool.DisplayTextLine($"Parameter file   {parameterFile}");
        }
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        // stopwatch
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        ConsoleTool.DisplayText("Building data report...");

        // tenant
        var tenant = await new TenantService(HttpClient)
            .GetAsync<Tenant>(new(), tenantIdentifier);
        if (tenant == null)
        {
            throw new PayrollException($"Invalid tenant {tenantIdentifier}");
        }
        // user
        var user = await new UserService(HttpClient)
            .GetAsync<User>(new(tenant.Id), userIdentifier);
        if (user == null)
        {
            throw new PayrollException($"Invalid user {userIdentifier}");
        }
        // regulation
        var regulation = await new RegulationService(HttpClient)
            .GetAsync<Regulation>(new(tenant.Id), regulationName);
        if (regulation == null)
        {
            throw new PayrollException($"Invalid regulation {regulationName}");
        }
        // report
        var report = await new ReportService(HttpClient)
            .GetAsync<Report>(new(tenant.Id, regulation.Id), reportName);
        if (report == null)
        {
            throw new PayrollException($"Invalid report {reportName}");
        }

        // execute report
        try
        {
            // report parameter
            Dictionary<string, string> parameters = null;
            if (!string.IsNullOrWhiteSpace(parameterFile) && File.Exists(parameterFile))
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    await File.ReadAllTextAsync(parameterFile));
            }

            // report request
            var reportRequest = new ReportRequest
            {
                UserId = user.Id,
                Language = language,
                Parameters = parameters
            };

            // report response
            var reportResponse = await new ReportService(HttpClient).ExecuteReportAsync(
                    new(tenant.Id, regulation.Id), report.Id, reportRequest);

            // result data set
            if (reportResponse.Result == null)
            {
                ConsoleTool.DisplayErrorLine($"Empty result in report {reportName}.");
            }
            else
            {
                var fileInfo = new FileInfo(outputFile);
                var fileName = $"{outputFile.Replace(fileInfo.Extension, string.Empty)}" +
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
                ConsoleTool.DisplaySuccessLine($"Report {reportName} to data file {new FileInfo(fileName).FullName}");
                ConsoleTool.DisplayNewLine();
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
        ConsoleTool.DisplayTextLine("          6. language");
        ConsoleTool.DisplayTextLine("          7. report parameter file with a json string/string dictionary (optional)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport German");
        ConsoleTool.DisplayTextLine("          DataReport MyReport.data.json MyTenant MyUser MyRegulation MyReport German MyParameters.json");
    }
}