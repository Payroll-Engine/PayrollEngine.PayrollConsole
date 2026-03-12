using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using DataSet = System.Data.DataSet;
using PayrollEngine.IO;
using PayrollEngine.Document;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Data;

namespace PayrollEngine.PayrollConsole.Commands.ReportCommands;

/// <summary>
/// Report build command — executes a report and exports the resulting DataSet as a
/// schema document for use during report template design.
///
/// Without TemplateFile: generates a new schema document from the DataSet.
/// With TemplateFile (CI mode): updates the schema section of the existing template,
/// preserving all design elements. Useful when regulation changes add or remove tables.
/// </summary>
[Command("ReportBuild")]
// ReSharper disable once UnusedType.Global
internal sealed class ReportBuildCommand : CommandBase<ReportBuildParameters>
{
    /// <summary>Build report schema document</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit code, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, ReportBuildParameters parameters)
    {
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

        // validate template file
        if (!string.IsNullOrWhiteSpace(parameters.TemplateFile) &&
            !File.Exists(parameters.TemplateFile))
        {
            context.Console.WriteErrorLine($"Template file not found: {parameters.TemplateFile}");
            return (int)ProgramExitCode.GenericError;
        }

        DisplayTitle(context.Console, $"Report Build - {parameters.Report}");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"User             {parameters.User}");
            context.Console.DisplayTextLine($"Regulation       {parameters.Regulation}");
            context.Console.DisplayTextLine($"Report           {parameters.Report}");
            context.Console.DisplayTextLine($"Culture          {parameters.Culture}");
            context.Console.DisplayTextLine($"Post action      {parameters.PostAction}");
            if (!string.IsNullOrWhiteSpace(parameters.TemplateFile))
            {
                context.Console.DisplayTextLine($"Template file    {parameters.TemplateFile}");
            }
            if (!string.IsNullOrWhiteSpace(parameters.ParameterFile))
            {
                context.Console.DisplayTextLine($"Parameter file   {parameters.ParameterFile}");
            }
            if (!string.IsNullOrWhiteSpace(parameters.TargetFile))
            {
                context.Console.DisplayTextLine($"Target file      {parameters.TargetFile}");
            }
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }
        context.Console.DisplayNewLine();

        try
        {
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

            // report parameters
            var reportParameters = await GetParametersAsync(context.Console,
                parameters.ParameterFile, parameters.DefaultParameterFileName);
            if (reportParameters == null)
            {
                return (int)ProgramExitCode.GenericError;
            }

            // execute report
            context.Console.DisplayTextLine("Executing report...");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var response = await new ReportService(context.HttpClient)
                .ExecuteReportAsync(new(tenant.Id, regulation.Id), report.Id, new ReportRequest
                {
                    UserId = user.Id,
                    Culture = parameters.Culture,
                    Parameters = reportParameters
                });

            if (response == null)
            {
                throw new PayrollException($"Report execution failed for {parameters.Report}.");
            }

            if (response.Result == null || response.Result.Tables == null || response.Result.Tables.Count == 0)
            {
                context.Console.DisplayNewLine();
                context.Console.DisplayErrorLine($"Empty report result for {parameters.Report}.");
                return (int)ProgramExitCode.Ok;
            }

            var executeTime = stopwatch.ElapsedMilliseconds;

            // convert to ADO.NET DataSet
            var dataSet = response.Result.ToSystemDataSet();
            if (!dataSet.HasRows())
            {
                context.Console.DisplayNewLine();
                context.Console.WriteErrorLine("Report without data.");
                context.Console.DisplayNewLine();
                return (int)ProgramExitCode.GenericError;
            }

            // generate schema document
            context.Console.DisplayTextLine("Building schema document...");
            var outputFile = await ExportAsync(context.Console, report, dataSet, parameters);

            stopwatch.Stop();

            if (string.IsNullOrWhiteSpace(outputFile))
            {
                context.Console.DisplayNewLine();
                context.Console.DisplayErrorLine("Report build failed.");
                context.Console.DisplayNewLine();
                return (int)ProgramExitCode.GenericError;
            }

            context.Console.DisplayTextLine("done.");

            var fileName = new FileInfo(outputFile).FullName;
            context.Console.DisplayNewLine();
            context.Console.DisplaySuccessLine($"Report schema document created {fileName}");
            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine("Report statistics:");
            context.Console.DisplayTextLine($"  Execute: {executeTime} ms");
            context.Console.DisplayTextLine($"  Export:  {stopwatch.ElapsedMilliseconds - executeTime} ms");
            context.Console.DisplayTextLine($"  Total:   {stopwatch.ElapsedMilliseconds} ms");
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

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    #region Export

    private static async Task<string> ExportAsync(ICommandConsole console, Report report,
        DataSet dataSet, ReportBuildParameters parameters)
    {
        // derive target file: explicit > template extension > no extension
        var targetFile = parameters.TargetFile;
        if (string.IsNullOrWhiteSpace(targetFile))
        {
            var extension = string.IsNullOrWhiteSpace(parameters.TemplateFile)
                ? string.Empty
                : Path.GetExtension(parameters.TemplateFile);
            targetFile = $"{report.Name}_{FileTool.CurrentTimeStamp()}{extension}";
        }

        Stream templateStream = string.IsNullOrWhiteSpace(parameters.TemplateFile)
            ? null
            : File.OpenRead(parameters.TemplateFile);

        try
        {
            var resultStream = await new DocumentService().GenerateAsync(dataSet, templateStream);
            await resultStream.WriteToFile(targetFile);
        }
        catch (Exception exception)
        {
            console.WriteErrorLine($"Error generating schema document: {exception.GetBaseMessage()}");
            return null;
        }
        finally
        {
            if (templateStream != null)
            {
                await templateStream.DisposeAsync();
            }
        }

        return targetFile;
    }

    #endregion

    #region Parameters

    private static async Task<Dictionary<string, string>> GetParametersAsync(
        ICommandConsole console, string parameterFile, string defaultParameterFile)
    {
        if (!string.IsNullOrWhiteSpace(parameterFile) && !File.Exists(parameterFile))
        {
            console.WriteErrorLine($"Invalid parameter file {parameterFile}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(parameterFile))
        {
            parameterFile = defaultParameterFile;
        }

        if (!File.Exists(parameterFile))
        {
            return new();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(
                await File.ReadAllTextAsync(parameterFile));
        }
        catch (Exception exception)
        {
            console.WriteErrorLine($"Error in parameter file {parameterFile}: {exception.GetBaseMessage()}");
            return null;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ReportBuildParameters.ParserFrom(parser);

    #endregion

    #region Help

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ReportBuild");
        console.DisplayTextLine("      Execute a report and generate a schema document for report template design.");
        console.DisplayTextLine("      Without TemplateFile: generates a new schema document from the DataSet.");
        console.DisplayTextLine("      With TemplateFile (CI mode): updates the schema section of an existing template.");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. user identifier [User]");
        console.DisplayTextLine("          3. regulation name [Regulation]");
        console.DisplayTextLine("          4. report name [Report]");
        console.DisplayTextLine("      Named:");
        console.DisplayTextLine("          existing template file for CI schema update [TemplateFile]");
        console.DisplayTextLine("          report parameter file, json string/string dictionary [ParameterFile]");
        console.DisplayTextLine("          report culture [Culture]");
        console.DisplayTextLine("          target file name [TargetFile]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          post action: /noaction or /shellopen (default: noaction)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ReportBuild MyTenant MyUser MyRegulation MyReport");
        console.DisplayTextLine("          ReportBuild MyTenant MyUser MyRegulation MyReport /shellopen");
        console.DisplayTextLine("          ReportBuild MyTenant MyUser MyRegulation MyReport templateFile:Report.frx");
        console.DisplayTextLine("          ReportBuild MyTenant MyUser MyRegulation MyReport parameterFile:parameters.json");
        console.DisplayTextLine("          ReportBuild MyTenant MyUser MyRegulation MyReport targetFile:MyReport.frx");
    }

    #endregion

}
