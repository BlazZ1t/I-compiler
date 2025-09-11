class CompilerException : Exception
{
    public int LineNumber { get; }
    public int ColumnNumber { get; }
    public CompilerException() : base() { }

    public CompilerException(string message) : base(message) { }

    public CompilerException(string message, Exception innerException) : base(message, innerException) { }

    public CompilerException(string message, int lineNumber, int columnNumber)
            : base($"{message} (at line {lineNumber}, column {columnNumber})")
    {
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }

}