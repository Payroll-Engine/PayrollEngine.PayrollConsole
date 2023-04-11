namespace PayrollEngine.PayrollConsole.Shared;

internal enum ProgramExitCode
{
    Ok = 0,
    GenericError = 1,
    ConnectionError = 2,
    HttpError = 3,
    FailedTest = 4
}