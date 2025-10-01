namespace ImperativeLang.SyntaxAnalyzer
{
    enum Operator
    {
        Plus,          // +
        Minus,         // -
        Multiply,      // *
        Divide,        // /
        Modulo,        // %
        Less,          // <
        LessEqual,     // <=
        Greater,       // >
        GreaterEqual,  // >=
        Equal,         // =
        NotEqual,      // /=
        Not,
        And,
        Or,
        Xor,
    }

    /// <summary>
    /// Any operation with a binary operator, for example:
    /// x + 1
    /// z + y
    /// true and false 
    /// </summary>

    class BinaryExpressionNode : ExpressionNode
    {
        public Operator Operator { get; set; } // +, -, *, /, %, etc.
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public BinaryExpressionNode(Operator Operator, ExpressionNode left, ExpressionNode right)
        {
            this.Operator = Operator;
            Left = left;
            Right = right;
        }
    }
}