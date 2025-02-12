using System;
using System.IO;
using System.Xml;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Xsl;
using System.Xml.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using DataSet = System.Data.DataSet;
using PayrollEngine.IO;
using PayrollEngine.Data;
using PayrollEngine.Client;
using PayrollEngine.Client.Command;
using PayrollEngine.Document;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Client.Scripting.Function.Api;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("Report")]
// ReSharper disable once UnusedType.Global
internal sealed class ReportCommand : CommandBase<ReportParameters>
{
    /// <summary>Build report</summary>
    protected override async Task<int> Execute(CommandContext context, ReportParameters parameters)
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

        DisplayTitle(context.Console, $"Report - {parameters.Report}");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"User             {parameters.User}");
            context.Console.DisplayTextLine($"Regulation       {parameters.Regulation}");
            context.Console.DisplayTextLine($"Report           {parameters.Report}");
            context.Console.DisplayTextLine($"Document type    {parameters.DocumentType}");
            context.Console.DisplayTextLine($"Culture          {parameters.Culture}");
            context.Console.DisplayTextLine($"Post action      {parameters.PostAction}");
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

            // report parameters
            var reportParameters = await GetParametersAsync(context.Console,
                context.HttpClient, tenant.Id, regulation.Id,
                parameters.ParameterFile, parameters.DefaultParameterFileName);

            // report request
            var reportRequest = new ReportRequest
            {
                Culture = parameters.Culture,
                UserId = user.Id,
                UserIdentifier = user.Identifier,
                Parameters = reportParameters
            };

            // report
            var report = await new ReportSetService(context.HttpClient)
                .GetAsync<ReportSet>(new(tenant.Id, regulation.Id), parameters.Report, reportRequest);
            if (report == null)
            {
                throw new PayrollException($"Invalid report {parameters.Report}.");
            }

            context.Console.DisplayText($"Building report {report.Name}...");

            // stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine("Executing report...");
            var response = await ExecuteReport(
                console: context.Console,
                httpClient: context.HttpClient,
                tenantId: tenant.Id,
                regulationId: regulation.Id,
                userId: user.Id,
                report: report,
                culture: parameters.Culture,
                parameters: reportParameters);
            if (response == null)
            {
                throw new PayrollException($"Invalid report response on report {parameters.Report}.");
            }
            if (response.Result.Tables.Count == 0)
            {
                throw new PayrollException($"Invalid report {parameters.Report}.");
            }
            context.Console.DisplayTextLine("done.");

            var executeTime = stopwatch.ElapsedMilliseconds;

            // report metadata
            context.Console.DisplayTextLine("Building report...");
            var now = DateTime.Now; // use local time (no UTC)
            var title = response.Culture.GetLocalization(report.NameLocalizations, report.Name);
            var documentMetadata = new DocumentMetadata
            {
                Author = user.Identifier,
                Category = report.Category,
                Company = tenant.Identifier,
                Title = title,
                Keywords = response.Culture,
                CustomProperties = reportParameters,
                Created = now,
                Modified = now
            };

            // data set
            var dataSet = response.Result.ToSystemDataSet();
            if (!dataSet.HasRows())
            {
                context.Console.DisplayNewLine();
                context.Console.DisplayNewLine();
                context.Console.WriteErrorLine("Report without data.");
                context.Console.DisplayNewLine();
                return (int)ProgramExitCode.GenericError;
            }

            var mergeParameters = new Dictionary<string, object>(reportParameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)));

            string outputFile;
            switch (parameters.DocumentType)
            {
                case DocumentType.Word:
                case DocumentType.Excel:
                case DocumentType.Pdf:
                    outputFile = await MergeAsync(
                        console: context.Console,
                        report: report,
                        dataSet: dataSet,
                        documentMetadata: documentMetadata,
                        documentType: parameters.DocumentType,
                        culture: parameters.Culture,
                        parameters: mergeParameters,
                        parameters.TargetFile);
                    break;
                case DocumentType.Xml:
                case DocumentType.XmlRaw:
                    outputFile = await TransformAsync(
                        console: context.Console,
                        report: report,
                        dataSet: dataSet,
                        culture: parameters.Culture,
                        rawData: parameters.DocumentType == DocumentType.XmlRaw,
                        targetFile: parameters.TargetFile);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            context.Console.DisplayTextLine("done.");

            stopwatch.Stop();

            context.Console.DisplayNewLine();
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                context.Console.DisplayNewLine();
                context.Console.DisplayErrorLine("Report failed.");
                context.Console.DisplayNewLine();
                return (int)ProgramExitCode.GenericError;
            }

            var fileName = new FileInfo(outputFile).FullName;
            context.Console.DisplayNewLine();
            context.Console.DisplaySuccessLine($"Report file created {fileName}");
            context.Console.DisplayNewLine();
            context.Console.DisplayTextLine("Report statistics:");
            context.Console.DisplayTextLine($"  Execute: {executeTime} ms");
            context.Console.DisplayTextLine($"  Convert: {stopwatch.ElapsedMilliseconds - executeTime} ms");
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
            context.Console.DisplayNewLine();
            context.Console.WriteErrorLine($"Report error: {exception.GetBaseMessage().Trim('"')}");
            return (int)ProgramExitCode.GenericError;
        }
    }

    private async Task<ReportResponse> ExecuteReport(ICommandConsole console,
        PayrollHttpClient httpClient, int tenantId, int regulationId,
        int userId, ReportSet report, string culture, Dictionary<string, string> parameters)
    {
        var request = new ReportRequest
        {
            UserId = userId,
            Culture = culture,
            Parameters = parameters
        };

        var response = await new ReportService(httpClient).ExecuteReportAsync(new(tenantId, regulationId), report.Id, request);
        if (response == null)
        {
            console.DisplayNewLine();
            console.WriteErrorLine($"Error while executing report {report.Name}");
            return null;
        }

        return response;
    }

    private async Task<Dictionary<string, string>> GetParametersAsync(ICommandConsole console,
        PayrollHttpClient httpClient, int tenantId, int regulationId, string parameterFile, string defaultParameterFile)
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

        // no parameter file available
        if (!File.Exists(parameterFile))
        {
            return new();
        }

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(parameterFile));
            if (parameters != null)
            {
                await new ReportParameterParser(httpClient, tenantId, regulationId).
                    ParseParametersAsync(parameters);
            }
            return parameters;
        }
        catch (Exception exception)
        {
            console.WriteErrorLine($"Error in parameter file {parameterFile}: {exception.GetBaseMessage()}");
            return null;
        }
    }

    #region Merge

    private async Task<string> MergeAsync(ICommandConsole console, ReportSet report,
        DataSet dataSet, DocumentMetadata documentMetadata,
        DocumentType documentType, string culture,
        IDictionary<string, object> parameters = null, string targetFile = null)
    {
        var merge = new DataMerge();
        if (!merge.IsMergeable(documentType))
        {
            console.WriteErrorLine($" report {report.Name}: merge of {documentType} is not supported");
            return null;
        }

        // target file
        var targetFileName = targetFile ??
                             $"{report.Name}_{FileTool.CurrentTimeStamp()}{documentType.GetFileExtension()}";
        // cleanup
        if (File.Exists(targetFileName))
        {
            File.Delete(targetFileName);
        }

        MemoryStream resultStream;
        if (documentType == DocumentType.Excel)
        {
            // excel report
            resultStream = merge.ExcelMerge(dataSet, documentMetadata, parameters);
        }
        else
        {
            // report template
            var template = GetReportTemplate(report, culture);
            if (template == null)
            {
                console.WriteErrorLine($"Invalid report template for report {report.Name}");
                return null;
            }

            // report merge into stream
            var contentStream = new MemoryStream(Convert.FromBase64String(template.Content));
            resultStream = merge.Merge(contentStream, dataSet, documentType, documentMetadata, parameters);
        }

        // file save
        await resultStream.WriteToFile(targetFileName);
        return targetFileName;
    }

    #endregion

    #region Transform

    private async Task<string> TransformAsync(ICommandConsole console, ReportSet report,
        DataSet dataSet, string culture, bool rawData = false, string targetFile = null)
    {
        // target file
        var rawName = rawData ? "_raw" : string.Empty;
        var targetFileName = targetFile ??
                             $"{report.Name}_{FileTool.CurrentTimeStamp()}{rawName}{FileExtensions.Xml}";

        // data set to xml
        string xml;
        var xmlDocument = DataSetToXml(dataSet);
        if (rawData)
        {
            xml = xmlDocument.OuterXml;
        }
        else
        {
            // report template
            var template = GetReportTemplate(report, culture);
            if (template == null)
            {
                return null;
            }
            // cleanup
            if (File.Exists(targetFileName))
            {
                File.Delete(targetFileName);
            }

            // report transformation
            try
            {
                console.DisplayNewLine();
                console.DisplayText("Transforming raw XML...");

                // style sheet
                await using var xslStream = new MemoryStream(Convert.FromBase64String(template.Content));
                using var xslReader = XmlReader.Create(xslStream);
                XslCompiledTransform xslt = new();
                xslt.Load(xslReader);

                // transformation
                await using var stringWriter = new StringWriter();
                xslt.Transform(xmlDocument, null, stringWriter);
                xml = stringWriter.ToString();
                console.DisplayTextLine("done");
            }
            catch (Exception exception)
            {
                console.DisplayNewLine();
                console.WriteErrorLine($"Error transforming XML report {report.Name}: {exception.GetBaseMessage()}");
                return null;
            }

            // report validation
            if (!string.IsNullOrWhiteSpace(xml) && !string.IsNullOrWhiteSpace(template.Schema))
            {
                try
                {
                    console.DisplayNewLine();
                    console.DisplayText("Validating XML...");

                    var xsdStream = new MemoryStream(Convert.FromBase64String(template.Schema));
                    using var xmlReader = XmlReader.Create(xsdStream);

                    var document = new XmlDocument();
                    document.LoadXml(xml);
                    document.Schemas.Add(null, xmlReader);
                    document.Validate(null);
                    console.DisplayTextLine("done");
                }
                catch (Exception exception)
                {
                    console.DisplayNewLine();
                    console.WriteErrorLine($"Error validating XML report {report.Name}: {exception.GetBaseMessage()}");
                    return null;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(xml))
        {
            console.WriteErrorLine($"Report {report.Name} failed to build");
            return null;
        }

        // target file
        await File.WriteAllTextAsync(targetFileName, XDocument.Parse(xml).ToString());

        return targetFileName;
    }

    private static XmlNode DataSetToXml(DataSet dataSet)
    {
        using MemoryStream stream = new();
        dataSet.WriteXml(stream, XmlWriteMode.WriteSchema);
        var xml = new UTF8Encoding().GetString(stream.ToArray());
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document;
    }

    #endregion

    #region Tools

    private static ReportTemplate GetReportTemplate(ReportSet report, string culture)
    {
        // ensure culture
        culture ??= CultureInfo.CurrentCulture.Name;

        // template by culture
        var template = report.Templates.FirstOrDefault(x => string.Equals(x.Culture, culture));

        // fallback template by base culture
        if (template == null)
        {
            var index = culture.IndexOf('-');
            if (index >= 0)
            {
                var baseCulture = culture.Substring(0, index);
                template = report.Templates.FirstOrDefault(x => string.Equals(x.Culture, baseCulture));
            }
        }
        return template;
    }

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ReportParameters.ParserFrom(parser);

    #endregion

    #region Help

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- Report");
        console.DisplayTextLine("      Report to file");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. user identifier [User]");
        console.DisplayTextLine("          3. regulation name [Regulation]");
        console.DisplayTextLine("          4. report name [Report]");
        console.DisplayTextLine("          5. report parameter file with a json string/string dictionary (optional) [ParameterFile]");
        console.DisplayTextLine("          6. report culture [Culture]");
        console.DisplayTextLine("          7. target file name [TargetFile]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          language (default: english");
        console.DisplayTextLine("          document type: /word, /excel, /pdf, /xml, /xmlraw (default: pdf)");
        console.DisplayTextLine("          post action: /noaction or /shellopen (default: noaction)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport /german");
        console.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport MyParameters.json /french /xml");
        console.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport /pdf targetFile:MyReport.pdf");
    }

    #endregion

}
