using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Xml;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting.Function.Api;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Document;
using PayrollEngine.IO;
using PayrollEngine.PayrollConsole.Arguments;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ReportCommand : HttpCommandBase
{
    internal ReportCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> ReportAsync(string tenantIdentifier,
        string userIdentifier, string regulationName, string reportName,
        DocumentType documentType, Language language, string parameterFile = null)
    {
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

        DisplayTitle("Report");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"User             {userIdentifier}");
        ConsoleTool.DisplayTextLine($"Regulation       {regulationName}");
        ConsoleTool.DisplayTextLine($"Report           {reportName}");
        ConsoleTool.DisplayTextLine($"Document type    {documentType}");
        ConsoleTool.DisplayTextLine($"Language         {language}");
        if (!string.IsNullOrWhiteSpace(parameterFile))
        {
            ConsoleTool.DisplayTextLine($"Parameter file   {parameterFile}");
        }
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        try
        {
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

            // document culture
            if (!string.IsNullOrWhiteSpace(user.Culture))
            {
                // https://www.syncfusion.com/kb/7467/how-is-excel-culture-defined-using-xlsio
                Thread.CurrentThread.CurrentCulture = new(user.Culture);
                Thread.CurrentThread.CurrentUICulture = new(user.Culture);
            }

            // regulation
            var regulation = await new RegulationService(HttpClient)
                .GetAsync<Regulation>(new(tenant.Id), regulationName);
            if (regulation == null)
            {
                throw new PayrollException($"Invalid regulation {regulationName}");
            }

            // report parameters
            var parameters = await GetParametersAsync(tenant.Id, regulation.Id,
                ReportArguments.ParameterFile, ReportArguments.DefaultParameterFileName);

            // report
            var report = await new ReportSetService(HttpClient)
                .GetAsync<ReportSet>(new(tenant.Id, regulation.Id), reportName);
            if (report == null)
            {
                throw new PayrollException($"Invalid report {reportName}");
            }

            ConsoleTool.DisplayText($"Building report {report.Name}...");

            // stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplayText("Executing report...");
            var response = await ExecuteReport(HttpClient, tenant.Id, regulation.Id, user.Id, report,
                language, parameters);
            if (response == null)
            {
                throw new PayrollException($"Invalid report response on report {reportName}");
            }
            if (response.Result.Tables.Count == 0)
            {
                throw new PayrollException($"Invalid report {reportName}");
            }
            ConsoleTool.DisplayTextLine("done.");

            var executeTime = stopwatch.ElapsedMilliseconds;

            // report metadata
            ConsoleTool.DisplayText("Building report...");
            var now = DateTime.Now; // use local time (no UTC)
            var title = response.Language.GetLocalization(report.NameLocalizations, report.Name);
            var documentMetadata = new DocumentMetadata
            {
                Author = user.Identifier,
                Category = report.Category,
                Company = tenant.Identifier,
                Title = title,
                Keywords = response.Language.ToString(),
                CustomProperties = parameters,
                Created = now,
                Modified = now
            };

            // data set
            DataSet dataSet = Data.DataSetExtensions.ToSystemDataSet(response.Result);

            string outputFile;
            switch (ReportArguments.DocumentType())
            {
                case DocumentType.Word:
                case DocumentType.Excel:
                case DocumentType.Pdf:
                    outputFile = await MergeAsync(tenant, regulation, report, dataSet, documentMetadata, documentType, language);
                    break;
                case DocumentType.Xml:
                    outputFile = await TransformAsync(tenant, regulation, report, dataSet, language);
                    break;
                case DocumentType.XmlRaw:
                    outputFile = await TransformAsync(tenant, regulation, report, dataSet, language, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ConsoleTool.DisplayTextLine("done.");

            stopwatch.Stop();

            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplaySuccessLine($"Report file created {new FileInfo(outputFile).FullName}");
            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplayTextLine("Report statistics:");
            ConsoleTool.DisplayTextLine($"  Execute: {executeTime} ms");
            ConsoleTool.DisplayTextLine($"  Convert: {stopwatch.ElapsedMilliseconds - executeTime} ms");
            ConsoleTool.DisplayTextLine($"  Total:   {stopwatch.ElapsedMilliseconds} ms");
            ConsoleTool.DisplayNewLine();

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
        int userId, ReportSet report, Language language, Dictionary<string, string> parameters)
    {
        var request = new ReportRequest
        {
            UserId = userId,
            Language = language,
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

    private async Task<string> MergeAsync(Tenant tenant, Regulation regulation,
        ReportSet report, DataSet dataSet, DocumentMetadata documentMetadata,
        DocumentType documentType, Language language)
    {
        var merge = new DataMerge();
        if (!merge.IsMergeable(documentType))
        {
            ConsoleTool.WriteErrorLine($" report {report.Name}: merge of {documentType} is not supported");
            return null;
        }

        var targetFileName =
            $"{report.Name}_{FileTool.CurrentTimeStamp()}{documentType.GetFileExtension()}";

        MemoryStream resultStream;
        if (documentType == DocumentType.Excel)
        {
            // excel report
            resultStream = merge.ExcelMerge(dataSet, documentMetadata);
        }
        else
        {
            // report template
            var template = await GetReportTemplateAsync(tenant.Id, regulation.Id, report, language);
            if (template == null)
            {
                ConsoleTool.WriteErrorLine($"Invalid report template for report {report.Name}");
                return null;
            }

            // report merge into stream
            var contentStream = new MemoryStream(Convert.FromBase64String(template.Content));
            resultStream = merge.Merge(contentStream, dataSet, documentType, documentMetadata);
        }

        // file save
        await resultStream.WriteToFile(targetFileName);
        return targetFileName;
    }

    private async Task<ReportTemplate> GetReportTemplateAsync(int tenantId,
        int regulationId, Report report, Language language)
    {
        var template = (await new ReportTemplateService(HttpClient).QueryAsync<ReportTemplate>(
                new(tenantId, regulationId, report.Id), new() { Language = language }))
            .FirstOrDefault();
        if (template == null)
        {
            return null;
        }
        return template;
    }

    #endregion

    #region Transform

    private async Task<string> TransformAsync(Tenant tenant, Regulation regulation,
        ReportSet report, DataSet dataSet, Language language, bool rawData = false)
    {
        var rawName = rawData ? "_raw" : string.Empty;
        var targetFileName = $"{report.Name}_{FileTool.CurrentTimeStamp()}{rawName}{FileExtensions.Xml}";

        // report template
        var template = await GetReportTemplateAsync(tenant.Id, regulation.Id, report, language);
        if (template == null)
        {
            return null;
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
                ConsoleTool.WriteErrorLine($"Error transforming XML report {report.Name}: {exception.GetBaseMessage()}");
                return null;
            }

            // report validation
            if (!string.IsNullOrWhiteSpace(xml) && !string.IsNullOrWhiteSpace(template.Schema))
            {
                try
                {
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
                    ConsoleTool.WriteErrorLine($"Error validating XML report {report.Name}: {exception.GetBaseMessage()}");
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

    private XmlNode DataSetToXml(DataSet dataSet)
    {
        using MemoryStream stream = new();
        dataSet.WriteXml(stream, XmlWriteMode.WriteSchema);
        var xml = new UTF8Encoding().GetString(stream.ToArray());
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document;
    }

    #endregion

    #region Help

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- Report");
        ConsoleTool.DisplayTextLine("      Report to file");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant identifier");
        ConsoleTool.DisplayTextLine("          2. user identifier");
        ConsoleTool.DisplayTextLine("          3. regulation name");
        ConsoleTool.DisplayTextLine("          4. report name");
        ConsoleTool.DisplayTextLine("          6. language:/language (default: /english)");
        ConsoleTool.DisplayTextLine("          7. document type: /pdf /xml or /xmlraw (default: /pdf)");
        ConsoleTool.DisplayTextLine("          8. report parameter file with a json string/string dictionary (optional)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport /german");
        ConsoleTool.DisplayTextLine("          Report MyTenant MyUser MyRegulation MyReport MyParameters.json /french /xml");
    }

    #endregion

}
