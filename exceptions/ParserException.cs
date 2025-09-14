class ParserException : CompilerException
{
    public ParserException(string message) : base(message) { }
    public ParserException(string message, Exception innerException) : base(message, innerException) { }

    public ParserException(string message, int lineNumber, int columnNumber) : base(message, lineNumber, columnNumber) { }
}