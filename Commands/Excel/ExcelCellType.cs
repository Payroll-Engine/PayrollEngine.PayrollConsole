namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Excel cell value type</summary>
internal enum ExcelCellType
{
    /// <summary>Empty cell</summary>
    Blank,
    /// <summary>Number, date or time value</summary>
    Numeric,
    /// <summary>Text value</summary>
    String,
    /// <summary>Boolean value</summary>
    Boolean,
    /// <summary>Formula cell</summary>
    Formula,
    /// <summary>Error value</summary>
    Error
}
