namespace ImperativeLang.SemanticalAnalyzerNS
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
                else if (declaration is RoutineDeclarationNode routineDeclaration)
                {
                    AddRoutineDeclaration(routineDeclaration);
                    TraverseRoutineBody(routineDeclaration);
                }
            }

            return AST;
        }
        
        private void TraverseRoutineBody(RoutineDeclarationNode routineDeclaration)
        {
            if (Scope.Peek()[routineDeclaration.Name] is RoutineSymbol routineSymbol)
            {
                if (routineDeclaration.Body != null)
                {
                    if (routineDeclaration.Body is BlockRoutineBodyNode blockRoutineBodyNode)
                    {
                        TraverseBody(routineSymbol.Parameters, blockRoutineBodyNode.Body, routineSymbol.ReturnType);
                    }
                    else if (routineDeclaration.Body is ExpressionRoutineBodyNode expressionRoutineBodyNode)
                    {
                        Scope.Push(new Dictionary<string, Symbol>());
                        foreach (var symbol in routineSymbol.Parameters)
                        {
                            Scope.Peek().Add(symbol.Name, symbol);
                        }   
                        TypeInfo expressionType = ResolveExpressionType(expressionRoutineBodyNode.Expression);
                        if (routineSymbol.ReturnType != null)
                        {
                            if (!expressionType.Equals(routineSymbol.ReturnType))
                            {
                                throw new AnalyzerException("Expression type doesn't match return type", expressionRoutineBodyNode.Line, expressionRoutineBodyNode.Column);
                            }
                        }
                        Scope.Pop();
                    }
                }                   
            }
            else
            {
                throw new AnalyzerException("Somehow the routine declaration was saved as something else");
            }
        }

        private void TraverseBody(List<VariableSymbol> declaredSymbols, List<Node> body, TypeInfo? returnType = null)
        //TODO: Add checking for return statements in each code path where applicable (where returnType != null)
        {
            Scope.Push(new Dictionary<string, Symbol>());
            foreach (var symbol in declaredSymbols)
            {
                Scope.Peek().Add(symbol.Name, symbol);
            }

            foreach (var node in body)
            {
                if (node is DeclarationNode declarationNode)
                {
                    if (declarationNode is VariableDeclarationNode variableDeclarationNode)
                    {
                        AddVariableDeclaration(variableDeclarationNode);
                    }
                    else if (declarationNode is TypeDeclarationNode typeDeclarationNode)
                    {
                        AddTypeDeclaration(typeDeclarationNode);
                    }
                    else if (declarationNode is RoutineDeclarationNode routineDeclarationNode)
                    {
                        throw new AnalyzerException("Can not declare nested routines", node.Line, node.Column);
                    }
                } else if (node is StatementNode statementNode)
                {
                    if (statementNode is ForLoopNode forLoopNode)
                    {
                        //TODO: Add checking for assignments to loop variable. It should be read only
                        if (forLoopNode.IsArrayTraversal)
                        {
                            //TODO: Fuck I can't read. Figure out array traversal with for loops
                        }
                        else if (ResolveExpressionType(forLoopNode.Range.Start) is PrimitiveTypeInfo rangeStart)
                        {
                            if (rangeStart.Type != PrimitiveType.Integer)
                            {
                                throw new AnalyzerException("Range values should be integers");
                            }
                            if (forLoopNode.Range.End != null)
                            {
                                if (ResolveExpressionType(forLoopNode.Range.End) is PrimitiveTypeInfo rangeEnd)
                                {
                                    if (rangeEnd.Type != PrimitiveType.Integer)
                                    {
                                        throw new AnalyzerException("Range values should be integers");
                                    }
                                }
                                if (TryEvaluateExpression(forLoopNode.Range.Start, out object rangeStartNum) && TryEvaluateExpression(forLoopNode.Range.End, out object rangeEndNum))
                                {
                                    if (!forLoopNode.Reverse && (int)rangeEndNum < (int)rangeStartNum)
                                    {
                                        System.Console.WriteLine($"Warning: Range end is smaller than range start at line {forLoopNode.Line}, column {forLoopNode.Column}");
                                    }
                                    if (forLoopNode.Reverse && (int)rangeEndNum > (int)rangeStartNum)
                                    {
                                        System.Console.WriteLine($"Warning: Range end is bigger than range start at line {forLoopNode.Line}, column {forLoopNode.Column}");
                                    }
                                }
                            }

                        }
                        else
                        {
                            throw new AnalyzerException("Range value is of wrong type", forLoopNode.Range.Start.Line, forLoopNode.Range.Start.Column);
                        }
                    }
                    else if (statementNode is WhileLoopNode whileLoopNode)
                    {
                        if (ResolveExpressionType(whileLoopNode.Condition) is PrimitiveTypeInfo conditionType)
                        {
                            if (conditionType.Type != PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Condition should be bool", whileLoopNode.Line, whileLoopNode.Column);
                            }

                            if (TryEvaluateExpression(whileLoopNode.Condition, out object condition))
                            {
                                if (condition is bool b)
                                {
                                    if (b) System.Console.WriteLine($"Warning: Infinite while loop at line: {whileLoopNode.Line}, column: {whileLoopNode.Column}");
                                }
                                else
                                {
                                    throw new AnalyzerException("Condition is not being evaluated to boolean value", whileLoopNode.Line, whileLoopNode.Column);
                                }
                            }

                            TraverseBody(new List<VariableSymbol>(), whileLoopNode.Body, returnType);
                        }
                        else
                        {
                            throw new AnalyzerException("Condition should be bool", whileLoopNode.Line, whileLoopNode.Column);
                        }
                    }
                    else if (statementNode is IfStatementNode ifStatementNode)
                    {
                        if (ResolveExpressionType(ifStatementNode.Condition) is PrimitiveTypeInfo conditionType)
                        {
                            if (conditionType.Type != PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Condition should be bool", ifStatementNode.Line, ifStatementNode.Column);
                            }

                            if (TryEvaluateExpression(ifStatementNode.Condition, out object condition))
                            {
                                if (condition is bool b)
                                {
                                    System.Console.WriteLine($"Warning: Condition is always {(b ? "True" : "False")} at line: {ifStatementNode.Line}, column: {ifStatementNode.Column}");
                                }
                                else
                                {
                                    throw new AnalyzerException("Condition is not being evaluated to boolean value", ifStatementNode.Line, ifStatementNode.Column);
                                }
                            }
                            TraverseBody(new List<VariableSymbol>(), ifStatementNode.ThenBody, returnType);

                            if (ifStatementNode.ElseBody != null && ifStatementNode.ElseBody.Count != 0)
                            {
                                TraverseBody(new List<VariableSymbol>(), ifStatementNode.ElseBody, returnType);
                            }
                        }
                        else
                        {
                            throw new AnalyzerException("Condition shold be bool", ifStatementNode.Line, ifStatementNode.Column);
                        }
                    }
                    else if (statementNode is RoutineCallStatementNode routineCallStatementNode)
                    {
                        Symbol? symbol = LookupSymbol(routineCallStatementNode.Call.Name);

                        if (symbol is RoutineSymbol routineSymbol)
                        {
                            if (routineCallStatementNode.Call.Arguments.Count != routineSymbol.Parameters.Count)
                            {
                                throw new AnalyzerException($"Expected {routineSymbol.Parameters.Count} arguments. Got {routineCallStatementNode.Call.Arguments.Count}.", routineCallStatementNode.Line, routineCallStatementNode.Column);
                            }

                            for (int i = 0; i < routineSymbol.Parameters.Count; i++)
                            {
                                if (!ResolveExpressionType(routineCallStatementNode.Call.Arguments[i]).Equals(routineSymbol.Parameters[i].Type))
                                {
                                    throw new AnalyzerException("Unexpected argument type", routineCallStatementNode.Call.Arguments[i].Line, routineCallStatementNode.Call.Arguments[i].Column);
                                }
                            }
                        }
                        else
                        {
                            throw new AnalyzerException("Can not call a non-routine object", routineCallStatementNode.Line, routineCallStatementNode.Column);
                        }
                    }
                    else if (statementNode is ReturnStatementNode returnStatementNode)
                    {
                        if (returnType == null && returnStatementNode.Value != null)
                        {
                            throw new AnalyzerException("Can not return anything here", returnStatementNode.Line, returnStatementNode.Column);
                        }

                        if (returnStatementNode.Value == null && returnType != null)
                        {
                            throw new AnalyzerException("Need a return value", returnStatementNode.Line, returnStatementNode.Column);
                        }

                        if (returnStatementNode.Value != null && returnType != null)
                        {
                            TypeInfo returnInfo = ResolveExpressionType(returnStatementNode.Value);

                            if (!returnInfo.Equals(returnType))
                            {
                                throw new AnalyzerException("Invalid return value", returnStatementNode.Value.Line, returnStatementNode.Value.Column);
                            }
                        }

                        if (!ReferenceEquals(node, body.Last()))
                        {
                            System.Console.WriteLine($"Warning: Unreachable code after return statement at line: {returnStatementNode.Line}, column: {returnStatementNode.Column}");
                        }

                        break;
                    }
                    else if (statementNode is AssignmentNode assignmentNode)
                    {
                        //TODO: Add checking for assignment of different types (e.g. integer to boolean)
                        if (!ResolveExpressionType(assignmentNode.Value).Equals(ResolveExpressionType(assignmentNode.Target)))
                        {
                            throw new AnalyzerException("Type mismatch", assignmentNode.Line, assignmentNode.Column);
                        }
                    }
                    else if (statementNode is PrintStatementNode printStatementNode)
                    {
                        foreach (var expression in printStatementNode.Expressions)
                        {
                            ResolveExpressionType(expression);
                        }
                    }
                }
            }

            Scope.Pop();
        }

        private void AddRoutineDeclaration(RoutineDeclarationNode routineDeclarationNode)
        {
            if (Scope.Peek().ContainsKey(routineDeclarationNode.Name))
            {
                if (Scope.Peek()[routineDeclarationNode.Name] is RoutineSymbol routineSymbol)
                {
                    if (!routineSymbol.IsForwardDeclared)
                    {
                        throw new AnalyzerException($"Routine '{routineDeclarationNode.Name}' already exists in the scope", routineDeclarationNode.Line, routineDeclarationNode.Column);
                    }
                } else
                {
                    throw new AnalyzerException("Something went terribely wrong with routine declarations", routineDeclarationNode.Line, routineDeclarationNode.Column);
                }
            }

            Scope.Peek().Add(routineDeclarationNode.Name, new RoutineSymbol(routineDeclarationNode.Name,
                routineDeclarationNode.ReturnType == null
                ? null
                : ResolveTypeFromTypeNodeReference(routineDeclarationNode.ReturnType), convertParameters(routineDeclarationNode.Parameters), routineDeclarationNode.Body == null));
        }
        
        private List<VariableSymbol> convertParameters(List<VariableDeclarationNode> variableDeclarationNodes)
        {
            List<VariableSymbol> result = new();
            foreach (var variableDeclarationNode in variableDeclarationNodes)
            {
                result.Add(
                    new VariableSymbol(variableDeclarationNode.Name,
                    ResolveTypeFromTypeNodeReference(variableDeclarationNode.VarType!),
                    variableDeclarationNode.Initializer != null)
                );
            }
            return result;
        }

        private void AddVariableDeclaration(VariableDeclarationNode variableDeclarationNode)
        {
            if (Scope.Peek().ContainsKey(variableDeclarationNode.Name))
            {
                throw new AnalyzerException($"Variable already declared: '{variableDeclarationNode.Name}'", variableDeclarationNode.Line, variableDeclarationNode.Column);
            }

            TypeInfo variableType;

            if (variableDeclarationNode.VarType == null)
            {
                variableType = ResolveExpressionType(variableDeclarationNode.Initializer!);
            }
            else
            {
                variableType = ResolveTypeFromTypeNodeReference(variableDeclarationNode.VarType);
                if (variableDeclarationNode.Initializer != null)
                {
                    if (!variableType.Equals(ResolveExpressionType(variableDeclarationNode.Initializer)))
                    {
                        throw new AnalyzerException("Invalid initializer type", variableDeclarationNode.Initializer.Line, variableDeclarationNode.Initializer.Column);
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
                throw new AnalyzerException($"Type already declared: '{typeDeclarationNode.Name}'", typeDeclarationNode.Line, typeDeclarationNode.Column);
            }

            Scope.Peek().Add(typeDeclarationNode.Name,
                new TypeSymbol(typeDeclarationNode.Name, ResolveTypeFromTypeNodeDeclarations(typeDeclarationNode.Type, typeDeclarationNode.Name)));
        }


        private TypeInfo ResolveExpressionType(ExpressionNode expression)
        {
            if (expression is BinaryExpressionNode binaryExpression)
            {
                TypeInfo left = ResolveExpressionType(binaryExpression.Left);
                TypeInfo right = ResolveExpressionType(binaryExpression.Right);
                if (left is PrimitiveTypeInfo leftType && right is PrimitiveTypeInfo rightType)
                {
                    switch (binaryExpression.Operator)
                    {
                        case Operator.Plus or Operator.Minus or Operator.Multiply or Operator.Modulo or Operator.Divide:
                            if (leftType.Type == PrimitiveType.Boolean || rightType.Type == PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Non numerical operands with numerical operator", binaryExpression.Line, binaryExpression.Column);
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
                                throw new AnalyzerException("Non numerical operands with numerical operator", binaryExpression.Line, binaryExpression.Column);
                            }
                            return new PrimitiveTypeInfo(PrimitiveType.Boolean);
                        default:
                            if (leftType.Type == PrimitiveType.Boolean && rightType.Type == PrimitiveType.Boolean)
                            {
                                return new PrimitiveTypeInfo(PrimitiveType.Boolean);
                            }
                            throw new AnalyzerException("Can't perform boolean operations on numerical values", binaryExpression.Line, binaryExpression.Column);
                    }
                }
                else
                {
                    throw new AnalyzerException("Invalid operand", binaryExpression.Line, binaryExpression.Column);
                }
            }
            else if (expression is LiteralNode literalNode)
            {
                return new PrimitiveTypeInfo(literalNode.Type);
            }
            else if (expression is UnaryExpressionNode unaryExpressionNode)
            {
                TypeInfo operandType = ResolveExpressionType(unaryExpressionNode.Operand);

                if (unaryExpressionNode.Operator == UnaryOperator.Minus || unaryExpressionNode.Operator == UnaryOperator.Plus)
                {
                    if (operandType is PrimitiveTypeInfo primitiveType && (primitiveType.Type == PrimitiveType.Integer || primitiveType.Type == PrimitiveType.Real))
                    {
                        return operandType;
                    }
                    else
                    {
                        throw new AnalyzerException("Invalid operand for mathematical expression", unaryExpressionNode.Line, unaryExpressionNode.Column);
                    }
                }
                else
                {
                    if (operandType is PrimitiveTypeInfo primitiveType && primitiveType.Type == PrimitiveType.Boolean)
                    {
                        return operandType;
                    }
                    else
                    {
                        throw new AnalyzerException("Invalid operand for logical not", unaryExpressionNode.Line, unaryExpressionNode.Column);
                    }
                }
            }
            else if (expression is RoutineCallNode routineCallNode)
            {
                Symbol? routineSymbol = LookupSymbol(routineCallNode.Name);

                if (routineSymbol != null && routineSymbol is RoutineSymbol routine)
                {
                    if (routine.ReturnType == null)
                    {
                        throw new AnalyzerException("Routines without return type cannot be used in expressions", routineCallNode.Line, routineCallNode.Column);
                    }
                    return routine.ReturnType;
                }
                else
                {
                    throw new AnalyzerException("Unidentified routine called", routineCallNode.Line, routineCallNode.Column);
                }
            }
            else if (expression is ModifiablePrimaryNode modifiablePrimaryNode)
            {
                Symbol? baseSymbol = LookupSymbol(modifiablePrimaryNode.BaseName);

                if (baseSymbol == null)
                {
                    throw new AnalyzerException("Unidentified variable", modifiablePrimaryNode.Line, modifiablePrimaryNode.Column);
                }
                if (baseSymbol is VariableSymbol variableSymbol)
                {
                    TypeInfo currentType = variableSymbol.Type;

                    if (modifiablePrimaryNode.AccessPart.Count() == 0)
                    {
                        return currentType;
                    }


                    foreach (var part in modifiablePrimaryNode.AccessPart)
                    {
                        if (part is FieldAccess field)
                        {
                            if (currentType is not RecordTypeInfo recordType)
                            {
                                throw new AnalyzerException($"Cannot access field '{field.Name}' on non-record types", field.Line, field.Column);
                            }

                            if (!recordType.Fields.TryGetValue(field.Name, out var fieldType))
                            {
                                throw new AnalyzerException($"Record has no field '{field.Name}'", field.Line, field.Column);
                            }

                            currentType = fieldType;
                        }
                        else if (part is ArrayAccess array)
                        {
                            if (currentType is not ArrayTypeInfo arrayType)
                            {
                                throw new AnalyzerException("Cannot access array part in non-array type", array.Line, array.Column);
                            }

                            var indexType = ResolveExpressionType(array.Index);
                            if (indexType is not PrimitiveTypeInfo { Type: PrimitiveType.Integer })
                            {
                                throw new AnalyzerException("Array index must be of type integer", array.Line, array.Column);
                            }

                            if (TryEvaluateExpression(array.Index, out object value))
                            {
                                if (value is int i)
                                {
                                    if (i > arrayType.Size)
                                    {
                                        throw new AnalyzerException("Index out of bounds", array.Line, array.Column);
                                    }
                                }
                            }

                            currentType = arrayType.Type;
                        }
                    }
                    return currentType;
                }
                else
                {
                    throw new AnalyzerException("Symbol is not a variable", modifiablePrimaryNode.Line, modifiablePrimaryNode.Column);
                }
            }
            else
            {
                throw new AnalyzerException("Unkown epxression type", expression.Line, expression.Column);
            }
        }

        private bool TryEvaluateExpression(ExpressionNode expression, out object value)
        {
            value = default!;

            switch (expression)
            {
                case LiteralNode literal:
                    value = literal.Value;
                    return true;

                case UnaryExpressionNode unary:
                    if (TryEvaluateExpression(unary.Operand, out var operand))
                    {
                        switch (unary.Operator)
                        {
                            case UnaryOperator.Plus:
                                if (operand is int i)
                                    value = Math.Abs(i);
                                else if (operand is double d)
                                    value = Math.Abs(d);
                                else
                                    throw new AnalyzerException("Cannot perform mathematical operations on booleans", unary.Line, unary.Column);
                                return true;
                            case UnaryOperator.Minus:
                                if (operand is int integer)
                                    value = -integer;
                                else if (operand is double d)
                                    value = -d;
                                else
                                    throw new AnalyzerException("Cannot perform mathematical operations on booleans", unary.Line, unary.Column);
                                return true;
                            case UnaryOperator.Not:
                                if (operand is bool b)
                                    value = !b;
                                else
                                    throw new AnalyzerException("Cannot perform boolean operations on integers/reals", unary.Line, unary.Column);
                                return true;
                        }
                    }
                    return false;
                case BinaryExpressionNode binary:
                    if (TryEvaluateExpression(binary.Left, out var leftVal) && TryEvaluateExpression(binary.Right, out var rightVal))
                    {
                        return TryEvaluateBinary(binary.Operator, leftVal, rightVal, out value);
                    }
                    return false;
                default:
                    return false;
            }
        }

        private bool TryEvaluateBinary(Operator op, object left, object right, out object value)
        {
            value = default!;

            double ToDouble(object o) => o is int i ? i : (double)o;

            try
            {
                switch (op)
                {
                    case Operator.Plus:
                        if (left is int li && right is int ri) { value = li + ri; return true; }
                        if (left is double || right is double) { value = ToDouble(left) + ToDouble(right); return true; }
                        return false;

                    case Operator.Minus:
                        if (left is int li2 && right is int ri2) { value = li2 - ri2; return true; }
                        if (left is double || right is double) { value = ToDouble(left) - ToDouble(right); return true; }
                        return false;

                    case Operator.Multiply:
                        if (left is int li3 && right is int ri3) { value = li3 * ri3; return true; }
                        if (left is double || right is double) { value = ToDouble(left) * ToDouble(right); return true; }
                        return false;

                    case Operator.Divide:
                        value = ToDouble(left) / ToDouble(right);
                        return true;
                    case Operator.Modulo:
                        if (left is int li4 && right is int ri4) { value = li4 % ri4; return true; }
                        return false;

                    case Operator.Less:
                        value = ToDouble(left) < ToDouble(right);
                        return true;
                    case Operator.LessEqual:
                        value = ToDouble(left) <= ToDouble(right);
                        return true;
                    case Operator.Greater:
                        value = ToDouble(left) > ToDouble(right);
                        return true;
                    case Operator.GreaterEqual:
                        value = ToDouble(left) >= ToDouble(right);
                        return true;
                    case Operator.Equal:
                        value = Equals(left, right);
                        return true;
                    case Operator.NotEqual:
                        value = !Equals(left, right);
                        return true;

                    case Operator.And:
                        if (left is bool lb1 && right is bool rb1) { value = lb1 && rb1; return true; }
                        return false;
                    case Operator.Or:
                        if (left is bool lb2 && right is bool rb2) { value = lb2 || rb2; return true; }
                        return false;
                    case Operator.Xor:
                        if (left is bool lb3 && right is bool rb3) { value = lb3 ^ rb3; return true; }
                        return false;

                    default:
                        return false;
                }
            } catch (Exception e)
            {
                if (e is DivideByZeroException)
                {
                    throw new AnalyzerException("Cannot divide by zero");
                }

                throw;
            }
        }
        private TypeInfo ResolveTypeFromTypeNodeDeclarations(TypeNode node, string name)
        {
            switch (node)
            {
                case PrimitiveTypeNode primitiveNode:
                    return new PrimitiveTypeInfo(primitiveNode.Type);

                case ArrayTypeNode arrayNode:
                    return ResolveArrayType(arrayNode, name);

                case RecordTypeNode recordNode:
                    return ResolveRecordType(recordNode, name);

                default:
                    throw new AnalyzerException($"Unsupported type node: {node.GetType().Name}", node.Line, node.Column);
            }
        }

        private TypeInfo ResolveTypeFromTypeNodeReference(TypeNode node)
        {
            if (node is UserTypeNode userTypeNode)
            {
                Symbol? typeSymbol = LookupSymbol(userTypeNode.Name);
                if (typeSymbol == null)
                {
                    throw new AnalyzerException("Unknown identifier", userTypeNode.Line, userTypeNode.Column);
                }
                if (typeSymbol is TypeSymbol type)
                {
                    return type.Type;
                }
                else
                {
                    throw new AnalyzerException("Identifier is not a type", userTypeNode.Line, userTypeNode.Column);
                }
            }
            else if (node is PrimitiveTypeNode primitiveTypeNode)
            {
                return new PrimitiveTypeInfo(primitiveTypeNode.Type);
            }
            else
            {
                throw new AnalyzerException("Unexpected type reference");
            }
        }



        private ArrayTypeInfo ResolveArrayType(ArrayTypeNode node, string name)
        {
            TypeInfo elementType = ResolveTypeFromTypeNodeReference(node.ElementType);

            int size = -1;
            if (node.Size != null)
            {
                if (!TryEvaluateExpression(node.Size, out object? result))
                {
                    throw new AnalyzerException("Array size must be a compile-time constant", node.Line, node.Column);
                }

                if (result is int i)
                {
                    if (i <= 0)
                    {
                        throw new AnalyzerException("Array size must be positive", node.Line, node.Column);
                    }
                    size = i;
                }
                else
                {
                    throw new AnalyzerException("Array size must be an integer", node.Line, node.Column);
                }
            }

            return new ArrayTypeInfo(elementType, size, name);
        }

        private RecordTypeInfo ResolveRecordType(RecordTypeNode node, string name)
        {
            var fields = new Dictionary<string, TypeInfo>();

            foreach (var field in node.Fields)
            {
                if (fields.ContainsKey(field.Name))
                {
                    throw new AnalyzerException($"Duplicate field '{field.Name}'", field.Line, field.Column);
                }

                TypeInfo fieldType = ResolveTypeFromTypeNodeReference(field.VarType!);
                fields[field.Name] = fieldType;
            }

            return new RecordTypeInfo(name, fields);
        }

        private Symbol? LookupSymbol(string name)
        {
            foreach (var scope in Scope)
            {
                if (scope.ContainsKey(name))
                {
                    return scope[name];
                }
            }
            return null;
        }
    }
}