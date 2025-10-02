using System.Diagnostics;
using System.Linq.Expressions;

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
                Token token = Advance();


            }

            return programNode;
        }
        //TYPE DECLARATIONS
        private TypeDeclarationNode ParseTypeDeclaration()
        {
            Token typeIdentifierToken = MatchAdvance(TokenType.Identifier, "Expected an identifier");
            MatchAdvance(TokenType.Is, "Expected is after type identifier");
            TypeNode typeNode = ParseType();
            return new TypeDeclarationNode(typeIdentifierToken.getLexeme(), typeNode);
        }

        //STATEMENTS ---------------------------------------------
        RoutineCallNode ParseRoutineCall(Token identifier)
        {
            List<ExpressionNode> args = new();

            if (!Check(TokenType.RParen))
            {
                do
                {
                    args.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            MatchAdvance(TokenType.RParen, "Expected ')' after arguments");

            return new RoutineCallNode(identifier.getLexeme(), args);
        }


        // TYPE REFERENCES AND DEFINITIONS
        TypeNode ParseType()
        {
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
                return ParseRecordDeclaration();
            }
            else if (typeToken.getTokenType() == TokenType.Array)
            {
                return ParseArrayTypeDeclaration();
            }
            else
            {
                throw new ParserException("Unexpected type", typeToken.getLine(), typeToken.getColumn());
            }
        }

        ArrayTypeNode ParseArrayTypeDeclaration()
        {
            MatchAdvance(TokenType.LBracket, "Expected '['");
            ExpressionNode size = ParseExpression();
            MatchAdvance(TokenType.RBracket, "Expected ']' after an expression");
            TypeNode type = ParseType();
            SkipSeparator();
            return new ArrayTypeNode(type, size);
        }

        RecordTypeNode ParseRecordDeclaration()
        {
            List<VariableDeclarationNode> variables = new();
            while (!Match(TokenType.End))
            {
                variables.Add(ParseVariableDeclaration());
            }
            SkipSeparator();
            return new RecordTypeNode(variables);
        }

        private VariableDeclarationNode ParseVariableDeclaration()
        {
            Token identifierToken = MatchAdvance(TokenType.Identifier, "Expected an identifier");

            if (Match(TokenType.Is))
            {
                ExpressionNode initializer = ParseExpression();
                SkipSeparator();
                return new VariableDeclarationNode(identifierToken.getLexeme(), initializer: initializer);
            }
            else if (Match(TokenType.Colon))
            {
                TypeNode type = ParseType();
                if (Match(TokenType.Is))
                {
                    ExpressionNode initializer = ParseExpression();
                    SkipSeparator();
                    return new VariableDeclarationNode(identifierToken.getLexeme(), type, initializer);
                }
                else
                {
                    SkipSeparator();
                    return new VariableDeclarationNode(identifierToken.getLexeme(), type);
                }
            }
            else
            {
                throw new ParserException("Expected an initializer or type reference", Peek().getLine(), Peek().getColumn());
            }
        }

        // EXPRESSION PARSING ---------------------------------------------------------- 
        private ExpressionNode ParseExpression()
        {
            ExpressionNode left = ParseRelation();

            while (Check(TokenType.And) || Check(TokenType.Or) || Check(TokenType.Xor))
            {
                Token op = Advance();
                ExpressionNode right = ParseRelation();
                left = new BinaryExpressionNode(TokenToOperator(op), left, right);
            }

            return left;
        }

        private ExpressionNode ParseRelation()
        {
            ExpressionNode left = ParseSimple();

            if (Check(TokenType.Less) || Check(TokenType.LessEqual) || Check(TokenType.Greater) ||
                Check(TokenType.GreaterEqual) || Check(TokenType.Equal) || Check(TokenType.NotEqual))
            {
                Token op = Advance();
                ExpressionNode right = ParseSimple();
                left = new BinaryExpressionNode(TokenToOperator(op), left, right);
            }

            return left;
        }

        private ExpressionNode ParseSimple()
        {
            ExpressionNode left = ParseFactor();

            while (Check(TokenType.Multiply) || Check(TokenType.Divide) || Check(TokenType.Modulo))
            {
                Token op = Advance();
                ExpressionNode right = ParseFactor();
                left = new BinaryExpressionNode(TokenToOperator(op), left, right);
            }

            return left;
        }

        private ExpressionNode ParseFactor()
        {
            ExpressionNode left = ParseSummand();

            while (Check(TokenType.Plus) || Check(TokenType.Minus))
            {
                Token op = Advance();
                ExpressionNode right = ParseSummand();
                left = new BinaryExpressionNode(TokenToOperator(op), left, right);
            }

            return left;
        }

        private ExpressionNode ParseSummand()
        {
            if (Match(TokenType.LParen))
            {
                ExpressionNode expression = ParseExpression();
                MatchAdvance(TokenType.RParen, "Expected ')' after an expression");
                return expression;
            }

            return ParsePrimary();
        }

        ExpressionNode ParsePrimary()
        {
            if (Check(TokenType.Plus) || Check(TokenType.Minus) || Check(TokenType.Not))
            {
                Token op = Advance();
                if (Check(TokenType.IntegerLiteral) || Check(TokenType.RealLiteral))
                {
                    if (op.getTokenType() == TokenType.Not && Check(TokenType.RealLiteral)) throw new ParserException("Unexpected unary operator for real literal");
                    Token literal = Advance();
                    return new UnaryExpressionNode(TokenToUnaryOperator(op), new LiteralNode(literal.getLexeme(),
                     literal.getTokenType() == TokenType.IntegerLiteral ? PrimitiveType.Integer : PrimitiveType.Real));
                }
                else
                {
                    throw new ParserException("Expected integer or real literal after a unary operator", Peek().getLine(), Peek().getColumn());
                }
            }
            else if (Check(TokenType.True) || Check(TokenType.False))
            {
                Token booleanLiteral = Advance();
                return new LiteralNode(booleanLiteral.getLexeme(), PrimitiveType.Boolean);
            }
            else if (Check(TokenType.Identifier))
            {
                Token id = Advance();

                if (Match(TokenType.LParen))
                {
                    return ParseRoutineCall(id);
                }
                
                return ParseModifiablePrimary(id);
            }
            else
            {
                throw new ParserException("Expected a primary in the expression", Peek().getLine(), Peek().getColumn());
            }
        }

        ModifiablePrimaryNode ParseModifiablePrimary(Token identifier)
        {
            var node = new ModifiablePrimaryNode(identifier.getLexeme());

            while (true)
            {
                if (Match(TokenType.Dot))
                {
                    Token fieldToken = MatchAdvance(TokenType.Identifier, "Expected an identifier");
                    node.AccessPart.Add(new FieldAccess(fieldToken.getLexeme()));
                }
                else if (Match(TokenType.LBracket))
                {
                    ExpressionNode index = ParseExpression();
                    MatchAdvance(TokenType.RBracket, "Expected ']' after array index expression");
                    node.AccessPart.Add(new ArrayAccess(index));
                }
                else
                {
                    break;
                }
            }

            return node;
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

        private Token MatchAdvance(TokenType type, string message)
        {
            if (!Check(type)) throw new ParserException(message, Peek().getLine(), Peek().getColumn());
            return Advance();
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