class AnalyzerException : CompilerException
{
    public AnalyzerException(string message) : base(message) { }
    public AnalyzerException(string message, Exception innerException) : base(message, innerException) { }

    public AnalyzerException(string message, int lineNumber, int columnNumber) : base(message, lineNumber, columnNumber) { }
}