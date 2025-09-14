namespace ImperativeLang.SyntaxAnalyzer
{
    abstract class ExpressionNode : AstNode { }

    /// <summary>
    /// Any operation with a binary operator, for example:
    /// x + 1
    /// z + y
    /// true and false 
    /// </summary>
    class BinaryExpressionNode : ExpressionNode
    {
        public string Operator { get; set; } // +, -, *, /, %, etc.
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public BinaryExpressionNode(string Operator, ExpressionNode left, ExpressionNode right)
        {
            this.Operator = Operator;
            Left = left;
            Right = right;
        }
    }

    /// <summary>
    /// Any unary operation, for example:
    /// -x,
    /// not y
    /// </summary>
    class UnaryExpressionNode : ExpressionNode
    {
        public string Operator { get; set; }
        public ExpressionNode Operand { get; set; }

        public UnaryExpressionNode(string Operator, ExpressionNode operand)
        {
            this.Operator = Operator;
            Operand = operand;
        }
    }

    /// <summary>
    /// Any literal expression for integers, reals and booleans
    /// </summary>
    class LiteralNode : ExpressionNode
    {
        public object Value { get; set; }
        public PrimitiveType Type;

        public LiteralNode(object Value, PrimitiveType type)
        {
            this.Value = Value;
            Type = type;
        }
    }

    
    /// <summary>
    /// Any identifier, for example: x
    /// </summary>
    class IdentifierNode : ExpressionNode
    {
        public string Name { get; set; }

        public IdentifierNode(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// Any routine call, for example: foo(1, 2)
    /// </summary>
    class RoutineCallNode : ExpressionNode
    {
        public string Name { get; set; }
        public List<ExpressionNode> Arguments { get; set; } = new();

        public RoutineCallNode(string name, List<ExpressionNode>? arguments = null)
        {
            Name = name;
            Arguments = arguments ?? new List<ExpressionNode>();
        }
    }
}