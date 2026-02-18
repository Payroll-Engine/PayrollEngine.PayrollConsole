namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Schema type
/// </summary>
public enum SchemaType
{
    /// <summary>
    /// Automatic schema base on file extension
    /// </summary>
    /// <remarks>
    /// case test: *.ct.json/yaml
    /// report test: *.et.json/yaml
    /// </remarks>
    Auto,

    /// <summary>
    /// Exchange schema
    /// </summary>
    Exchange,

    /// <summary>
    /// Case test schema
    /// </summary>
    CaseTest,

    /// <summary>
    /// Report test schema
    /// </summary>
    ReportTest
}