namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Any print statement
    /// </summary>
    class PrintStatementNode : StatementNode
    {
        public List<ExpressionNode> Expressions { get; set; } = new();

            public PrintStatementNode(List<ExpressionNode>? expressions = null, int line = 0, int column = 0) : base(line, column)
        {
            Expressions = expressions ?? new List<ExpressionNode>();
        }
    }
}