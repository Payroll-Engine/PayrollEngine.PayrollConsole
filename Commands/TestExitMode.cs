namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Controls the program exit behavior when test failures are detected
/// </summary>
public enum TestExitMode
{
    /// <summary>Exit with FailedTest when any test fails (default)</summary>
    ErrorOnFail,

    /// <summary>
    /// Exit with Ok even when tests fail — all test files are processed and all failures
    /// are logged, but the process exits with code 0. Use for bulk runs where errors
    /// should be collected without stopping the pipeline.
    /// </summary>
    OkOnFail
}
