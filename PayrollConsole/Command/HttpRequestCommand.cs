using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class HttpRequestCommand : HttpCommandBase
{
    internal HttpRequestCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>Http GET request</summary>
    /// <param name="url">The endpoint url</param>
    internal async Task<ProgramExitCode> GetRequestAsync(string url)
    {
        try
        {
            url = GetRequestUrl(url);
            DisplayTitle($"GET {url}");

            using var response = await HttpClient.GetAsync(url);

            ConsoleTool.DisplaySuccessLine($"GET request successfully ({response.StatusCode})");
            await DisplayResponseContent(response);
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ConsoleTool.DisplayErrorLine($"GET request failed: {exception.GetBaseMessage()}");
            return ProgramExitCode.HttpError;
        }
    }

    /// <summary>Http POST request</summary>
    /// <param name="url">The endpoint url</param>
    /// <param name="fileName">The file name</param>
    internal async ValueTask<ProgramExitCode> PostRequestAsync(string url, string fileName = null)
    {
        try
        {
            url = GetRequestUrl(url);
            DisplayTitle($"POST {url}");

            var content = await GetFileContent(fileName);
            using var response = await HttpClient.PostAsync(url, new(content));

            ConsoleTool.DisplaySuccessLine($"POST request successfully ({response.StatusCode})");
            await DisplayResponseContent(response);
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ConsoleTool.DisplayErrorLine($"POST request failed: {exception.GetBaseMessage()}");
            return ProgramExitCode.HttpError;
        }
    }

    /// <summary>Http PUT request</summary>
    /// <param name="url">The endpoint url</param>
    /// <param name="fileName">The file name</param>
    internal async Task<ProgramExitCode> PutRequestAsync(string url, string fileName = null)
    {
        try
        {
            url = GetRequestUrl(url);
            DisplayTitle($"PUT {url}");

            var content = await GetFileContent(fileName);
            await HttpClient.PutAsync(url, new StringContent(content));

            ConsoleTool.DisplaySuccessLine("PUT request successfully");
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ConsoleTool.DisplayErrorLine($"PUT request failed: {exception.GetBaseMessage()}");
            return ProgramExitCode.HttpError;
        }
    }

    /// <summary>Http DELETE request</summary>
    /// <param name="url">The endpoint url</param>
    internal async Task<ProgramExitCode> DeleteRequestAsync(string url)
    {
        try
        {
            url = GetRequestUrl(url);
            DisplayTitle($"DELETE {url}");

            await HttpClient.DeleteAsync(url);

            ConsoleTool.DisplaySuccessLine("DELETE request successfully");
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ConsoleTool.DisplayErrorLine($"DELETE request failed: {exception.GetBaseMessage()}");
            return ProgramExitCode.HttpError;
        }
    }

    internal static void ShowGetRequestHelp()
    {
        ConsoleTool.DisplayTitleLine("- HttpGet");
        ConsoleTool.DisplayTextLine("      Execute http GET request");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. End point url [Url]");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          HttpGet tenants");
        ConsoleTool.DisplayTextLine("          HttpGet tenants/1");
        ConsoleTool.DisplayTextLine("          HttpGet admin/application/stop /post");
    }

    internal static void ShowPostRequestHelp()
    {
        ConsoleTool.DisplayTitleLine("- HttpPost");
        ConsoleTool.DisplayTextLine("      Execute http POST request");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. End point url [Url]");
        ConsoleTool.DisplayTextLine("          2. Content file name (optional) [FileName]");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          HttpPost tenants/1 MyTenant.json");
        ConsoleTool.DisplayTextLine("          HttpPost admin/application/stop");
    }

    internal static void ShowPutRequestHelp()
    {
        ConsoleTool.DisplayTitleLine("- HttpPut");
        ConsoleTool.DisplayTextLine("      Execute http PUT request");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. End point url [Url]");
        ConsoleTool.DisplayTextLine("          2. Content file name (optional) [FileName]");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          HttpPut tenants/1 MyTenant.json");
    }

    internal static void ShowDeleteRequestHelp()
    {
        ConsoleTool.DisplayTitleLine("- HttpDelete");
        ConsoleTool.DisplayTextLine("      Execute http DELETE request");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. End point url [Url]");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          HttpDelete tenants/1");
    }

    private string GetRequestUrl(string url)
    {
        url = url.RemoveFromStart("api/");
        if (!url.StartsWith(HttpClient.BaseUrl, StringComparison.CurrentCultureIgnoreCase))
        {
            url = $"{HttpClient.BaseUrl}:{HttpClient.Port}/api/{url}";
        }
        return url;
    }

    private static async Task<string> GetFileContent(string fileName)
    {
        var content = string.Empty;
        if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
        {
            content = await File.ReadAllTextAsync(fileName);
        }
        return content;
    }

    private static async Task DisplayResponseContent(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }
        var formattedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(json),
            typeof(object), new JsonSerializerOptions { WriteIndented = true });
        ConsoleTool.DisplayInfoLine(formattedJson);
    }
}