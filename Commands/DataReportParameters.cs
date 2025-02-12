using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class DataReportParameters : ICommandParameters
{
    public string OutputFile { get; init; }
    public string Tenant { get; init; }
    public string User { get; init; }
    public string Regulation { get; init; }
    public string Report { get; init; }
    public string ParametersFile { get; init; }
    public string Culture { get; init; }
    public ReportPostAction PostAction { get; private init; } = ReportPostAction.NoAction;

    public Type[] Toggles => null;
    public string Test() => null;

    public string TestArguments()
    {
        if (string.IsNullOrWhiteSpace(OutputFile))
        {
            return "Missing output file";
        }
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(User))
        {
            return "Missing user";
        }
        if (string.IsNullOrWhiteSpace(Regulation))
        {
            return "Missing regulation";
        }
        if (string.IsNullOrWhiteSpace(Report))
        {
            return "Missing report";
        }
        return null;
    }

    public static DataReportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            OutputFile = parser.Get(2, nameof(OutputFile)),
            Tenant = parser.Get(3, nameof(Tenant)),
            User = parser.Get(4, nameof(User)),
            Regulation = parser.Get(5, nameof(Regulation)),
            Report = parser.Get(6, nameof(Report)),
            ParametersFile = parser.Get(7, nameof(ParametersFile)),
            Culture = parser.Get(8, nameof(Culture)),
            PostAction = parser.GetEnumToggle(ReportPostAction.NoAction)
        };
}