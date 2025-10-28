namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any while loop statement
    /// </summary>
    class WhileLoopNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public List<Node> Body { get; set; } = new();

        public WhileLoopNode(ExpressionNode condition, List<Node>? body = null, int line = 0, int column = 0) : base(line, column)
        {
            Condition = condition;
            Body = body ?? new List<Node>();
        }
    }
}