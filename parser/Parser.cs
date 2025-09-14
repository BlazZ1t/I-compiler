namespace ImperativeLang.SyntaxAnalyzer
{
    class Parser
    {
        private List<Token> Tokens;

        private int position = 0;

        private ProgramNode ProgramNode = new ProgramNode();

        private AstNode currentNode;

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
            currentNode = ProgramNode;
        }

        public ProgramNode getAST()
        {

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

            return ProgramNode;
        }

        RoutineDeclarationNode ParseRoutine()
        {
            //TODO: Routine
        }

        TypeDeclarationNode ParseTypeDeclaration()
        {
            Token typeIdentifierToken = Peek(1);

            if (typeIdentifierToken.getTokenType() == TokenType.Identifier)
            {
                Advance();
                string typeIdentifier = Peek().getLexeme();
                Token isToken = Peek(1);
                if (isToken.getTokenType() == TokenType.Is)
                {
                    Advance();
                    //Parse TypeNode
                }
                else
                {
                    throw new ParserException("Expected is after an identifier", Peek(1).getLine(), Peek(1).getColumn())
                }
            }
            else
            {
                throw new ParserException("Expected an identifier token", Peek(1).getLine(), Peek(1).getColumn());
            }
        }

        TypeNode ParseType() {
            Advance();
            if (
                Tokens[position].getTokenType() == TokenType.Integer ||
                Tokens[position].getTokenType() == TokenType.Real ||
                Tokens[position].getTokenType() == TokenType.Boolean
              )
            {
                switch (Tokens[position].getTokenType())
                {
                    case TokenType.Integer:
                        return new PrimitiveTypeNode(PrimitiveType.Integer);

                    case TokenType.Real:
                        return new PrimitiveTypeNode(PrimitiveType.Real);

                    case TokenType.Boolean:
                        return new PrimitiveTypeNode(PrimitiveType.Boolean);

                    default:
                        throw new ParserException("Primitive type parsing error", Tokens[position].getLine(), Tokens[position].getColumn());
               } 
            }
        }

        VariableDeclarationNode ParseVariable()
        {
            //TODO: Variable
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