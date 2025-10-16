class AnalyzerException : Exception
{
    public int LineNumber { get; }
    public int ColumnNumber { get; }
    public AnalyzerException() : base() { }

    public AnalyzerException(string message) : base(message) { }

    public AnalyzerException(string message, Exception innerException) : base(message, innerException) { }

    public AnalyzerException(string message, int lineNumber, int columnNumber)
            : base($"{message} (at line {lineNumber}, column {columnNumber})")
    {
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }

}