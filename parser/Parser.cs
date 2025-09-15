using System.ComponentModel.Design;

namespace ImperativeLang.SyntaxAnalyzer
{
    class Parser
    {
        private List<Token> Tokens;

        private int position = 0;

        private ProgramNode ProgramNode = new ProgramNode();

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
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
                        ProgramNode.variables.Add(ParseVariableDeclaration());
                        break;

                    default:
                        throw new ParserException("Action rather than declaration in global scope", token.getLine(), token.getColumn());
                }
            }

            return ProgramNode;
        }

        RoutineDeclarationNode ParseRoutineDeclaration()
        {
            Token identifierToken = Advance();

            if (Match(TokenType.LParen))
            {
                List<ParameterNode> parameters = new();
                TypeNode? returnType = null;
                while (!Match(TokenType.RParen))
                {
                    parameters.Add(ParseParameter());
                }

                if (Match(TokenType.Colon))
                {
                    returnType = ParseType();
                }
                if (Match(TokenType.Is))
                {
                    BlockRoutineBodyNode body = ParseBlockBody();
                    SkipSeparator();
                    return new RoutineDeclarationNode(identifierToken.getLexeme(), parameters, returnType, body);
                }
                else if (Match(TokenType.RoutineExpression))
                {
                    ExpressionRoutineBodyNode body = ParseExpressionBody();
                    SkipSeparator();
                    return new RoutineDeclarationNode(identifierToken.getLexeme(), parameters, returnType, body);
                }
                else
                {
                    throw new ParserException("Unexpected routine body structure", Peek().getLine(), Peek().getColumn());
                }
            }
            else
            {
                throw new ParserException("Expected '('", Peek().getLine(), Peek().getColumn());
            }
        }

        ExpressionRoutineBodyNode ParseExpressionBody() {
            return new ExpressionRoutineBodyNode(ParseExpression());
        }

        BlockRoutineBodyNode ParseBlockBody()
        {
            List<AstNode> bodyContents = new();
            while (!Match(TokenType.End))
            {
                
            }
            return new BlockRoutineBodyNode(bodyContents);
        }

        ParameterNode ParseParameter()
        {
            TypeNode type = ParseType();
            Token identifierToken = Advance();
            if (!Check(TokenType.Identifier))
            {
                throw new ParserException("Expected an identifier", Peek().getLine(), Peek().getColumn());
            }
            Consume(TokenType.Comma, "Expected a comma after a parameter");
            return new ParameterNode(identifierToken.getLexeme(), type);
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
                return ParseArrayDeclaration();
            }
            else
            {
                throw new ParserException("Unexpected type", typeToken.getLine(), typeToken.getColumn());
            }
        }

        ArrayTypeNode ParseArrayDeclaration()
        {
            Consume(TokenType.LBracket, "Expected '['");
            ExpressionNode size = ParseExpression();
            Consume(TokenType.RBracket, "Expected ']' after an expression");
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

        VariableDeclarationNode ParseVariableDeclaration()
        {
            Token identifierToken = Advance();

            if (Match(TokenType.Identifier))
            {
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
                    throw new ParserException("Expected an initalizer or type reference", Peek().getLine(), Peek().getColumn());
                }
            }
            else
            {
                throw new ParserException("Expected an identifier", Peek().getLine(), Peek().getColumn());
            }
        }


        ExpressionNode ParseExpression()
        {
            return ParseRelationChain();
        }

        ExpressionNode ParseRelationChain()
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

        ExpressionNode ParseRelation()
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

        ExpressionNode ParseSimple()
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

        ExpressionNode ParseFactor()
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

        ExpressionNode ParseSummand()
        {
            if (Check(TokenType.LParen))
            {
                Advance();
                ExpressionNode expr = ParseExpression();
                Consume(TokenType.RParen, "Expected ')' after an expression");
                return expr;
            }

            return ParsePrimary();
        }

        ExpressionNode ParsePrimary()
        {
            if (Check(TokenType.Plus) || Check(TokenType.Minus) || Check(TokenType.Not))
            {
                Token op = Advance();
                ExpressionNode right = ParsePrimary();
                return new UnaryExpressionNode(TokenToUnaryOperator(op), right);
            }

            if (Check(TokenType.IntegerLiteral) || Check(TokenType.RealLiteral) ||
                Check(TokenType.True) || Check(TokenType.False))
            {
                Token literal = Advance();
                PrimitiveType primitiveType;
                if (Check(TokenType.True) || Check(TokenType.False))
                {
                    primitiveType = PrimitiveType.Boolean;
                }
                else if (Check(TokenType.IntegerLiteral))
                {
                    primitiveType = PrimitiveType.Integer;
                }
                else
                {
                    primitiveType = PrimitiveType.Real;
                }
                return new LiteralNode(literal.getLexeme(), primitiveType);
            }

            if (Check(TokenType.Identifier))
            {
                Token id = Advance();

                if (Check(TokenType.LParen))
                {
                    Advance();
                    List<ExpressionNode> args = new();
                    if (!Check(TokenType.RParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RParen, "Expected ')' after arguments");

                    return new RoutineCallNode(id.getLexeme(), args);
                }
                else
                {
                    return new IdentifierNode(id.getLexeme());
                }
            }

            throw new ParserException("Unexpected token in expression", Peek().getLine(), Peek().getColumn());
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

        private void SkipSeparator()
        {
            if (Match(TokenType.NewLine))
            {
                return;
            }

            if (Match(TokenType.Semicolon))
            {
                Match(TokenType.NewLine);
                return;
            }
            else
            {
                throw new ParserException("Expected a separator", Peek().getLine(), Peek().getColumn());
            }
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new ParserException(message, Peek().getLine(), Peek().getColumn());
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