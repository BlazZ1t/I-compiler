class LexerException : CompilerException
{
    public LexerException(string message) : base(message) { }

    public LexerException(string message, Exception innerException) : base(message, innerException) { }

    public LexerException(string message, int lineNumber, int columnNumber) : base(message, lineNumber, columnNumber) { }
}