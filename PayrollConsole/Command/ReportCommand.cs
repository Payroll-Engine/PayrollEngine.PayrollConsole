using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Xml;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting.Function.Api;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Data;
using PayrollEngine.Document;
using PayrollEngine.IO;
using PayrollEngine.PayrollConsole.Arguments;
using PayrollEngine.PayrollConsole.Shared;
using DataSet = System.Data.DataSet;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ReportCommand : HttpCommandBase
{
    internal ReportCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> ReportAsync(ReportCommandSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
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

        DisplayTitle("Report");
        ConsoleTool.DisplayTextLine($"Tenant           {settings.TenantIdentifier}");
        ConsoleTool.DisplayTextLine($"User             {settings.UserIdentifier}");
        ConsoleTool.DisplayTextLine($"Regulation       {settings.RegulationName}");
        ConsoleTool.DisplayTextLine($"Report           {settings.ReportName}");
        ConsoleTool.DisplayTextLine($"Document type    {settings.DocumentType}");
        ConsoleTool.DisplayTextLine($"Culture          {settings.Culture}");
        ConsoleTool.DisplayTextLine($"Post action      {settings.PostAction}");
        if (!string.IsNullOrWhiteSpace(settings.ParameterFile))
        {
            ConsoleTool.DisplayTextLine($"Parameter file   {settings.ParameterFile}");
        }
        if (!string.IsNullOrWhiteSpace(settings.TargetFile))
        {
            ConsoleTool.DisplayTextLine($"Target file      {settings.TargetFile}");
        }
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
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

            // report parameters
            var parameters = await GetParametersAsync(tenant.Id, regulation.Id,
                ReportArguments.ParameterFile, ReportArguments.DefaultParameterFileName);

            // report
            var report = await new ReportSetService(HttpClient)
                .GetAsync<ReportSet>(new(tenant.Id, regulation.Id), settings.ReportName);
            if (report == null)
            {
                throw new PayrollException($"Invalid report {settings.ReportName}");
            }

            ConsoleTool.DisplayText($"Building report {report.Name}...");

            // stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplayTextLine("Executing report...");
            var response = await ExecuteReport(HttpClient, tenant.Id, regulation.Id, user.Id, report,
                settings.Culture, parameters);
            if (response == null)
            {
                throw new PayrollException($"Invalid report response on report {settings.ReportName}");
            }
            if (response.Result.Tables.Count == 0)
            {
                throw new PayrollException($"Invalid report {settings.ReportName}");
            }
            ConsoleTool.DisplayTextLine("done.");

            var executeTime = stopwatch.ElapsedMilliseconds;

            // report metadata
            ConsoleTool.DisplayTextLine("Building report...");
            var now = DateTime.Now; // use local time (no UTC)
            var title = response.Culture.GetLocalization(report.NameLocalizations, report.Name);
            var documentMetadata = new DocumentMetadata
            {
                Author = user.Identifier,
                Category = report.Category,
                Company = tenant.Identifier,
                Title = title,
                Keywords = response.Culture,
                CustomProperties = parameters,
                Created = now,
                Modified = now
            };

            // data set
            DataSet dataSet = response.Result.ToSystemDataSet();
            if (!dataSet.HasRows())
            {
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplayNewLine();
                ConsoleTool.WriteErrorLine("Report without data.");
                ConsoleTool.DisplayNewLine();
                return ProgramExitCode.GenericError;
            }

            var mergeParameters = new Dictionary<string, object>(parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)));

            string outputFile;
            switch (settings.DocumentType)
            {
                case DocumentType.Word:
                case DocumentType.Excel:
                case DocumentType.Pdf:
                    outputFile = await MergeAsync(
                        report: report,
                        dataSet: dataSet,
                        documentMetadata: documentMetadata,
                        documentType: settings.DocumentType,
                        culture: settings.Culture,
                        parameters: mergeParameters,
                        settings.TargetFile);
                    break;
                case DocumentType.Xml:
                case DocumentType.XmlRaw:
                    outputFile = await TransformAsync(
                        report: report,
                        dataSet: dataSet,
                        culture: settings.Culture,
                        rawData: settings.DocumentType == DocumentType.XmlRaw,
                        targetFile: settings.TargetFile);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ConsoleTool.DisplayTextLine("done.");

            stopwatch.Stop();

            ConsoleTool.DisplayNewLine();
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplayErrorLine("Report failed.");
                ConsoleTool.DisplayNewLine();
                return ProgramExitCode.GenericError;
            }

            var fileName = new FileInfo(outputFile).FullName;
            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplaySuccessLine($"Report file created {fileName}");
            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplayTextLine("Report statistics:");
            ConsoleTool.DisplayTextLine($"  Execute: {executeTime} ms");
            ConsoleTool.DisplayTextLine($"  Convert: {stopwatch.ElapsedMilliseconds - executeTime} ms");
            ConsoleTool.DisplayTextLine($"  Total:   {stopwatch.ElapsedMilliseconds} ms");
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

            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ConsoleTool.DisplayNewLine();
            ConsoleTool.WriteErrorLine($"Report error: {exception.GetBaseMessage()}");
            return ProgramExitCode.GenericError;
        }
    }

    private async Task<ReportResponse> ExecuteReport(PayrollHttpClient httpClient, int tenantId, int regulationId,
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
            ConsoleTool.DisplayNewLine();
            ConsoleTool.WriteErrorLine($"Error while executing report {report.Name}");
            return null;
        }

        return response;
    }

    private async Task<Dictionary<string, string>> GetParametersAsync(int tenantId, int regulationId,
        string parameterFile, string defaultParameterFile)
    {
        if (!string.IsNullOrWhiteSpace(parameterFile) && !File.Exists(parameterFile))
        {
            ConsoleTool.WriteErrorLine($"Invalid parameter file {parameterFile}");
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
                await new ReportParameterParser(HttpClient, tenantId, regulationId).
                    ParseParametersAsync(parameters);
            }
            return parameters;
        }
        catch (Exception exception)
        {
            ConsoleTool.WriteErrorLine($"Error in parameter file {parameterFile}: {exception.GetBaseMessage()}");
            return null;
        }
    }

    #region Merge

    private async Task<string> MergeAsync(ReportSet report, DataSet dataSet, DocumentMetadata documentMetadata,
        DocumentType documentType, string culture, IDictionary<string, object> parameters = null, string targetFile = null)
    {
        var merge = new DataMerge();
        if (!merge.IsMergeable(documentType))
        {
            ConsoleTool.WriteErrorLine($" report {report.Name}: merge of {documentType} is not supported");
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
                ConsoleTool.WriteErrorLine($"Invalid report template for report {report.Name}");
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

    private async Task<string> TransformAsync(ReportSet report, DataSet dataSet, string culture,
        bool rawData = false, string targetFile = null)
    {
        // report template
        var template = GetReportTemplate(report, culture);
        if (template == null)
        {
            return null;
        }

        // target file
        var rawName = rawData ? "_raw" : string.Empty;
        var targetFileName = targetFile ??
                             $"{report.Name}_{FileTool.CurrentTimeStamp()}{rawName}{FileExtensions.Xml}";
        // cleanup
        if (File.Exists(targetFileName))
        {
            File.Delete(targetFileName);
        }

        // data set to xml
        var xmlDocument = DataSetToXml(dataSet);
        string xml;
        if (rawData)
        {
            xml = xmlDocument.OuterXml;
        }
        else
        {
            // report transformation
            try
            {
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplayText("Transforming raw XML...");

                // style sheet
                await using var xslStream = new MemoryStream(Convert.FromBase64String(template.Content));
                using var xslReader = XmlReader.Create(xslStream);
                XslCompiledTransform xslt = new();
                xslt.Load(xslReader);

                // transformation
                await using var stringWriter = new StringWriter();
                xslt.Transform(xmlDocument, null, stringWriter);
                xml = stringWriter.ToString();
                ConsoleTool.DisplayTextLine("done");
            }
            catch (Exception exception)
            {
                ConsoleTool.DisplayNewLine();
                ConsoleTool.WriteErrorLine($"Error transforming XML report {report.Name}: {exception.GetBaseMessage()}");
                return null;
            }

            // report validation
            if (!string.IsNullOrWhiteSpace(xml) && !string.IsNullOrWhiteSpace(template.Schema))
            {
                try
                {
                    ConsoleTool.DisplayNewLine();
                    ConsoleTool.DisplayText("Validating XML...");

                    var xsdStream = new MemoryStream(Convert.FromBase64String(template.Schema));
                    using var xmlReader = XmlReader.Create(xsdStream);

                    var document = new XmlDocument();
                    document.LoadXml(xml);
                    document.Schemas.Add(null, xmlReader);
                    document.Validate(null);
                    ConsoleTool.DisplayTextLine("done");
                }
                catch (Exception exception)
                {
                    ConsoleTool.DisplayNewLine();
                    ConsoleTool.WriteErrorLine($"Error validating XML report {report.Name}: {exception.GetBaseMessage()}");
                    return null;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(xml))
        {
            ConsoleTool.WriteErrorLine($"Report {report.Name} failed to build");
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

    #endregion

    #region Help

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- Report");
        ConsoleTool.DisplayTextLine("      Report to file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant identifier [Tenant]");
        ConsoleTool.DisplayTextLine("          2. user identifier [User]");
        ConsoleTool.DisplayTextLine("          3. regulation name [Regulation]");
        ConsoleTool.DisplayTextLine("          4. report name [Report]");
        ConsoleTool.DisplayTextLine("          5. report parameter file with a json string/string dictionary (optional) [ParameterFile]");
        ConsoleTool.DisplayTextLine("          6. report culture [Culture]");
        ConsoleTool.DisplayTextLine("          7. target file name [TargetFile]");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          language: default is english)");
        ConsoleTool.DisplayTextLine("          document type: /word, /excel, /pdf, /xml, /xmlraw (default: pdf)");
        ConsoleTool.DisplayTextLine("          post action: /noaction or /shellopen (default: noaction)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport /german");
        ConsoleTool.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport MyParameters.json /french /xml");
        ConsoleTool.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport /pdf targetFile:MyReport.pdf");
    }

    #endregion

}
