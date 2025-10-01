namespace ImperativeLang.SyntaxAnalyzer {
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