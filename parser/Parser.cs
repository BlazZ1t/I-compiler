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

        private Token? Peek(int offset = 0)
        {
            if (position + offset < Tokens.Count)
            {
                return Tokens[position + offset];
            }
            else
            {
                return null;
            }
        }
    }
}