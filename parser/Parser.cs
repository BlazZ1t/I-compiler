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
                if (Match(TokenType.Type))
                {
                    programNode.declarations.Add(ParseTypeDeclaration());
                    SkipSeparator();
                }
                else if (Match(TokenType.Var))
                {
                    programNode.declarations.Add(ParseVariableDeclaration());
                    SkipSeparator();
                }
                else if (Match(TokenType.Routine))
                {
                    programNode.declarations.Add(ParseRoutineDeclaration());
                }
                else
                {
                    throw new ParserException("Only declarations can be performed globaly", Peek().getLine(), Peek().getColumn());
                }

            }

            return programNode;
        }
        //DECLARATIONS ---------------------------------------
        private TypeDeclarationNode ParseTypeDeclaration()
        {
            Token typeIdentifierToken = MatchAdvance(TokenType.Identifier, "Expected an identifier");
            MatchAdvance(TokenType.Is, "Expected is after type identifier");
            TypeNode typeNode = ParseType();
            return new TypeDeclarationNode(typeIdentifierToken.getLexeme(), typeNode);
        }

        private VariableDeclarationNode ParseVariableDeclaration()
        {
            Token identifierToken = MatchAdvance(TokenType.Identifier, "Expected an identifier");

            if (Match(TokenType.Is))
            {
                ExpressionNode initializer = ParseExpression();
                return new VariableDeclarationNode(identifierToken.getLexeme(), initializer: initializer);
            }
            else if (Match(TokenType.Colon))
            {
                TypeNode type = ParseType();
                if (Match(TokenType.Is))
                {
                    ExpressionNode initializer = ParseExpression();
                    return new VariableDeclarationNode(identifierToken.getLexeme(), type, initializer);
                }
                else
                {
                    return new VariableDeclarationNode(identifierToken.getLexeme(), type);
                }
            }
            else
            {
                throw new ParserException("Expected an initializer or type reference", Peek().getLine(), Peek().getColumn());
            }
        }

        private RoutineDeclarationNode ParseRoutineDeclaration()
        {
            Token identifierToken = MatchAdvance(TokenType.Identifier, "Expected an identifier");
            MatchAdvance(TokenType.LParen, "Expected '(' before arguments");
            List<VariableDeclarationNode> parameters = new();
            if (!Check(TokenType.RParen))
            {
                do
            {
                parameters.Add(ParseVariableDeclaration());
            } while (Match(TokenType.Comma));
            }
            MatchAdvance(TokenType.RParen, "Expected ')' after arguments");
            TypeNode? returnType = null;
            if (Match(TokenType.Colon))
            {
                returnType = ParseType();
            }
            if (Check(TokenType.Semicolon) || Check(TokenType.NewLine))
            {
                SkipSeparator();
                return new RoutineDeclarationNode(identifierToken.getLexeme(), parameters, returnType);
            }
            else if (Match(TokenType.RoutineExpression))
            {
                ExpressionNode expression = ParseExpression();
                SkipSeparator();
                return new RoutineDeclarationNode(identifierToken.getLexeme(), parameters, returnType, new ExpressionRoutineBodyNode(expression));
            }
            else if (Match(TokenType.Is))
            {
                SkipSeparator();
                List<Node> body = ParseSimpleBody();
                return new RoutineDeclarationNode(identifierToken.getLexeme(), parameters, returnType, new BlockRoutineBodyNode(body));
            }
            else
            {
                throw new ParserException("Expected either a body or a separator");
            }
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

            return new RoutineCallNode(identifier.getLexeme(), args, identifier.getLine(), identifier.getColumn());
        }

        List<Node> ParseSimpleBody()
        {
            List<Node> resultBody = new();
            while (!Match(TokenType.End) && !Check(TokenType.Else))
            {
                if (Match(TokenType.While))
                {
                    resultBody.Add(ParseWhileLoopStatement());
                }
                else if (Match(TokenType.For))
                {
                    resultBody.Add(ParseForLoopStatement());
                }
                else if (Match(TokenType.If))
                {
                    resultBody.Add(ParseIfStatement());
                }
                else if (Match(TokenType.Print))
                {
                    resultBody.Add(ParsePrintStatement());
                    SkipSeparator();
                }
                else if (Match(TokenType.Type))
                {
                    resultBody.Add(ParseTypeDeclaration());
                    SkipSeparator();
                }
                else if (Match(TokenType.Var))
                {
                    resultBody.Add(ParseVariableDeclaration());
                    SkipSeparator();
                }
                else if (Check(TokenType.Identifier))
                {
                    Token id = Advance();
                    if (Match(TokenType.LParen))
                    {
                        resultBody.Add(ParseRoutineCall(id));
                    }
                    else
                    {
                        ModifiablePrimaryNode modifiablePrimary = ParseModifiablePrimary(id);
                        MatchAdvance(TokenType.Assign, "Expected an assignment");
                        ExpressionNode value = ParseExpression();
                        resultBody.Add(new AssignmentNode(modifiablePrimary, value, id.getLine(), id.getColumn()));
                    }
                    SkipSeparator();
                }
                else if (Check(TokenType.Return))
                {
                    Token returnToken = Advance();
                    ExpressionNode expression = ParseExpression();
                    resultBody.Add(new ReturnStatementNode(expression, returnToken.getLine(), returnToken.getColumn()));
                    SkipSeparator();
                }
                else
                {
                    throw new ParserException("Unexpected token", Peek().getLine(), Peek().getColumn());
                }
            }
            if (!Check(TokenType.Else))
            {
                SkipSeparator();   
            }
            return resultBody;
        }

        WhileLoopNode ParseWhileLoopStatement()
        {
            ExpressionNode condition = ParseExpression();
            MatchAdvance(TokenType.Loop, "Expected 'loop' keyword");
            SkipSeparator();
            List<Node> body = ParseSimpleBody();
            return new WhileLoopNode(condition, body);
        }

        ForLoopNode ParseForLoopStatement()
        {
            Token iteratorToken = MatchAdvance(TokenType.Identifier, "Expected an iterator variable");
            MatchAdvance(TokenType.In, "Expected 'in'");
            ExpressionNode rangeStart = ParseExpression();
            RangeNode rangeNode = new RangeNode(rangeStart);
            if (Match(TokenType.DoubleDot))
            {
                ExpressionNode rangeEnd = ParseExpression();
                rangeNode.End = rangeEnd;
            }
            bool reverse = Match(TokenType.Reverse);
            MatchAdvance(TokenType.Loop, "Expected 'loop' keyword");
            SkipSeparator();
            List<Node> body = ParseSimpleBody();
            return new ForLoopNode(iteratorToken.getLexeme(), rangeNode, reverse, body);
        }

        PrintStatementNode ParsePrintStatement()
        {
            Token printToken = Peek(-1);
            List<ExpressionNode> printArgs = new();
            do
            {
                printArgs.Add(ParseExpression());
            } while (Match(TokenType.Comma));
            return new PrintStatementNode(printArgs, printToken.getLine(), printToken.getColumn());
        }

        IfStatementNode ParseIfStatement()
        {
            Token ifToken = Peek(-1);
            ExpressionNode condition = ParseExpression();
            MatchAdvance(TokenType.Then, "Expected 'then'");
            SkipSeparator();
            List<Node> body = ParseSimpleBody();
            List<Node>? elseBody = null;
            if (Match(TokenType.Else))
            {
                SkipSeparator();
                elseBody = ParseSimpleBody();
            }
            return new IfStatementNode(condition, body, elseBody, ifToken.getLine(), ifToken.getColumn());
        }

        // TYPE REFERENCES AND DEFINITIONS
        TypeNode ParseType()
        {
            Token typeKeyword = Peek();
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
                        return new PrimitiveTypeNode(PrimitiveType.Integer, typeKeyword.getLine(), typeKeyword.getColumn());

                    case TokenType.Real:
                        return new PrimitiveTypeNode(PrimitiveType.Real, typeKeyword.getLine(), typeKeyword.getColumn());

                    case TokenType.Boolean:
                        return new PrimitiveTypeNode(PrimitiveType.Boolean, typeKeyword.getLine(), typeKeyword.getColumn());

                    default:
                        throw new ParserException("Primitive type parsing error", typeToken.getLine(), typeToken.getColumn());
                }
            }
            else if (typeToken.getTokenType() == TokenType.Identifier)
            {
                return new UserTypeNode(typeToken.getLexeme(), typeKeyword.getLine(), typeKeyword.getColumn());
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
            Token tokenKeyword = Peek(-1);
            MatchAdvance(TokenType.LBracket, "Expected '['");
            ExpressionNode size = ParseExpression();
            MatchAdvance(TokenType.RBracket, "Expected ']' after an expression");
            TypeNode type = ParseType();
            return new ArrayTypeNode(type, size, tokenKeyword.getLine(), tokenKeyword.getColumn());
        }

        RecordTypeNode ParseRecordDeclaration()
        {
            Token tokenKeyword = Peek(-1);
            List<VariableDeclarationNode> variables = new();
            SkipSeparator();
            while (!Match(TokenType.End))
            {
                MatchAdvance(TokenType.Var, "Expected 'var'");
                variables.Add(ParseVariableDeclaration());
                SkipSeparator();
            }
            return new RecordTypeNode(variables, tokenKeyword.getLine(), tokenKeyword.getColumn());
        }

        // EXPRESSION PARSING ---------------------------------------------------------- 
        private ExpressionNode ParseExpression()
        {
            ExpressionNode left = ParseRelation();

            while (Check(TokenType.And) || Check(TokenType.Or) || Check(TokenType.Xor))
            {
                Token op = Advance();
                ExpressionNode right = ParseRelation();
                left = new BinaryExpressionNode(TokenToOperator(op), left, right, op.getLine(), op.getColumn());
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
                left = new BinaryExpressionNode(TokenToOperator(op), left, right, op.getLine(), op.getColumn());
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
                left = new BinaryExpressionNode(TokenToOperator(op), left, right, op.getLine(), op.getColumn());
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
                left = new BinaryExpressionNode(TokenToOperator(op), left, right, op.getLine(), op.getColumn());
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
                     literal.getTokenType() == TokenType.IntegerLiteral ? PrimitiveType.Integer : PrimitiveType.Real,
                     literal.getLine(), literal.getColumn()), op.getLine(), op.getColumn());
                }
                else if (Check(TokenType.Identifier))
                {
                    Token id = Advance();

                    if (Match(TokenType.LParen))
                    {
                        return new UnaryExpressionNode(TokenToUnaryOperator(op), ParseRoutineCall(id), op.getLine(), op.getColumn());
                    }

                    return new UnaryExpressionNode(TokenToUnaryOperator(op), ParseModifiablePrimary(id), op.getLine(), op.getColumn());
                }
                else
                {
                    throw new ParserException("Expected integer or real literal after a unary operator", Peek().getLine(), Peek().getColumn());
                }
            }
            else if (Check(TokenType.IntegerLiteral) || Check(TokenType.RealLiteral))
            {
                Token literal = Advance();
                return new LiteralNode(literal.getLexeme(),
                     literal.getTokenType() == TokenType.IntegerLiteral ? PrimitiveType.Integer : PrimitiveType.Real,
                     literal.getLine(), literal.getColumn());
            }
            else if (Check(TokenType.True) || Check(TokenType.False))
            {
                Token booleanLiteral = Advance();
                return new LiteralNode(booleanLiteral.getLexeme(), PrimitiveType.Boolean, booleanLiteral.getLine(), booleanLiteral.getColumn());
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
                    node.AccessPart.Add(new FieldAccess(fieldToken.getLexeme(), fieldToken.getLine(), fieldToken.getColumn()));
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
            else if (Check(TokenType.EOF))
            {
                return;
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