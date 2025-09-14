namespace ImperativeLang.Parser
{
    abstract class StatementNode : AstNode { }
    /// <summary>
    /// Any assignment of type target := expression
    /// </summary>
    class AssignmentNode : StatementNode
    {
        public ModifiablePrimaryNode Target { get; set; }
        public ExpressionNode Value { get; set; }

        public AssignmentNode(ModifiablePrimaryNode target, ExpressionNode value)
        {
            Target = target;
            Value = value;
        }
    }


    /// <summary>
    /// Usage of routine calls in statements
    /// </summary>
    class RoutineCallStatementNode : StatementNode
    {
        public required RoutineCallNode Call { get; set; }
    }

    /// <summary>
    /// Any while loop statement
    /// </summary>
    class WhileLoopNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public List<AstNode> Body { get; set; } = new();

        public WhileLoopNode(ExpressionNode condition, List<AstNode>? body = null)
        {
            Condition = condition;
            Body = body ?? new List<AstNode>();
        }
    }


    /// <summary>
    /// Any for loop statement
    /// </summary>
    class ForLoopNode : StatementNode
    {
        public string Iterator { get; set; }
        public RangeNode Range { get; set; }
        public bool Reverse { get; set; }
        public List<AstNode> Body { get; set; } = new();

        public ForLoopNode(string iterator, RangeNode range, bool reverse, List<AstNode>? body = null)
        {
            Iterator = iterator;
            Range = range;
            Reverse = reverse;
            Body = body ?? new List<AstNode>();
        }
    }
    /// <summary>
    /// Helper for for loop node
    /// </summary>
    class RangeNode : AstNode
    {
        public ExpressionNode Start { get; set; }
        public ExpressionNode? End { get; set; }

        public RangeNode(ExpressionNode start, ExpressionNode? end = null)
        {
            Start = start;
            End = end;
        }
    }

    /// <summary>
    /// Any if statement
    /// </summary>
    class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public List<AstNode> ThenBody { get; set; } = new();
        public List<AstNode>? ElseBody { get; set; }

        public IfStatementNode(ExpressionNode condition, List<AstNode>? thenBody = null, List<AstNode>? elseBody = null)
        {
            Condition = condition;
            ThenBody = thenBody ?? new List<AstNode>();
            ElseBody = elseBody;
        }
    }


    /// <summary>
    /// Any print statement
    /// </summary>
    class PrintStatementNode : StatementNode
    {
        public List<ExpressionNode> Expressions { get; set; } = new();

        public PrintStatementNode(List<ExpressionNode>? expressions = null)
        {
            Expressions = expressions ?? new List<ExpressionNode>();
        }
    }

    /// <summary>
    /// Any return statement
    /// </summary>
    class ReturnStatementNode : StatementNode
    {
        public ExpressionNode? Value { get; set; }

        public ReturnStatementNode(ExpressionNode? value = null)
        {
            Value = value;
        }
    }
}