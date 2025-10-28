namespace ImperativeLang.SyntaxAnalyzer {
    enum UnaryOperator
    {
        Plus,
        Minus,
        Not
    }

    /// <summary>
    /// Any unary operation, for example:
    /// -x,
    /// not y
    /// </summary>
    class UnaryExpressionNode : ExpressionNode
    {
        public UnaryOperator Operator { get; set; }
        public ExpressionNode Operand { get; set; }

        public UnaryExpressionNode(UnaryOperator Operator, ExpressionNode operand, int line = 0, int column = 0) : base(line, column)
        {
            this.Operator = Operator;
            Operand = operand;
        }
    }
}