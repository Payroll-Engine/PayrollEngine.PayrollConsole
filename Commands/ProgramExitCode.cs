namespace PayrollEngine.PayrollConsole.Commands;

public enum ProgramExitCode
{
    /// <summary>
    /// No error.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// Generic application error (from client core command)
    /// </summary>
    GenericError = -1,

    /// <summary>
    /// Command file error (from client core command)
    /// </summary>
    CommandFile = -2,

    ConnectionError = 2,
    HttpError = 3,
    FailedTest = 4,
    InvalidOptions = 5
}