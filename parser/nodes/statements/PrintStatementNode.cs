namespace ImperativeLang.SyntaxAnalyzer {
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
}