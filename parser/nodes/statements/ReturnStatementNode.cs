namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Any return statement
    /// </summary>
    class ReturnStatementNode : StatementNode
    {
        public ExpressionNode? Value { get; set; }

        public ReturnStatementNode(ExpressionNode? value = null, int line = 0, int column = 0) : base(line, column)
        {
            Value = value;
        }
    }
}