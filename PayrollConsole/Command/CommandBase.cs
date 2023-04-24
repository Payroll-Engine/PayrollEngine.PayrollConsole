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
        Log.Error(exception, exception.GetBaseException().Message);

        // display
        var message = exception.GetApiMessage();
        ConsoleTool.DisplayErrorLine(message);
    }
}