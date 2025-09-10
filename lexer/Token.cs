class Token
{
    private TokenType tokenType;
    private string lexeme;
    private int line;
    private int column;
    public Token(TokenType tokenType, string lexeme, int line, int column)
    {
        this.tokenType = tokenType;
        this.lexeme = lexeme;
        this.line = line;
        this.column = column;
    }


    public override string ToString()
    {
        return $"Type: {tokenType}, lexeme: {lexeme}, line: {line}, column: {column}";
    }

    
}