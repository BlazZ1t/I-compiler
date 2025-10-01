using System.Diagnostics;

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



            return programNode;
        }


        // HELPER METHODS --------------------------------------------------------------------
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

        private bool Check(TokenType type)
        {
            return Peek().getTokenType() == type;
        }

        private bool Match(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        private void MatchWithException(TokenType type, string message)
        {
            if (!Check(type)) throw new ParserException(message, Peek().getLine(), Peek().getColumn());
            Advance();
        }

        private void SkipSeparator()
        {
            if (Match(TokenType.NewLine))
            {
                return;
            }

            if (Match(TokenType.Semicolon))
            {
                Match(TokenType.NewLine);
            }
            else
            {
                throw new ParserException("Expected a separator", Peek().getLine(), Peek().getColumn());
            }
        }

        static Operator TokenToOperator(Token token)
        {
            return token.getTokenType() switch
            {
                TokenType.Plus => Operator.Plus,
                TokenType.Minus => Operator.Minus,
                TokenType.Multiply => Operator.Multiply,
                TokenType.Divide => Operator.Divide,
                TokenType.Modulo => Operator.Modulo,

                TokenType.Less => Operator.Less,
                TokenType.LessEqual => Operator.LessEqual,
                TokenType.Greater => Operator.Greater,
                TokenType.GreaterEqual => Operator.GreaterEqual,
                TokenType.Equal => Operator.Equal,
                TokenType.NotEqual => Operator.NotEqual,

                TokenType.Not => Operator.Not,
                TokenType.And => Operator.And,
                TokenType.Or => Operator.Or,
                TokenType.Xor => Operator.Xor,

                _ => throw new ParserException(
                        $"Unexpected token '{token.getLexeme()}' as operator",
                        token.getLine(),
                        token.getColumn())
            };
        }
        
        static UnaryOperator TokenToUnaryOperator(Token token)
        {
            return token.getTokenType() switch
            {
                TokenType.Plus  => UnaryOperator.Plus,
                TokenType.Minus => UnaryOperator.Minus,
                TokenType.Not   => UnaryOperator.Not,

                _ => throw new ParserException(
                        $"Unexpected token '{token.getLexeme()}' as unary operator",
                        token.getLine(),
                        token.getColumn())
            };
        }
    }
}