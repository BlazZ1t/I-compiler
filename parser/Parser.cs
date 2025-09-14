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
                Token token = Advance();
                switch (token.getTokenType())
                {
                    case TokenType.Routine:
                        ProgramNode.routines.Add(ParseRoutineDeclaration());
                        break;
                    case TokenType.Type:
                        ProgramNode.types.Add(ParseTypeDeclaration());
                        break;
                    case TokenType.Var:
                        ProgramNode.variables.Add(ParseVariable());
                        break;

                    default:
                        throw new ParserException("Action rather than declaration in global scope", token.getLine(), token.getColumn());
                }
            }

            return ProgramNode;
        }

        RoutineDeclarationNode ParseRoutineDeclaration()
        {
            //TODO: Routine
        }

        TypeDeclarationNode ParseTypeDeclaration()
        {
            Token typeIdentifierToken = Advance();

            if (typeIdentifierToken.getTokenType() == TokenType.Identifier)
            {
                string typeIdentifier = typeIdentifierToken.getLexeme();
                Token isToken = Advance();
                if (isToken.getTokenType() == TokenType.Is)
                {
                    TypeNode typeNode = ParseType();
                    return new TypeDeclarationNode(typeIdentifier, typeNode);
                }
                else
                {
                    throw new ParserException("Expected is after an identifier", isToken.getLine(), isToken.getColumn());
                }
            }
            else
            {
                throw new ParserException("Expected an identifier token", typeIdentifierToken.getLine(), typeIdentifierToken.getColumn());
            }
        }

        TypeNode ParseType() {
            Token typeToken = Advance();
            if (
                typeToken.getTokenType() == TokenType.Integer ||
                typeToken.getTokenType() == TokenType.Real ||
                typeToken.getTokenType() == TokenType.Boolean
              )
            {
                switch (typeToken.getTokenType())
                {
                    case TokenType.Integer:
                        return new PrimitiveTypeNode(PrimitiveType.Integer);

                    case TokenType.Real:
                        return new PrimitiveTypeNode(PrimitiveType.Real);

                    case TokenType.Boolean:
                        return new PrimitiveTypeNode(PrimitiveType.Boolean);

                    default:
                        throw new ParserException("Primitive type parsing error", typeToken.getLine(), typeToken.getColumn());
                }
            }
            else if (typeToken.getTokenType() == TokenType.Identifier)
            {
                return new UserTypeNode(typeToken.getLexeme());
            }
            else if (typeToken.getTokenType() == TokenType.Record)
            {
                //Handle record type node
            }
            else if (typeToken.getTokenType() == TokenType.Array)
            {
                //Handle array type node
            }
            else
            {
                throw new ParserException("Unexpected type", typeToken.getLine(), typeToken.getColumn());
            }
        }

        VariableDeclarationNode ParseVariable()
        {
            //TODO: Variable
        }

        private Token Advance()
        {
            Token token = Tokens[position];
            if (token.getTokenType() != TokenType.EOF)
            {
                position++;
            }
            return token;
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