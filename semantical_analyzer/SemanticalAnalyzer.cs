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
                        // Traverse the block body and check whether every path returns
                        bool hasReturn = TraverseBody(routineSymbol.Parameters, blockRoutineBodyNode.Body, routineSymbol.ReturnType);
                        if (routineDeclaration.ReturnType != null && !hasReturn)
                        {
                            throw new AnalyzerException("Not all code paths have a return value", routineDeclaration.Line, routineDeclaration.Column);
                        }
                    }
                    else if (routineDeclaration.Body is ExpressionRoutineBodyNode expressionRoutineBodyNode)
                    {
                        // Expression-bodied routine: create a temporary scope with parameters
                        Scope.Push(new Dictionary<string, Symbol>());
                        foreach (var symbol in routineSymbol.Parameters)
                        {
                            Scope.Peek().Add(symbol.Name, symbol);
                        }
                        // Resolve and constant-fold the expression, then check the return type
                        TypeInfo expressionType = ResolveExpressionType(expressionRoutineBodyNode.Expression);
                        expressionRoutineBodyNode.Expression = TryFoldExpression(expressionRoutineBodyNode.Expression);
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

        // Walk a sequence of statements/declarations (a routine or block body).
        // declaredSymbols: parameters/locals visible in this body.
        // returnType: expected return type for this body (null if none allowed).
        // Returns true if every execution path is guaranteed to return.
        private bool TraverseBody(List<VariableSymbol> declaredSymbols, List<Node> body, TypeInfo? returnType = null)
        {
            bool hasGuaranteedReturn = false;
            Scope.Push(new Dictionary<string, Symbol>());
            foreach (var symbol in declaredSymbols)
            {
                Scope.Peek().Add(symbol.Name, symbol);
            }

            for (int i = 0; i < body.Count; i++)
            {
                var node = body[i];
                if (hasGuaranteedReturn) break; // stop walking after a guaranteed return

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
                        // canExecute: whether the loop is possibly non-empty (range/order checks)
                        bool canExecute = true;
                        TypeInfo iteratorType;
                        if (forLoopNode.IsArrayTraversal)
                        {
                            TypeInfo rangeArray = ResolveExpressionType(forLoopNode.Range.Start);
                            forLoopNode.Range.Start = TryFoldExpression(forLoopNode.Range.Start);
                            if (rangeArray is not ArrayTypeInfo)
                            {
                                throw new AnalyzerException("Can not iterate on a non-array object", forLoopNode.Line, forLoopNode.Column);
                            }
                            // array traversal may be empty depending on runtime size; conservatively mark as not guaranteed
                            canExecute = false;
                            iteratorType = rangeArray;
                        }
                        else if (ResolveExpressionType(forLoopNode.Range.Start) is PrimitiveTypeInfo rangeStart)
                        {
                            forLoopNode.Range.Start = TryFoldExpression(forLoopNode.Range.Start);
                            if (rangeStart.Type != PrimitiveType.Integer)
                            {
                                throw new AnalyzerException("Range values should be integers");
                            }
                            if (forLoopNode.Range.End != null)
                            {
                                if (ResolveExpressionType(forLoopNode.Range.End) is PrimitiveTypeInfo rangeEnd)
                                {
                                    forLoopNode.Range.End = TryFoldExpression(forLoopNode.Range.End);
                                    if (rangeEnd.Type != PrimitiveType.Integer)
                                    {
                                        throw new AnalyzerException("Range values should be integers");
                                    }
                                }
                                if (TryEvaluateExpression(forLoopNode.Range.Start, out object rangeStartNum) && TryEvaluateExpression(forLoopNode.Range.End, out object rangeEndNum))
                                {
                                    forLoopNode.Range.Start = new LiteralNode((int)rangeStartNum, PrimitiveType.Integer, forLoopNode.Range.Start.Line, forLoopNode.Range.Start.Column);
                                    forLoopNode.Range.Start = new LiteralNode((int)rangeEndNum, PrimitiveType.Integer, forLoopNode.Range.End.Line, forLoopNode.Range.End.Column);
                                    // If range is known at compile-time, warn when it cannot iterate
                                    if (!forLoopNode.Reverse && (int)rangeEndNum < (int)rangeStartNum)
                                    {
                                        System.Console.WriteLine($"Warning: Range end is smaller than range start at line {forLoopNode.Line}, column {forLoopNode.Column}");
                                        canExecute = false;
                                    }
                                    if (forLoopNode.Reverse && (int)rangeEndNum > (int)rangeStartNum)
                                    {
                                        System.Console.WriteLine($"Warning: Range end is bigger than range start at line {forLoopNode.Line}, column {forLoopNode.Column}");
                                        canExecute = false;
                                    }
                                }
                                else
                                {
                                    canExecute = false;
                                }
                            }
                            iteratorType = new PrimitiveTypeInfo(PrimitiveType.Integer);

                        }
                        else
                        {
                            throw new AnalyzerException("Range value is of wrong type", forLoopNode.Range.Start.Line, forLoopNode.Range.Start.Column);
                        }

                        // Create iterator symbol (read-only) and traverse the loop body
                        bool bodyReturns = TraverseBody(new List<VariableSymbol>([new VariableSymbol(forLoopNode.Iterator, iteratorType, true)]), forLoopNode.Body);
                        // Loop guarantees return only if body always returns and the loop is provably enterable
                        hasGuaranteedReturn = bodyReturns && canExecute;
                    }
                    else if (statementNode is WhileLoopNode whileLoopNode)
                    {
                        // Track if loop condition is a compile-time true (infinite)
                        bool isInfinite = false;
                        if (ResolveExpressionType(whileLoopNode.Condition) is PrimitiveTypeInfo conditionType)
                        {
                            whileLoopNode.Condition = TryFoldExpression(whileLoopNode.Condition);
                            if (conditionType.Type != PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Condition should be bool", whileLoopNode.Line, whileLoopNode.Column);
                            }

                            if (TryEvaluateExpression(whileLoopNode.Condition, out object condition))
                            {
                                if (condition is bool b)
                                {
                                    // Replace condition with folded literal and warn about infinite loops
                                    whileLoopNode.Condition = new LiteralNode(b, PrimitiveType.Boolean, whileLoopNode.Condition.Line, whileLoopNode.Condition.Column);
                                    if (b) { System.Console.WriteLine($"Warning: Infinite while loop at line: {whileLoopNode.Condition.Line}, column: {whileLoopNode.Condition.Column}"); isInfinite = true; }
                                }
                                else
                                {
                                    throw new AnalyzerException("Condition is not being evaluated to boolean value", whileLoopNode.Line, whileLoopNode.Column);
                                }
                            }

                            bool bodyReturns = TraverseBody(new List<VariableSymbol>(), whileLoopNode.Body, returnType);
                            hasGuaranteedReturn = isInfinite && bodyReturns;
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
                            ifStatementNode.Condition = TryFoldExpression(ifStatementNode.Condition);
                            if (conditionType.Type != PrimitiveType.Boolean)
                            {
                                throw new AnalyzerException("Condition should be bool", ifStatementNode.Line, ifStatementNode.Column);
                            }

                            bool thenReturns = TraverseBody(new List<VariableSymbol>(), ifStatementNode.ThenBody, returnType);
                            bool elseReturns = ifStatementNode.ElseBody != null &&
                                               ifStatementNode.ElseBody.Count > 0 &&
                                               TraverseBody(new List<VariableSymbol>(), ifStatementNode.ElseBody, returnType);

                            if (thenReturns && elseReturns) hasGuaranteedReturn = true;

                            if (TryEvaluateExpression(ifStatementNode.Condition, out object condition))
                            {
                                if (condition is bool b)
                                {
                                    // If condition is constant, remove the unreachable branch and warn
                                    if (b) ifStatementNode.ElseBody = new List<Node>(); else ifStatementNode.ThenBody = new List<Node>();
                                    ifStatementNode.Condition = new LiteralNode(b, PrimitiveType.Boolean, ifStatementNode.Condition.Line, ifStatementNode.Condition.Column);
                                    System.Console.WriteLine($"Warning: Condition is always {(b ? "True" : "False")} at line: {ifStatementNode.Line}, column: {ifStatementNode.Column}");
                                }
                                else
                                {
                                    throw new AnalyzerException("Condition is not being evaluated to boolean value", ifStatementNode.Line, ifStatementNode.Column);
                                }
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

                            for (int j = 0; j < routineSymbol.Parameters.Count; j++)
                            {
                                CheckAssignmentPossibility(routineSymbol.Parameters[j].Type, ResolveExpressionType(routineCallStatementNode.Call.Arguments[j]), routineCallStatementNode.Call.Arguments[j]);
                                routineCallStatementNode.Call.Arguments[j] = TryFoldExpression(routineCallStatementNode.Call.Arguments[j]);
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
                            returnStatementNode.Value = TryFoldExpression(returnStatementNode.Value);
                            if (!returnInfo.Equals(returnType))
                            {
                                throw new AnalyzerException("Invalid return value", returnStatementNode.Value.Line, returnStatementNode.Value.Column);
                            }
                        }

                        if (i + 1 < body.Count)
                        {
                            System.Console.WriteLine($"Warning: Unreachable code after return statement at line: {returnStatementNode.Line}, column: {returnStatementNode.Column}");
                            body.RemoveRange(i + 1, body.Count - (i + 1));
                        }
                        hasGuaranteedReturn = true;
                        break;
                    }
                    else if (statementNode is AssignmentNode assignmentNode)
                    {
                        Symbol? targetSymbol = LookupSymbol(assignmentNode.Target.BaseName);
                        if (targetSymbol != null && targetSymbol is VariableSymbol {IsReadOnly: true})
                        {
                            throw new AnalyzerException("Can not perform assigments on iterators", assignmentNode.Line, assignmentNode.Column);
                        }

                        TypeInfo targetType = ResolveExpressionType(assignmentNode.Target);
                        TypeInfo valueType = ResolveExpressionType(assignmentNode.Value);
                        assignmentNode.Value = TryFoldExpression(assignmentNode.Target);
                        CheckAssignmentPossibility(targetType, valueType, assignmentNode.Value);
                    }
                    else if (statementNode is PrintStatementNode printStatementNode)
                    {
                        for (int j = 0; j < printStatementNode.Expressions.Count; j++)
                        {
                            var expression = printStatementNode.Expressions[j];
                            ResolveExpressionType(expression);
                            printStatementNode.Expressions[j] = TryFoldExpression(printStatementNode.Expressions[j]);

                        }
                    }
                }
            }

            Scope.Pop();
            return hasGuaranteedReturn;
        }

    // Add or update a routine symbol in the current scope. Handles forward
    // declarations by replacing placeholders and keeps signature information.
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

                    Scope.Peek()[routineDeclarationNode.Name] = new RoutineSymbol(routineDeclarationNode.Name,
                        routineDeclarationNode.ReturnType == null
                        ? null
                        : ResolveTypeFromTypeNodeReference(routineDeclarationNode.ReturnType), ConvertParameters(routineDeclarationNode.Parameters), routineDeclarationNode.Body == null);
                    return;
                } else
                {
                    throw new AnalyzerException("Something went terribely wrong with routine declarations", routineDeclarationNode.Line, routineDeclarationNode.Column);
                }
            }

            Scope.Peek().Add(routineDeclarationNode.Name, new RoutineSymbol(routineDeclarationNode.Name,
                routineDeclarationNode.ReturnType == null
                ? null
                : ResolveTypeFromTypeNodeReference(routineDeclarationNode.ReturnType), ConvertParameters(routineDeclarationNode.Parameters), routineDeclarationNode.Body == null));
        }
        
    // Convert AST parameter declarations into internal VariableSymbol list
    private List<VariableSymbol> ConvertParameters(List<VariableDeclarationNode> variableDeclarationNodes)
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

    // Add a variable to the current scope, resolving its type from an
    // explicit type annotation or from its initializer expression.
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
                variableDeclarationNode.Initializer = TryFoldExpression(variableDeclarationNode.Initializer!);
            }
            else
            {
                variableType = ResolveTypeFromTypeNodeReference(variableDeclarationNode.VarType);
                if (variableDeclarationNode.Initializer != null)
                {
                    CheckAssignmentPossibility(variableType, ResolveExpressionType(variableDeclarationNode.Initializer), variableDeclarationNode.Initializer);
                    variableDeclarationNode.Initializer = TryFoldExpression(variableDeclarationNode.Initializer);
                }
            }

            Scope.Peek().Add(variableDeclarationNode.Name,
                 new VariableSymbol(variableDeclarationNode.Name, variableType));
        }

    // Register a type declaration in the current scope after resolving
    // its structure to a TypeInfo.
    private void AddTypeDeclaration(TypeDeclarationNode typeDeclarationNode)
        {
            if (Scope.Peek().ContainsKey(typeDeclarationNode.Name))
            {
                throw new AnalyzerException($"Type already declared: '{typeDeclarationNode.Name}'", typeDeclarationNode.Line, typeDeclarationNode.Column);
            }

            Scope.Peek().Add(typeDeclarationNode.Name,
                new TypeSymbol(typeDeclarationNode.Name, ResolveTypeFromTypeNodeDeclarations(typeDeclarationNode.Type, typeDeclarationNode.Name)));
        }


    // Infer the TypeInfo for the given expression node. Throws
    // AnalyzerException on type errors or unsupported constructs.
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
                                    array.Index = new LiteralNode(value, PrimitiveType.Integer, array.Index.Line, array.Index.Column);
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

    // Try to evaluate an expression at compile-time. Returns true if the
    // expression can be folded to a constant and sets value accordingly.
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

    // Evaluate a binary operation on two constant operands. Supports
    // numeric and boolean operators; used by constant folding.
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
    // Resolve a type definition node (primitive/array/record) into a
    // TypeInfo describing the actual type.
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

    // Resolve a type reference: either a primitive, a user-defined type
    // lookup, or report an error for unexpected nodes.
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



    // Resolve an array type declaration, ensuring size (if present) is a
    // compile-time positive integer and resolving the element type.
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

    // Resolve a record type declaration into a map of field names -> types.
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

        // Lookup a symbol by name across nested scopes (from innermost to outer)
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

        // Check whether a value of `value` type can be assigned to `target`.
        // Performs special-case checks (booleans from ints, real/int coercions)
        // and throws AnalyzerException on incompatibility.
        private void CheckAssignmentPossibility(TypeInfo target, TypeInfo value, ExpressionNode valueExpression)
        {
            if (target.Equals(value))
            {
                return;
            }

            if (target is PrimitiveTypeInfo { Type: PrimitiveType.Boolean }
                && value is PrimitiveTypeInfo { Type: PrimitiveType.Integer })
            {
                if (TryEvaluateExpression(valueExpression, out object result) && result is int i)
                {
                    if (i != 0 || i != 1) throw new AnalyzerException("Can not assign integers other than '1' and '0' to boolean variables", valueExpression.Line, valueExpression.Column);
                }
                return;
            }


            if (target is PrimitiveTypeInfo { Type: PrimitiveType.Boolean }
                && value is PrimitiveTypeInfo { Type: PrimitiveType.Real })
            {
                throw new AnalyzerException("Can not assign real values to boolean variables", valueExpression.Line, valueExpression.Column);
            }

            if ((target is PrimitiveTypeInfo { Type: PrimitiveType.Integer }
                && value is PrimitiveTypeInfo { Type: PrimitiveType.Real })

                || (target is PrimitiveTypeInfo { Type: PrimitiveType.Real }
                && value is PrimitiveTypeInfo { Type: PrimitiveType.Integer })

                || (target is PrimitiveTypeInfo { Type: PrimitiveType.Integer }
                && value is PrimitiveTypeInfo { Type: PrimitiveType.Boolean })

                || (target is PrimitiveTypeInfo { Type: PrimitiveType.Real }
                && value is PrimitiveTypeInfo { Type: PrimitiveType.Boolean })
                )
            {
                return;
            }


            if (!target.Equals(value))
            {
                throw new AnalyzerException($"Type mismatch. Expected {target}. Got {value}", valueExpression.Line, valueExpression.Column);
            }
        }
    
    // Attempt to constant-fold the expression. If successful, return a
    // corresponding LiteralNode; otherwise return the original expression.
    private ExpressionNode TryFoldExpression(ExpressionNode expression)
        {
            if (TryEvaluateExpression(expression, out object foldedResult))
            {
                return foldedResult switch
                {
                    int i => new LiteralNode(i, PrimitiveType.Integer, expression.Line, expression.Column),
                    double d => new LiteralNode(d, PrimitiveType.Real, expression.Line, expression.Column),
                    bool b => new LiteralNode(b, PrimitiveType.Boolean, expression.Line, expression.Column),
                    _ => expression
                };
            }

            return expression;
        }
    }
}