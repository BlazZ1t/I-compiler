using System.Text;

class Lexer
{
    private readonly string _source;
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;

    private static readonly HashSet<char> LanguageSymbols = new HashSet<char>
    {
        ':', '+', '-', '*', '/', '%', '<', '>', '=', '/', '(', ')', '[', ']', '.', ',', ';'    
    };


    public Lexer(string source)
    {
        _source = source;
    }

    public IEnumerable<Token> Tokenize()
    {
        while (!IsEndOfFile())
        {
            Token gottenToken = NextToken();
            yield return gottenToken;
        }
        yield return new Token(TokenType.EOF, "", _line, _column);
    }

    private Token NextToken()
    {
        SkipWhitespacesAndComments();

        if (IsEndOfFile())
            return new Token(TokenType.EOF, "", _line, _column);

        int startLine = _line;
        int startColumn = _column;

        char c = Advance();

        if (c == '\n')
            return new Token(TokenType.NewLine, "\\n", startLine, startColumn);
        //All operators and delimeters
        if (LanguageSymbols.Contains(c))
        {
            return SymbolTokenHelper(c, startLine, startColumn);
        }
        else if (char.IsDigit(c))
        {
            return NumberTokenHelper(c, startLine, startColumn);
        }
        else if (char.IsLetter(c) || c == '_')
        {
            return LetterTokenHelper(c, startLine, startColumn);
        }
        else
        {
            throw new LexerException("Unexpected character", startLine, startColumn);
        }


    }

    private Token LetterTokenHelper(char c, int startLine, int startColumn)
    {
        StringBuilder tokenLexema = new StringBuilder(c.ToString());

        while (true)
        {
            char nextChar = Peek();
            if (char.IsLetter(nextChar) || char.IsDigit(nextChar) || nextChar == '_')
            {
                tokenLexema.Append(Advance());
            }
            else if (char.IsWhiteSpace(nextChar) || nextChar == '\0' || nextChar == '\n' || LanguageSymbols.Contains(nextChar))
            {
                break;
            }
            else
            {
                throw new LexerException("Invalid character", startLine, startColumn);
            }
        }

        string wordString = tokenLexema.ToString();

        switch (wordString)
        {
            case "var":
                return new Token(TokenType.Var, wordString, startLine, startColumn);
            case "type":
                return new Token(TokenType.Type, wordString, startLine, startColumn);
            case "is":
                return new Token(TokenType.Is, wordString, startLine, startColumn);
            case "record":
                return new Token(TokenType.Record, wordString, startLine, startColumn);
            case "end":
                return new Token(TokenType.End, wordString, startLine, startColumn);
            case "array":
                return new Token(TokenType.Array, wordString, startLine, startColumn);
            case "while":
                return new Token(TokenType.While, wordString, startLine, startColumn);
            case "loop":
                return new Token(TokenType.Loop, wordString, startLine, startColumn);
            case "for":
                return new Token(TokenType.For, wordString, startLine, startColumn);
            case "in":
                return new Token(TokenType.In, wordString, startLine, startColumn);
            case "reverse":
                return new Token(TokenType.Reverse, wordString, startLine, startColumn);
            case "if":
                return new Token(TokenType.If, wordString, startLine, startColumn);
            case "then":
                return new Token(TokenType.Then, wordString, startLine, startColumn);
            case "else":
                return new Token(TokenType.Else, wordString, startLine, startColumn);
            case "print":
                return new Token(TokenType.Print, wordString, startLine, startColumn);
            case "routine":
                return new Token(TokenType.Routine, wordString, startLine, startColumn);
            case "true":
                return new Token(TokenType.True, wordString, startLine, startColumn);
            case "false":
                return new Token(TokenType.False, wordString, startLine, startColumn);
            case "not":
                return new Token(TokenType.Not, wordString, startLine, startColumn);
            case "and":
                return new Token(TokenType.And, wordString, startLine, startColumn);
            case "or":
                return new Token(TokenType.Or, wordString, startLine, startColumn);
            case "xor":
                return new Token(TokenType.Xor, wordString, startLine, startColumn);
            case "integer":
                return new Token(TokenType.Integer, wordString, startLine, startColumn);
            case "real":
                return new Token(TokenType.Real, wordString, startLine, startColumn);
            case "boolean":
                return new Token(TokenType.Boolean, wordString, startLine, startColumn);
            default:
                return new Token(TokenType.Identifier, wordString, startLine, startColumn);
        }
    }

    private Token NumberTokenHelper(char c, int startLine, int startColumn)
    {
        StringBuilder tokenLexema = new StringBuilder(c.ToString());

        //Iterate and build a token until the next character is a whitespace, end of file, semicolon or new line
        while (true)
        {
            char nextChar = Peek();
            if (char.IsDigit(nextChar) || nextChar == '.')
            {
                tokenLexema.Append(Advance());
            }
            else if (char.IsWhiteSpace(nextChar) || nextChar == '\0' || nextChar == '\n' || nextChar == ';' || IsOperator(nextChar) || IsBracket(nextChar))
            {
                break;
            }
            else
            {
                throw new LexerException($"Invalid character '{nextChar}' in numeric literal", startLine, startColumn);
            }
        }

        string numericString = tokenLexema.ToString();
        if (numericString.Contains('.'))
        {
            if (numericString.Count(c => c == '.') != 1)
            {
                throw new LexerException("Too many dots in a real number", startLine, startColumn);
            }
            return new Token(TokenType.RealLiteral, numericString, startLine, startColumn);
        }
        else
        {
            return new Token(TokenType.IntegerLiteral, numericString, startLine, startColumn);
        }
    }

    private bool IsOperator(char c)
    {
        return c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '<' || c == '>' || c == '=';
    }

    private bool IsBracket(char c) {
        return c == '(' || c == ')' || c == '[' || c == ']';
    }

    private Token SymbolTokenHelper(char c, int startLine, int startColumn)
    {
        switch (c)
        {
            case '(':
                return new Token(TokenType.LParen, c.ToString(), startLine, startColumn);
            case ')':
                return new Token(TokenType.RParen, c.ToString(), startLine, startColumn);
            case '[':
                return new Token(TokenType.LBracket, c.ToString(), startLine, startColumn);
            case ']':
                return new Token(TokenType.RBracket, c.ToString(), startLine, startColumn);
            case '.':
                return new Token(TokenType.Dot, c.ToString(), startLine, startColumn);
            case ',':
                return new Token(TokenType.Comma, c.ToString(), startLine, startColumn);
            case ';':
                return new Token(TokenType.Semicolon, c.ToString(), startLine, startColumn);
            case ':':
                if (Peek() == '=')
                {
                    Advance();
                    return new Token(TokenType.Assign, c.ToString() + _source[_position - 1].ToString(), startLine, startColumn);
                }
                else
                {
                    return new Token(TokenType.Colon, c.ToString(), startLine, startColumn);
                }
            case '+':
                return new Token(TokenType.Plus, c.ToString(), startLine, startColumn);
            case '-':
                return new Token(TokenType.Minus, c.ToString(), startLine, startColumn);
            case '*':
                return new Token(TokenType.Multiply, c.ToString(), startLine, startColumn);
            case '/':
                if (Peek() == '=')
                {
                    Advance();
                    return new Token(TokenType.NotEqual, c.ToString() + _source[_position - 1].ToString(), startLine, startColumn);
                }
                return new Token(TokenType.Divide, c.ToString(), startLine, startColumn);
            case '%':
                return new Token(TokenType.Modulo, c.ToString(), startLine, startColumn);
            case '<':
                if (Peek() == '=')
                {
                    Advance();
                    return new Token(TokenType.LessEqual, c.ToString() + _source[_position - 1].ToString(), startLine, startColumn);
                }
                else
                {
                    return new Token(TokenType.Less, c.ToString(), startLine, startColumn);
                }
            case '>':
                if (Peek() == '=')
                {
                    Advance();
                    return new Token(TokenType.GreaterEqual, c.ToString() + _source[_position - 1].ToString(), startLine, startColumn);
                }
                else
                {
                    return new Token(TokenType.Greater, c.ToString(), startLine, startColumn);
                }
            case '=':
                return new Token(TokenType.Equal, c.ToString(), startLine, startColumn);

            default:
                throw new LexerException("Invalid symbol", startLine, startColumn);
        }
    }

    private bool IsEndOfFile()
    {
        return _position >= _source.Length;
    }

    private char Advance()
    {

        if (IsEndOfFile())
        {
            return '\0';
        }

        char c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private void SkipWhitespacesAndComments()
    {
        while (!IsEndOfFile())
        {
            char c = Peek();

            if (char.IsWhiteSpace(c) && c != '\n')
            {
                Advance();
            }
            else if (c == '/' && Peek(1) == '/')
            {
                while (!IsEndOfFile() && Peek() != '\n')
                    Advance();
                Advance();
            }
            else
            {
                break;
            }
        }
    }


    /**
    Checkout the next character
    **/
    private char Peek(int offset = 0)
    {
        int pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }
}