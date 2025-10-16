namespace ImperativeLang.SemanticalAnalyzer
{
    using ImperativeLang.SyntaxAnalyzer;

    class SemanticalAnalyzer
    {
        private ProgramNode AST;
        private Stack<Dictionary<string, Symbol>> Scope = new Stack<Dictionary<string, Symbol>>();


        public SemanticalAnalyzer(ProgramNode ast)
        {
            AST = ast;
        }

        public ProgramNode Analyze()
        {
            Scope.Push(new Dictionary<string, Symbol>()); // Global scope

            foreach (DeclarationNode declaration in AST.declarations)
            {
                if (declaration is VariableDeclarationNode variableDeclaration)
                {
                    AddVariableDeclaration(variableDeclaration);
                }
                else if (declaration is TypeDeclarationNode typeDeclaration)
                {
                    AddTypeDeclaration(typeDeclaration);
                }
            }

            return AST;
        }

        private void AddVariableDeclaration(VariableDeclarationNode variableDeclarationNode)
        {
            if (Scope.Peek().ContainsKey(variableDeclarationNode.Name))
            {
                throw new AnalyzerException($"Variable already declared: '{variableDeclarationNode.Name}'");
            }

            TypeInfo variableType;

            if (variableDeclarationNode.VarType == null)
            {
                variableType = ResolveExpression(variableDeclarationNode.Initializer!);
            }
            else
            {
                variableType = ResolveType(variableDeclarationNode.VarType);
                if (variableDeclarationNode.Initializer != null)
                {
                    if (!variableType.Equals(ResolveExpression(variableDeclarationNode.Initializer)))
                    {
                        throw new AnalyzerException("Invalid initializer type");
                    }
                }
            }

            Scope.Peek().Add(variableDeclarationNode.Name,
                 new VariableSymbol(variableDeclarationNode.Name, variableType, variableDeclarationNode.Initializer != null));
        }

        private void AddTypeDeclaration(TypeDeclarationNode typeDeclarationNode)
        {
            if (Scope.Peek().ContainsKey(typeDeclarationNode.Name))
            {
                throw new AnalyzerException($"Type already declared: '{typeDeclarationNode.Name}'");
            }

            Scope.Peek().Add(typeDeclarationNode.Name,
                new TypeSymbol(typeDeclarationNode.Name, ResolveType(typeDeclarationNode.Type)));
        }


        private TypeInfo ResolveExpression(ExpressionNode expression)
        {
            if (expression is BinaryExpressionNode binaryExpression)
            {
                TypeInfo left = ResolveExpression(binaryExpression.Left);
                TypeInfo right = ResolveExpression(binaryExpression.Right);
                if (left is PrimitiveTypeInfo leftType && right is PrimitiveTypeInfo rightType)
                {
                    switch (binaryExpression.Operator)
                    {
                        case Operator.Plus or Operator.Minus or Operator.Multiply or Operator.Modulo or Operator.Divide:
                            if (leftType.Type == PrimitiveType.Boolean || rightType.Type == PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Non numerical operands with numerical operator");
                            }
                            if (leftType.Type == PrimitiveType.Real || rightType.Type == PrimitiveType.Real || binaryExpression.Operator == Operator.Divide)
                            {
                                return new PrimitiveTypeInfo(PrimitiveType.Real);
                            }
                            else
                            {
                                return new PrimitiveTypeInfo(PrimitiveType.Integer);
                            }
                        case Operator.Less or Operator.LessEqual or Operator.Greater or Operator.GreaterEqual or Operator.Equal or Operator.NotEqual:
                            if (leftType.Type == PrimitiveType.Boolean || rightType.Type == PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Non numerical operands with numerical operator");
                            }
                            return new PrimitiveTypeInfo(PrimitiveType.Boolean);
                        default:
                            if (leftType.Type == PrimitiveType.Boolean && rightType.Type == PrimitiveType.Boolean)
                            {
                                return new PrimitiveTypeInfo(PrimitiveType.Boolean);
                            }
                            throw new AnalyzerException("Can't perform boolean operations on numerical values");
                    }
                }
                else
                {
                    throw new AnalyzerException("Invalid operand");
                }
            }
            else if (expression is LiteralNode literalNode)
            {
                return new PrimitiveTypeInfo(literalNode.Type);
            }
            else if (expression is UnaryExpressionNode unaryExpressionNode)
            {
                TypeInfo operandType = ResolveExpression(unaryExpressionNode.Operand);

                
            }
            return null;
        }

        private TypeInfo ResolveType(TypeNode type)
        {
            return null;
        }
    }
}