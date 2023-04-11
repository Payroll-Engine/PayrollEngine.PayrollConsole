using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal abstract class CommandBase
{
    protected void DisplayTitle(string title)
    {
        ConsoleTool.DisplayTitleLine($"=== {title} ===");
    }

    protected void ProcessError(Exception exception)
    {
        if (exception == null)
        {
            return;
        }

        // log
        var message = exception.GetBaseException().Message;
        Log.Error(exception, message);

        // display
        var apiError = exception.ToApiError();
        if (apiError == null)
        {
            ConsoleTool.DisplayErrorLine(message);
        }
        else
        {
            if (apiError.StatusCode != 0)
            {
                ConsoleTool.DisplayErrorLine($"Http status code: {apiError.StatusCode}");
            }
            ConsoleTool.DisplayErrorLine(apiError.Message.Trim());
            if (!string.IsNullOrWhiteSpace(apiError.StackTrace))
            {
                ConsoleTool.DisplayInfoLine(apiError.StackTrace);
            }
        }
    }
}