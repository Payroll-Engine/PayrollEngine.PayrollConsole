using System;

namespace PayrollEngine.PayrollConsole.Shared;

internal static class ConsoleTool
{
    internal static ConsoleDisplayMode DisplayMode { get; set; }
    internal static ConsoleErrorMode ErrorMode { private get; set; }
    internal static PayrollConsoleWaitMode WaitMode { get; set; }

    internal static void DisplaySuccessLine(string text)
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.WriteSuccessLine(text);
        }
    }

    internal static void DisplayTitleLine(string text)
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.WriteTitleLine(text);
        }
    }

    internal static void DisplayInfo(string text)
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.WriteInfo(text);
        }
    }

    internal static void DisplayInfoLine(string text)
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.WriteInfoLine(text);
        }
    }

    internal static void DisplayErrorLine(string text = null)
    {
        if (ErrorMode == ConsoleErrorMode.Errors)
        {
            // ensure error is displayed in separate line
            if (Console.CursorLeft > 0)
            {
                Client.ConsoleToolBase.WriteLine();
            }
            WriteErrorLine(text);
        }
    }

    internal static void DisplayText(string text)
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.Write(text);
        }
    }

    internal static void DisplayNewLine()
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.WriteLine();
        }
    }

    internal static void DisplayTextLine(string text)
    {
        if (DisplayMode == ConsoleDisplayMode.Show)
        {
            Client.ConsoleToolBase.WriteLine(text);
        }
    }

    internal static void WriteErrorLine(string text) =>
        Client.ConsoleToolBase.WriteErrorLine(text);
}