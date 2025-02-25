using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Base class for http commands
/// </summary>
/// <typeparam name="TArgs"></typeparam>
internal abstract class HttpCommandBase<TArgs> : CommandBase<TArgs> where TArgs : ICommandParameters, new()
{
    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <summary>Get request url</summary>
    /// <param name="url"></param>
    protected string GetRequestUrl(string url)
    {
        url = url.RemoveFromStart("api/");
        return url;
    }

    /// <summary>Get the file content</summary>
    /// <param name="fileName">File name</param>
    protected static async Task<string> GetFileContent(string fileName)
    {
        var content = string.Empty;
        if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
        {
            content = await File.ReadAllTextAsync(fileName);
        }
        return content;
    }

}