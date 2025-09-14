namespace ImperativeLang.SyntaxAnalyzer
{
    class Parser
    {
        private List<Token> Tokens;

        private int position = 0;

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
        }

        public ProgramNode getAST()
        {
            ProgramNode programNode = new ProgramNode();

            while (Tokens[position].getTokenType() != TokenType.EOF)
            {
                Token token = Tokens[position];
                switch (token.getTokenType())
                {
                    case TokenType.Routine:

                        break;
                    case TokenType.Type:
                        break;

                    case TokenType.Var:
                        break;

                    default:
                        throw new ParserException("Action rather than declaration in global scope", token.getLine(), token.getColumn());
                }
            }

            return programNode;
        }

        RoutineDeclarationNode ParseRoutine()
        {
            //TODO: Routine
        }

        TypeDeclarationNode ParseType()
        {
            
        }

        VariableDeclarationNode ParseVariable()
        {
            string identifier;

            Token identifierToken = Peek(1);
            if (identifierToken.getTokenType() == TokenType.EOF) {
                throw new ParserException($"Incomplete variable declaration", Tokens[position].getLine(), Tokens[position].getColumn());
            } else if (identifierToken.getTokenType() != TokenType.Identifier) {
                throw new ParserException($"Icorrect identifier in variable declaration '{identifierToken.getLexeme}'", identifierToken.getLine(), identifierToken.getColumn());
            }

            identifier = identifierToken.getLexeme();

            Token separatorToken = Peek(2);

            if (separatorToken.getTokenType() == TokenType.EOF) {
                throw new ParserException($"Incomplete variable declaration", identifierToken.getLine(), identifierToken.getColumn());
            }

            if (separatorToken.getTokenType() == TokenType.Colon)
            {
                //TODO: find Type
            }
            else if (separatorToken.getTokenType() == TokenType.Is)
            {
                //TODO: find Expression
            }
            else
            {
                throw new ParserException($"Expected ':' or 'is' after variable identifier but found '{separatorToken.getLexeme}'", separatorToken.getLine(), separatorToken.getColumn());
            }

            return null;
        }

        

        private Token Advance()
        {
            if (Tokens[position].getTokenType() != TokenType.EOF)
            {
                position++;
            }
            return Tokens[position];
        }

        private Token Peek(int offset = 0)
        {
            if (position + offset < Tokens.Count)
            {
                return Tokens[position + offset];
            }
            else
            {
                return Tokens.Last();
            }
        }
    }
}