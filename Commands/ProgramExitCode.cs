namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Program exit code
/// </summary>
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

    /// <summary>
    /// Backend connection error
    /// </summary>
    ConnectionError = 2,

    /// <summary>
    /// Http error
    /// </summary>
    HttpError = 3,

    /// <summary>
    /// Failed test
    /// </summary>
    FailedTest = 4,

    /// <summary>
    /// Invalid options
    /// </summary>
    InvalidOptions = 5
}